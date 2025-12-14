namespace BeQuestionBank.Shared.DTOs.Tool;

public class ConvertLatexRequest
{
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Nếu true thì sẽ merge các span text-content liền kề sau khi convert LaTeX
    /// </summary>
    public bool MergeSpans { get; set; } = true;
}