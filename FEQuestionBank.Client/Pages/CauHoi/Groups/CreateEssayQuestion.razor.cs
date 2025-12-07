using BeQuestionBank.Shared.DTOs.CauHoi;
using BeQuestionBank.Shared.DTOs.CauHoi.Create;
using BeQuestionBank.Shared.DTOs.Khoa;
using BeQuestionBank.Shared.DTOs.MonHoc;
using BeQuestionBank.Shared.DTOs.Phan;
using BeQuestionBank.Shared.Enums;
using FEQuestionBank.Client.Component;
using FEQuestionBank.Client.Component.Preview;
using FEQuestionBank.Client.Services;
using FEQuestionBank.Client.Services.Interface;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace FEQuestionBank.Client.Pages.CauHoi;

public partial class CreateEssayQuestionBase : ComponentBase
{
    [Inject] protected ICauHoiApiClient CauHoiApiClient { get; set; } = default!;
    [Inject] protected IKhoaApiClient KhoaApiClient { get; set; } = default!;
    [Inject] protected IMonHocApiClient MonHocApiClient { get; set; } = default!;
    [Inject] protected IPhanApiClient PhanApiClient { get; set; } = default!;
    [Inject] protected NavigationManager Nav { get; set; } = default!;
    [Inject] protected ISnackbar Snackbar { get; set; } = default!;
    [Inject] protected IDialogService Dialog { get; set; } = default!;

    //protected List<CreateCauHoiTuLuanDto> Questions = new() { new CreateCauTraLoiDienTuDto() };
    protected List<EnumCLO> CLOs { get; set; } = Enum.GetValues(typeof(EnumCLO)).Cast<EnumCLO>().ToList();

    // ==== Dữ liệu dropdown ====
    protected List<KhoaDto> Khoas { get; set; } = new();
    protected List<MonHocDto> MonHocs { get; set; } = new();
    protected List<PhanDto> Phans { get; set; } = new();

    // ==== Form fields (đều có get; set; rõ ràng) ====
    protected Guid? SelectedKhoaId { get; set; }
    protected Guid? SelectedMonHocId { get; set; }
    protected Guid? SelectedPhanId { get; set; }
    protected EnumCLO SelectedCLO { get; set; }
    protected short CapDo { get; set; }
    protected bool HoanVi { get; set; } = true;
    protected string NoiDung { get; set; } = string.Empty;

    protected List<EssayQuestionItem> Questions { get; set; } = new()
    {
        new EssayQuestionItem(),
        new EssayQuestionItem()
    };


    protected List<BreadcrumbItem> _breadcrumbs { get; set; } = new()
    {
        new("Trang chủ", "/"),
        new("Tạo câu hỏi tự luận", "/create-question/essay")
    };

    protected override async Task OnInitializedAsync()
    {
        await LoadKhoas();
    }

    private async Task LoadKhoas()
    {
        var res = await KhoaApiClient.GetAllKhoasAsync();
        if (res.Success) Khoas = res.Data ?? new();
    }

    protected async Task OnKhoaChanged(Guid? khoaId)
    {
        SelectedKhoaId = khoaId;
        SelectedMonHocId = null;
        SelectedPhanId = null;
        MonHocs.Clear();
        Phans.Clear();

        if (khoaId.HasValue)
        {
            var res = await MonHocApiClient.GetMonHocsByMaKhoaAsync(khoaId.Value);
            if (res.Success && res.Data != null) MonHocs = res.Data;
        }
    }

    protected async Task OnMonHocChanged(Guid? monHocId)
    {
        SelectedMonHocId = monHocId;
        SelectedPhanId = null;
        Phans.Clear();

        if (monHocId.HasValue)
        {
            var res = await PhanApiClient.GetPhanByMonHocAsync(monHocId.Value);
            if (res.Success && res.Data != null) Phans = res.Data;
        }
    }

    protected void AddQuestion() => Questions.Add(new EssayQuestionItem());
    protected void RemoveQuestion(int index) => Questions.RemoveAt(index);

    protected static async Task<string> ReadFileContent(IBrowserFile file)
    {
        using var stream = file.OpenReadStream(2 * 1024 * 1024);
        using var reader = new StreamReader(stream);
        return (await reader.ReadToEndAsync()).Trim();
    }

    protected async Task PreviewQuestion()
    {
        if (string.IsNullOrWhiteSpace(NoiDung))
        {
            Snackbar.Add("Vui lòng nhập hướng dẫn chung (đoạn văn)", Severity.Warning);
            return;
        }

        if (Questions.Count == 0 || Questions.All(q => string.IsNullOrWhiteSpace(q.NoiDung)))
        {
            Snackbar.Add("Vui lòng nhập ít nhất 1 câu hỏi con", Severity.Warning);
            return;
        }

        var tenPhan = Phans.FirstOrDefault(p => p.MaPhan == SelectedPhanId)?.TenPhan ?? "Chưa chọn";

        var parameters = new DialogParameters
        {
            ["Passage"] = NoiDung,
            ["Questions"] = Questions.Where(q => !string.IsNullOrWhiteSpace(q.NoiDung)).ToList(),
            ["TenPhan"] = tenPhan,
            ["CloName"] = SelectedCLO.ToString(),
            ["CapDo"] = CapDo
        };

        var dialog = Dialog.Show<EssayPreviewDialog>("Xem trước câu hỏi tự luận", parameters,
            new DialogOptions { FullWidth = true, MaxWidth = MaxWidth.Large });

        var result = await dialog.Result;
        if (!result.Canceled && result.Data is true)
            await SaveQuestion();
    }

    protected async Task SaveQuestion()
    {
        if (!SelectedPhanId.HasValue)
        {
            Snackbar.Add("Chọn chương/phần!", Severity.Error);
            return;
        }

        if (string.IsNullOrWhiteSpace(NoiDung))
        {
            Snackbar.Add("Nhập đoạn văn chung!", Severity.Error);
            return;
        }

        if (Questions.All(q => string.IsNullOrWhiteSpace(q.NoiDung)))
        {
            Snackbar.Add("Nhập ít nhất 1 câu hỏi!", Severity.Error);
            return;
        }

        var dto = new CreateCauHoiTuLuanDto
        {
            MaPhan = SelectedPhanId.Value,
            MaSoCauHoi = 0,
            NoiDung = NoiDung,
            CapDo = CapDo,
            HoanVi = HoanVi,
            CLO = SelectedCLO,
            CauHoiCons = Questions
                .Where(q => !string.IsNullOrWhiteSpace(q.NoiDung))
                .Select(q => new CreateCauHoiDto
                {
                    NoiDung = q.NoiDung,
                    CapDo = CapDo,
                    HoanVi = HoanVi,
                    CLO = SelectedCLO
                }).ToList()
        };

        var res = await CauHoiApiClient.CreateEssayQuestionAsync(dto);
        if (res.Success)
        {
            Snackbar.Add("Tạo câu hỏi tự luận thành công!", Severity.Success);
            Nav.NavigateTo("/questions");
        }
        else Snackbar.Add(res.Message ?? "Lỗi!", Severity.Error);
    }

    protected void GoBack() => Nav.NavigateTo("/questions");

    public class EssayQuestionItem
    {
        public string NoiDung { get; set; } = string.Empty;
        public bool HoanVi { get; set; }
    }
}