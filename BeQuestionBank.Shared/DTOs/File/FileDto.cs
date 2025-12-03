using BeQuestionBank.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeQuestionBank.Shared.DTOs.File
{
    public class FileDto
    {
        public Guid MaFile { get; set; }
        public string TenFile { get; set; } = string.Empty;
        public FileType LoaiFile { get; set; }
        public string? Url { get; set; } // Đường dẫn để phát audio
        public Guid? MaCauHoi { get; set; }
        public Guid? MaCauTraLoi { get; set; }
        public bool XoaTam { get; set; }
    }
}
