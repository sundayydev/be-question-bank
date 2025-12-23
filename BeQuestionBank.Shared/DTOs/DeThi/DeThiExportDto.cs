using BeQuestionBank.Shared.DTOs.CauHoi;
using BeQuestionBank.Shared.DTOs.CauTraLoi;

namespace BeQuestionBank.Shared.DTOs.DeThi
{
    /// <summary>
    /// DTO for exporting exam with full question and answer details to .ezp file
    /// </summary>
    public class DeThiExportDto
    {
        public string ExportVersion { get; set; } = "1.0";
        public DateTime ExportDate { get; set; }
        public DeThiInfoDto DeThiInfo { get; set; } = new();
        public List<CauHoiExportDto> CauHois { get; set; } = new();
    }

    public class DeThiInfoDto
    {
        public Guid MaDeThi { get; set; }
        public string TenDeThi { get; set; } = string.Empty;
        public string? TenMonHoc { get; set; }
        public string? TenKhoa { get; set; }
        public int? SoCauHoi { get; set; }
        public DateTime? NgayTao { get; set; }
        public bool DaDuyet { get; set; }
    }

    public class CauHoiExportDto
    {
        public Guid MaCauHoi { get; set; }
        public Guid MaPhan { get; set; }
        public string? TenPhan { get; set; }
        public int MaSoCauHoi { get; set; }
        public string? NoiDung { get; set; }
        public int? ThuTu { get; set; }
        public bool HoanVi { get; set; }
        public short CapDo { get; set; }
        public string? CLO { get; set; }
        public string? LoaiCauHoi { get; set; }
        public int SoCauHoiCon { get; set; }
        public Guid? MaCauHoiCha { get; set; }
        
        // Đáp án của câu hỏi này
        public List<CauTraLoiExportDto> CauTraLois { get; set; } = new();
        
        // Câu hỏi con (nếu có)
        public List<CauHoiExportDto> CauHoiCons { get; set; } = new();
    }

    public class CauTraLoiExportDto
    {
        public Guid MaCauTraLoi { get; set; }
        public Guid MaCauHoi { get; set; }
        public string? NoiDung { get; set; }
        public int ThuTu { get; set; }
        public bool LaDapAn { get; set; }
        public bool? HoanVi { get; set; }
    }
}
