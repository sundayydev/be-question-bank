using System.ComponentModel.DataAnnotations;
using BeQuestionBank.Domain.Enums;

namespace BeQuestionBank.Shared.DTOs.CauHoi;

public class UpdateCauHoiNhomDto
{
    // Mã ph?n mà câu h?i nhóm thu?c v?
    [Required(ErrorMessage = "Mã ph?n không du?c d? tr?ng.")]
    public Guid MaPhan { get; set; }

    // N?i dung do?n van/ng? c?nh c?a câu h?i nhóm
    public required string NoiDung { get; set; }

    // C?p d? c?a câu h?i nhóm
    [Required(ErrorMessage = "C?p d? không du?c d? tr?ng.")]
    public short CapDo { get; set; }

    public EnumCLO? CLO { get; set; }

    public int SoCauHoiCon { get; set; } = 0;
    public bool HoanVi { get; set; } = false;
    public Guid? MaCauHoiCha { get; set; } = null;
    public bool XoaTam { get; set; } = false;
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
    public Guid NguoiTao { get; set; }

    // Danh sách các câu h?i con n?m trong nhóm này
    [Required]
    [MinLength(1, ErrorMessage = "Câu h?i nhóm ph?i có ít nh?t 1 câu h?i con.")]
    public List<CauHoiWithCauTraLoiDto> CauHoiCons { get; set; } = new List<CauHoiWithCauTraLoiDto>();
}
