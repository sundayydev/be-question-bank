using System.ComponentModel.DataAnnotations;
using BeQuestionBank.Domain.Enums;

namespace BeQuestionBank.Shared.DTOs.NguoiDung;

public class RegisterDto
{
    [RegularExpression(@"^\S+$", ErrorMessage = "Tên dang nh?p ph?i vi?t li?n, không du?c ch?a kho?ng tr?ng")]
    public string TenDangNhap { get; set; }
    public string MatKhau { get; set; }
    public string HoTen { get; set; }
    public string? Email { get; set; }
    public EnumRole VaiTro { get; set; }
}
