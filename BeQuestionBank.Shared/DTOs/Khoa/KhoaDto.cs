using BeQuestionBank.Shared.DTOs.MonHoc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeQuestionBank.Shared.DTOs.Khoa;

public class KhoaDto
{
    public Guid MaKhoa { get; set; }
    public string TenKhoa { get; set; }
    public string? MoTa { get; set; }
    public bool? XoaTam { get; set; }
    public List<MonHocDto>? DanhSachMonHoc { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
    public DateTime NgayCapNhat { get; set; } = DateTime.UtcNow;
}

