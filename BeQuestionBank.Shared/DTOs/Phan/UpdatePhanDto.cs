using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeQuestionBank.Shared.DTOs.Phan;

public class UpdatePhanDto
{
    [Required(ErrorMessage = "Mã môn h?c là b?t bu?c.")]
    public Guid MaMonHoc { get; set; }

    [Required(ErrorMessage = "Tên ph?n là b?t bu?c.")]
    [StringLength(255, ErrorMessage = "Tên ph?n không du?c vu?t quá 255 ký t?.")]
    public required string TenPhan { get; set; }

    [StringLength(1000, ErrorMessage = "N?i dung không du?c vu?t quá 1000 ký t?.")]
    public string? NoiDung { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Th? t? ph?i là s? không âm.")]
    public int ThuTu { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "S? lu?ng câu h?i ph?i là s? không âm.")]
    public int SoLuongCauHoi { get; set; }

    public Guid? MaPhanCha { get; set; }

    public bool LaCauHoiNhom { get; set; } = false;

    public int? MaSoPhan { get; set; } = null;

    public bool XoaTam { get; set; } = false;

    public DateTime NgayCapNhat { get; set; } = DateTime.UtcNow;
}
