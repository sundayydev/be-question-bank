using BEQuestionBank.Shared.DTOs.ChiTietDeThi;
using BEQuestionBank.Shared.DTOs.DeThi;

namespace BeQuestionBank.Shared.DTOs.DeThi;

public class DeThiWithChiTietDto: DeThiDto
{
    public List<ChiTietDeThiDto> ChiTietDeThis { get; set; } = new List<ChiTietDeThiDto>();
}