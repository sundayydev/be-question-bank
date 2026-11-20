using System.ComponentModel.DataAnnotations;
using BeQuestionBank.Domain.Enums;

namespace BEQuestionBank.Shared.DTOs.user;

public class CreateNguoiDungDto
{
    public Guid MaNguoiDung { get; set; }

    [Required(ErrorMessage = "Tên dang nh?p không du?c d? tr?ng")]
    [RegularExpression(@"^\S+$", ErrorMessage = "Tên dang nh?p ph?i vi?t li?n, không du?c ch?a kho?ng tr?ng")]
    public string TenDangNhap { get; set; }

    [Required(ErrorMessage = "M?t kh?u không du?c d? tr?ng")]
    [MinLength(6, ErrorMessage = "M?t kh?u ph?i có ít nh?t 6 ký t?")]
    public string MatKhau { get; set; }
    public Guid? MaKhoa { get; set; }
    public string ?TenKhoa { get; set; }

    [Required(ErrorMessage = "H? tên không du?c d? tr?ng")]
    public string HoTen { get; set; }

    [Required(ErrorMessage = "Email không du?c d? tr?ng")]
    [EmailAddress(ErrorMessage = "Email không dúng d?nh d?ng (ví d?: user@domain.com)")]
    public string? Email { get; set; }
    public EnumRole VaiTro { get; set; }
    public bool BiKhoa { get; set; }
    public DateTime? NgayDangNhapCuoi { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
    public DateTime NgayCapNhat { get; set; } = DateTime.UtcNow;
}
