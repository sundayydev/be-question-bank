using BeQuestionBank.Shared.DTOs.File;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeQuestionBank.Shared.DTOs.CauTraLoi
{
    public class CauTraLoiWithFile : CauTraLoiDto
    {
        public List<FileDto> Files { get; set; } = new List<FileDto>();
    }
}
