using System.ComponentModel.DataAnnotations;
using BeQuestionBank.Domain.Enums;

namespace BEQuestionBank.Shared.DTOs.user;

public class UpdateNguoiDungDto
{
    [RegularExpression(@"^\S+$", ErrorMessage = "Tên dang nh?p ph?i vi?t li?n, không du?c ch?a kho?ng tr?ng")]
    public string TenDangNhap { get; set; }

    [MinLength(6, ErrorMessage = "M?t kh?u m?i ph?i có ít nh?t 6 ký t?")]
    public string? MatKhau { get; set; }
    public Guid? MaKhoa { get; set; }
    public string HoTen { get; set; }

    [EmailAddress(ErrorMessage = "Email không dúng d?nh d?ng")]
    public string? Email { get; set; }
    public EnumRole VaiTro { get; set; }
    public bool BiKhoa { get; set; }
    public DateTime NgayCapNhat { get; set; } = DateTime.UtcNow;
}
