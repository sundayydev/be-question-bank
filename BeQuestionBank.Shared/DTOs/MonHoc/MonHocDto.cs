using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeQuestionBank.Shared.DTOs.MonHoc;

public class MonHocDto
{
    public Guid MaMonHoc { get; set; }
    public required string MaSoMonHoc { get; set; }
    public string TenMonHoc { get; set; } = string.Empty;
    public int? SoTinChi { get; set; }
    public Guid MaKhoa { get; set; }
    public string? TenKhoa { get; set; } 
    public bool? XoaTam { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
    public DateTime NgayCapNhat { get; set; } = DateTime.UtcNow;
}

