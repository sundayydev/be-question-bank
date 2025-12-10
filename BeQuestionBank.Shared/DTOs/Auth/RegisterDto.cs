using System.ComponentModel.DataAnnotations;
using BeQuestionBank.Shared.Enums;

namespace BeQuestionBank.Shared.DTOs.NguoiDung;

public class RegisterDto
{
    [RegularExpression(@"^\S+$", ErrorMessage = "Tên đăng nhập phải viết liền, không được chứa khoảng trắng")]
    public string TenDangNhap { get; set; }
    public string MatKhau { get; set; }
    public string HoTen { get; set; }
    public string? Email { get; set; }
    public EnumRole VaiTro { get; set; }
}