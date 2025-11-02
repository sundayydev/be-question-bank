using BeQuestionBank.Shared.Enums;

namespace BeQuestionBank.Shared.DTOs.NguoiDung;

public class RegisterDto
{
    public string TenDangNhap { get; set; }
    public string MatKhau { get; set; }
    public string HoTen { get; set; }
    public string? Email { get; set; }
    public EnumRole VaiTro { get; set; }
}