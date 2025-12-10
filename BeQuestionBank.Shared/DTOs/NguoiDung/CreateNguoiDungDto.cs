using System.ComponentModel.DataAnnotations;
using BeQuestionBank.Shared.Enums;

namespace BEQuestionBank.Shared.DTOs.user;

public class CreateNguoiDungDto
{
    public Guid MaNguoiDung { get; set; }

    [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
    [RegularExpression(@"^\S+$", ErrorMessage = "Tên đăng nhập phải viết liền, không được chứa khoảng trắng")]
    public string TenDangNhap { get; set; }

    [Required(ErrorMessage = "Mật khẩu không được để trống")]
    [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
    public string MatKhau { get; set; }
    public Guid? MaKhoa { get; set; }
    public string ?TenKhoa { get; set; }

    [Required(ErrorMessage = "Họ tên không được để trống")]
    public string HoTen { get; set; }

    [Required(ErrorMessage = "Email không được để trống")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng (ví dụ: user@domain.com)")]
    public string? Email { get; set; }
    public EnumRole VaiTro { get; set; }
    public bool BiKhoa { get; set; }
    public DateTime? NgayDangNhapCuoi { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
    public DateTime NgayCapNhat { get; set; } = DateTime.UtcNow;
}