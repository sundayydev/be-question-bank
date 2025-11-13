using BeQuestionBank.Shared.DTOs.YeuCauRutTrich;
using BeQuestionBank.Shared.DTOs.Common;
using BeQuestionBank.Shared.DTOs.Pagination;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FEQuestionBank.Client.Pages.YeuCauRutTrich;
using FEQuestionBank.Client.Services;

namespace FEQuestionBank.Client.Pages.YeuCau
{
    public class YeuCauHistoryBase : ComponentBase
    {
        [Inject] protected IYeuCauRutTrichApiClient YeuCauApi { get; set; }
        [Inject] protected ISnackbar Snackbar { get; set; } = default!;
        [Inject] protected IDialogService DialogService { get; set; } = default!;
        [Inject] protected NavigationManager Navigation { get; set; } = default!;

        protected MudTable<YeuCauRutTrichDto>? table;
        protected string? _searchTerm;
        protected bool? _filterTrangThai;

        // Thống kê
        protected int TotalYeuCau;
        protected int DaXuLyCount;
        protected int ChuaXuLyCount;
        protected int UniqueUsers;

        protected List<BreadcrumbItem> _breadcrumbs = new()
        {
            new("Trang chủ", href: "/"),
            new("Yêu cầu rút trích", href: "#", disabled: true),
            new("Lịch sử yêu cầu", href: "/tools/extract-history")
        };

        protected override async Task OnInitializedAsync()
        {
            await LoadStatsAsync();
        }

        private async Task LoadStatsAsync()
        {
            try
            {
                var response = await YeuCauApi.GetAllAsync();
                if (response?.Success == true && response.Data != null)
                {
                    var data = response.Data;
                    TotalYeuCau = data.Count;
                    DaXuLyCount = data.Count(x => x.DaXuLy == true);
                    ChuaXuLyCount = data.Count(x => x.DaXuLy == false);
                    UniqueUsers = data.Select(x => x.MaNguoiDung).Distinct().Count();
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Lỗi tải thống kê: {ex.Message}", Severity.Error);
            }
        }

        protected async Task<TableData<YeuCauRutTrichDto>> LoadServerData(TableState state, CancellationToken cancellationToken)
        {
            try
            {
                int page = state.Page + 1;
                int pageSize = state.PageSize;

                string sort = !string.IsNullOrEmpty(state.SortLabel)
                    ? $"{state.SortLabel},{(state.SortDirection == SortDirection.Ascending ? "asc" : "desc")}"
                    : "NgayYeuCau,desc";

                // var url = $"api/YeuCauRutTrich/paged?page={page}&pageSize={pageSize}&sort={sort}";
                //
                // if (!string.IsNullOrWhiteSpace(_searchTerm))
                //     url += $"&search={Uri.EscapeDataString(_searchTerm)}";
                //
                // if (_filterTrangThai.HasValue)
                //     url += $"&daXuLy={_filterTrangThai.Value}";

                var response = await YeuCauApi.GetPagedAsync(page, pageSize, sort, _searchTerm, _filterTrangThai);

                if (response?.Success == true && response.Data != null)
                {
                    return new TableData<YeuCauRutTrichDto>
                    {
                        Items = response.Data.Items ?? new(),
                        TotalItems = response.Data.TotalCount
                    };
                }

                return new TableData<YeuCauRutTrichDto> { Items = new List<YeuCauRutTrichDto>(), TotalItems = 0 };
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Lỗi tải dữ liệu: {ex.Message}", Severity.Error);
                return new TableData<YeuCauRutTrichDto> { Items = new List<YeuCauRutTrichDto>(), TotalItems = 0 };
            }
        }

        protected async Task ReloadTable()
        {
            if (table != null) await table.ReloadServerData();
            await LoadStatsAsync();
        }

        protected void OnViewDetail(YeuCauRutTrichDto yc)
        {
            var parameters = new DialogParameters { ["YeuCau"] = yc };
            DialogService.Show<YeuCauDetailDialog>("Chi tiết yêu cầu", parameters);
        }

        protected void OnViewDeThi(Guid maYeuCau)
        {
            // Giả sử có API lấy MaDeThi từ MaYeuCau
            Navigation.NavigateTo($"/exams/{maYeuCau}");
        }
    }
}