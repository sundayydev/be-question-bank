using System.ComponentModel.DataAnnotations;
using BeQuestionBank.Shared.DTOs.CauTraLoi;
using BeQuestionBank.Shared.Enums;

namespace BeQuestionBank.Shared.DTOs.CauHoi;

public class CreateCauHoiDienTuDto
{
    [Required(ErrorMessage = "Mã phần không được để trống.")]
    public Guid MaPhan { get; set; }

    [Required(ErrorMessage = "Mã số câu hỏi không được để trống.")]
    public int MaSoCauHoi { get; set; }

    [Required(ErrorMessage = "Nội dung câu hỏi không được để trống.")]
    public string NoiDung { get; set; } = string.Empty;
    // Ví dụ: "Thủ đô của Việt Nam là _____. Nước ta có ____ tỉnh thành."

    [Required(ErrorMessage = "Cấp độ không được để trống.")]
    public short CapDo { get; set; }

    public EnumCLO? CLO { get; set; }

    /// <summary>
    /// Danh sách đáp án theo đúng thứ tự điền
    /// Tất cả đều là đáp án đúng (LaDapAn = true)
    /// Thứ tự cực kỳ quan trọng → dùng để chấm điểm theo vị trí
    /// </summary>
    [Required(ErrorMessage = "Câu hỏi điền từ phải có ít nhất 1 đáp án.")]
    [MinLength(1, ErrorMessage = "Phải có ít nhất 1 ô điền.")]
    public List<CreateCauTraLoiDienTuDto> CauTraLois { get; set; } = new();
}

public class CreateCauTraLoiDienTuDto
{
    [Required] public string NoiDung { get; set; } = string.Empty;
    // Ví dụ: "Hà Nội", "64"
}