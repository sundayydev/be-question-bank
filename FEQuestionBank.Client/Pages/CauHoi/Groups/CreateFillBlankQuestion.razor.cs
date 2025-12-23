using BeQuestionBank.Shared.DTOs.CauHoi;
using BeQuestionBank.Shared.DTOs.CauHoi.Create;
using BeQuestionBank.Shared.DTOs.Khoa;
using BeQuestionBank.Shared.DTOs.MonHoc;
using BeQuestionBank.Shared.DTOs.Phan;
using BeQuestionBank.Shared.Enums;
using FEQuestionBank.Client.Component.Preview;
using FEQuestionBank.Client.Services.Interface;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Text.RegularExpressions;
using BeQuestionBank.Shared.DTOs.CauTraLoi;
using BeQuestionBank.Shared.DTOs.Common;
using FEQuestionBank.Client.Services;

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

        // Dropdown
        protected List<KhoaDto> Khoas = new();
        protected List<MonHocDto> MonHocs = new();
        protected List<PhanDto> Phans = new();
        protected List<EnumCLO> CLOs = Enum.GetValues(typeof(EnumCLO)).Cast<EnumCLO>().ToList();

        protected Guid? SelectedKhoaId { get; set; }
        protected Guid? SelectedMonHocId { get; set; }
        protected Guid? SelectedPhanId { get; set; }
        protected EnumCLO SelectedCLO { get; set; } = EnumCLO.CLO1;
        protected short CapDo { get; set; } = 1;

        protected string NoiDungCha { get; set; } = string.Empty;
        protected List<FillBlankAnswer> CauTraLoi { get; set; } = new();

        protected List<BreadcrumbItem> _breadcrumbs = new()
        {
            new("Trang chủ", "/"),
            new("Tạo câu hỏi điền từ", href: null, disabled: true)
        };

        protected override async Task OnInitializedAsync()
        {
            await LoadKhoas();
            // Không thêm đáp án ở đây vì EditId chưa được bind từ route
        }

        protected override async Task OnParametersSetAsync()
        {
            if (IsEditMode && EditId.HasValue)
            {
                _breadcrumbs[1] = new("Chỉnh sửa câu hỏi điền từ", href: null, disabled: true);
                await LoadForEditAsync(EditId.Value);
            }
            else
            {
                // Chỉ thêm đáp án mới khi chắc chắn không phải edit mode
                if (CauTraLoi.Count == 0)
                {
                    CauTraLoi.Add(new FillBlankAnswer());
                }
            }

            await base.OnParametersSetAsync();
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
                if (res.Success) MonHocs = res.Data ?? new();
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
                if (res.Success) Phans = res.Data ?? new();
            }
        }

        // Tự động phát hiện chỗ trống (1), (2)...
        protected List<int> DetectedBlanks => Regex.Matches(NoiDungCha, @"\(\d+\)")
            .Select(m => int.Parse(Regex.Match(m.Value, @"\d+").Value))
            .OrderBy(x => x)
            .Distinct()
            .ToList();

        protected void SyncBlanks()
        {
            var count = DetectedBlanks.Count;
            while (CauTraLoi.Count < count) CauTraLoi.Add(new FillBlankAnswer());
            while (CauTraLoi.Count > count) CauTraLoi.RemoveAt(CauTraLoi.Count - 1);
            StateHasChanged();
        }

        protected string RenderPreview()
        {
            var text = System.Web.HttpUtility.HtmlEncode(NoiDungCha);
            return Regex.Replace(text, @"\(\d+\)", "<span class='blank-input'>_____</span>");
        }

        protected async Task PreviewQuestion()
        {
            if (string.IsNullOrWhiteSpace(NoiDungCha))
            {
                Snackbar.Add("Vui lòng nhập nội dung câu hỏi!", Severity.Warning);
                return;
            }

            // Lấy tên hiển thị
            var tenKhoa = Khoas.FirstOrDefault(k => k.MaKhoa == SelectedKhoaId)?.TenKhoa ?? "Chưa chọn";
            var tenMon = MonHocs.FirstOrDefault(m => m.MaMonHoc == SelectedMonHocId)?.TenMonHoc ?? "Chưa chọn";
            var tenPhan = Phans.FirstOrDefault(p => p.MaPhan == SelectedPhanId)?.TenPhan ?? "Chưa chọn";

            // Convert FillBlankAnswer to CreateCauTraLoiDienTuDto
            var cauTraLoiDtos = CauTraLoi.Select(x => new CreateCauTraLoiDienTuDto
            {
                NoiDung = x.NoiDung,
                HoanVi = x.HoanVi
            }).ToList();

            var dialog = Dialog.Show<QuestionFillBlankPreviewDialog>("Xem trước", new DialogParameters
            {
                ["NoiDungCha"] = NoiDungCha,
                ["CauTraLoi"] = cauTraLoiDtos,
                ["TenKhoa"] = tenKhoa,
                ["TenMon"] = tenMon,
                ["TenPhan"] = tenPhan,
                ["CloName"] = SelectedCLO.ToString()
            }, new DialogOptions { FullWidth = true });

            var result = await dialog.Result;
            if (!result.Canceled && result.Data is true)
            {
                await SaveQuestion();
            }
        }

        protected async Task SaveQuestion()
        {
            if (!Validate()) return;

            // Tạo danh sách câu hỏi con (mỗi chỗ trống là 1 câu con) - dùng CreateChilDienTu
            var cauHoiCons = CauTraLoi.Select((ans, i) => new CreateChilDienTu
            {
                NoiDung = $"({i + 1})", // Nội dung câu con: (1), (2), (3)...
                MaPhan = SelectedPhanId.Value,
                CapDo = CapDo,
                CLO = SelectedCLO,
                HoanVi = false,
                CauTraLois = new List<CreateCauTraLoiDienTuDto>
                {
                    new CreateCauTraLoiDienTuDto
                    {
                        NoiDung = ans.NoiDung.Trim(),
                        HoanVi = ans.HoanVi
                    }
                }
            }).ToList();

            var createDto = new CreateCauHoiDienTuDto
            {
                NoiDung = NoiDungCha,
                MaPhan = SelectedPhanId.Value,
                CapDo = CapDo,
                CLO = SelectedCLO,
                HoanVi = false,
                CauHoiCons = cauHoiCons
            };

            ApiResponse<object> res;

            if (IsEditMode)
            {
                var updateDto = new UpdateDienTuQuestionDto
                {
                    MaCauHoi = EditId.Value,
                    NoiDung = createDto.NoiDung,
                    MaPhan = createDto.MaPhan,
                    CapDo = createDto.CapDo,
                    CLO = createDto.CLO,
                    HoanVi = createDto.HoanVi,
                    CauHoiCons = CauTraLoi
                        .Where(a => a.MaCauHoi.HasValue && a.MaCauHoi != Guid.Empty)
                        .Select((ans, i) => new CauHoiDto
                        {
                            MaCauHoi = ans.MaCauHoi!.Value,
                            NoiDung = $"({i + 1})",
                            MaPhan = createDto.MaPhan,
                            CapDo = createDto.CapDo,
                            CLO = createDto.CLO,
                            HoanVi = false,
                            CauTraLois = new List<CauTraLoiDto>
                            {
                                new CauTraLoiDto
                                {
                                    MaCauTraLoi = ans.MaCauTraLoi ?? Guid.Empty,
                                    MaCauHoi = ans.MaCauHoi!.Value,
                                    NoiDung = ans.NoiDung.Trim(),
                                    LaDapAn = true,
                                    HoanVi = ans.HoanVi,
                                    ThuTu = ans.ThuTu ?? i + 1
                                }
                            }
                        }).ToList()
                };

                res = await CauHoiApiClient.UpdateDienTuQuestionAsync(EditId.Value, updateDto);
            }
            else
            {
                // Gọi API tạo câu hỏi điền từ (DT) thay vì nhóm (NH)
                res = await CauHoiApiClient.CreateFillingQuestionAsync(createDto);
            }

            if (res.Success)
            {
                Snackbar.Add(IsEditMode ? "Cập nhật thành công!" : "Tạo câu hỏi điền từ thành công!", Severity.Success);
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
                Snackbar.Add("Chưa nhập nội dung!", Severity.Error);
                return false;
            }

            if (!SelectedPhanId.HasValue)
            {
                Snackbar.Add("Chưa chọn chương!", Severity.Error);
                return false;
            }

            if (CauTraLoi.Any(a => string.IsNullOrWhiteSpace(a.NoiDung)))
            {
                Snackbar.Add("Có đáp án trống!", Severity.Error);
                return false;
            }

            return true;
        }

        private async Task LoadForEditAsync(Guid id)
        {
            var res = await CauHoiApiClient.GetByIdAsync(id);
            if (!res.Success || res.Data == null)
            {
                Snackbar.Add("Không tải được câu hỏi!", Severity.Error);
                Navigation.NavigateTo("/question/list");
                return;
            }

            var q = res.Data;

            // Kiểm tra loại câu hỏi
            if (q.LoaiCauHoi != "DT")
            {
                Snackbar.Add("Đây không phải là câu hỏi điền từ!", Severity.Error);
                Navigation.NavigateTo("/question/list");
                return;
            }

            NoiDungCha = StripHtml(q.NoiDung ?? "");
            SelectedPhanId = q.MaPhan;
            CapDo = q.CapDo;
            SelectedCLO = q.CLO ?? EnumCLO.CLO1;
            bool loadSuccess = false;
            // Load cascading dropdown
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

            // Load đáp án điền từ
            CauTraLoi.Clear();
            if (q.CauHoiCons != null && q.CauHoiCons.Any())
            {
                foreach (var child in q.CauHoiCons.OrderBy(x => x.MaSoCauHoi))
                {
                    var firstAnswer = child.CauTraLois?.OrderBy(a => a.ThuTu).FirstOrDefault();
                    CauTraLoi.Add(new FillBlankAnswer
                    {
                        MaCauHoi = child.MaCauHoi,
                        NoiDungChild = StripHtml(child.NoiDung ?? string.Empty),
                        NoiDung = StripHtml(firstAnswer?.NoiDung ?? string.Empty),
                        HoanVi = firstAnswer?.HoanVi ?? true,
                        MaCauTraLoi = firstAnswer?.MaCauTraLoi,
                        ThuTu = firstAnswer?.ThuTu
                    });
                }
            }
            else if (q.CauTraLois != null && q.CauTraLois.Any())
            {
                foreach (var a in q.CauTraLois.OrderBy(x => x.ThuTu))
                {
                    CauTraLoi.Add(new FillBlankAnswer
                    {
                        NoiDung = StripHtml(a.NoiDung ?? string.Empty),
                        HoanVi = a.HoanVi ?? true,
                        MaCauTraLoi = a.MaCauTraLoi,
                        ThuTu = a.ThuTu
                    });
                }
            }

            if (CauTraLoi.Count == 0) CauTraLoi.Add(new FillBlankAnswer());

            StateHasChanged();
        }

        protected void GoBack() => Navigation.NavigateTo("/question/list");

        // DTO cho UI
        public class FillBlankAnswer
        {
            public Guid? MaCauHoi { get; set; }
            public Guid? MaCauTraLoi { get; set; }
            public int? ThuTu { get; set; }
            public string NoiDungChild { get; set; } = string.Empty;
            public string NoiDung { get; set; } = "";
            public bool HoanVi { get; set; } = true;
        }

        private static string StripHtml(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return Regex.Replace(input, "<.*?>", string.Empty);
        }
    }
}