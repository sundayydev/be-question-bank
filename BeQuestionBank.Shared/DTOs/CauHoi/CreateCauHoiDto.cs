using BeQuestionBank.Shared.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeQuestionBank.Shared.DTOs.CauHoi;

public class CreateCauHoiDto
{
    [Required(ErrorMessage = "Mã phần không được để trống.")]
    public Guid MaPhan { get; set; }

    public int MaSoCauHoi { get; set; }

    public required string NoiDung { get; set; }

    [Required(ErrorMessage = "Hoán vị không được để trống.")]
    public bool HoanVi { get; set; }

    [Required(ErrorMessage = "Cấp độ không được để trống.")]
    public short CapDo { get; set; } = 1;
    public int SoCauHoiCon { get; set; } = 0;

    public Guid? MaCauHoiCha { get; set; }

    public EnumCLO? CLO { get; set; }
}
