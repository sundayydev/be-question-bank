using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BeQuestionBank.Shared.DTOs.YeuCauRutTrich;

namespace BEQuestionBank.Shared.DTOs.MaTran;

public class PartDto
{
    [Required(ErrorMessage = "Mã phần không được để trống.")]
    public Guid MaPhan { get; set; }

    [Required(ErrorMessage = "Số câu hỏi không được để trống.")]
    public int NumQuestions { get; set; }

    [Required(ErrorMessage = "Danh sách CLO không được để trống.")]
    public List<CloDto> Clos { get; set; }

    [Required(ErrorMessage = "Danh sách loại câu hỏi không được để trống.")]
    public List<QuestionTypeDto> QuestionTypes { get; set; }
    
}