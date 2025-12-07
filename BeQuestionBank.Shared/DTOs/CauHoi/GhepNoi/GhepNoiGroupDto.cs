namespace BeQuestionBank.Shared.DTOs.CauHoi;

public class GhepNoiGroupDto
{
    public CauHoiDto NhomCha { get; set; }
    public List<GhepNoiDto> Pairs { get; set; } = new();
}