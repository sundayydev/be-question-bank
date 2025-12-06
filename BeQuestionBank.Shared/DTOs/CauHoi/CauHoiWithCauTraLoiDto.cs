using BeQuestionBank.Shared.DTOs.CauTraLoi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BeQuestionBank.Shared.DTOs.CauHoi;

public class CauHoiWithCauTraLoiDto : CauHoiDto
{
    [JsonPropertyOrder(99)] 
    public List<CauTraLoiDto> CauTraLois { get; set; } = new List<CauTraLoiDto>();

    // public List<GhepNoiDto>? GhepNoiPairs { get; set; }

}
