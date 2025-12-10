using BeQuestionBank.Shared.DTOs.CauHoi;
using BeQuestionBank.Shared.DTOs.CauTraLoi;
using BeQuestionBank.Shared.DTOs.Khoa;
using BeQuestionBank.Shared.DTOs.MonHoc;
using BeQuestionBank.Shared.DTOs.Phan;
using BeQuestionBank.Shared.Enums;
using FEQuestionBank.Client.Component;
using FEQuestionBank.Client.Component.Preview;
using FEQuestionBank.Client.Services;
using FEQuestionBank.Client.Services.Interface;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using MudBlazor;

namespace FEQuestionBank.Client.Pages.CauHoi
{
    public partial class CreateSingleQuestionBase : ComponentBase
    {
        // 1. Inject các Service API
        [Parameter] public Guid? EditId { get; set; }
        protected bool IsEditMode => EditId.HasValue;
        [Inject] protected ICauHoiApiClient CauHoiApiClient { get; set; } = default!;
        [Inject] protected IKhoaApiClient KhoaApiClient { get; set; } = default!;
        [Inject] protected IMonHocApiClient MonHocApiClient { get; set; } = default!;
        [Inject] protected IPhanApiClient PhanApiClient { get; set; } = default!;

        [Inject] protected NavigationManager Navigation { get; set; } = default!;
        [Inject] protected ISnackbar Snackbar { get; set; } = default!;
        [Inject] protected IDialogService DialogService { get; set; } = default!;

        // 2. Các biến dữ liệu Dropdown
        protected List<KhoaDto> Khoas { get; set; } = new();
        protected List<MonHocDto> MonHocs { get; set; } = new();
        protected List<PhanDto> Phans { get; set; } = new();

        // Dùng Enum.GetValues để lấy list CLO
        protected List<EnumCLO> CLOs { get; set; } = Enum.GetValues(typeof(EnumCLO)).Cast<EnumCLO>().ToList();

        // 3. Các biến lưu giá trị được chọn
        protected Guid? SelectedKhoaId { get; set; }
        protected Guid? SelectedMonHocId { get; set; }
        protected Guid? SelectedPhanId { get; set; }
        protected EnumCLO SelectedCLO { get; set; } = EnumCLO.CLO1;
        protected short CapDo { get; set; } = 1;
        protected string QuestionContent { get; set; } = string.Empty;

        protected List<CauTraLoiDto> Answers { get; set; } = new()
        {
            new CauTraLoiDto { NoiDung = "", LaDapAn = true },
            new CauTraLoiDto { NoiDung = "", LaDapAn = false },
            new CauTraLoiDto { NoiDung = "", LaDapAn = false },
            new CauTraLoiDto { NoiDung = "", LaDapAn = false }
        };

        protected List<BreadcrumbItem> _breadcrumbs = new()
        {
            new BreadcrumbItem("Trang chủ", href: "/"),
            new BreadcrumbItem("Tạo câu hỏi đơn", href: "/create-question/single")
        };

        // 4. Load danh sách Khoa khi trang khởi tạo
        protected override async Task OnInitializedAsync()
        {
            await LoadKhoas();
        }

        private async Task LoadKhoas()
        {
            // Gọi API lấy tất cả Khoa
            var res = await KhoaApiClient.GetAllKhoasAsync();
            if (res.Success && res.Data != null)
            {
                Khoas = res.Data;
            }
        }

        // 5. Logic Cascading: Chọn Khoa -> Load Môn
        protected async Task OnKhoaChanged(Guid? khoaId)
        {
            SelectedKhoaId = khoaId;
            SelectedMonHocId = null; // Reset Môn
            SelectedPhanId = null;   // Reset Phần
            MonHocs.Clear();
            Phans.Clear();

            if (khoaId.HasValue)
            {
                // Giả định MonHocApiClient có hàm lấy theo Khoa
                // Nếu chưa có, bạn có thể dùng GetPaged và filter, hoặc thêm API GetByKhoa
                var res = await MonHocApiClient.GetMonHocsByMaKhoaAsync(khoaId.Value);
                if (res.Success && res.Data != null)
                {
                    MonHocs = res.Data;
                }
            }
        }

        // 6. Logic Cascading: Chọn Môn -> Load Phần
        protected async Task OnMonHocChanged(Guid? monHocId)
        {
            SelectedMonHocId = monHocId;
            SelectedPhanId = null; // Reset Phần
            Phans.Clear();

            if (monHocId.HasValue)
            {
                // Gọi API lấy danh sách Phần theo Môn (như trong PhanController bạn đã có)
                var res = await PhanApiClient.GetPhanByMonHocAsync(monHocId.Value);
                if (res.Success && res.Data != null)
                {
                    Phans = res.Data;
                }
            }
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
                Snackbar.Add("Vui lòng chọn Phần/Chương", Severity.Error);
                return;
            }

            if (!Answers.Any(a => a.LaDapAn))
            {
                Snackbar.Add("Vui lòng chọn đáp án đúng", Severity.Error);
                return;
            }

            var request = new CreateCauHoiWithCauTraLoiDto
            {
                NoiDung = QuestionContent,
                MaPhan = SelectedPhanId.Value, // Lấy ID thật từ Dropdown
                MaSoCauHoi = 0,
                HoanVi = true,
                CapDo = 1,
                CLO = SelectedCLO,
                CauTraLois = Answers.Select(a => new CreateCauTraLoiDto
                {
                    NoiDung = a.NoiDung,
                    LaDapAn = a.LaDapAn,
                    HoanVi = true
                }).ToList()
            };

