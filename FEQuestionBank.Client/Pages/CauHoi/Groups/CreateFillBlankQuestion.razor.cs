using BeQuestionBank.Shared.DTOs.CauHoi;
using BeQuestionBank.Shared.DTOs.Khoa;
using BeQuestionBank.Shared.DTOs.MonHoc;
using BeQuestionBank.Shared.DTOs.Phan;
using BeQuestionBank.Shared.Enums;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Text.RegularExpressions;
using FEQuestionBank.Client.Component;
using FEQuestionBank.Client.Component.Preview;
using FEQuestionBank.Client.Services;
using FEQuestionBank.Client.Services.Interface;

namespace FEQuestionBank.Client.Pages.CauHoi
{
    public partial class CreateFillBlankQuestionBase : ComponentBase
    {
        [Inject] protected ICauHoiApiClient CauHoiApiClient { get; set; } = default!;
        [Inject] protected IKhoaApiClient KhoaApiClient { get; set; } = default!;
        [Inject] protected IMonHocApiClient MonHocApiClient { get; set; } = default!;
        [Inject] protected IPhanApiClient PhanApiClient { get; set; } = default!;
        [Inject] protected NavigationManager Nav { get; set; } = default!;
        [Inject] protected ISnackbar Snackbar { get; set; } = default!;
        [Inject] protected IDialogService Dialog { get; set; } = default!;

        // Dropdown data
        protected List<KhoaDto> Khoas = new();
        protected List<MonHocDto> MonHocs = new();
        protected List<PhanDto> Phans = new();
        protected List<EnumCLO> CLOs { get; set; } = Enum.GetValues(typeof(EnumCLO)).Cast<EnumCLO>().ToList();

        protected Guid? SelectedKhoaId { get; set; }
        protected Guid? SelectedMonHocId { get; set; }
        protected Guid? SelectedPhanId { get; set; }
        protected EnumCLO SelectedCLO { get; set; }

        protected string QuestionContent { get; set; } = string.Empty;
        protected List<CreateCauTraLoiDienTuDto> BlankAnswers = new() { new CreateCauTraLoiDienTuDto() };

