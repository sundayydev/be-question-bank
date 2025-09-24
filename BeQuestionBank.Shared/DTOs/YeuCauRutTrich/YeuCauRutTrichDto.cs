namespace BeQuestionBank.Shared.DTOs.YeuCauRutTrich;

public class YeuCauRutTrichDto
{
    public Guid MaYeuCau { get; set; }
    public Guid MaNguoiDung { get; set; }
    public Guid MaMonHoc { get; set; }
    public string? NoiDungRutTrich { get; set; }
    public string? GhiChu { get; set; }
    public DateTime? NgayYeuCau { get; set; }
    public DateTime? NgayXuLy { get; set; }
    public bool? DaXuLy { get; set; }
    public string? TenNguoiDung { get; set; }
    public string? TenMonHoc { get; set; }
    public string? TenKhoa { get; set; }
    public string? MaTran { get; set; }
}