using System.ComponentModel.DataAnnotations;
using BeQuestionBank.Shared.DTOs.CauTraLoi;
using BeQuestionBank.Domain.Enums;

namespace BeQuestionBank.Shared.DTOs.CauHoi;

public class CreateCauHoiDienTuDto : CreateCauHoiDto
{
    [Required]
    [MinLength(1, ErrorMessage = "Câu h?i nhóm ph?i có ít nh?t 1 câu h?i con.")]
    public List<CreateChilDienTu> CauHoiCons { get; set; } = new();
}
