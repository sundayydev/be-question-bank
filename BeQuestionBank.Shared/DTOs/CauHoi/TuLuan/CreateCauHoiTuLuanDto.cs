using System.ComponentModel.DataAnnotations;
using BeQuestionBank.Domain.Enums;

namespace BeQuestionBank.Shared.DTOs.CauHoi.Create;

public class CreateCauHoiTuLuanDto : CreateCauHoiDto
{

    [Required]
    [MinLength(1, ErrorMessage = "Câu h?i T? lu?n ph?i có ít nh?t 1 câu h?i con.")]
    public List<CreateCauHoiDto> CauHoiCons { get; set; } = new();
}
