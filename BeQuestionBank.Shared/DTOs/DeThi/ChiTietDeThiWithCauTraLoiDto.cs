using System.Text.Json.Serialization;
using BeQuestionBank.Shared.DTOs.CauHoi;
using BEQuestionBank.Shared.DTOs.ChiTietDeThi;

namespace BeQuestionBank.Shared.DTOs.DeThi;

public class ChiTietDeThiWithCauTraLoiDto : ChiTietDeThiDto
{
    [JsonPropertyOrder(99)] 
    public CauHoiWithCauTraLoiDto CauHoi { get; set; }
}
