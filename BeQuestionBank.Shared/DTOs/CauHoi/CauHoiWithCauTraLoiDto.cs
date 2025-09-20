using BeQuestionBank.Shared.DTOs.CauTraLoi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeQuestionBank.Shared.DTOs.CauHoi;

public class CauHoiWithCauTraLoiDto : CauHoiDto
{
    public List<CauTraLoiDto> CauTraLois { get; set; } = new List<CauTraLoiDto>();
}
