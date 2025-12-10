using BeQuestionBank.Shared.DTOs.CauHoi;
using BeQuestionBank.Shared.DTOs.Khoa;
using BeQuestionBank.Shared.DTOs.MonHoc;
using BeQuestionBank.Shared.DTOs.Phan;
using BeQuestionBank.Shared.Enums;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Text.RegularExpressions;
using FEQuestionBank.Client.Component.Preview;
using FEQuestionBank.Client.Services;
using FEQuestionBank.Client.Services.Interface;
using Microsoft.AspNetCore.WebUtilities;

namespace FEQuestionBank.Client.Pages.CauHoi
{
    public partial class CreateFillBlankQuestionBase : ComponentBase
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

        // Dropdown data
        protected List<KhoaDto> Khoas = new();
        protected List<MonHocDto> MonHocs = new();
        protected List<PhanDto> Phans = new();
        protected List<EnumCLO> CLOs { get; set; } = Enum.GetValues(typeof(EnumCLO)).Cast<EnumCLO>().ToList();

        protected Guid? SelectedKhoaId { get; set; }
        protected Guid? SelectedMonHocId { get; set; }
        protected Guid? SelectedPhanId { get; set; }
        protected EnumCLO SelectedCLO { get; set; } = EnumCLO.CLO1;

        protected string QuestionContent { get; set; } = string.Empty;
        protected List<CreateCauTraLoiDienTuDto> CauTraLoi = new() { new CreateCauTraLoiDienTuDto() };

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
            while (CauTraLoi.Count < count)
                CauTraLoi.Add(new CreateCauTraLoiDienTuDto());
            while (CauTraLoi.Count > count)
                CauTraLoi.RemoveAt(CauTraLoi.Count - 1);
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
            var answerCount = CauTraLoi.Count;

            if (blankCount == 0)
            {
                Snackbar.Add("Chưa phát hiện chỗ trống nào. Hãy dùng (1), (2)...", Severity.Error);
                return;
            }

            //  KIỂM TRA KHỚP SỐ LƯỢNG
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
            if (CauTraLoi.Any(a => string.IsNullOrWhiteSpace(a.NoiDung)))
            {
                Snackbar.Add("Tất cả chỗ trống phải có đáp án", Severity.Error);
                return;
            }

            //  Tạo dto
            var dto = new CreateCauHoiDienTuDto
            {
                MaPhan = SelectedPhanId.Value,
                MaSoCauHoi = 0,
                NoiDung = QuestionContent,
                CapDo = 1,
                CLO = SelectedCLO,
                CauHoiCons = CauTraLoi.Select((answer, i) => new CreateChilDienTu
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
                Navigation.NavigateTo("/question/list");
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
                [nameof(QuestionFillBlankPreviewDialog.CauTraLoi)] = CauTraLoi,
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
                    text: "Chỉnh sửa câu hỏi Multiple Choice",
                    href: _breadcrumbs[^1].Href,
                    disabled: _breadcrumbs[^1].Disabled,
                    icon: _breadcrumbs[^1].Icon
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

            // Gán dữ liệu chính
            QuestionContent = q.NoiDung ?? "";
            SelectedCLO = q.CLO ?? EnumCLO.CLO1;
            SelectedPhanId = q.MaPhan;

            // ========= Load Khoa → Môn → Phần =========

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
                    Console.WriteLine($"[EDIT] Lỗi load Khoa/Môn/Phần: {ex.Message}");
                }
            }

            // ========= Load câu trả lời (điền từ) =========

            CauTraLoi.Clear();

            if (q.CauHoiCons != null && q.CauHoiCons.Any())
            {
                // Sắp xếp các câu hỏi con theo số thứ tự trong (1), (2)...
                var sortedCons = q.CauHoiCons
                    .Where(c => c.NoiDung != null && Regex.IsMatch(c.NoiDung, @"\(\d+\)"))
                    .Select(c => new
                    {
                        Child = c,
                        Number = int.Parse(Regex.Match(c.NoiDung, @"\d+").Value)
                    })
                    .OrderBy(x => x.Number)
                    .ToList();

                foreach (var item in sortedCons)
                {
                    var answer = item.Child.CauTraLois?.FirstOrDefault()?.NoiDung ?? "";
                    CauTraLoi.Add(new CreateCauTraLoiDienTuDto
                    {
                        NoiDung = answer
                    });
                }
            }

            // Nếu không có CauHoiCon nào (dữ liệu cũ), thử fallback từ DetectedBlanks
            if (CauTraLoi.Count == 0)
            {
                SyncBlanks(); // Tự động tạo số lượng chỗ trống theo nội dung
            }

            StateHasChanged();
        }
    }
}