// CreateUserManual.razor.cs
using BeQuestionBank.Shared.DTOs.Khoa;
using BEQuestionBank.Shared.DTOs.user;
using BeQuestionBank.Shared.Enums;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using FEQuestionBank.Client.Services;

namespace FEQuestionBank.Client.Pages.User
{
    public class CreateUserManualBase : ComponentBase
    {
        [Inject] private INguoiDungApiClient UserApi { get; set; } = default!;
        [Inject] private IKhoaApiClient KhoaApi { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        
        protected CreateNguoiDungDto Model { get; set; } = new();

        protected List<KhoaDto> DanhSachKhoa { get; set; } = new();
        protected bool IsSubmitting { get; set; } = false;

        protected List<BreadcrumbItem> _breadcrumbs = new()
        {
            new BreadcrumbItem("Trang chủ", href: "/"),
            new BreadcrumbItem("Quản lý", href: "#", disabled: true),
            new BreadcrumbItem("Người dùng", href: "/user/list"),
            new BreadcrumbItem("Tạo thủ công", href: "/user/create-manual")
        };

        // Validation chính xác
        protected bool IsValid =>
            !string.IsNullOrWhiteSpace(Model.TenDangNhap) &&
            !string.IsNullOrWhiteSpace(Model.MatKhau) &&
            !string.IsNullOrWhiteSpace(Model.HoTen) &&
            !string.IsNullOrWhiteSpace(Model.Email);

        protected override async Task OnInitializedAsync()
        {
            var response = await KhoaApi.GetAllKhoasAsync();
            if (response.Success && response.Data != null)
            {
                DanhSachKhoa = response.Data.ToList();
            }
            else
            {
                Snackbar.Add("Không tải được danh sách khoa", Severity.Error);
            }
        }

        protected async Task SaveUser()
        {
            if (!IsValid)
            {
                Snackbar.Add("Vui lòng điền đầy đủ và đúng thông tin.", Severity.Warning);
                return;
            }

            IsSubmitting = true;
            try
            {
                // Gọi đúng DTO
                var response = await UserApi.CreateNguoiDungAsync(Model);

                if (response.Success)
                {
                    Snackbar.Add("Tạo người dùng thành công!", Severity.Success);
                    Navigation.NavigateTo("/user/list", forceLoad: true);
                }
                else
                {
                    Snackbar.Add(response.Message ?? "Tạo người dùng thất bại", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Lỗi hệ thống: {ex.Message}", Severity.Error);
            }
            finally
            {
                IsSubmitting = false;
            }
        }

        protected void GoBack() => Navigation.NavigateTo("/user/list");
    }
}