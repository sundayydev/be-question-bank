using BeQuestionBank.Shared.DTOs.File;
using BeQuestionBank.Shared.Enums;
using FEQuestionBank.Client.Pages.OtherPage; // Để dùng MessageDialog
using FEQuestionBank.Client.Services;
using FEQuestionBank.Client.Services.Implementation;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FEQuestionBank.Client.Pages.File
{
    public partial class ManageAudioBase : ComponentBase
    {
        [Inject] protected IFileApiClient FileApiClient { get; set; } = default!;
        [Inject] protected IDialogService DialogService { get; set; } = default!;
        [Inject] protected ISnackbar Snackbar { get; set; } = default!;
        [Inject] protected NavigationManager NavigationManager { get; set; } = default!;

        protected string? _searchTerm;
        protected MudTable<FileDto>? table;
        protected List<BreadcrumbItem> _breadcrumbs = new();

        protected override void OnInitialized()
        {
            UpdateBreadcrumbs();
        }

        private void UpdateBreadcrumbs()
        {
            _breadcrumbs.Clear();
            _breadcrumbs.Add(new BreadcrumbItem("Trang chủ", href: "/"));
            _breadcrumbs.Add(new BreadcrumbItem("Quản lý Audio", href: "/manage-audio", disabled: true));
        }

        protected async Task<TableData<FileDto>> LoadServerData(TableState state, CancellationToken cancellationToken)
        {
            try
            {
                int page = state.Page + 1;
                int pageSize = state.PageSize;

                string? sort = null;
                if (!string.IsNullOrEmpty(state.SortLabel))
                {
                    sort = $"{state.SortLabel},{(state.SortDirection == SortDirection.Ascending ? "asc" : "desc")}";
                }

                // Gọi API lấy danh sách file chỉ có Type = Audio
                var response = await FileApiClient.GetFilesPagedAsync(
                    page: page,
                    pageSize: pageSize,
                    sort: sort,
                    search: _searchTerm,
                    fileType: FileType.Audio
                );

                if (response is { Success: true, Data: not null })
                {
                    return new TableData<FileDto>
                    {
                        Items = response.Data.Items ?? new List<FileDto>(),
                        TotalItems = response.Data.TotalCount
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return new TableData<FileDto> { Items = new List<FileDto>(), TotalItems = 0 };
        }

        protected Task OnUploadAudio()
        {
            // Mở dialog upload (Bạn cần tạo component UploadFileDialog tương tự các dialog khác)
            // var dialog = DialogService.Show<UploadFileDialog>("Tải lên Audio");
            Snackbar.Add("Tính năng đang phát triển", Severity.Info);
            return Task.CompletedTask;
        }

        protected void OnPlayPreview(FileDto file)
        {
            // Có thể mở một Dialog nhỏ chỉ để phát audio nếu không muốn dùng thẻ <audio> trên bảng
            var parameters = new DialogParameters
            {
                ["ContentText"] = $"Đang phát: {file.TenFile}",
                // Bạn có thể truyền thêm URL vào dialog custom nếu cần
            };
            DialogService.Show<MessageDialog>("Nghe thử", parameters);
        }

        protected async Task OnConfirmDelete(FileDto file)
        {
            var parameters = new DialogParameters
            {
                ["ContentText"] = $"Bạn có chắc chắn muốn xóa file audio '{file.TenFile}' không?"
            };
            var dialog = DialogService.Show<MessageDialog>("Xác nhận xóa", parameters);
            var result = await dialog.Result;

            if (!result.Canceled)
            {
                var response = await FileApiClient.DeleteFileAsync(file.MaFile);
                if (response.Success)
                {
                    Snackbar.Add("Xóa thành công!", Severity.Success);
                    await table!.ReloadServerData();
                }
                else
                {
                    Snackbar.Add($"Lỗi: {response.Message}", Severity.Error);
                }
            }
        }
    }
}