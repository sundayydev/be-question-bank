using BeQuestionBank.Shared.Enums;

namespace BEQuestionBank.Shared.DTOs.user;

public class CreateNguoiDungDto
{
    public Guid MaNguoiDung { get; set; }
    public string TenDangNhap { get; set; }
    public string MatKhau { get; set; }
    public Guid? MaKhoa { get; set; }
    public string ?TenKhoa { get; set; }
    public string HoTen { get; set; }
    public string? Email { get; set; }
    public EnumRole VaiTro { get; set; }
    public bool BiKhoa { get; set; }
    public DateTime? NgayDangNhapCuoi { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
    public DateTime NgayCapNhat { get; set; } = DateTime.UtcNow;
}