// Trong file LoginBase.cs hoặc block @code của LoginPage.razor
using Microsoft.AspNetCore.Components;
using MudBlazor;
using FEQuestionBank.Client.Services; // Thêm
using BeQuestionBank.Shared.DTOs.NguoiDung; // Thêm

public class LoginBase : ComponentBase
{
    // Tiêm các service mới
    //[Inject] protected AuthApiClient AuthApiClient { get; set; }
    [Inject] protected NavigationManager NavManager { get; set; }

    // Các thuộc tính binding từ MudForm
    protected string Username { get; set; } // Giữ nguyên tên này
    protected string Password { get; set; } // Giữ nguyên tên này
    protected bool IsSubmitting { get; set; }
    
    // Thêm thuộc tính này để báo lỗi
    protected string ErrorMessage { get; set; }

    // Các thuộc tính có sẵn của bạn
    protected MudForm Form;
    protected bool IsPasswordVisible { get; set; }
    protected InputType PasswordInputType { get; set; } = InputType.Password; 
    protected string PasswordIcon { get; set; } = Icons.Material.Filled.VisibilityOff; 

    protected void TogglePasswordVisibility()
    {
        if (IsPasswordVisible)
        {
            IsPasswordVisible = false;
            PasswordInputType = InputType.Password;
            PasswordIcon = Icons.Material.Filled.VisibilityOff;
        }
        else
        {
            IsPasswordVisible = true;
            PasswordInputType = InputType.Text;
            PasswordIcon = Icons.Material.Filled.Visibility;
        }
    }

    protected void HandleInvalidSubmit()
    {
        // MudForm tự xử lý
    }

    /// <summary>
    /// Đây là phần được "FIX"
    /// </summary>
    protected async Task HandleValidSubmit()
    {
        IsSubmitting = true;
        ErrorMessage = null; // Xóa lỗi cũ
        
        // **FIX:** Tạo DTO với đúng tên thuộc tính
        var loginDto = new LoginDto
        {
            TenDangNhap = this.Username, // Lấy từ 'Username' của form
            MatKhau = this.Password     // Lấy từ 'Password' của form
        };

        // try
        // {
        //     bool success = await AuthApiClient.Login(loginDto);
        //
        //     if (success)
        //     {
        //         NavManager.NavigateTo("/"); // Đăng nhập thành công
        //     }
        //     else
        //     {
        //         ErrorMessage = "Tên đăng nhập hoặc mật khẩu không đúng.";
        //     }
        // }
        // catch (Exception ex)
        // {
        //     // (Bạn có thể log 'ex' ra console để debug)
        //     ErrorMessage = "Không thể kết nối đến máy chủ.";
        // }
        // finally
        // {
        //     IsSubmitting = false;
        // }
    }
}