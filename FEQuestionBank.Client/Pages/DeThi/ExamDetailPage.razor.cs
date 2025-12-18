using BeQuestionBank.Shared.DTOs.DeThi;
using BEQuestionBank.Shared.DTOs.DeThi;
using FEQuestionBank.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using System;
using System.Threading.Tasks;

namespace FEQuestionBank.Client.Pages.DeThi
{
    public class DeThiDetailBase : ComponentBase
    {
        [Parameter] public Guid MaDeThi { get; set; }
        protected DeThiWithChiTietAndCauTraLoiDto? DeThi;

        [Inject] protected IDeThiApiClient DeThiApiClient { get; set; } = default!;
        [Inject] protected NavigationManager Nav { get; set; } = default!;
        [Inject] protected ISnackbar Snackbar { get; set; } = default!;
        protected List<BreadcrumbItem> _breadcrumbs = new();
        [Inject] protected IDialogService DialogService { get; set; } = default!;
        [Inject] protected IJSRuntime JS { get; set; } = default!;

        // Property để check xem có phải đề thi tự luận không
        protected bool IsTuLuanExam => CheckIfTuLuan();

        protected override async Task OnInitializedAsync()
        {
            var res = await DeThiApiClient.GetByIdWithChiTietAndCauTraLoiAsync(MaDeThi);
            if (res.Success && res.Data != null)
            {
                DeThi = res.Data;
                UpdateBreadcrumbs();
            }
            else
            {
                Snackbar.Add("Không tải được đề thi!", Severity.Error);
                Nav.NavigateTo("/exams");
            }
        }

        protected void GoBack() => Nav.NavigateTo("/exams");
        
        private void UpdateBreadcrumbs()
        {
            _breadcrumbs.Clear();
            _breadcrumbs.Add(new BreadcrumbItem("Trang chủ", href: "/"));
            _breadcrumbs.Add(new BreadcrumbItem("Danh sách đề thi", href: "/exams"));
            _breadcrumbs.Add(new BreadcrumbItem(DeThi?.TenDeThi ?? "Chi tiết đề thi", null, disabled: true));
        }

        /// <summary>
        /// Kiểm tra xem đề thi có phải tự luận không
        /// Logic: Đề tự luận = TẤT CẢ câu hỏi KHÔNG CÓ đáp án trắc nghiệm (CauTraLoi)
        /// </summary>
        private bool CheckIfTuLuan()
        {
            if (DeThi?.ChiTietDeThis == null || !DeThi.ChiTietDeThis.Any())
                return false;

            // Check tất cả câu hỏi cha
            foreach (var ct in DeThi.ChiTietDeThis)
            {
                var ch = ct.CauHoi;
                if (ch == null) continue;

                // Nếu câu hỏi ĐƠN (không có con) mà CÓ đáp án -> không phải tự luận
                if (ch.SoCauHoiCon == 0 && ch.CauTraLois?.Any() == true)
                    return false;

                // Nếu câu hỏi NHÓM, check các câu con
                if (ch.SoCauHoiCon > 0 && ch.CauHoiCons?.Any() == true)
                {
                    foreach (var con in ch.CauHoiCons)
                    {
                        // Nếu câu con CÓ đáp án -> không phải tự luận
                        if (con.CauTraLois?.Any() == true)
                            return false;
                    }
                }
            }

            // Tất cả câu hỏi đều KHÔNG CÓ đáp án -> đây là đề tự luận
            return true;
        }

        protected async Task OpenExportDialog(string format)
        {
            var parameters = new DialogParameters
            {
                { "Model", new YeuCauXuatDeThiDto
                    {
                        MaDeThi = MaDeThi,
                        NgayThi = DateTime.Today,
                        HoanViDapAn = true
                    }
                }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true
            };

            var dialog = DialogService.Show<ExamExportDialog>("Xuất đề thi", parameters, options);
            var result = await dialog.Result;

            if (!result.Canceled)
            {
                var model = (YeuCauXuatDeThiDto)result.Data;
                await ExportFile(model, format);
            }
        }

        protected async Task ExportFile(YeuCauXuatDeThiDto model, string format)
        {
            try
            {
                model.Format = format;

                var bytes = await DeThiApiClient.ExportAsync(model.MaDeThi, model);

                string fileName = format switch
                {
                    "word" => $"DeThi_{model.MaDeThi}.docx",
                    "pdf" => $"DeThi_{model.MaDeThi}.pdf",
                    _ => $"DeThi_{model.MaDeThi}.{format}"
                };

                await JS.InvokeVoidAsync(
                    "downloadFile",
                    fileName,
                    Convert.ToBase64String(bytes)
                );

                Snackbar.Add("Xuất đề thi thành công!", Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add("Lỗi khi xuất đề thi: " + ex.Message, Severity.Error);
            }
        }

        protected async Task OpenExportTuLuanDialog()
        {
            if (!IsTuLuanExam)
            {
                Snackbar.Add("Đề thi này không phải dạng tự luận!", Severity.Warning);
                return;
            }

            var parameters = new DialogParameters
    {
        { "Model", new YeuCauXuatDeThiDto
            {
                MaDeThi = MaDeThi,
                NgayThi = DateTime.Today,
                HocKy = "1",
                NamHoc = $"{DateTime.Now.Year}-{DateTime.Now.Year + 1}",
                ThoiLuong = 90
            }
        }
    };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true
            };

            var dialog = DialogService.Show<ExamTuLuanExportDialog>("Xuất đề thi tự luận", parameters, options);
            var result = await dialog.Result;

            if (!result.Canceled)
            {
                var model = (YeuCauXuatDeThiDto)result.Data;
                await ExportTuLuanWordWithModel(model);
            }
        }

        protected async Task ExportTuLuanWordWithModel(YeuCauXuatDeThiDto model)
        {
            try
            {
                var bytes = await DeThiApiClient.ExportTuLuanWordAsync(model.MaDeThi, model);

                string fileName = $"DeThi_TuLuan_{model.MaDeThi}.docx";
                await JS.InvokeVoidAsync("downloadFile", fileName, Convert.ToBase64String(bytes));

                Snackbar.Add("Xuất đề thi tự luận thành công!", Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add("Lỗi khi xuất đề thi tự luận: " + ex.Message, Severity.Error);
            }
        }
    }
}