using BeQuestionBank.Shared.DTOs.Common;
using BeQuestionBank.Shared.DTOs.Khoa;
using BeQuestionBank.Shared.DTOs.MonHoc;
using BeQuestionBank.Shared.DTOs.Pagination;
using FEQuestionBank.Client.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FEQuestionBank.Client.Pages.Khoa;
using FEQuestionBank.Client.Pages.OtherPage;

namespace FEQuestionBank.Client.Pages.MonHoc
{
    public partial class MonHocBase : ComponentBase
    {
        [Parameter] public Guid? MaKhoa { get; set; } 

        [Inject] protected IMonHocApiClient MonHocApiClient { get; set; } = default!;
        [Inject] protected IKhoaApiClient KhoaApiClient { get; set; } = default!;
        [Inject] protected ISnackbar Snackbar { get; set; } = default!;
        [Inject] protected IDialogService DialogService { get; set; } = default!;
        [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
        [Inject] protected HttpClient Http { get; set; } = default!;

        protected string? _searchTerm;
        protected MudTable<MonHocDto>? table;
        protected List<KhoaDto> Khoas { get; set; } = new();
        protected Guid? MaKhoaFilter { get; set; }
        protected string PageTitle { get; set; } = "Danh sách Môn Học";

        protected override async Task OnInitializedAsync()
        {
            var khoaResponse = await KhoaApiClient.GetAllKhoasAsync();
            if (khoaResponse.Success && khoaResponse.Data != null)
                Khoas = khoaResponse.Data;

            await UpdateMaKhoaFilterAsync();
        }

        protected override async Task OnParametersSetAsync()
        {
            await UpdateMaKhoaFilterAsync();
        }

        private async Task UpdateMaKhoaFilterAsync()
        {
            if (MaKhoa.HasValue)
            {
                MaKhoaFilter = MaKhoa.Value;
                var khoa = Khoas.Find(k => k.MaKhoa == MaKhoa.Value);
                PageTitle = khoa != null
                    ? $"Danh sách Môn Học của Khoa {khoa.TenKhoa}"
                    : "Danh sách Môn Học (Khoa không xác định)";

                if (table != null)
                {
                    await table.ReloadServerData();
                }
            }
            else
            {
                MaKhoaFilter = null;
                PageTitle = "Danh sách Môn Học";
               
            }

        }

        protected async Task<TableData<MonHocDto>> LoadServerData(TableState state, CancellationToken cancellationToken)
        {
            try
            {
                if (MaKhoaFilter.HasValue)
                {
                    var response = await Http.GetFromJsonAsync<ApiResponse<List<MonHocDto>>>(
                        $"api/monhoc/khoa/{MaKhoaFilter}", cancellationToken);

                    if (response == null)
                    {
                        return new TableData<MonHocDto> { Items = new List<MonHocDto>(), TotalItems = 0 };
                    }

                    if (response.Success && response.Data != null)
                    {
                        return new TableData<MonHocDto>
                        {
                            Items = response.Data,
                            TotalItems = response.Data.Count
                        };
                    }
                    else
                    {
                        return new TableData<MonHocDto> { Items = new List<MonHocDto>(), TotalItems = 0 };
                    }
                }
                
                int page = state.Page + 1;
                int pageSize = state.PageSize;
                string? sort = null;

                if (!string.IsNullOrEmpty(state.SortLabel))
                    sort = $"{state.SortLabel},{(state.SortDirection == SortDirection.Ascending ? "asc" : "desc")}";

                string url = $"api/monhoc/paged?page={page}&limit={pageSize}";
                if (!string.IsNullOrEmpty(sort)) url += $"&sort={sort}";
                if (!string.IsNullOrEmpty(_searchTerm))
                    url += $"&filter={Uri.EscapeDataString(_searchTerm)}";
                
                var pagedResponse = await Http.GetFromJsonAsync<ApiResponse<PagedResult<MonHocDto>>>(url, cancellationToken);
                if (pagedResponse is { Success: true, Data: not null })
                {
                    return new TableData<MonHocDto>
                    {
                        Items = pagedResponse.Data.Items ?? new List<MonHocDto>(),
                        TotalItems = pagedResponse.Data.TotalCount
                    };
                }

                return new TableData<MonHocDto> { Items = new List<MonHocDto>(), TotalItems = 0 };
            }
            catch (Exception ex)
            {
                return new TableData<MonHocDto> { Items = new List<MonHocDto>(), TotalItems = 0 };
            }
        }

        protected async Task OnSearch(string? text)
        {
            _searchTerm = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
            if (table != null)
                await table.ReloadServerData();
        }

        protected async Task OnCreateNew()
        {
            var parameters = new DialogParameters
            {
                ["MonHoc"] = new MonHocDto { MaSoMonHoc = "", MaKhoa = MaKhoaFilter ?? Guid.Empty },
                ["DialogTitle"] = "Tạo mới Môn Học",
                ["Khoas"] = Khoas
            };

            var dialog = DialogService.Show<EditMonHocDialog>("Tạo mới Môn Học", parameters);
            var result = await dialog.Result;
            if (!result.Canceled)
            {
                var updated = (MonHocDto)result.Data;
                await SaveMonHocAsync(updated);
                await table!.ReloadServerData();
            }
        }

        protected async Task OnEdit(MonHocDto monHoc)
        {
            var parameters = new DialogParameters
            {
                ["MonHoc"] = monHoc,
                ["DialogTitle"] = "Chỉnh sửa Môn Học",
                ["Khoas"] = Khoas
            };
            var dialog = DialogService.Show<EditMonHocDialog>("Chỉnh sửa Môn Học", parameters);
            var result = await dialog.Result;

            if (!result.Canceled)
            {
                var updated = (MonHocDto)result.Data;
                await SaveMonHocAsync(updated);
                await table!.ReloadServerData();
            }
        }

        protected async Task OnConfirmDelete(MonHocDto monHoc)
        {
            var parameters = new DialogParameters
            {
                ["ContentText"] = $"Bạn có chắc chắn muốn xóa môn học '{monHoc.TenMonHoc}' không?"
            };
            var dialog = DialogService.Show<MessageDialog>("Xác nhận xóa", parameters);
            var result = await dialog.Result;

            if (!result.Canceled)
            {
                await DeleteMonHocAsync(monHoc.MaMonHoc.ToString());
                await table!.ReloadServerData();
            }
        }

        protected async Task OnRestore(MonHocDto monHoc)
        {
            var parameters = new DialogParameters
            {
                ["ContentText"] = $"Bạn có chắc chắn muốn khôi phục môn học '{monHoc.TenMonHoc}' không?"
            };
            var dialog = DialogService.Show<MessageDialog>("Xác nhận khôi phục", parameters);
            var result = await dialog.Result;

            if (!result.Canceled)
            {
                await RestoreMonHocAsync(monHoc.MaMonHoc.ToString());
                await table!.ReloadServerData();
            }
        }

        protected void OnViewDetail(MonHocDto monHoc)
        {
            var parameters = new DialogParameters
            {
                ["MonHoc"] = monHoc,
                ["Khoas"] = Khoas
            };
            DialogService.Show<MonHocDetailDialog>("Chi tiết Môn Học", parameters);
        }

        private async Task SaveMonHocAsync(MonHocDto monHoc)
        {
            if (string.IsNullOrWhiteSpace(monHoc.TenMonHoc) || string.IsNullOrWhiteSpace(monHoc.MaSoMonHoc) || monHoc.MaKhoa == Guid.Empty)
            {
                Snackbar.Add("Tên môn học, mã số môn học và khoa là bắt buộc!", Severity.Error);
                return;
            }

            try
            {
                if (monHoc.MaMonHoc == Guid.Empty)
                {
                    var create = new CreateMonHocDto
                    {
                        MaSoMonHoc = monHoc.MaSoMonHoc,
                        TenMonHoc = monHoc.TenMonHoc,
                        MaKhoa = monHoc.MaKhoa,
                        XoaTam = monHoc.XoaTam
                    };
                    var response = await MonHocApiClient.CreateMonHocAsync(create);
                    Snackbar.Add(response.Success ? "Tạo môn học thành công!" : $"Lỗi: {response.Message}", response.Success ? Severity.Success : Severity.Error);
                }
                else
                {
                    var update = new UpdateMonHocDto
                    {
                        TenMonHoc = monHoc.TenMonHoc,
                        MaKhoa = monHoc.MaKhoa,
                        XoaTam = monHoc.XoaTam
                    };
                    var response = await MonHocApiClient.UpdateMonHocAsync(monHoc.MaMonHoc.ToString(), update);
                    Snackbar.Add(response.Success ? "Cập nhật môn học thành công!" : $"Lỗi: {response.Message}", response.Success ? Severity.Success : Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Lỗi hệ thống: {ex.Message}", Severity.Error);
            }
        }

        private async Task DeleteMonHocAsync(string id)
        {
            var response = await MonHocApiClient.SoftDeleteMonHocAsync(id);
            Snackbar.Add(response.Success ? "Xóa tạm thời thành công!" : $"Lỗi: {response.Message}", response.Success ? Severity.Success : Severity.Error);
        }

        private async Task RestoreMonHocAsync(string id)
        {
            var response = await MonHocApiClient.RestoreMonHocAsync(id);
            Snackbar.Add(response.Success ? "Khôi phục thành công!" : $"Lỗi: {response.Message}", response.Success ? Severity.Success : Severity.Error);
        }

        protected string GetKhoaName(Guid maKhoa)
        {
            var khoa = Khoas.Find(k => k.MaKhoa == maKhoa);
            return khoa?.TenKhoa ?? "Không xác định";
        }
    }
}
