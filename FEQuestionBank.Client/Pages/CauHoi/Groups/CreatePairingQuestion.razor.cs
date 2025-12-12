using BeQuestionBank.Shared.DTOs.CauHoi;
using BeQuestionBank.Shared.DTOs.Common;
using BeQuestionBank.Shared.DTOs.CauTraLoi;
using BeQuestionBank.Shared.DTOs.Khoa;
using BeQuestionBank.Shared.DTOs.MonHoc;
using BeQuestionBank.Shared.DTOs.Phan;
using BeQuestionBank.Shared.Enums;
using FEQuestionBank.Client.Component;
using FEQuestionBank.Client.Component.Preview;
using FEQuestionBank.Client.Services;
using FEQuestionBank.Client.Services.Implementation;
using FEQuestionBank.Client.Services.Interface;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using MudBlazor;

namespace FEQuestionBank.Client.Pages.CauHoi;

public partial class CreatePairingQuestionBase : ComponentBase
{
    [Parameter] public Guid? EditId { get; set; }
    protected bool IsEditMode => EditId.HasValue;
    [Inject] protected ICauHoiApiClient CauHoiApiClient { get; set; } = default!;
    [Inject] protected IKhoaApiClient KhoaApiClient { get; set; } = default!;
    [Inject] protected IMonHocApiClient MonHocApiClient { get; set; } = default!;
    [Inject] protected IPhanApiClient PhanApiClient { get; set; } = default!;
    [Inject] protected NavigationManager Navigation { get; set; } = default!;
    [Inject] protected ISnackbar Snackbar { get; set; } = default!;
    [Inject] protected IDialogService Dialog { get; set; } = default!;

    protected List<KhoaDto> Khoas = new();
    protected List<MonHocDto> MonHocs = new();
    protected List<PhanDto> Phans = new();
    protected List<EnumCLO> CLOs = Enum.GetValues(typeof(EnumCLO)).Cast<EnumCLO>().ToList();

    protected Guid? SelectedKhoaId { get; set; }
    protected Guid? SelectedMonHocId { get; set; }
    protected Guid? SelectedPhanId { get; set; }
    protected EnumCLO SelectedCLO { get; set; } = EnumCLO.CLO1;
    protected string NoiDungCha { get; set; } = "Ghép các khái niệm ở cột trái với đáp án phù hợp ở cột phải";
    protected short CapDo { get; set; } = 1;
    protected bool HoanVi { get; set; } = true;

    protected List<PairItem> Pairs = new()
    {
        new PairItem(),
        new PairItem()
    };

    protected List<BreadcrumbItem> _breadcrumbs = new()
    {
        new("Trang chủ", "/"),
        new("Tạo câu hỏi", "/questions"),
        new("Ghép nối", "/create-question/create-pairing", disabled: true)
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


    protected void AddPair() => Pairs.Add(new PairItem());
    protected void RemovePair(int index) => Pairs.RemoveAt(index);

    protected async Task PreviewQuestion()
    {
        if (Pairs.Count < 2 ||
            Pairs.Any(p => string.IsNullOrWhiteSpace(p.Question) || string.IsNullOrWhiteSpace(p.Answer)))
        {
            Snackbar.Add("Vui lòng nhập ít nhất 2 cặp đầy đủ", Severity.Warning);
            return;
        }

        var tenPhan = Phans.FirstOrDefault(p => p.MaPhan == SelectedPhanId)?.TenPhan ?? "Chưa chọn";

        var parameters = new DialogParameters
        {
            ["Pairs"] = Pairs,
            ["TenPhan"] = tenPhan,
            ["NoiDungCha"] = NoiDungCha,
            ["CapDo"] = CapDo,
            ["HoanVi"] = HoanVi,
            ["CloName"] = SelectedCLO.ToString(),
            [nameof(QuestionFillBlankPreviewDialog.CloName)] = SelectedCLO.ToString()
        };

        var options = new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true };
        var dialog = Dialog.Show<PairingPreviewDialog>("Xem trước câu hỏi ghép nối", parameters, options);
        var result = await dialog.Result;

        if (!result.Canceled && result.Data is true)
            await SaveQuestion();
    }

