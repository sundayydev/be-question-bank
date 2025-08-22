using BeQuestionBank.Domain.Common;
using BeQuestionBank.Shared.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeQuestionBank.Domain.Models;

[Table("NguoiDung")]
public class NguoiDung : ModelBase
{
    [Key]
    public Guid MaNguoiDung { get; set; }
    [Required]
    public required string TenDangNhap { get; set; }
    [Required]
    public required string MatKhau { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public string? Email { get; set; }
    public EnumRole VaiTro { get; set; } = EnumRole.User;
    public bool BiKhoa { get; set; } = false;
    [ForeignKey("Khoa")]
    public Guid? MaKhoa { get; set; }
    public DateTime? NgayDangNhapCuoi { get; set; }

    public virtual Khoa? Khoa { get; set; }
}