namespace BeQuestionBank.Shared.DTOs.CauHoi;

public class UpdateBlankAnswerDto
{
    public Guid? MaCauHoi { get; set; } // ID câu h?i con cu (n?u có)
    public Guid? MaCauTraLoi { get; set; } // ID dáp án cu
    public string NoiDung { get; set; } = string.Empty;
    public bool HoanVi { get; set; } = true;
}
