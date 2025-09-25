using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BeQuestionBank.Shared.DTOs.YeuCauRutTrich;

namespace BEQuestionBank.Shared.DTOs.MaTran;

public class MaTranDto
{
    [Required(ErrorMessage = "Tổng số câu hỏi không được để trống.")]
    public int TotalQuestions { get; set; }

    [Required(ErrorMessage = "CloPerPart không được để trống.")]
    public bool CloPerPart { get; set; }

    [Required(ErrorMessage = "Danh sách phần không được để trống.")]
    public List<PartDto> Parts { get; set; }
}