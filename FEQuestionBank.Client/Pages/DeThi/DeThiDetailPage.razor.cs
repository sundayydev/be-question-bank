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

            var dialog = DialogService.Show<XuatDeThiDialog>("Xuất đề thi", parameters, options);
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
                var bytes = await DeThiApiClient.ExportAsync(model.MaDeThi, model);

                string fileName = format switch
                {
                    "word" => $"DeThi_{model.MaDeThi}.docx",
                    "pdf" => $"DeThi_{model.MaDeThi}.pdf",
                    _ => $"DeThi_{model.MaDeThi}.{format}"
                };

                await JS.InvokeVoidAsync("downloadFile", fileName, Convert.ToBase64String(bytes));

                Snackbar.Add("Xuất đề thi thành công!", Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add("Lỗi khi xuất đề thi: " + ex.Message, Severity.Error);
            }
        }
    }
}