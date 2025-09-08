using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeQuestionBank.Shared.DTOs.Phan;

public class UpdatePhanDto
{
    [Required(ErrorMessage = "Mã môn học là bắt buộc.")]
    public Guid MaMonHoc { get; set; }

    [Required(ErrorMessage = "Tên phần là bắt buộc.")]
    [StringLength(255, ErrorMessage = "Tên phần không được vượt quá 255 ký tự.")]
    public required string TenPhan { get; set; }

    [StringLength(1000, ErrorMessage = "Nội dung không được vượt quá 1000 ký tự.")]
    public string? NoiDung { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Thứ tự phải là số không âm.")]
    public int ThuTu { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Số lượng câu hỏi phải là số không âm.")]
    public int SoLuongCauHoi { get; set; }

    public Guid? MaPhanCha { get; set; }

    public bool LaCauHoiNhom { get; set; } = false;

    public int? MaSoPhan { get; set; } = null;

    public bool XoaTam { get; set; } = false;

    public DateTime NgayCapNhat { get; set; } = DateTime.UtcNow;
}