    protected async Task SaveQuestion()
    {
        if (!SelectedPhanId.HasValue)
        {
            Snackbar.Add("Vui lòng chọn chương/phần", Severity.Error);
            return;
        }

        if (Pairs.Count < 2 ||
            Pairs.Any(p => string.IsNullOrWhiteSpace(p.Question) || string.IsNullOrWhiteSpace(p.Answer)))
        {
            Snackbar.Add("Vui lòng nhập đầy đủ ít nhất 2 cặp", Severity.Error);
            return;
        }

        ApiResponse<object> result;

        if (IsEditMode && EditId.HasValue)
        {
            // Chỉ update các cặp đã có ID (đã tồn tại trong DB)
            var existingPairs = Pairs
                .Where(p => p.MaCauHoi.HasValue && p.MaCauHoi != Guid.Empty)
                .ToList();

            if (existingPairs.Count < 2)
            {
                Snackbar.Add("Cần ít nhất 2 cặp hợp lệ để cập nhật. Câu hỏi/đáp án mới thêm sẽ không được lưu.",
                    Severity.Warning);
                return;
            }

            var updateDto = new UpdateCauHoiNhomDto
            {
                MaPhan = SelectedPhanId.Value,
                NoiDung = NoiDungCha,
                CapDo = CapDo,
                HoanVi = HoanVi,
                CLO = SelectedCLO,
                CauHoiCons = existingPairs.Select(p => new CauHoiWithCauTraLoiDto
                {
                    MaCauHoi = p.MaCauHoi!.Value,
                    NoiDung = p.Question,
                    CapDo = CapDo,
                    HoanVi = HoanVi,
                    CLO = SelectedCLO,
                    CauTraLois = p.MaCauTraLoi.HasValue && p.MaCauTraLoi != Guid.Empty
                        ? new List<CauTraLoiDto>
                        {
                            new()
                            {
                                MaCauTraLoi = p.MaCauTraLoi.Value,
                                NoiDung = p.Answer,
                                LaDapAn = true,
                                HoanVi = HoanVi,
                                ThuTu = 1
                            }
                        }
                        : new List<CauTraLoiDto>()
                }).ToList()
            };

            result = await CauHoiApiClient.UpdateGhepNoiQuestionAsync(EditId.Value, updateDto);

            if (result.Success)
            {
                Snackbar.Add("Cập nhật câu hỏi ghép nối thành công!", Severity.Success);
                Navigation.NavigateTo("/question/list");
            }
            else
            {
                Snackbar.Add(result.Message ?? "Có lỗi xảy ra khi cập nhật", Severity.Error);
            }
        }
        else
        {
            // Tạo mới
            var dto = new CreateCauHoiGhepNoiDto
            {
                MaPhan = SelectedPhanId.Value,
                NoiDung = NoiDungCha,
                CapDo = CapDo,
                HoanVi = HoanVi,
                CLO = SelectedCLO,
                CauHoiCons = Pairs.Select(p => new CreateCauHoiWithCauTraLoiDto
                {
                    NoiDung = p.Question,
                    CapDo = CapDo,
                    HoanVi = HoanVi,
                    CauTraLois = new List<CreateCauTraLoiDto>
                    {
                        new() { NoiDung = p.Answer, LaDapAn = true }
                    }
                }).ToList()
            };

            result = await CauHoiApiClient.CreatePairingQuestionAsync(dto);

            if (result.Success)
            {
                Snackbar.Add("Tạo câu hỏi ghép nối thành công!", Severity.Success);
                Navigation.NavigateTo("/question/list");
            }
            else
            {
                Snackbar.Add(result.Message ?? "Có lỗi xảy ra", Severity.Error);
            }
        }
    }

