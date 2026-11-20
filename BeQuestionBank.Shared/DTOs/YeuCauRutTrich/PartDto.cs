using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BeQuestionBank.Shared.DTOs.YeuCauRutTrich;

namespace BEQuestionBank.Shared.DTOs.MaTran;

public class PartDto
{
    [Required(ErrorMessage = "Mã ph?n không du?c d? tr?ng.")]
    public Guid MaPhan { get; set; }

    [Required(ErrorMessage = "S? câu h?i không du?c d? tr?ng.")]
    public int NumQuestions { get; set; }

    [Required(ErrorMessage = "Danh sách CLO không du?c d? tr?ng.")]
    public List<CloDto> Clos { get; set; }

    [Required(ErrorMessage = "Danh sách lo?i câu h?i không du?c d? tr?ng.")]
    public List<QuestionTypeDto> QuestionTypes { get; set; }

    /// <summary>
    /// Chi ti?t t?ng ô trong ma tr?n CLO × Lo?i câu h?i cho ph?n này
    /// </summary>
    public List<MatrixCellDto>? MatrixCells { get; set; }
}
