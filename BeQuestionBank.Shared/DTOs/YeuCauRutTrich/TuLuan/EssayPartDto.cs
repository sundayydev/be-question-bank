using BEQuestionBank.Shared.DTOs.MaTran;

namespace BeQuestionBank.Shared.DTOs.YeuCauRutTrich;

public class EssayPartDto
{
    public Guid Part { get; set; }
    public List<CloDto> Clos { get; set; } = [];
}