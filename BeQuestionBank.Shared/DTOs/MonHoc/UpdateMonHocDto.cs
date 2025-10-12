using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeQuestionBank.Shared.DTOs.MonHoc;

public class UpdateMonHocDto
{
    public string MaSoMonHoc { get; set; }
    public string TenMonHoc { get; set; } = string.Empty;
    public Guid MaKhoa { get; set; }
    public bool? XoaTam { get; set; }
}