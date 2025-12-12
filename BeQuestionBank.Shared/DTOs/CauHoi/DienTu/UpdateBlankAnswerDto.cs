namespace BeQuestionBank.Shared.DTOs.CauHoi;

public class UpdateBlankAnswerDto
{
    public Guid? MaCauHoi { get; set; } // ID câu hỏi con cũ (nếu có)
    public Guid? MaCauTraLoi { get; set; } // ID đáp án cũ
    public string NoiDung { get; set; } = string.Empty;
    public bool HoanVi { get; set; } = true;
}