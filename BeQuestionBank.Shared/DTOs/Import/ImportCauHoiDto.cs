using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeQuestionBank.Shared.DTOs.Import;

public class ImportCauHoiDto
{
    public string NoiDung { get; set; } // HTML (Text + ?nh Base64)
    public short CapDo { get; set; } = 1;
    public string LoaiCauHoi { get; set; } = "TracNghiem"; // M?c d?nh
    public string? CLO { get; set; } // Ví d?: CLO1, CLO2 l?y t? text
    public List<ImportCauTraLoiDto> CauTraLois { get; set; } = new List<ImportCauTraLoiDto>();
}

public class ImportCauTraLoiDto
{
    public string NoiDung { get; set; }
    public bool LaDapAn { get; set; }
    public int ThuTu { get; set; }
    public bool HoanVi { get; set; } = true; // M?c d?nh là True (Có hoán v?)
}

public class ImportRequestDto
{
    [Required(ErrorMessage = "Vui lòng ch?n file.")]
    public IFormFile File { get; set; }

    [Required(ErrorMessage = "Vui lòng ch?n Ph?n (Chuong) d? luu câu h?i.")]
    public Guid MaPhan { get; set; }
}
