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
    public string NoiDung { get; set; } // HTML (Text + Ảnh Base64)
    public short CapDo { get; set; } = 1;
    public string LoaiCauHoi { get; set; } = "TracNghiem"; // Mặc định
    public string? CLO { get; set; } // Ví dụ: CLO1, CLO2 lấy từ text
    public List<ImportCauTraLoiDto> CauTraLois { get; set; } = new List<ImportCauTraLoiDto>();
}

public class ImportCauTraLoiDto
{
    public string NoiDung { get; set; }
    public bool LaDapAn { get; set; }
    public int ThuTu { get; set; }
    public bool HoanVi { get; set; } = true; // Mặc định là True (Có hoán vị)
}

public class ImportRequestDto
{
    [Required(ErrorMessage = "Vui lòng chọn file.")]
    public IFormFile File { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn Phần (Chương) để lưu câu hỏi.")]
    public Guid MaPhan { get; set; }
}