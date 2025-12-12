using System.ComponentModel.DataAnnotations;
using BeQuestionBank.Shared.Enums;

namespace BeQuestionBank.Shared.DTOs.CauHoi;

public class UpdateCauHoiNhomDto
{
    // Mã phần mà câu hỏi nhóm thuộc về
    [Required(ErrorMessage = "Mã phần không được để trống.")]
    public Guid MaPhan { get; set; }

    // Nội dung đoạn văn/ngữ cảnh của câu hỏi nhóm
    public required string NoiDung { get; set; }

    // Cấp độ của câu hỏi nhóm
    [Required(ErrorMessage = "Cấp độ không được để trống.")]
    public short CapDo { get; set; }

    public EnumCLO? CLO { get; set; }

    public int SoCauHoiCon { get; set; } = 0;
    public bool HoanVi { get; set; } = false;
    public Guid? MaCauHoiCha { get; set; } = null;
    public bool XoaTam { get; set; } = false;
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
    public Guid NguoiTao { get; set; }

    // Danh sách các câu hỏi con nằm trong nhóm này
    [Required]
    [MinLength(1, ErrorMessage = "Câu hỏi nhóm phải có ít nhất 1 câu hỏi con.")]
    public List<CauHoiWithCauTraLoiDto> CauHoiCons { get; set; } = new List<CauHoiWithCauTraLoiDto>();
}