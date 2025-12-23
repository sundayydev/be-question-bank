using System.ComponentModel.DataAnnotations;

namespace BEQuestionBank.Shared.DTOs.MaTran;

public class CloDto
{
    [Required(ErrorMessage = "CLO không được để trống.")]
    public int Clo { get; set; }

    [Required(ErrorMessage = "Số lượng câu hỏi cho CLO không được để trống.")]
    public int Num { get; set; }

    /// <summary>
    /// Số câu con tối đa cho mỗi câu hỏi cha (chỉ dành cho đề tự luận).
    /// Nếu null: không giới hạn số câu con
    /// Nếu có giá trị: chỉ chọn câu hỏi có SoCauHoiCon <= SubQuestionCount
    /// </summary>
    public int? SubQuestionCount { get; set; }
}
