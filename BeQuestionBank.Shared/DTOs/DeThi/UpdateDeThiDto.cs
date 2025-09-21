using System;
using System.Collections.Generic;
using BEQuestionBank.Shared.DTOs.ChiTietDeThi;

namespace BEQuestionBank.Shared.DTOs.DeThi
{
    public class UpdateDeThiDto
    {
        public Guid MaMonHoc { get; set; }
        public string TenDeThi { get; set; }
        public bool? DaDuyet { get; set; }
        public int? SoCauHoi { get; set; }
    }
}