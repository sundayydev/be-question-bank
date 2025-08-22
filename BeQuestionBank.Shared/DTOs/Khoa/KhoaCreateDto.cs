using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeQuestionBank.Shared.DTOs.Khoa;

public class KhoaCreateDto
{
    public required string TenKhoa { get; set; }
    public bool? XoaTam { get; set; }
}
