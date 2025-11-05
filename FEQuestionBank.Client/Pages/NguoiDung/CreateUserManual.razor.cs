using BeQuestionBank.Shared.DTOs.Khoa;
using BEQuestionBank.Shared.DTOs.user;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using FEQuestionBank.Client.Services;
using System.Linq;


namespace FEQuestionBank.Client.Pages.User
{
    public class CreateUserManualBase : ComponentBase
    {
        [Inject] private INguoiDungApiClient UserApi { get; set; } = default!;
        [Inject] private IKhoaApiClient KhoaApi { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;

        protected NguoiDungDto User { get; set; } = new();
        protected List<KhoaDto> DanhSachKhoa { get; set; } = new();
        protected bool IsSubmitting { get; set; }

        protected bool IsValid =>
            !string.IsNullOrWhiteSpace(User.TenDangNhap) &&
            !string.IsNullOrWhiteSpace(User.MatKhau) &&
            !string.IsNullOrWhiteSpace(User.HoTen) &&
            !string.IsNullOrWhiteSpace(User.Email);

        protected override async Task OnInitializedAsync()
        {
            try
            {
                // Lấy danh sách khoa từ API
                var response = await KhoaApi.GetAllKhoasAsync();

                if (response.Success && response.Data != null)
                    DanhSachKhoa = response.Data.ToList();
                User.NgayTao = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Không thể tải danh sách khoa: {ex.Message}", Severity.Error);
            }
        }

        protected async Task SaveUser()
        {
            if (!IsValid)
            {
                Snackbar.Add("Vui lòng điền đầy đủ thông tin hợp lệ.", Severity.Warning);
                return;
            }

            IsSubmitting = true;
            try
            {
                var response = await UserApi.CreateNguoiDungAsync(User);

                if (response.Success)
                {
                    Snackbar.Add("Tạo người dùng thành công!", Severity.Success);
                    Navigation.NavigateTo("/user/list", forceLoad: true);
                }
                else
                {
                    Snackbar.Add(response.Message ?? "Lỗi khi tạo người dùng", Severity.Error);
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
