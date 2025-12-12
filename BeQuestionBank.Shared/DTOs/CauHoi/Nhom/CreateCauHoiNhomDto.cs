using BeQuestionBank.Shared.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeQuestionBank.Shared.DTOs.CauHoi
{
    public class CreateCauHoiNhomDto : CreateCauHoiDto
    {   
        [Required]
        [MinLength(1, ErrorMessage = "Câu hỏi nhóm phải có ít nhất 1 câu hỏi con.")]
        public List<CreateCauHoiWithCauTraLoiDto> CauHoiCons { get; set; } = new();
    }
}
