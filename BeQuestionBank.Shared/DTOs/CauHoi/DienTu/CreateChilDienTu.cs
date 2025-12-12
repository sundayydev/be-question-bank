using System.ComponentModel.DataAnnotations;

namespace BeQuestionBank.Shared.DTOs.CauHoi;

public class CreateChilDienTu : CreateCauHoiDto
{
    public Guid? MaCauHoi { get; set; }
    public List<CreateCauTraLoiDienTuDto> CauTraLois { get; set; } = new();
}

public class CreateCauTraLoiDienTuDto
{
    [Required] public string NoiDung { get; set; } = string.Empty;

    public bool? HoanVi { get; set; }
    // Ví dụ: "Hà Nội", "64"
}