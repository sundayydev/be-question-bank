using System.ComponentModel.DataAnnotations;

namespace BEQuestionBank.Shared.DTOs.MaTran;

public class CloDto
{
    [Required(ErrorMessage = "CLO không du?c d? tr?ng.")]
    public int Clo { get; set; }

    [Required(ErrorMessage = "S? lu?ng câu h?i cho CLO không du?c d? tr?ng.")]
    public int Num { get; set; }

    /// <summary>
    /// S? câu con t?i da cho m?i câu h?i cha (ch? dành cho d? t? lu?n).
    /// N?u null: không gi?i h?n s? câu con
    /// N?u có giá tr?: ch? ch?n câu h?i có SoCauHoiCon <= SubQuestionCount
    /// </summary>
    public int? SubQuestionCount { get; set; }
}
