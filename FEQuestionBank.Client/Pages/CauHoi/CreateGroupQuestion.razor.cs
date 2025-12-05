using BeQuestionBank.Shared.DTOs.CauHoi;
using BeQuestionBank.Shared.DTOs.CauTraLoi;
using BeQuestionBank.Shared.DTOs.Khoa;
using BeQuestionBank.Shared.DTOs.MonHoc;
using BeQuestionBank.Shared.DTOs.Phan;
using BeQuestionBank.Shared.Enums;
using FEQuestionBank.Client.Component;
using FEQuestionBank.Client.Services;
using FEQuestionBank.Client.Services.Interface;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FEQuestionBank.Client.Pages.CauHoi
{
    public partial class CreateGroupQuestionBase : ComponentBase
    {
        [Inject] protected ICauHoiApiClient CauHoiApiClient { get; set; } = default!;
        [Inject] protected IKhoaApiClient KhoaApiClient { get; set; } = default!;
        [Inject] protected IMonHocApiClient MonHocApiClient { get; set; } = default!;
        [Inject] protected IPhanApiClient PhanApiClient { get; set; } = default!;
        [Inject] protected NavigationManager Navigation { get; set; } = default!;
        [Inject] protected ISnackbar Snackbar { get; set; } = default!;
        [Inject] protected IDialogService DialogService { get; set; } = default!;

        // --- Data Sources ---
        protected List<KhoaDto> Khoas { get; set; } = new();
        protected List<MonHocDto> MonHocs { get; set; } = new();
        protected List<PhanDto> Phans { get; set; } = new();
        protected List<EnumCLO> CLOs { get; set; } = Enum.GetValues(typeof(EnumCLO)).Cast<EnumCLO>().ToList();

        // --- Form State ---
        protected string ParentContent { get; set; } = string.Empty;
        protected Guid? SelectedKhoaId { get; set; }
        protected Guid? SelectedMonHocId { get; set; }
        protected Guid? SelectedPhanId { get; set; }
        protected EnumCLO SelectedCLO { get; set; }

        protected List<ChildQuestionModel> ChildQuestions { get; set; } = new();

        protected List<BreadcrumbItem> _breadcrumbs = new()
        {
            new BreadcrumbItem("Trang chủ", "/"),
            new BreadcrumbItem("Tạo câu hỏi nhóm", "/create-question/group")
        };

        protected override async Task OnInitializedAsync()
        {
            // Load Khoa ban đầu
            var res = await KhoaApiClient.GetAllKhoasAsync();
            if (res.Success && res.Data != null) Khoas = res.Data;

            // Thêm sẵn 1 câu hỏi con mẫu
            AddChildChildQuestion();
        }

        // --- Logic Dropdown Cascading ---
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

        // --- Logic Child Questions ---
        protected void AddChildQuestion() => AddChildChildQuestion(); // Alias
        private void AddChildChildQuestion()
        {
            ChildQuestions.Add(new ChildQuestionModel
            {
                Answers = new List<AnswerModel>
                {
                    new AnswerModel { IsCorrect = true },
                    new AnswerModel(),
                    new AnswerModel(),
                    new AnswerModel()
                }
            });
        }

        protected void RemoveChildQuestion(ChildQuestionModel item)
        {
            ChildQuestions.Remove(item);
        }

        // --- Answer Management for Child Questions ---
        protected void AddAnswer(ChildQuestionModel question)
        {
            question.Answers.Add(new AnswerModel());
        }

        protected void RemoveAnswer(ChildQuestionModel question, AnswerModel answer)
        {
            if (question.Answers.Count > 2)
            {
                question.Answers.Remove(answer);
            }
            else
            {
                Snackbar.Add("Cần tối thiểu 2 câu trả lời", Severity.Warning);
            }
        }

        protected void ToggleCorrectAnswer(ChildQuestionModel question, AnswerModel answer)
        {
            // Bỏ chọn tất cả đáp án khác trong câu hỏi này
            foreach (var a in question.Answers)
            {
                a.IsCorrect = false;
            }
            // Chọn đáp án hiện tại
            answer.IsCorrect = true;
        }

        // --- Actions ---
        protected async Task PreviewGroupQuestion()
        {
            if (string.IsNullOrWhiteSpace(ParentContent))
            {
                Snackbar.Add("Vui lòng nhập nội dung đoạn văn trước khi xem", Severity.Warning);
                return;
            }

            // Lấy tên hiển thị
            var tenKhoa = Khoas.FirstOrDefault(k => k.MaKhoa == SelectedKhoaId)?.TenKhoa ?? "";
            var tenMon = MonHocs.FirstOrDefault(m => m.MaMonHoc == SelectedMonHocId)?.TenMonHoc ?? "";
            var tenPhan = Phans.FirstOrDefault(p => p.MaPhan == SelectedPhanId)?.TenPhan ?? "";

            var parameters = new DialogParameters
            {
                ["ParentContent"] = ParentContent,
                ["ChildQuestions"] = ChildQuestions,
                ["TenKhoa"] = tenKhoa,
                ["TenMon"] = tenMon,
                ["TenPhan"] = tenPhan,
                ["CloName"] = SelectedCLO.ToString()
            };

            var options = new DialogOptions { MaxWidth = MaxWidth.Large, FullWidth = true, CloseButton = true };
            var dialog = DialogService.Show<GroupQuestionPreviewDialog>("Xem trước câu hỏi nhóm", parameters, options);
            var result = await dialog.Result;

            if (!result.Canceled && result.Data is bool confirm && confirm)
            {
                await SaveGroupQuestion();
            }
        }

        protected async Task SaveGroupQuestion()
        {
            // Validate
            if (string.IsNullOrWhiteSpace(ParentContent))
            {
                Snackbar.Add("Chưa nhập nội dung đoạn văn!", Severity.Error);
                return;
            }
            if (!SelectedPhanId.HasValue)
            {
                Snackbar.Add("Chưa chọn Phần/Chương!", Severity.Error);
                return;
            }
            if (ChildQuestions.Count == 0)
            {
                Snackbar.Add("Cần ít nhất 1 câu hỏi con!", Severity.Error);
                return;
            }

            // Validate từng câu con
            foreach (var q in ChildQuestions)
            {
                if (string.IsNullOrWhiteSpace(q.Content))
                {
                    Snackbar.Add("Có câu hỏi con chưa nhập nội dung!", Severity.Error);
                    return;
                }
                if (!q.Answers.Any(a => a.IsCorrect))
                {
                    Snackbar.Add($"Câu hỏi '{q.Content}' chưa có đáp án đúng!", Severity.Error);
                    return;
                }
            }

            // Mapping DTO
            var dto = new CreateCauHoiNhomDto
            {
                NoiDung = ParentContent,
                MaPhan = SelectedPhanId.Value,
                MaSoCauHoi = 0,
                HoanVi = false,
                CapDo = 2,
                CLO = SelectedCLO, // CLO của đoạn văn (Cha)

                CauHoiCons = ChildQuestions.Select(c => new CreateCauHoiWithCauTraLoiDto
                {
                    NoiDung = c.Content,
                    MaPhan = SelectedPhanId.Value,
                    HoanVi = true,
                    CapDo = 1,

                    // LOGIC QUAN TRỌNG: 
                    // Nếu câu con có chọn CLO riêng -> dùng nó.
                    // Nếu không -> dùng CLO chung của câu cha (SelectedCLO).
                    CLO = c.CLO ?? SelectedCLO,

                    CauTraLois = c.Answers.Select(a => new CreateCauTraLoiDto
                    {
                        NoiDung = a.Text,
                        LaDapAn = a.IsCorrect,
                        HoanVi = true
                    }).ToList()
                }).ToList()
            };

            // Call API
            var res = await CauHoiApiClient.CreateGroupQuestionAsync(dto);
            if (res.Success)
            {
                Snackbar.Add("Tạo câu hỏi nhóm thành công!", Severity.Success);
                Navigation.NavigateTo("/questions");
            }
            else
            {
                Snackbar.Add($"Lỗi: {res.Message}", Severity.Error);
            }
        }

        protected void GoBack() => Navigation.NavigateTo("/questions");

        // UI Models
        public class ChildQuestionModel
        {
            public string Content { get; set; } = "";

            // Thêm thuộc tính CLO (nullable để biết người dùng có chọn hay không)
            public EnumCLO? CLO { get; set; }

            public List<AnswerModel> Answers { get; set; } = new();
        }

        public class AnswerModel
        {
            public string Text { get; set; } = "";
            public bool IsCorrect { get; set; } = false;
        }
    }
}