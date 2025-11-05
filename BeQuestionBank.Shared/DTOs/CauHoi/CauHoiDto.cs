using BeQuestionBank.Shared.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeQuestionBank.Shared.DTOs.CauTraLoi;

namespace BeQuestionBank.Shared.DTOs.CauHoi;

public class CauHoiDto
{
    public Guid MaCauHoi { get; set; }
    public Guid MaPhan { get; set; }
    public int MaSoCauHoi { get; set; }
    public string? NoiDung { get; set; }
    public bool HoanVi { get; set; }
    public short CapDo { get; set; }
    public int SoCauHoiCon { get; set; }
    public float? DoPhanCach { get; set; }
    public Guid? MaCauHoiCha { get; set; }
    public bool? XoaTam { get; set; }
    public int? SoLanDuocThi { get; set; }
    public int? SoLanDung { get; set; }
    public DateTime? NgayTao { get; set; }
    public DateTime? NgaySua { get; set; }
    public EnumCLO? CLO { get; set; }
    public string LoaiCauHoi { get; set; }
    public List<CauHoiDto> CauHoiCons { get; set; } = new List<CauHoiDto>();
    public List<CauTraLoiDto> CauTraLois { get; set; } = new List<CauTraLoiDto>();
}
