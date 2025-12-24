namespace BEQuestionBank.Shared.DTOs.MaTran;

/// <summary>
/// Đại diện cho một ô trong ma trận CLO × Loại câu hỏi
/// </summary>
public class MatrixCellDto
{
    /// <summary>
    /// CLO (1-5)
    /// </summary>
    public int Clo { get; set; }

    /// <summary>
    /// Loại câu hỏi (NH, TL, TN, MN, GN, DT)
    /// </summary>
    public string Loai { get; set; } = string.Empty;

    /// <summary>
    /// Số lượng câu hỏi trong ô này
    /// </summary>
    public int Num { get; set; }

    /// <summary>
    /// Số câu con cho loại NH/TL (null nếu không áp dụng)
    /// </summary>
    public int? SubQuestionCount { get; set; }
}