        // Breadcrumb
        protected List<BreadcrumbItem> _breadcrumbs = new()
        {
            new("Trang chủ", "/"),
            new("Tạo câu hỏi điền từ", "/create-question/create-fill-blank", disabled: true)
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

        // Tự động phát hiện số lượng (1), (2)...
        protected List<int> DetectedBlanks => Regex.Matches(QuestionContent, @"\(\d+\)")
            .Cast<Match>()
            .Select(m => int.Parse(Regex.Match(m.Value, @"\d+").Value))
            .OrderBy(x => x)
            .Distinct()
            .ToList();

        protected void SyncBlanks()
        {
            var count = DetectedBlanks.Count;
            while (BlankAnswers.Count < count)
                BlankAnswers.Add(new CreateCauTraLoiDienTuDto());
            while (BlankAnswers.Count > count)
                BlankAnswers.RemoveAt(BlankAnswers.Count - 1);
            StateHasChanged();
        }

        protected string RenderPreview()
        {
            var text = QuestionContent;
            var matches = Regex.Matches(text, @"\(\d+\)").Cast<Match>().OrderByDescending(m => m.Index);
            foreach (Match m in matches)
            {
                text = text.Remove(m.Index, m.Length)
                    .Insert(m.Index, "<span class='blank-input'>_____</span>");
            }

            return text.Replace("\n", "<br/>");
        }

        protected async Task SaveQuestion()
        {
            if (string.IsNullOrWhiteSpace(QuestionContent))
            {
                Snackbar.Add("Vui lòng nhập nội dung câu hỏi", Severity.Error);
                return;
            }

            if (!SelectedPhanId.HasValue)
            {
                Snackbar.Add("Vui lòng chọn Chương/Phần", Severity.Error);
                return;
            }

            var blankCount = DetectedBlanks.Count;
            var answerCount = BlankAnswers.Count;

            if (blankCount == 0)
            {
                Snackbar.Add("Chưa phát hiện chỗ trống nào. Hãy dùng (1), (2)...", Severity.Error);
                return;
            }

            // ❗❗ KIỂM TRA KHỚP SỐ LƯỢNG
            if (answerCount > blankCount)
            {
                Snackbar.Add($"Số đáp án ({answerCount}) NHIỀU HƠN số chỗ trống ({blankCount}). " +
                             $"Hãy xóa bớt đáp án.", Severity.Error);
                return;
            }

            if (answerCount < blankCount)
            {
                Snackbar.Add($"Số đáp án ({answerCount}) ÍT HƠN số chỗ trống ({blankCount}). " +
                             $"Hãy thêm đáp án cho đủ.", Severity.Error);
                return;
            }

            // Kiểm tra trống nội dung đáp án
            if (BlankAnswers.Any(a => string.IsNullOrWhiteSpace(a.NoiDung)))
            {
                Snackbar.Add("Tất cả chỗ trống phải có đáp án", Severity.Error);
                return;
            }

            // ✨ Tạo dto
            var dto = new CreateCauHoiDienTuDto
            {
                MaPhan = SelectedPhanId.Value,
                MaSoCauHoi = 0,
                NoiDung = QuestionContent,
                CapDo = 1,
                CLO = SelectedCLO,
                CauHoiCons = BlankAnswers.Select((answer, i) => new CreateChilDienTu
                {
                    NoiDung = $"({i + 1})",
                    HoanVi = false,
                    CapDo = 1,
                    CauTraLois = new List<CreateCauTraLoiDienTuDto> { answer }
                }).ToList()
            };

            var result = await CauHoiApiClient.CreateFillingQuestionAsync(dto);

            if (result.Success)
            {
                Snackbar.Add("Tạo câu hỏi điền từ thành công!", Severity.Success);
                Nav.NavigateTo("/questions");
            }
            else
            {
                Snackbar.Add(result.Message ?? "Có lỗi xảy ra", Severity.Error);
            }
        }


        protected async Task PreviewQuestion()
        {
            if (string.IsNullOrWhiteSpace(QuestionContent))
            {
                Snackbar.Add("Vui lòng nhập nội dung câu hỏi để xem trước", Severity.Warning);
                return;
            }

            if (DetectedBlanks.Count == 0)
            {
                Snackbar.Add("Chưa có chỗ trống nào. Hãy dùng (1), (2)...", Severity.Warning);
                return;
            }

            var tenKhoa = Khoas.FirstOrDefault(k => k.MaKhoa == SelectedKhoaId)?.TenKhoa ?? "Chưa chọn";
            var tenMon = MonHocs.FirstOrDefault(m => m.MaMonHoc == SelectedMonHocId)?.TenMonHoc ?? "Chưa chọn";
            var tenPhan = Phans.FirstOrDefault(p => p.MaPhan == SelectedPhanId)?.TenPhan ?? "Chưa chọn phần";

            // DÙNG DialogParameters KHÔNG GENERIC → HẾT LỖI INFER NGAY LẬP TỨC
            var parameters = new DialogParameters
            {
                [nameof(QuestionFillBlankPreviewDialog.QuestionContent)] = QuestionContent,
                [nameof(QuestionFillBlankPreviewDialog.BlankAnswers)] = BlankAnswers,
                [nameof(QuestionFillBlankPreviewDialog.TenKhoa)] = tenKhoa,
                [nameof(QuestionFillBlankPreviewDialog.TenMon)] = tenMon,
                [nameof(QuestionFillBlankPreviewDialog.TenPhan)] = tenPhan,
                [nameof(QuestionFillBlankPreviewDialog.CloName)] = SelectedCLO.ToString()
            };

            var options = new DialogOptions
            {
                MaxWidth = MaxWidth.Medium,
                FullWidth = true,
                CloseButton = true,
                BackdropClick = false
            };

            var dialog = Dialog.Show<QuestionFillBlankPreviewDialog>("Xem trước câu hỏi điền từ", parameters, options);
            var result = await dialog.Result;

            if (!result.Canceled && result.Data is true)
            {
                await SaveQuestion();
            }
        }

        protected void GoBack() => Nav.NavigateTo("/questions");
    }
}