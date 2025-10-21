using BeQuestionBank.Shared.DTOs.Khoa;
using BeQuestionBank.Shared.DTOs.Common;
using FEQuestionBank.Client.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BeQuestionBank.Shared.DTOs.Pagination;
using FEQuestionBank.Client.Pages.Khoa;
using FEQuestionBank.Client.Pages.OtherPage;

namespace FEQuestionBank.Client.Pages
{
    public partial class KhoaBase : ComponentBase
    {
        [Inject] protected IKhoaApiClient KhoaApiClient { get; set; }
        [Inject] protected ISnackbar Snackbar { get; set; }
        [Inject] protected NavigationManager NavigationManager { get; set; }
        [Inject] protected IDialogService DialogService { get; set; }
        [Inject] protected HttpClient Http { get; set; } = default!;

        protected string? _searchTerm;
        protected MudTable<KhoaDto>? table;

        protected async Task<TableData<KhoaDto>> LoadServerData(TableState state, CancellationToken cancellationToken)
        {
            try
            {
                int page = state.Page + 1;
                int pageSize = state.PageSize;
                string? sort = null;

                if (!string.IsNullOrEmpty(state.SortLabel))
                    sort = $"{state.SortLabel},{(state.SortDirection == SortDirection.Ascending ? "asc" : "desc")}";

                string url = $"api/Khoa/paged?page={page}&pageSize={pageSize}";
                if (!string.IsNullOrEmpty(sort)) url += $"&sort={sort}";
                if (!string.IsNullOrEmpty(_searchTerm))
                    url += $"&filter={Uri.EscapeDataString(_searchTerm)}";

                var response = await Http.GetFromJsonAsync<ApiResponse<PagedResult<KhoaDto>>>(url, cancellationToken);

                if (response is { Success: true, Data: not null })
                {
                    var tableData = new TableData<KhoaDto>
                    {
                        Items = response.Data.Items ?? new List<KhoaDto>(),
                        TotalItems = response.Data.TotalCount
                    };
                    return tableData;
                }

                return new TableData<KhoaDto> { Items = new List<KhoaDto>(), TotalItems = 0 };
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Lỗi: {ex.Message}", Severity.Error);
                return new TableData<KhoaDto> { Items = new List<KhoaDto>(), TotalItems = 0 };
            }
        }


        protected async Task OnSearch(string? text)
        {
            _searchTerm = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
            if (table != null)
            {
                await table.ReloadServerData();
                StateHasChanged();
            }
        }

        protected async Task OnCreateNew()
        {
            var parameters = new DialogParameters
            {
                ["Khoa"] = new KhoaDto { NgayCapNhat = DateTime.Now },
                ["DialogTitle"] = "Tạo mới Khoa"
            };

            var options = new DialogOptions { MaxWidth = MaxWidth.Small, CloseButton = true };
            var dialog = DialogService.Show<EditKhoaDialog>("Tạo mới Khoa", parameters, options);
            var result = await dialog.Result;

            if (!result.Canceled)
            {
                var updated = (KhoaDto)result.Data;
                await SaveKhoaAsync(updated);
                if (table != null) await table.ReloadServerData();
            }
        }

        protected async Task OnEdit(KhoaDto khoa)
        {
            var parameters = new DialogParameters
            {
                ["Khoa"] = khoa,
                ["DialogTitle"] = "Chỉnh sửa Khoa"
            };
            var dialog = DialogService.Show<EditKhoaDialog>("Chỉnh sửa Khoa", parameters);
            var result = await dialog.Result;

            if (!result.Canceled)
            {
                var updated = (KhoaDto)result.Data;
                await SaveKhoaAsync(updated);
                if (table != null) await table.ReloadServerData();
            }
        }

        protected async Task OnConfirmDelete(KhoaDto khoa)
        {
            var parameters = new DialogParameters
            {
                ["ContentText"] = $"Bạn có chắc chắn muốn xóa khoa '{khoa.TenKhoa}' không?"
            };
            var dialog = DialogService.Show<MessageDialog>("Xác nhận xóa", parameters);
            var result = await dialog.Result;

            if (!result.Canceled)
            {
                await DeleteKhoaAsync(khoa.MaKhoa.ToString());
                if (table != null) await table.ReloadServerData();
            }
        }

        protected void OnViewDetail(KhoaDto khoa)
        {
            var parameters = new DialogParameters { ["Khoa"] = khoa };
            DialogService.Show<KhoaDetailDialog>("Chi tiết Khoa", parameters);
        }

        private async Task SaveKhoaAsync(KhoaDto khoa)
        {
            if (string.IsNullOrWhiteSpace(khoa.TenKhoa))
            {
                Snackbar.Add("Tên khoa là bắt buộc!", Severity.Error);
                return;
            }

            try
            {
                if (khoa.MaKhoa == Guid.Empty)
                {
                    var create = new CreateKhoaDto { TenKhoa = khoa.TenKhoa, MoTa = khoa.MoTa };
                    var response = await KhoaApiClient.CreateKhoaAsync(create);
                    Snackbar.Add(response.Success ? "Tạo khoa thành công!" : $"Lỗi: {response.Message}", response.Success ? Severity.Success : Severity.Error);
                }
                else
                {
                    var update = new UpdateKhoaDto { TenKhoa = khoa.TenKhoa, MoTa = khoa.MoTa };
                    var response = await KhoaApiClient.UpdateKhoaAsync(khoa.MaKhoa.ToString(), update);
                    Snackbar.Add(response.Success ? "Cập nhật khoa thành công!" : $"Lỗi: {response.Message}", response.Success ? Severity.Success : Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Lỗi hệ thống: {ex.Message}", Severity.Error);
            }
        }

        private async Task DeleteKhoaAsync(string id)
        {
            var response = await KhoaApiClient.DeleteKhoaAsync(id);
            Snackbar.Add(response.Success ? "Xóa thành công!" : $"Lỗi: {response.Message}", response.Success ? Severity.Success : Severity.Error);
        }
        protected void OnViewSubjects(KhoaDto khoa)
        {
            NavigationManager.NavigateTo($"/monhoc/khoa/{khoa.MaKhoa}");
        }
    }
}