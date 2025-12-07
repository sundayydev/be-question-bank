using System.ComponentModel.DataAnnotations;
using BeQuestionBank.Shared.DTOs.CauTraLoi;
using BeQuestionBank.Shared.Enums;

namespace BeQuestionBank.Shared.DTOs.CauHoi;

public class CreateCauHoiDienTuDto
{
    public Guid MaPhan { get; set; }
    
    public int MaSoCauHoi { get; set; }
    
    public string NoiDung { get; set; } = string.Empty;
    // Ví dụ: "Thủ đô của Việt Nam là _____. Nước ta có ____ tỉnh thành."
    
    public short CapDo { get; set; }

    public EnumCLO? CLO { get; set; }

    /// <summary>
    /// Danh sách đáp án theo đúng thứ tự điền
    /// Tất cả đều là đáp án đúng (LaDapAn = true)
    /// Thứ tự cực kỳ quan trọng → dùng để chấm điểm theo vị trí
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "Câu hỏi nhóm phải có ít nhất 1 câu hỏi con.")]
    public List<CreateChilDienTu> CauHoiCons { get; set; } = new();
}

// public class CreateCauTraLoiDienTuDto
// {
//     [Required] public string NoiDung { get; set; } = string.Empty;
//     // Ví dụ: "Hà Nội", "64"
// }