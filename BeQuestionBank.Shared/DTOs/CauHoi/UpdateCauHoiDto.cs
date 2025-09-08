using BeQuestionBank.Shared.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeQuestionBank.Shared.DTOs.CauHoi;

public class UpdateCauHoiDto
{
    [Required(ErrorMessage = "Mã phần không được để trống.")]
    public Guid MaPhan { get; set; }

    [Required(ErrorMessage = "Mã số câu hỏi không được để trống.")]
    public int MaSoCauHoi { get; set; }

    public required string NoiDung { get; set; }

    [Required(ErrorMessage = "Hoán vị không được để trống.")]
    public bool HoanVi { get; set; }

    [Required(ErrorMessage = "Cấp độ không được để trống.")]
    public short CapDo { get; set; }

    public int SoCauHoiCon { get; set; }

    public float? DoPhanCach { get; set; }

    public Guid? MaCauHoiCha { get; set; }

    public bool? XoaTam { get; set; }

    public int? SoLanDuocThi { get; set; }

    public int? SoLanDung { get; set; }

    public EnumCLO? CLO { get; set; }
}
