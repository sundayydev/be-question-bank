using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

public class CreateSingleQuestionBase : ComponentBase
{
    [Inject] NavigationManager Navigation { get; set; } = default!;

    protected string QuestionContent { get; set; } = string.Empty;

    protected List<AnswerModel> Answers { get; set; } = new()
    {
        new AnswerModel { Text = "Câu trả lời A" },
        new AnswerModel { Text = "Câu trả lời B" }
    };

    protected List<string> Faculties { get; set; } = new() { "CNTT", "Kinh tế", "Cơ khí" };
    protected List<string> Subjects { get; set; } = new() { "Lập trình C#", "CSDL", "AI" };
    protected List<string> CLOs { get; set; } = new() { "CLO1", "CLO2", "CLO3" };

    protected string SelectedFaculty { get; set; } = string.Empty;
    protected string SelectedSubject { get; set; } = string.Empty;
    protected string Chapter { get; set; } = string.Empty;
    protected string SelectedCLO { get; set; } = string.Empty;

    protected void AddAnswer() => Answers.Add(new AnswerModel());

    protected void RemoveAnswer(AnswerModel answer) => Answers.Remove(answer);

    protected void ToggleCorrectAnswer(AnswerModel answer)
    {
        foreach (var a in Answers)
            a.IsCorrect = false; // chỉ 1 đáp án đúng

        answer.IsCorrect = true;
    }

    protected void SaveQuestion()
    {
        // TODO: lưu dữ liệu vào DB hoặc gọi API
        Console.WriteLine($"Lưu câu hỏi: {QuestionContent}");
    }

    protected void PreviewQuestion()
    {
        // TODO: mở dialog xem trước
        Console.WriteLine("Xem trước câu hỏi");
    }

    protected void GoBack() => Navigation.NavigateTo("/create-question");
}

public class AnswerModel
{
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}