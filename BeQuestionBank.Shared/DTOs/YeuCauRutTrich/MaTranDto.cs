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
    /// Chi ti?t t?ng ô trong ma tr?n CLO × Lo?i câu h?i
    /// Ch?a s? lu?ng câu h?i chính xác cho t?ng c?p (CLO, Lo?i)
    /// </summary>
    public List<MatrixCellDto>? MatrixCells { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CloPerPart && (Parts == null || !Parts.Any()))
        {
            yield return new ValidationResult(
                "Danh sách ph?n không du?c d? tr?ng khi CloPerPart = true.",
                new[] { nameof(Parts) });
        }

        if (!CloPerPart && (Clos == null || QuestionTypes == null))
        {
            yield return new ValidationResult(
                "Clos và QuestionTypes không du?c d? tr?ng khi CloPerPart = false.",
                new[] { nameof(Clos), nameof(QuestionTypes) });
        }
    }
}
