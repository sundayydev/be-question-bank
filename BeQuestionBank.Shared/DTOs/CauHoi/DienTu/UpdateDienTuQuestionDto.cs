using BeQuestionBank.Domain.Enums;

namespace BeQuestionBank.Shared.DTOs.CauHoi;

public class UpdateDienTuQuestionDto : CauHoiDto
{
    public List<CreateCauTraLoiDienTuDto> CauTraLois { get; set; } = new();
}
