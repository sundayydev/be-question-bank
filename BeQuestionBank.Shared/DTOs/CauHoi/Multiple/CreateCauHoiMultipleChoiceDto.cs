using BeQuestionBank.Shared.DTOs.CauTraLoi;
using BeQuestionBank.Shared.Enums;

namespace BeQuestionBank.Shared.DTOs.CauHoi;

public class CreateCauHoiMultipleChoiceDto : CreateCauHoiDto
{
    // public Guid MaPhan { get; set; }
    // public int MaSoCauHoi { get; set; }
    // public string NoiDung { get; set; }
    // public Guid? UserId { get; set; }
    // public bool HoanVi { get; set; }
    // public short CapDo { get; set; }
    // public EnumCLO? CLO { get; set; }

    public List<CreateCauTraLoiDto> CauTraLois { get; set; } = new List<CreateCauTraLoiDto>();
}