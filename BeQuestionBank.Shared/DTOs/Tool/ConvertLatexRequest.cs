namespace BeQuestionBank.Shared.DTOs.Tool;

public class ConvertLatexRequest
{
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// N?u true thì s? merge các span text-content li?n k? sau khi convert LaTeX
    /// </summary>
    public bool MergeSpans { get; set; } = true;
}
