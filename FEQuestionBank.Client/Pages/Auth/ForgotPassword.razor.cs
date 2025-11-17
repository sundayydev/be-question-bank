using FEQuestionBank.Client.Services.Interface;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FEQuestionBank.Client.Pages.Auth
{
    public partial class ForgotPassword
    {
        [Inject] private IAuthApiClient AuthApiClient { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        [Inject] private NavigationManager Nav { get; set; } = default!;

        private MudForm form = default!;
        private bool IsValid;
        private bool Loading;
        private string? Error;
        private bool ShowPassword = false;
        private int Step = 1;
        private string ButtonText = "Gửi mã OTP";

        // Form fields
        private string Email = "";
        private string Otp = "";
        private string MatKhauMoi = "";
        private string NhapLaiMatKhauMoi = "";

        private void TogglePassword() => ShowPassword = !ShowPassword;

        private IEnumerable<string> EmailValidator(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                yield return "Email không được để trống";

            var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            if (!regex.IsMatch(email))
                yield return "Email không hợp lệ";
        }

        private IEnumerable<string> ValidatePasswordMatch(string value)
        {
            if (!string.IsNullOrEmpty(MatKhauMoi) && value != MatKhauMoi)
                yield return "Mật khẩu nhập lại không khớp";
        }

        private async Task HandleStep()
        {
            await form.Validate();
            if (!form.IsValid)
            {
                Snackbar.Add("Vui lòng nhập đúng và đủ thông tin!", Severity.Warning);
                return;
            }

            try
            {
                Loading = true;
                Error = null;

                switch (Step)
                {
                    case 1:
                        await SendOtp();
                        break;
                    case 2:
                        await VerifyOtp();
                        break;
                    case 3:
                        await ResetPassword();
                        break;
                }
            }
            catch (Exception ex)
            {
                Error = "Lỗi kết nối hệ thống. Vui lòng thử lại.";
                Snackbar.Add(Error, Severity.Error);
                Console.WriteLine(ex);
            }
            finally
            {
                Loading = false;
            }
        }

        private async Task SendOtp()
        {
            var response = await AuthApiClient.SendOtpAsync(Email);
            if (response.StatusCode >= 200 && response.StatusCode < 300)
            {
                Snackbar.Add("Mã OTP đã được gửi đến email!", Severity.Success);
                Step = 2;
                ButtonText = "Xác nhận OTP";
            }
            else
            {
                Error = response.Message ?? "Không thể gửi OTP. Email có thể không tồn tại.";
                Snackbar.Add(Error, Severity.Error);
            }
        }

        private async Task VerifyOtp()
        {
            var response = await AuthApiClient.VerifyOtpAsync(Email, Otp);
            if (response.StatusCode >= 200 && response.StatusCode < 300)
            {
                Snackbar.Add("Xác nhận OTP thành công!", Severity.Success);
                Step = 3;
                ButtonText = "Đặt lại mật khẩu";
            }
            else
            {
                Error = response.Message ?? "Mã OTP không đúng hoặc đã hết hạn.";
                Snackbar.Add(Error, Severity.Error);
            }
        }

        private async Task ResetPassword()
        {
            var response = await AuthApiClient.ResetPasswordAsync(Email, Otp, MatKhauMoi);
            if (response.StatusCode >= 200 && response.StatusCode < 300)
            {
                Snackbar.Add("Đặt lại mật khẩu thành công! Vui lòng đăng nhập.", Severity.Success);
                Nav.NavigateTo("/login");
            }
            else
            {
                Error = response.Message ?? "Không thể đặt lại mật khẩu. Vui lòng thử lại.";
                Snackbar.Add(Error, Severity.Error);
            }
        }
    }
}