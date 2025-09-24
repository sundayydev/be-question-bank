using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using BeQuestionBank.Shared.DTOs.CauHoi;

namespace BEQuestionBank.Shared.DTOs.ChiTietDeThi
{
    public class ChiTietDeThiDto
    {
        public Guid? MaDeThi { get; set; }
        public Guid MaPhan { get; set; }
        public Guid MaCauHoi { get; set; }
        public int? ThuTu { get; set; }
        [JsonPropertyOrder(99)] 
        public CauHoiDto CauHoi { get; set; }
    }
}