    protected void GoBack() => Navigation.NavigateTo("/question/list");

    

    protected override async Task OnParametersSetAsync()
    {
        var uri = Navigation.ToAbsoluteUri(Navigation.Uri);
        if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("id", out var idStr))
        {
            EditId = Guid.Parse(idStr!);
        }

        if (IsEditMode && EditId.HasValue)
        {
            _breadcrumbs[^1] = new BreadcrumbItem(
                text: "Chỉnh sửa câu hỏi ghép nối",
                href: null,
                disabled: true
            );

            await LoadForEdit(EditId.Value);
        }

        await base.OnParametersSetAsync();
    }

    private async Task LoadForEdit(Guid id)
    {
        var res = await CauHoiApiClient.GetByIdAsync(id);
        if (!res.Success || res.Data == null)
        {
            Snackbar.Add("Không tải được câu hỏi!", Severity.Error);
            Navigation.NavigateTo("/question/list");
            return;
        }

        var q = res.Data;

        // === Gán dữ liệu chung ===
        NoiDungCha = q.NoiDung ?? "Ghép các khái niệm ở cột trái với đáp án phù hợp ở cột phải";
        SelectedCLO = q.CLO ?? EnumCLO.CLO1;
        CapDo = q.CapDo;
        HoanVi = q.HoanVi;
        SelectedPhanId = q.MaPhan;

        // === Load Khoa → Môn → Phần (giống các trang khác) ===
        if (q.MaPhan != Guid.Empty)
        {
            try
            {
                var phanRes = await PhanApiClient.GetPhanByIdAsync(q.MaPhan);
                if (phanRes.Success && phanRes.Data != null)
                {
                    var phan = phanRes.Data;
                    SelectedMonHocId = phan.MaMonHoc;

                    var monRes = await MonHocApiClient.GetMonHocByIdAsync(phan.MaMonHoc);
                    if (monRes.Success && monRes.Data != null)
                    {
                        SelectedKhoaId = monRes.Data.MaKhoa;

                        await LoadKhoas();
                        var monListRes = await MonHocApiClient.GetMonHocsByMaKhoaAsync(SelectedKhoaId.Value);
                        if (monListRes.Success) MonHocs = monListRes.Data ?? new();

                        var phanListRes = await PhanApiClient.GetPhanByMonHocAsync(SelectedMonHocId.Value);
                        if (phanListRes.Success) Phans = phanListRes.Data ?? new();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EDIT Pairing] Lỗi load dropdown: {ex.Message}");
            }
        }

        // === Load các cặp ghép nối ===
        Pairs.Clear();

        if (q.CauHoiCons != null && q.CauHoiCons.Any())
        {
            // Sắp xếp theo thứ tự tự nhiên (nếu muốn có thể sort theo gì đó, nhưng thường DB đã đúng)
            foreach (var child in q.CauHoiCons.OrderBy(c => c.MaSoCauHoi))
            {
                var questionText = child.NoiDung ?? "";
                var correctAnswer = child.CauTraLois?
                    .FirstOrDefault(a => a.LaDapAn == true);
                var answerText = correctAnswer?.NoiDung ?? "";

                Pairs.Add(new PairItem
                {
                    MaCauHoi = child.MaCauHoi,
                    MaCauTraLoi = correctAnswer?.MaCauTraLoi,
                    Question = questionText,
                    Answer = answerText
                });
            }
        }

        // Đảm bảo luôn có ít nhất 2 cặp (tránh lỗi giao diện)
        while (Pairs.Count < 2)
            Pairs.Add(new PairItem());

        StateHasChanged();
    }

    public class PairItem
    {
        public Guid? MaCauHoi { get; set; } // ID của câu hỏi con (để update)
        public Guid? MaCauTraLoi { get; set; } // ID của đáp án (để update)
        public string Question { get; set; } = "";
        public string Answer { get; set; } = "";
    }
}