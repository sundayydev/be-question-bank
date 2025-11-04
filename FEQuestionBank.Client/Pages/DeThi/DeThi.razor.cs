using BeQuestionBank.Shared.DTOs.Common;
using BeQuestionBank.Shared.DTOs.Pagination;
using BeQuestionBank.Shared.DTOs.DeThi;
using FEQuestionBank.Client.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using BEQuestionBank.Shared.DTOs.DeThi;
using FEQuestionBank.Client.Pages.OtherPage;

namespace FEQuestionBank.Client.Pages.DeThi
{
    public partial class DeThiBase : ComponentBase
    {
        [Inject] protected IDeThiApiClient DeThiApiClient { get; set; } = default!;
        [Inject] protected ISnackbar Snackbar { get; set; } = default!;
        [Inject] protected IDialogService DialogService { get; set; } = default!;
        [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
        [Inject] protected HttpClient Http { get; set; } = default!;

        protected string? _searchTerm;
        protected MudTable<DeThiDto>? table;

        protected async Task<TableData<DeThiDto>> LoadServerData(TableState state, CancellationToken cancellationToken)
        {
            try
            {
                int page = state.Page + 1;
                int pageSize = state.PageSize;
                string? sort = null;

                if (!string.IsNullOrEmpty(state.SortLabel))
                    sort = $"{state.SortLabel},{(state.SortDirection == SortDirection.Ascending ? "asc" : "desc")}";

                string url = $"api/dethi/paged?page={page}&pageSize={pageSize}";
                if (!string.IsNullOrEmpty(sort)) url += $"&sort={sort}";
                if (!string.IsNullOrEmpty(_searchTerm))
                    url += $"&filter={Uri.EscapeDataString(_searchTerm)}";

                var response = await Http.GetFromJsonAsync<ApiResponse<PagedResult<DeThiDto>>>(url, cancellationToken);

                if (response is { Success: true, Data: not null })
                {
                    return new TableData<DeThiDto>
                    {
                        Items = response.Data.Items ?? new List<DeThiDto>(),
                        TotalItems = response.Data.TotalCount
                    };
                }

                return new TableData<DeThiDto> { Items = new List<DeThiDto>(), TotalItems = 0 };
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Lỗi: {ex.Message}", Severity.Error);
                return new TableData<DeThiDto> { Items = new List<DeThiDto>(), TotalItems = 0 };
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
                ["DeThi"] = new DeThiDto { NgayTao = DateTime.Now },
                ["DialogTitle"] = "Tạo mới Đề thi"
            };

            var options = new DialogOptions { MaxWidth = MaxWidth.Small, CloseButton = true };
            var dialog = DialogService.Show<EditDeThiDialog>("Tạo mới Đề thi", parameters, options);
            var result = await dialog.Result;

            if (!result.Canceled)
            {
                var updated = (DeThiDto)result.Data;
                await SaveDeThiAsync(updated);
                if (table != null) await table.ReloadServerData();
            }
        }

        protected async Task OnEdit(DeThiDto deThi)
        {
            var parameters = new DialogParameters
            {
                ["DeThi"] = deThi,
                ["DialogTitle"] = "Chỉnh sửa Đề thi"
            };
            var dialog = DialogService.Show<EditDeThiDialog>("Chỉnh sửa Đề thi", parameters);
            var result = await dialog.Result;

            if (!result.Canceled)
            {
                var updated = (DeThiDto)result.Data;
                await SaveDeThiAsync(updated);
                if (table != null) await table.ReloadServerData();
            }
        }

        protected async Task OnConfirmDelete(DeThiDto deThi)
        {
            var parameters = new DialogParameters
            {
                ["ContentText"] = $"Bạn có chắc chắn muốn xóa đề thi '{deThi.TenDeThi}' không?"
            };
            var dialog = DialogService.Show<MessageDialog>("Xác nhận xóa", parameters);
            var result = await dialog.Result;

            if (!result.Canceled)
            {
                await DeleteDeThiAsync(deThi.MaDeThi.ToString());
                if (table != null) await table.ReloadServerData();
            }
        }

        protected void OnViewDetail(DeThiDto deThi)
        {
            var parameters = new DialogParameters { ["DeThi"] = deThi };
            DialogService.Show<DeThiDetailDialog>("Chi tiết Đề thi", parameters);
        }

        private async Task SaveDeThiAsync(DeThiDto deThi)
        {
            if (string.IsNullOrWhiteSpace(deThi.TenDeThi))
            {
                Snackbar.Add("Tên đề thi là bắt buộc!", Severity.Error);
                return;
            }

            try
            {
                if (deThi.MaDeThi == Guid.Empty)
                {
                    var create = new CreateDeThiDto
                    {
                        TenDeThi = deThi.TenDeThi,
                        MaMonHoc = deThi.MaMonHoc,
                    };
                    var response = await DeThiApiClient.CreateAsync(create);
                    Snackbar.Add(response.Success ? "Tạo đề thi thành công!" : $"Lỗi: {response.Message}", response.Success ? Severity.Success : Severity.Error);
                }
                else
                {
                    var update = new UpdateDeThiDto
                    {
                        TenDeThi = deThi.TenDeThi,
                        MaMonHoc = deThi.MaMonHoc
                    };
                    var response = await DeThiApiClient.UpdateAsync(deThi.MaDeThi, update);
                    Snackbar.Add(response.Success ? "Cập nhật đề thi thành công!" : $"Lỗi: {response.Message}", response.Success ? Severity.Success : Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Lỗi hệ thống: {ex.Message}", Severity.Error);
            }
        }

        private async Task DeleteDeThiAsync(string id)
        {
            var response = await DeThiApiClient.DeleteAsync(Guid.Parse(id));
            Snackbar.Add(response.Success ? "Xóa thành công!" : $"Lỗi: {response.Message}", response.Success ? Severity.Success : Severity.Error);
        }
    }
}
