using System.Security.Claims;
using BeQuestionBank.Shared.DTOs.YeuCauRutTrich;
using FEQuestionBank.Client.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Text.Json;
using BeQuestionBank.Shared.DTOs.Khoa;
using BEQuestionBank.Shared.DTOs.MaTran; // Kiểm tra namespace này trong project của bạn
using BeQuestionBank.Shared.DTOs.MonHoc;
using BeQuestionBank.Shared.DTOs.Phan;
using BeQuestionBank.Shared.Enums;
using FEQuestionBank.Client.Implementation;
using FEQuestionBank.Client.Component.DiaLog;

namespace FEQuestionBank.Client.Pages.YeuCauRutTrich;

public partial class ExtractEssayPage : ComponentBase
{
    [Inject] private IYeuCauRutTrichApiClient ApiClient { get; set; } = default!;
    [Inject] CustomAuthStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] protected IKhoaApiClient KhoaApiClient { get; set; } = default!;
    [Inject] protected IMonHocApiClient MonHocApiClient { get; set; } = default!;
    [Inject] protected IPhanApiClient PhanApiClient { get; set; } = default!;

    // Khởi tạo Model đầy đủ để tránh NullReferenceException
    private CreateTuLuanRequestDto _model = new()
    {
        MaTranTuLuan = new MaTranTuLuan
        {
            TotalQuestions = 10, // Giá trị mặc định
            Parts = new List<PartTuLuanDto>()
        }
    };

    private List<KhoaDto> Khoas = new();
    private List<MonHocDto> MonHocs = new();
    private List<PhanDto> Phans = new();

    protected Guid? SelectedKhoaId { get; set; }
    protected Guid? SelectedMonHocId { get; set; }

    private bool _isLoading = false;
    private string _previewJson = "";

    protected List<BreadcrumbItem> _breadcrumbs = new()
    {
        new("Trang chủ", href: "/"),
        new("Yêu cầu rút trích", href: "#", disabled: true),
        new("Tạo rút trích", href: "/tools/exam-extract"),
        new("Đề tự luận", href: "/tools/exam-extract/essay"),
    };

    protected override async Task OnInitializedAsync()
    {
        // Lấy authentication state
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated ?? false)
        {
            // Lấy claim
            var userIdClaim = user.FindFirst("UserId") ?? user.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                _model.MaNguoiDung = userId;
                Console.WriteLine($"Current user id: {_model.MaNguoiDung}");
            }
            else
            {
                Console.WriteLine("WARNING: No valid user id claim found.");
            }
        }
        else
        {
            Console.WriteLine("User is not authenticated.");
        }

        await LoadKhoas();
    }

    private async Task LoadKhoas()
    {
        var res = await KhoaApiClient.GetAllKhoasAsync();
        if (res.Success && res.Data != null) Khoas = res.Data;
    }

    protected async Task OnKhoaChanged(Guid? khoaId)
    {
        SelectedKhoaId = khoaId;
        SelectedMonHocId = null;
        MonHocs.Clear();
        Phans.Clear();

        // Reset ma trận
        _model.MaTranTuLuan.Parts?.Clear();

        if (khoaId.HasValue)
        {
            var res = await MonHocApiClient.GetMonHocsByMaKhoaAsync(khoaId.Value);
            if (res.Success && res.Data != null) MonHocs = res.Data;
        }
    }

    protected async Task OnMonHocChanged(Guid? monHocId)
    {
        SelectedMonHocId = monHocId;
        Phans.Clear();

        // Reset ma trận
        _model.MaTranTuLuan.Parts?.Clear();

        if (monHocId.HasValue)
        {
            _model.MaMonHoc = monHocId.Value;
            var res = await PhanApiClient.GetPhanByMonHocAsync(monHocId.Value);
            if (res.Success && res.Data != null) Phans = res.Data;
        }
    }

    private void AddPart()
    {
        Console.WriteLine($"AddPart called. Phans count: {Phans.Count}");
        
        // Khởi tạo Part mới theo đúng DTO PartTuLuanDto
        if (_model.MaTranTuLuan.Parts == null) _model.MaTranTuLuan.Parts = new List<PartTuLuanDto>();

        // Mặc định chọn phần đầu tiên (nếu có)
        var defaultPartId = Phans.Any() ? Phans.First().MaPhan : Guid.Empty;
        
        Console.WriteLine($"Default Part ID: {defaultPartId}");

        _model.MaTranTuLuan.Parts.Add(new PartTuLuanDto
        {
            Part = defaultPartId, // Mặc định chọn phần đầu tiên
            Clos = new List<CloDto>
            {
                // Mặc định thêm 1 dòng CLO (Lưu ý: Cast Enum sang int)
                new CloDto { Clo = (int)EnumCLO.CLO1, Num = 1, SubQuestionCount = 1 }
            }
        });
        
        Console.WriteLine($"Part added. Total parts: {_model.MaTranTuLuan.Parts.Count}");
    }

    private void RemovePart(int index)
    {
        if (_model.MaTranTuLuan.Parts != null && index >= 0 && index < _model.MaTranTuLuan.Parts.Count)
        {
            _model.MaTranTuLuan.Parts.RemoveAt(index);
        }
    }

    private void AddClo(PartTuLuanDto part)
    {
        part.Clos.Add(new CloDto { Clo = (int)EnumCLO.CLO1, Num = 1, SubQuestionCount = 1 });
    }

    private void RemoveClo(PartTuLuanDto part, int index)
    {
        if (index >= 0 && index < part.Clos.Count)
        {
            part.Clos.RemoveAt(index);
        }
    }

    private void PreviewMaTran()
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            _previewJson = JsonSerializer.Serialize(_model.MaTranTuLuan, options);
        }
        catch (Exception ex)
        {
            _previewJson = $"Lỗi: {ex.Message}";
        }
    }

    private async Task ShowErrorDialog(string title, string message)
    {
        var parameters = new DialogParameters
        {
            { "ContentText", message },
            { "ButtonText", "Đóng" },
            { "Color", Color.Error }
        };

        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small };
        await DialogService.ShowMessageBox(title, message, "Đóng", null, null, options);
    }

    private async Task ShowGuideDialog()
    {
        var options = new DialogOptions 
        { 
            CloseButton = true, 
            MaxWidth = MaxWidth.Medium,
            FullWidth = true
        };

        await DialogService.ShowAsync<EssayGuideDialog>();
    }

    private async Task SubmitAsync()
    {
        if (SelectedKhoaId == null || _model.MaMonHoc == Guid.Empty)
        {
            Snackbar.Add("Vui lòng chọn Khoa và Môn học.", Severity.Error);
            return;
        }

        if (_model.MaTranTuLuan.Parts == null || _model.MaTranTuLuan.Parts.Count == 0)
        {
            Snackbar.Add("Vui lòng thêm ít nhất một Phần vào ma trận.", Severity.Warning);
            return;
        }

        // Validate: Kiểm tra xem đã chọn Part (Guid) chưa
        if (_model.MaTranTuLuan.Parts.Any(p => p.Part == Guid.Empty))
        {
            Snackbar.Add("Vui lòng chọn chương/phần cho tất cả các dòng.", Severity.Warning);
            return;
        }

        // Đảm bảo UserId luôn có
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        var userIdClaim = user.FindFirst("UserId") ?? user.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            _model.MaNguoiDung = userId;
        }
        else
        {
            Snackbar.Add("Không xác định được UserId.", Severity.Error);
            return;
        }

        _isLoading = true;
        try
        {
            // Gọi API tạo yêu cầu
            var response = await ApiClient.CreateAndRutTrichDeThiTuLuanAsync(_model);

            if (response.Success)
            {
                var maYeuCau = response.Data.MaYeuCau;
                var maDeThi = response.Data.MaDeThi;
                var tenDeThi = response.Data.TenDeThi;

                // Hiển thị dialog thành công với 2 button
                var parameters = new DialogParameters
                {
                    { "MaDeThi", maDeThi },
                    { "TenDeThi", tenDeThi }
                };

                var options = new DialogOptions 
                { 
                    CloseButton = true, 
                    MaxWidth = MaxWidth.Small,
                    FullWidth = true
                };

                await DialogService.ShowAsync<ExamExtractSuccessDialog>("Rút trích thành công", parameters, options);

                // Reset data
                _model.MaTranTuLuan.Parts.Clear();
                _model.NoiDungRutTrich = "";
                _model.GhiChu = "";
                _previewJson = "";
            }
            else
            {
                // Hiển thị lỗi validation qua Dialog
                await ShowErrorDialog("Lỗi rút trích", response.Message ?? "Có lỗi xảy ra.");
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Lỗi hệ thống: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }
}