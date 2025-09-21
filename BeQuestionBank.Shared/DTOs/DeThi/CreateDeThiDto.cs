using System;
using System.Collections.Generic;
using BEQuestionBank.Shared.DTOs.ChiTietDeThi;

namespace BEQuestionBank.Shared.DTOs.DeThi
{
    public class CreateDeThiDto : DeThiDto
    {
        public List<ChiTietDeThiDto> ChiTietDeThis { get; set; }
    }
}