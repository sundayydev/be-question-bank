using BeQuestionBank.Shared.DTOs.CauTraLoi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeQuestionBank.Shared.DTOs.CauHoi
{
    public class CreateCauHoiWithCauTraLoiDto : CreateCauHoiDto
    {
        public Guid? MaCauHoi { get; set; }
        public List<CreateCauTraLoiDto> CauTraLois { get; set; } = new();
    }
}
