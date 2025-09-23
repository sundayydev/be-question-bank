namespace BeQuestionBank.Shared.DTOs.DeThi;

public class ChiTietDeThiWithCauHoiDto
{
    public Guid? MaDeThi { get; set; }
    public Guid MaPhan { get; set; }
    public Guid MaCauHoi { get; set; }
    public int? ThuTu { get; set; }
}