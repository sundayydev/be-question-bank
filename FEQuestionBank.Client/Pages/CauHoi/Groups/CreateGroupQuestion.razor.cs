using BeQuestionBank.Shared.DTOs.CauHoi;
using BeQuestionBank.Shared.DTOs.CauHoi.Create;
using BeQuestionBank.Shared.DTOs.CauTraLoi;
using BeQuestionBank.Shared.DTOs.Common;
using BeQuestionBank.Shared.DTOs.Khoa;
using BeQuestionBank.Shared.DTOs.MonHoc;
using BeQuestionBank.Shared.DTOs.Phan;
using BeQuestionBank.Shared.Enums;
using FEQuestionBank.Client.Component.Preview;
using FEQuestionBank.Client.Services;
using FEQuestionBank.Client.Services.Interface;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FEQuestionBank.Client.Pages.CauHoi
{
    public partial class CreateGroupQuestionBase : ComponentBase
    {
        [Parameter] public Guid? EditId { get; set; }
        protected bool IsEditMode => EditId.HasValue;

        [Inject] protected ICauHoiApiClient CauHoiApiClient { get; set; } = default!;
        [Inject] protected IKhoaApiClient KhoaApiClient { get; set; } = default!;
        [Inject] protected IMonHocApiClient MonHocApiClient { get; set; } = default!;
        [Inject] protected IPhanApiClient PhanApiClient { get; set; } = default!;
        [Inject] protected NavigationManager Navigation { get; set; } = default!;
        [Inject] protected ISnackbar Snackbar { get; set; } = default!;
        [Inject] protected IDialogService DialogService { get; set; } = default!;

        // Data Sources
        protected List<KhoaDto> Khoas { get; set; } = new();
        protected List<MonHocDto> MonHocs { get; set; } = new();
        protected List<PhanDto> Phans { get; set; } = new();
        protected List<EnumCLO> CLOs { get; set; } = Enum.GetValues(typeof(EnumCLO)).Cast<EnumCLO>().ToList();

        // Form State
        protected string NoiDungCha { get; set; } = string.Empty;
        protected Guid? SelectedKhoaId { get; set; }
        protected Guid? SelectedMonHocId { get; set; }
        protected Guid? SelectedPhanId { get; set; }
        protected EnumCLO SelectedCLO { get; set; } = EnumCLO.CLO1;
        protected short CapDo { get; set; } = 1;
        protected bool HoanViCha { get; set; } = false;

        protected List<CauHoiCon> ChildQuestions { get; set; } = new();

        protected List<BreadcrumbItem> _breadcrumbs = new()
        {
            new BreadcrumbItem("Trang chủ", "/"),
            new BreadcrumbItem("Tạo câu hỏi nhóm", href: null, disabled: true)
        };

        protected override async Task OnInitializedAsync()
        {
            await LoadKhoas();

            // Chỉ thêm câu hỏi con khi TẠO MỚI
            if (!IsEditMode)
            {
                AddChildChildQuestion();
            }
        }

        protected override async Task OnParametersSetAsync()
        {
            if (IsEditMode && EditId.HasValue)
            {
                _breadcrumbs[1] = new BreadcrumbItem("Chỉnh sửa câu hỏi nhóm", href: null, disabled: true);
                await LoadForEdit(EditId.Value);
            }

            await base.OnParametersSetAsync();
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

        private void AddChildChildQuestion()
        {
            ChildQuestions.Add(new CauHoiCon
            {
                CauTraLois = new List<CauTraLoi>
                {
                    new CauTraLoi { LaDapAn = true },
                    new CauTraLoi(),
                    new CauTraLoi(),
                    new CauTraLoi()
                }
            });
        }

        protected void AddChildQuestion() => AddChildChildQuestion();

        protected void RemoveChildQuestion(CauHoiCon item) => ChildQuestions.Remove(item);

        protected void AddAnswer(CauHoiCon question) => question.CauTraLois.Add(new CauTraLoi());

        protected void RemoveAnswer(CauHoiCon question, CauTraLoi answer)
        {
            if (question.CauTraLois.Count > 2)
                question.CauTraLois.Remove(answer);
            else
                Snackbar.Add("Cần tối thiểu 2 đáp án", Severity.Warning);
        }

        protected void ToggleCorrectAnswer(CauHoiCon question, CauTraLoi answer)
        {
            foreach (var a in question.CauTraLois) a.LaDapAn = false;
            answer.LaDapAn = true;
        }

        protected async Task PreviewGroupQuestion()
        {
            if (string.IsNullOrWhiteSpace(NoiDungCha))
            {
                Snackbar.Add("Vui lòng nhập nội dung đoạn văn!", Severity.Warning);
                return;
            }

            var tenKhoa = Khoas.FirstOrDefault(k => k.MaKhoa == SelectedKhoaId)?.TenKhoa ?? "";
            var tenMon = MonHocs.FirstOrDefault(m => m.MaMonHoc == SelectedMonHocId)?.TenMonHoc ?? "";
            var tenPhan = Phans.FirstOrDefault(p => p.MaPhan == SelectedPhanId)?.TenPhan ?? "";

            var parameters = new DialogParameters
            {
                ["NoiDungCha"] = NoiDungCha,
                ["ChildQuestions"] = ChildQuestions,
                ["TenKhoa"] = tenKhoa,
                ["TenMon"] = tenMon,
                ["TenPhan"] = tenPhan,
                ["CloName"] = SelectedCLO.ToString()
            };

            var dialog = DialogService.Show<GroupQuestionPreviewDialog>("Xem trước", parameters,
                new DialogOptions { FullWidth = true, MaxWidth = MaxWidth.Large });
            var result = await dialog.Result;

            if (!result.Canceled && result.Data is true)
            {
                await SaveGroupQuestion();
            }
        }

        protected async Task SaveGroupQuestion()
        {
            if (!Validate()) return;

            var dto = new CreateCauHoiNhomDto
            {
                NoiDung = NoiDungCha,
                MaPhan = SelectedPhanId.Value,
                HoanVi = HoanViCha,
                CapDo = CapDo,
                CLO = SelectedCLO,
                CauHoiCons = ChildQuestions.Select(c => new CreateCauHoiWithCauTraLoiDto
                {
                    NoiDung = c.NoiDung,
                    MaPhan = SelectedPhanId.Value,
                    HoanVi = c.HoanVi,
                    CapDo = CapDo,
                    CauTraLois = c.CauTraLois.Select(a => new CreateCauTraLoiDto
                    {
                        NoiDung = a.NoiDung,
                        LaDapAn = a.LaDapAn,
                        HoanVi = a.HoanVi
                    }).ToList()
                }).ToList()
            };

            ApiResponse<object> res;

            if (IsEditMode)
            {
                // Backend chỉ cập nhật được các câu hỏi/đáp án đã tồn tại (có Id)
                var existingChildren = ChildQuestions
                    .Where(c => c.Id.HasValue)
                    .ToList();

                var updateDto = new UpdateCauHoiNhomDto
                {
                    NoiDung = dto.NoiDung,
                    MaPhan = dto.MaPhan,
                    HoanVi = dto.HoanVi,
                    CapDo = dto.CapDo,
                    CLO = dto.CLO,
                    CauHoiCons = existingChildren.Select(c => new CauHoiWithCauTraLoiDto
                    {
                        MaCauHoi = c.Id!.Value,
                        NoiDung = c.NoiDung,
                        HoanVi = c.HoanVi,
                        CapDo = c.CapDo, // đảm bảo không gửi 0 xuống BE
                        CLO = c.CLO ,
                        CauTraLois = c.CauTraLois
                            .Where(a => a.Id.HasValue) // tránh null khi thêm mới ở FE
                            .Select((a, idx) => new CauTraLoiDto
                            {
                                MaCauTraLoi = a.Id!.Value,
                                NoiDung = a.NoiDung,
                                LaDapAn = a.LaDapAn,
                                ThuTu = idx + 1,
                                HoanVi = a.HoanVi
                            }).ToList()
                    }).ToList()
                };

                // Thông báo nếu người dùng đã thêm mới trong lúc chỉnh sửa (không được gửi lên)
                if (ChildQuestions.Any(c => !c.Id.HasValue))
                    Snackbar.Add("Câu hỏi/đáp án mới thêm sẽ không được cập nhật. Vui lòng tạo mới thay vì chỉnh sửa.", Severity.Info);

                res = await CauHoiApiClient.UpdateGroupQuestionAsync(EditId.Value, updateDto);
            }
            else
            {
                res = await CauHoiApiClient.CreateGroupQuestionAsync(dto);
            }

            if (res.Success)
            {
                Snackbar.Add(IsEditMode ? "Cập nhật thành công!" : "Tạo câu hỏi nhóm thành công!", Severity.Success);
                Navigation.NavigateTo("/question/list");
            }
            else
            {
                Snackbar.Add(res.Message ?? "Lỗi không xác định", Severity.Error);
            }
        }

        private bool Validate()
        {
            if (string.IsNullOrWhiteSpace(NoiDungCha))
            {
                Snackbar.Add("Chưa nhập nội dung đoạn văn!", Severity.Error);
                return false;
            }

            if (!SelectedPhanId.HasValue)
            {
                Snackbar.Add("Chưa chọn Phần/Chương!", Severity.Error);
                return false;
            }

            if (ChildQuestions.Count == 0)
            {
                Snackbar.Add("Cần ít nhất 1 câu hỏi con!", Severity.Error);
                return false;
            }

            foreach (var q in ChildQuestions)
            {
                if (string.IsNullOrWhiteSpace(q.NoiDung))
                {
                    Snackbar.Add("Có câu hỏi con chưa nhập nội dung!", Severity.Error);
                    return false;
                }

                if (!q.CauTraLois.Any(a => a.LaDapAn))
                {
                    Snackbar.Add("Có câu hỏi con chưa chọn đáp án đúng!", Severity.Error);
                    return false;
                }
            }

            return true;
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

            var group = res.Data;

            // Kiểm tra loại câu hỏi
            if (group.LoaiCauHoi != "NH")
            {
                Snackbar.Add("Đây không phải là câu hỏi nhóm!", Severity.Error);
                Navigation.NavigateTo("/question/list");
                return;
            }

            NoiDungCha = group.NoiDung ?? "";
            SelectedPhanId = group.MaPhan;
            CapDo = group.CapDo;
            SelectedCLO = group.CLO ?? EnumCLO.CLO1;
            HoanViCha = group.HoanVi;
            bool loadSuccess = false;

            if (group.MaPhan != Guid.Empty)
            {
                try
                {
                    // 1. Lấy Phần
                    var phanRes = await PhanApiClient.GetPhanByIdAsync(group.MaPhan);
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

            // Load câu hỏi con
            ChildQuestions.Clear();
            if (group.CauHoiCons != null)
            {
                foreach (var con in group.CauHoiCons)
                {
                    var child = new CauHoiCon
                    {
                        Id = con.MaCauHoi,
                        NoiDung = con.NoiDung ?? "",
                        HoanVi = con.HoanVi,
                        CauTraLois = new List<CauTraLoi>()
                    };

                    if (con.CauTraLois != null)
                    {
                        foreach (var ctl in con.CauTraLois.OrderBy(x => x.ThuTu))
                        {
                            child.CauTraLois.Add(new CauTraLoi
                            {
                                Id = ctl.MaCauTraLoi,
                                NoiDung = ctl.NoiDung ?? "",
                                LaDapAn = ctl.LaDapAn,
                                HoanVi = ctl.HoanVi ?? true
                            });
                        }
                    }

                    ChildQuestions.Add(child);
                }
            }

            if (ChildQuestions.Count == 0) AddChildChildQuestion();

            StateHasChanged();
        }

        protected void GoBack() => Navigation.NavigateTo("/questions");

        public class CauHoiCon
        {
            public Guid? Id { get; set; }
            public string NoiDung { get; set; } = "";
            public bool HoanVi { get; set; } = true;
            public short CapDo { get; set; }         
            public EnumCLO? CLO { get; set; }  
            public List<CauTraLoi> CauTraLois { get; set; } = new();
        }

        public class CauTraLoi
        {
            public Guid? Id { get; set; }
            public string NoiDung { get; set; } = "";
            public bool LaDapAn { get; set; }
            public bool HoanVi { get; set; } = true;
        }
    }
}