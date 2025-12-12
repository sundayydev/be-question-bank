using System.ComponentModel.DataAnnotations;
using BeQuestionBank.Shared.DTOs.CauTraLoi;
using BeQuestionBank.Shared.Enums;

namespace BeQuestionBank.Shared.DTOs.CauHoi;

public class CreateCauHoiDienTuDto : CreateCauHoiDto
{
    [Required]
    [MinLength(1, ErrorMessage = "Câu hỏi nhóm phải có ít nhất 1 câu hỏi con.")]
    public List<CreateChilDienTu> CauHoiCons { get; set; } = new();
}
