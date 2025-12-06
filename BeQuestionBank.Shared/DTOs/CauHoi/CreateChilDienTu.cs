using System.ComponentModel.DataAnnotations;

namespace BeQuestionBank.Shared.DTOs.CauHoi;

public class CreateChilDienTu : CreateCauHoiDto
{
    public List<CreateCauTraLoiDienTuDto> CauTraLois { get; set; } = new List<CreateCauTraLoiDienTuDto>();
}

public class CreateCauTraLoiDienTuDto
{
    [Required] public string NoiDung { get; set; } = string.Empty;
    // Ví dụ: "Hà Nội", "64"
}