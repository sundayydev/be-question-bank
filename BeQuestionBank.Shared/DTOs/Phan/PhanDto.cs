using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeQuestionBank.Shared.DTOs.Phan;

public class PhanDto
{
    public Guid MaPhan { get; set; }
    public Guid MaMonHoc { get; set; }
    public string TenPhan { get; set; } = string.Empty;
    public string? NoiDung { get; set; }
    public int ThuTu { get; set; }
    public int SoLuongCauHoi { get; set; }
    public Guid? MaPhanCha { get; set; }
    public int? MaSoPhan { get; set; }
    public bool? XoaTam { get; set; }
    public bool LaCauHoiNhom { get; set; }
    public string? TenMonHoc { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
    public DateTime NgayCapNhat { get; set; } = DateTime.UtcNow;

    public List<PhanDto> PhanCons { get; set; } = new List<PhanDto>();
}
