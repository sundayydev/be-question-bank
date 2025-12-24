using System.ComponentModel.DataAnnotations;
using BEQuestionBank.Shared.DTOs.MaTran;

public class MaTranDto : IValidatableObject
{
    public int TotalQuestions { get; set; }
    public bool CloPerPart { get; set; }
    public List<PartDto>? Parts { get; set; }
    public List<CloDto>? Clos { get; set; }
    public List<QuestionTypeDto>? QuestionTypes { get; set; }

    /// <summary>
    /// Chi tiết từng ô trong ma trận CLO × Loại câu hỏi
    /// Chứa số lượng câu hỏi chính xác cho từng cặp (CLO, Loại)
    /// </summary>
    public List<MatrixCellDto>? MatrixCells { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CloPerPart && (Parts == null || !Parts.Any()))
        {
            yield return new ValidationResult(
                "Danh sách phần không được để trống khi CloPerPart = true.",
                new[] { nameof(Parts) });
        }

        if (!CloPerPart && (Clos == null || QuestionTypes == null))
        {
            yield return new ValidationResult(
                "Clos và QuestionTypes không được để trống khi CloPerPart = false.",
                new[] { nameof(Clos), nameof(QuestionTypes) });
        }
    }
}