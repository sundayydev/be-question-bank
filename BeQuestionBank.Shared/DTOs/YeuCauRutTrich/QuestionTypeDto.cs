using System.ComponentModel.DataAnnotations;

namespace BEQuestionBank.Shared.DTOs.MaTran;

public class QuestionTypeDto
{
    [Required(ErrorMessage = "Loại câu hỏi không được để trống.")]
    public string Loai { get; set; }

    [Required(ErrorMessage = "Số lượng câu hỏi cho loại không được để trống.")]
    public int Num { get; set; }
}