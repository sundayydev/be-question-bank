using System.ComponentModel.DataAnnotations;

namespace BEQuestionBank.Shared.DTOs.MaTran;

public class CloDto
{
    [Required(ErrorMessage = "CLO không được để trống.")]
    public int Clo { get; set; }

    [Required(ErrorMessage = "Số lượng câu hỏi cho CLO không được để trống.")]
    public int Num { get; set; }
}