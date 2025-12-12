using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeQuestionBank.Shared.DTOs.CauHoi.TuLuan
{
    public class UpdateCauHoiTuLuanDto :UpdateCauHoiDto
    {
        public List<UpdateCauHoiDto> CauHoiCons { get; set; } = new();
    }
}
