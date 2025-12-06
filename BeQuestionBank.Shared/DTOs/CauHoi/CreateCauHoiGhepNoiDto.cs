using System.ComponentModel.DataAnnotations;
using BeQuestionBank.Shared.Enums;

namespace BeQuestionBank.Shared.DTOs.CauHoi;

public class CreateCauHoiGhepNoiDto
{
    [Required]
    public Guid MaPhan { get; set; }

    [Required]
    public int MaSoCauHoi { get; set; }

    [Required(ErrorMessage = "Tiêu đề hoặc hướng dẫn ghép nối không được để trống.")]
    public string NoiDung { get; set; } = string.Empty;
    // Ví dụ: "Hãy ghép các khái niệm ở cột A với định nghĩa ở cột B"

    [Required]
    public short CapDo { get; set; }

    public bool HoanVi { get; set; } = true; // Có được xáo trộn thứ tự khi thi không

    public EnumCLO? CLO { get; set; }

    /// <summary>
    /// Danh sách các cặp Trái - Phải
    /// Mỗi cặp là một mối quan hệ đúng
    /// </summary>
    [Required(ErrorMessage = "Phải có ít nhất 1 cặp ghép nối.")]
    [MinLength(1, ErrorMessage = "Câu hỏi ghép nối phải có ít nhất 2 cặp.")]
    public List<GhepNoiPairDto> Pairs { get; set; } = new();
}

public class GhepNoiPairDto
{
    [Required]
    public CreateCauHoiSimpleDto Trai { get; set; } = new();

    [Required]
    public CreateCauHoiSimpleDto Phai { get; set; } = new();
}

/// <summary>
/// Dùng chung cho Trái/Phải trong ghép nối - chỉ cần nội dung
/// </summary>
public class CreateCauHoiSimpleDto
{
    [Required(ErrorMessage = "Nội dung không được để trống.")]
    public string NoiDung { get; set; } = string.Empty;
}