            var result = await CauHoiApiClient.CreateSingleQuestionAsync(request);

            if (result.Success)
            {
                Snackbar.Add("Tạo câu hỏi thành công!", Severity.Success);
                Navigation.NavigateTo("/question/list");
            }
            else
            {
                Snackbar.Add($"Lỗi: {result.Message}", Severity.Error);
            }
        }

        // ... Các hàm AddAnswer, RemoveAnswer giữ nguyên ...
        protected void AddAnswer() => Answers.Add(new CauTraLoiDto());

        protected void RemoveAnswer(CauTraLoiDto answer)
        {
            if (Answers.Count > 2) Answers.Remove(answer);
            else Snackbar.Add("Cần tối thiểu 2 câu trả lời", Severity.Warning);
        }

        protected void ToggleCorrectAnswer(CauTraLoiDto answer)
        {
            foreach (var a in Answers) a.LaDapAn = false;
            answer.LaDapAn = true;
        }

        protected void GoBack() => Navigation.NavigateTo("/question/list");
        protected async Task PreviewQuestion()
        {
            // Validate sơ bộ
            if (string.IsNullOrWhiteSpace(QuestionContent))
            {
                Snackbar.Add("Vui lòng nhập nội dung câu hỏi để xem trước", Severity.Warning);
                return;
            }

            // 1. Lấy thông tin hiển thị (Lookup name from ID)
            var tenKhoa = Khoas.FirstOrDefault(k => k.MaKhoa == SelectedKhoaId)?.TenKhoa ?? "Chưa chọn khoa";
            var tenMon = MonHocs.FirstOrDefault(m => m.MaMonHoc == SelectedMonHocId)?.TenMonHoc ?? "Chưa chọn môn";
            var tenPhan = Phans.FirstOrDefault(p => p.MaPhan == SelectedPhanId)?.TenPhan ?? "Chưa chọn phần";
            var cloName = SelectedCLO.ToString();

            // 2. Truyền tham số
            var parameters = new DialogParameters
            {
                ["QuestionContent"] = QuestionContent,
                ["Answers"] = Answers,
                // Truyền thêm metadata
                ["TenKhoa"] = tenKhoa,
                ["TenMon"] = tenMon,
                ["TenPhan"] = tenPhan,
                ["CloName"] = cloName,
                ["CapDo"] = 1 // Hoặc biến CapDo nếu bạn đã bind từ UI
            };

            var options = new DialogOptions
            {
                MaxWidth = MaxWidth.Medium,
                FullWidth = true,
                CloseButton = true,
                BackdropClick = false
            };

            // 3. Hiển thị Dialog
            var dialog = DialogService.Show<QuestionPreviewDialog>("Xem trước chi tiết", parameters, options);
            var result = await dialog.Result;

            // Nếu người dùng ấn "Lưu ngay" trong Dialog Preview
            if (!result.Canceled && result.Data is bool confirm && confirm)
            {
                await SaveQuestion();
            }
        }

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
                    text: "Chỉnh sửa câu hỏi đơn",
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
                Navigation.NavigateTo("/cauhoi");
                return;
            }

            var q = res.Data;

            QuestionContent = q.NoiDung ?? "";
            SelectedCLO = q.CLO ?? EnumCLO.CLO1;
            CapDo = q.CapDo;
            SelectedPhanId = q.MaPhan;
            bool loadSuccess = false;

            // --- CHỈ XỬ LÝ KHI CÓ MaPhan ---
            if (q.MaPhan != Guid.Empty)
            {
                try
                {
                    // 1. Lấy Phần
                    var phanRes = await PhanApiClient.GetPhanByIdAsync(q.MaPhan);
                    if (phanRes.Success && phanRes.Data != null)
                    {
                        var phan = phanRes.Data;
                        SelectedMonHocId = phan.MaMonHoc;

                        // 2. Lấy Môn
                        var monRes = await MonHocApiClient.GetMonHocByIdAsync(phan.MaMonHoc);
                        if (monRes.Success && monRes.Data != null)
                        {
                            SelectedKhoaId = monRes.Data.MaKhoa;

                            // 3. Load danh sách theo thứ tự
                            await LoadKhoas();

                            var monListRes = await MonHocApiClient.GetMonHocsByMaKhoaAsync(SelectedKhoaId.Value);
                            if (monListRes.Success) MonHocs = monListRes.Data ?? new();

                            var phanListRes = await PhanApiClient.GetPhanByMonHocAsync(SelectedMonHocId.Value);
                            if (phanListRes.Success) Phans = phanListRes.Data ?? new();

                            loadSuccess = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[EDIT] Lỗi load Khoa/Môn: {ex.Message}");
                }
            }

            // Load đáp án
            Answers.Clear();
            if (q.CauTraLois != null)
            {
                for (int i = 0; i < q.CauTraLois.Count; i++)
                {
                    var a = q.CauTraLois[i];
                    Answers.Add(new CauTraLoiDto
                    {
                        NoiDung = a.NoiDung ?? "",
                        LaDapAn = a.LaDapAn,
                        HoanVi = a.HoanVi ?? true
                    });
                }
            }

            StateHasChanged();
        }
    }
    
}