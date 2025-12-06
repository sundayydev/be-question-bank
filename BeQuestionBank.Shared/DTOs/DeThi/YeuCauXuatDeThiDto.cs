namespace BeQuestionBank.Shared.DTOs.DeThi;

public class YeuCauXuatDeThiDto
{
    public Guid MaDeThi { get; set; }
    public string? Lop { get; set; }
    public int? ThoiLuong { get; set; } = 90;
    public DateTime? NgayThi { get; set; }
    public string? HocKy { get; set; }
    public string? NamHoc { get; set; }
    public bool HoanViDapAn { get; set; } = true;
    public bool IncludeDapAn { get; set; } = false;
    public string? Format { get; set; } = "word"; // "word" hoặc "pdf"
}