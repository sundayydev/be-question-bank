using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeQuestionBank.Shared.DTOs.CauTraLoi
{
    public class CauTraLoiDto
    {
        public Guid MaCauTraLoi { get; set; }
        public Guid MaCauHoi { get; set; }
        public string? NoiDung { get; set; }
        public int ThuTu { get; set; }
        public bool LaDapAn { get; set; }
        public bool? HoanVi { get; set; }
    }
}
