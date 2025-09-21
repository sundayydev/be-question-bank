using System;
using System.Collections.Generic;
using BeQuestionBank.Shared.DTOs.CauHoi;

namespace BEQuestionBank.Shared.DTOs.ChiTietDeThi
{
    public class ChiTietDeThiDto
    {
        public Guid? MaDeThi { get; set; }
        public Guid MaPhan { get; set; }
        public Guid MaCauHoi { get; set; }
        public int? ThuTu { get; set; }

        public CauHoiDto CauHoi { get; set; }
    }
}