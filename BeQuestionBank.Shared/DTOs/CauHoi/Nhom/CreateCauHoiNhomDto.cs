using BeQuestionBank.Domain.Enums;
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
        [MinLength(1, ErrorMessage = "Câu h?i nhóm ph?i có ít nh?t 1 câu h?i con.")]
        public List<CreateCauHoiWithCauTraLoiDto> CauHoiCons { get; set; } = new();
    }
}
