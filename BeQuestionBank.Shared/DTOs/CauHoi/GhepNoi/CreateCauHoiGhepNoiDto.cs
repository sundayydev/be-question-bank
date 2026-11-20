using System.ComponentModel.DataAnnotations;
using BeQuestionBank.Shared.DTOs.CauTraLoi;
using BeQuestionBank.Domain.Enums;

namespace BeQuestionBank.Shared.DTOs.CauHoi;

public class CreateCauHoiGhepNoiDto:CreateCauHoiDto
{
    //[Required] public Guid MaPhan { get; set; }
    //[Required] public int MaSoCauHoi { get; set; }
    //[Required] public string NoiDung { get; set; } 
    //[Required] public short CapDo { get; set; }
    //public bool HoanVi { get; set; } = true;
    //public EnumCLO? CLO { get; set; }
    //[Required]
    [MinLength(1, ErrorMessage = "Câu h?i ghepnoi ph?i có ít nh?t 1 câu h?i con.")]
    public List<CreateCauHoiWithCauTraLoiDto> CauHoiCons { get; set; } = new();
    
    
}
