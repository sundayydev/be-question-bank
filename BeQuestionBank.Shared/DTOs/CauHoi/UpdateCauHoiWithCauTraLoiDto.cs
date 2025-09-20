using BeQuestionBank.Shared.DTOs.CauTraLoi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeQuestionBank.Shared.DTOs.CauHoi
{
    public class UpdateCauHoiWithCauTraLoiDto : UpdateCauHoiDto
    {
        public List<UpdateCauTraLoiDto> CauTraLois { get; set; } = new List<UpdateCauTraLoiDto>();
    }
}
