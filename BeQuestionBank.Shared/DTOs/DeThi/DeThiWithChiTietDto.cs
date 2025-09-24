using System.Text.Json.Serialization;
using BEQuestionBank.Shared.DTOs.ChiTietDeThi;
using BEQuestionBank.Shared.DTOs.DeThi;

namespace BeQuestionBank.Shared.DTOs.DeThi;

public class DeThiWithChiTietDto: DeThiDto
{
    [JsonPropertyOrder(99)] 
    public List<ChiTietDeThiDto> ChiTietDeThis { get; set; } = new List<ChiTietDeThiDto>();
}