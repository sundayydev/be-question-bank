using System.ComponentModel.DataAnnotations;

namespace BEQuestionBank.Shared.DTOs.MaTran;

public class QuestionTypeDto
{
    [Required(ErrorMessage = "Lo?i câu h?i không du?c d? tr?ng.")]
    public string Loai { get; set; }

    [Required(ErrorMessage = "S? lu?ng câu h?i cho lo?i không du?c d? tr?ng.")]
    public int Num { get; set; }
}
