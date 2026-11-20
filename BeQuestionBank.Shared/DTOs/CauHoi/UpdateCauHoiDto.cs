using BeQuestionBank.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeQuestionBank.Shared.DTOs.CauHoi;

public class UpdateCauHoiDto
{
    [Required(ErrorMessage = "Mã ph?n không du?c d? tr?ng.")]
    public Guid MaPhan { get; set; }

    [Required(ErrorMessage = "Mã s? câu h?i không du?c d? tr?ng.")]
    public int MaSoCauHoi { get; set; }

    public Guid? MaCauHoi { get; set; }
    public required string NoiDung { get; set; }

    [Required(ErrorMessage = "Hoán v? không du?c d? tr?ng.")]
    public bool HoanVi { get; set; }

    [Required(ErrorMessage = "C?p d? không du?c d? tr?ng.")]
    public short CapDo { get; set; }

    public int SoCauHoiCon { get; set; }

    public float? DoPhanCach { get; set; }

    public Guid? MaCauHoiCha { get; set; }

    public bool? XoaTam { get; set; }

    public int? SoLanDuocThi { get; set; }

    public int? SoLanDung { get; set; }
    public int ThuTu { get; set; }

    public EnumCLO? CLO { get; set; }
    
}
