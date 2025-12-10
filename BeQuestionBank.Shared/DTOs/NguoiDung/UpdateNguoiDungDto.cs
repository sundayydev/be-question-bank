using System.ComponentModel.DataAnnotations;
using BeQuestionBank.Shared.Enums;

namespace BEQuestionBank.Shared.DTOs.user;

public class UpdateNguoiDungDto
{
    [RegularExpression(@"^\S+$", ErrorMessage = "Tên đăng nhập phải viết liền, không được chứa khoảng trắng")]
    public string TenDangNhap { get; set; }

    [MinLength(6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự")]
    public string? MatKhau { get; set; }
    public Guid? MaKhoa { get; set; }
    public string HoTen { get; set; }

    [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
    public string? Email { get; set; }
    public EnumRole VaiTro { get; set; }
    public bool BiKhoa { get; set; }
    public DateTime NgayCapNhat { get; set; } = DateTime.UtcNow;
}