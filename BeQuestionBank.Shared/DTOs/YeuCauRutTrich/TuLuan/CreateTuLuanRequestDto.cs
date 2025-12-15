namespace BeQuestionBank.Shared.DTOs.YeuCauRutTrich;

public class CreateTuLuanRequestDto
{
    public Guid MaNguoiDung { get; set; }
    public Guid MaMonHoc { get; set; }
    public string? NoiDungRutTrich { get; set; }
    public string? GhiChu { get; set; }
    public MaTranTuLuan MaTranTuLuan { get; set; } = null!;
}