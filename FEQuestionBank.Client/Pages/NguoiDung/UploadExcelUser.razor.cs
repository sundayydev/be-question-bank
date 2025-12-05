using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using System.Net.Http.Headers;
using FEQuestionBank.Client.Services;
using BeQuestionBank.Shared.DTOs.user;
using FEQuestionBank.Client.Pages.NguoiDung;

namespace FEQuestionBank.Client.Pages.User
{
    public class UploadExcelUserBase : ComponentBase
    {
        [Inject] private INguoiDungApiClient UserApi { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        [Inject] private IDialogService DialogService { get; set; } = default!;

        protected IBrowserFile? SelectedFile;
        protected bool IsUploading;
        protected bool ShowResultDialog;
        protected ImportResultDto? ResultData;

        protected List<BreadcrumbItem> _breadcrumbs = new()
        {
            new BreadcrumbItem("Trang chủ", href: "/"),
            new BreadcrumbItem("Quản lý", href: "#", disabled: true),
            new BreadcrumbItem("Tạo mới người dùng", href: "/user/create-user"),
            new BreadcrumbItem("Tải lên tệp người dùng", href: "/user/upload-excel"),
        };

        protected void OnFileSelected(InputFileChangeEventArgs e)
        {
            SelectedFile = e.File;
            StateHasChanged();
        }

        protected async Task UploadFile()
        {
            if (SelectedFile == null) return;

            IsUploading = true;
            ShowResultDialog = false;
            ResultData = null;

            try
            {
                using var content = new MultipartFormDataContent();
                var fileStream = SelectedFile.OpenReadStream(15 * 1024 * 1024); // max 15MB
                var fileContent = new StreamContent(fileStream);
                fileContent.Headers.ContentType = 
                    new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                
                content.Add(fileContent, "File", SelectedFile.Name);

                var response = await UserApi.ImportUsersAsync(content);
                // var result = response.Data;
                // var msg = result.ErrorCount == 0
                //     ? $"Nhập thành công {result.SuccessCount} người dùng!"
                //     : $"Thành công: {result.SuccessCount} | Lỗi: {result.ErrorCount}";
                //
                // Snackbar.Add(msg, result.ErrorCount == 0 ? Severity.Success : Severity.Warning);

                if (response.Success && response.Data != null)
                {
                    var parameters = new DialogParameters
                    {
                        ["Result"] = response.Data
                    };

                    var options = new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true };
                    var dialog = DialogService.Show<ImportResultDialog>("Kết quả import", parameters, options);

                    var result = await dialog.Result;
                    if (!result.Canceled && result.Data?.ToString() == "view-list")
                    {
                        Navigation.NavigateTo("/user/list");
                    }
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add("Lỗi hệ thống: " + ex.Message, Severity.Error);
            }
            finally
            {
                IsUploading = false;
                StateHasChanged();
            }
        }

        protected void CloseDialogAndGoToList()
        {
            ShowResultDialog = false;
            Navigation.NavigateTo("/user/list");
        }

        protected void CloseDialog()
        {
            ShowResultDialog = false;
        }

        protected void GoBack() => Navigation.NavigateTo("/user/list");

        protected void CancelFile()
        {
            SelectedFile = null;
            Snackbar.Add("Đã hủy chọn file.", Severity.Info);
        }
    }
}

