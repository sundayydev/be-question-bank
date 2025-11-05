
using BeQuestionBank.Shared.DTOs.Common;
using FEQuestionBank.Client.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BeQuestionBank.Shared.DTOs.Pagination;
using BEQuestionBank.Shared.DTOs.user;
using FEQuestionBank.Client.Pages.NguoiDung;
using System.Linq;

namespace FEQuestionBank.Client.Pages
{
    public partial class NguoiDungBase : ComponentBase
    {
        [Inject] protected INguoiDungApiClient NguoiDungApiClient { get; set; } = default!;
        [Inject] protected ISnackbar Snackbar { get; set; } = default!;
        [Inject] protected IDialogService DialogService { get; set; } = default!;
        [Inject] protected HttpClient Http { get; set; } = default!;

        protected string? _searchTerm;
        protected MudTable<NguoiDungDto>? table;
        protected int TotalUsers { get; set; }
        protected int ActiveUsers { get; set; }
        protected int LockedUsers { get; set; }
        

        protected async Task<TableData<NguoiDungDto>> LoadServerData(TableState state, CancellationToken cancellationToken)
        {
            try
            {
                int page = state.Page + 1;
                int pageSize = state.PageSize;
                string? sort = null;

                if (!string.IsNullOrEmpty(state.SortLabel))
                    sort = $"{state.SortLabel},{(state.SortDirection == SortDirection.Ascending ? "asc" : "desc")}";

                var url = $"api/NguoiDung/paged?page={page}&pageSize={pageSize}";
                if (!string.IsNullOrEmpty(sort)) url += $"&sort={sort}";
                if (!string.IsNullOrEmpty(_searchTerm))
                    url += $"&filter={Uri.EscapeDataString(_searchTerm)}";

                var response = await Http.GetFromJsonAsync<ApiResponse<PagedResult<NguoiDungDto>>>(url, cancellationToken);
                
                if (response?.Success == true && response.Data != null)
                {
                    // Tổng số người dùng
                    TotalUsers = response.Data.TotalCount;

                    // Tạm tính số lượng active/locked trong trang hiện tại
                    ActiveUsers = response.Data.Items.Count(u => !u.BiKhoa);
                    LockedUsers = response.Data.Items.Count(u => u.BiKhoa);
                    StateHasChanged(); 

                    return new TableData<NguoiDungDto>
                    {
                        Items = response.Data.Items ?? new List<NguoiDungDto>(),
                        TotalItems = response.Data.TotalCount
                    };
                }
                
                if (response?.Success == true && response.Data != null)
                {
                    return new TableData<NguoiDungDto>
                    {
                        Items = response.Data.Items ?? new List<NguoiDungDto>(),
                        TotalItems = response.Data.TotalCount
                    };
                }

                return new TableData<NguoiDungDto> {   Items = new List<NguoiDungDto>(), TotalItems = 0 };
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Lỗi: {ex.Message}", Severity.Error);
                return new TableData<NguoiDungDto> {   Items = new List<NguoiDungDto>(), TotalItems = 0 };
            }
        }

        protected async Task OnCreateNew()
        {
            var parameters = new DialogParameters
            {
                ["NguoiDung"] = new NguoiDungDto { NgayTao = DateTime.Now },
                ["DialogTitle"] = "Tạo người dùng mới"
            };

            var dialog = DialogService.Show<EditNguoiDungDialog>("Tạo mới", parameters);
            var result = await dialog.Result;

            if (!result.Canceled)
            {
                var user = (NguoiDungDto)result.Data;
                await SaveNguoiDungAsync(user);
                await table!.ReloadServerData();
            }
        }

        protected async Task OnEdit(NguoiDungDto user)
        {
            var parameters = new DialogParameters
            {
                ["NguoiDung"] = user,
                ["DialogTitle"] = "Chỉnh sửa người dùng"
            };

            var dialog = DialogService.Show<EditNguoiDungDialog>("Chỉnh sửa", parameters);
            var result = await dialog.Result;

            if (!result.Canceled)
            {
                var updated = (NguoiDungDto)result.Data;
                await SaveNguoiDungAsync(updated);
                await table!.ReloadServerData();
            }
        }

        protected async Task OnToggleLock(NguoiDungDto user)
        {
            var action = user.BiKhoa ? "mở khóa" : "khóa";
            var confirm = await DialogService.ShowMessageBox(
                "Xác nhận",
                $"Bạn có chắc muốn {action} tài khoản '{user.TenDangNhap}'?",
                yesText: "Có", cancelText: "Hủy");

            if (confirm == true)
            {
                var response = user.BiKhoa
                    ? await NguoiDungApiClient.UnlockNguoiDungAsync(user.MaNguoiDung.ToString())
                    : await NguoiDungApiClient.LockNguoiDungAsync(user.MaNguoiDung.ToString());

                Snackbar.Add(response.Success ? $"{(user.BiKhoa ? "Mở khóa" : "Khóa")} thành công!" : response.Message,
                             response.Success ? Severity.Success : Severity.Error);

                await table!.ReloadServerData();
            }
        }

        protected async Task OnConfirmDelete(NguoiDungDto user)
        {
            var confirm = await DialogService.ShowMessageBox(
                "Xác nhận xóa",
                $"Xóa người dùng '{user.HoTen}'? Hành động này không thể hoàn tác.",
                yesText: "Xóa", cancelText: "Hủy");

            if (confirm == true)
            {
                var response = await NguoiDungApiClient.DeleteNguoiDungAsync(user.MaNguoiDung.ToString());
                Snackbar.Add(response.Success ? "Xóa thành công!" : response.Message,
                             response.Success ? Severity.Success : Severity.Error);
                await table!.ReloadServerData();
            }
        }

        protected void OnViewDetail(NguoiDungDto user)
        {
            var parameters = new DialogParameters { ["NguoiDung"] = user };
            DialogService.Show<NguoiDungDetailDialog>("Chi tiết người dùng", parameters);
        }

        private async Task SaveNguoiDungAsync(NguoiDungDto user)
        {
            try
            {
                if (user.MaNguoiDung == Guid.Empty)
                {
                    var createDto = new NguoiDungDto
                    {
                        TenDangNhap = user.TenDangNhap,
                        MatKhau = user.MatKhau ?? "123456", // Mật khẩu mặc định
                        HoTen = user.HoTen,
                        Email = user.Email,
                        VaiTro = user.VaiTro,
                        BiKhoa = user.BiKhoa
                    };

                    var response = await NguoiDungApiClient.CreateNguoiDungAsync(createDto);

                    if (response.Success)
                    {
                        Snackbar.Add("Tạo người dùng thành công!", Severity.Success);
                        var createdUser = response.Data; 
                    }
                    else
                    {
                        Snackbar.Add(response.Message, Severity.Error);
                    }
                }
                else
                {
                    var updateDto = new NguoiDungDto
                    {
                        TenDangNhap = user.TenDangNhap,
                        HoTen = user.HoTen,
                        Email = user.Email,
                        VaiTro = user.VaiTro,
                        BiKhoa = user.BiKhoa
                    };

                    if (!string.IsNullOrWhiteSpace(user.MatKhau))
                        updateDto.MatKhau = user.MatKhau;

                    var response = await NguoiDungApiClient.UpdateNguoiDungAsync(user.MaNguoiDung.ToString(), updateDto);

                    if (response.Success)
                        Snackbar.Add("Cập nhật thành công!", Severity.Success);
                    else
                        Snackbar.Add(response.Message, Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Lỗi: {ex.Message}", Severity.Error);
            }
        }

    }
}