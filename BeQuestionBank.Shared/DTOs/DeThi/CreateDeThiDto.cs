using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using BEQuestionBank.Shared.DTOs.ChiTietDeThi;
using BeQuestionBank.Shared.DTOs.DeThi;

namespace BEQuestionBank.Shared.DTOs.DeThi
{
    public class CreateDeThiDto{
        public Guid MaMonHoc { get; set; }
        public string TenDeThi { get; set; }
        public bool? DaDuyet { get; set; }
        public int? SoCauHoi { get; set; }
        [JsonPropertyOrder(99)] 
        public List<ChiTietDeThiWithCauHoiDto> ChiTietDeThis { get; set; }
    }
}