using BeQuestionBank.Domain.Common;
using BeQuestionBank.Shared.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeQuestionBank.Domain.Models;

[Table("CauHoi")]
public class CauHoi : ModelBase
{
    [Key]
    public Guid MaCauHoi { get; set; } = Guid.NewGuid();

    [ForeignKey("Phan")]
    public Guid MaPhan { get; set; }
    public int MaSoCauHoi { get; set; }
    public string? NoiDung { get; set; } = string.Empty;
    public bool HoanVi { get; set; } = false;
    public short CapDo { get; set; } = 0;
    public int SoCauHoiCon { get; set; } = 0;
    public Guid? MaCauHoiCha { get; set; } = Guid.Empty;
    public bool? TrangThai { get; set; } = true;
    public int? SoLanDuocThi { get; set; } = 0;
    public int? SoLanDung { get; set; } = 0;
    [Column("DoPhanCachCauHoi")]
    public float? DoPhanCach { get; set; }
    public bool? XoaTam { get; set; } = false;
    public EnumCLO? CLO { get; set; }
    [ForeignKey("NguoiDung")]
    public Guid? NguoiTao { get; set; }
    public string LoaiCauHoi { get; set; }

    // Navigation property cho phần
    [ForeignKey("MaPhan")]
    public Phan? Phan { get; set; }

    // Navigation property cho câu hỏi cha (optional)
    [ForeignKey("MaCauHoiCha")]
    public CauHoi? CauHoiCha { get; set; }

    // Navigation property cho các câu hỏi con
    public ICollection<CauHoi> CauHoiCons { get; set; } = new List<CauHoi>();
    public ICollection<CauTraLoi> CauTraLois { get; set; }  = new List<CauTraLoi>();
}

