using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using System.Net.Http.Headers;

using FEQuestionBank.Client.Services;

namespace FEQuestionBank.Client.Pages.User
{
    public class UploadExcelUserBase : ComponentBase
    {
        [Inject] private INguoiDungApiClient UserApi { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;

        protected IBrowserFile? SelectedFile;
        protected bool IsUploading;
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
            try
            {
                using var content = new MultipartFormDataContent();
                var fileStream = SelectedFile.OpenReadStream(10 * 1024 * 1024);
                var fileContent = new StreamContent(fileStream);
                fileContent.Headers.ContentType = 
                    new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                content.Add(fileContent, "File", SelectedFile.Name);

                var response = await UserApi.ImportUsersAsync(content);

                if (response.Success && response.Data != null)
                {
                    var result = response.Data;
                    var msg = result.ErrorCount == 0
                        ? $"Nhập thành công {result.SuccessCount} người dùng!"
                        : $"Thành công: {result.SuccessCount} | Lỗi: {result.ErrorCount}";

                    Snackbar.Add(msg, result.ErrorCount == 0 ? Severity.Success : Severity.Warning);

                    if (result.ErrorCount > 0)
                    {
                        Snackbar.Add(msg, Severity.Warning);

                        foreach (var err in result.Errors)
                            Snackbar.Add(err, Severity.Error);

                        // Không navigate ngay, hoặc delay
                        return; 
                    }
                    else
                    {
                        Snackbar.Add(msg, Severity.Success);
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
            }
        }

        protected void GoBack() => Navigation.NavigateTo("/user/list");
        protected void CancelFile()
        {
            SelectedFile = null;
            Snackbar.Add("Đã hủy chọn file.", Severity.Info);
        }
    }
}
