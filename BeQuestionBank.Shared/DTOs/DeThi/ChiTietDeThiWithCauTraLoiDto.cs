using BeQuestionBank.Shared.DTOs.CauHoi;

namespace BeQuestionBank.Shared.DTOs.DeThi;

public class ChiTietDeThiWithCauTraLoiDto
{
    public Guid? MaDeThi { get; set; }
    public Guid MaPhan { get; set; }
    public Guid MaCauHoi { get; set; }
    public int? ThuTu { get; set; }

    public CauHoiWithCauTraLoiDto CauHoi { get; set; }
}