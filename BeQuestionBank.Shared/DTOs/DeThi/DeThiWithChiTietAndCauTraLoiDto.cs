using System.Text.Json.Serialization;
using BEQuestionBank.Shared.DTOs.ChiTietDeThi;
using BeQuestionBank.Shared.DTOs.DeThi;

namespace BEQuestionBank.Shared.DTOs.DeThi
{
    public class DeThiWithChiTietAndCauTraLoiDto : DeThiDto
    {
        [JsonPropertyOrder(99)] 
        public List<ChiTietDeThiWithCauTraLoiDto> ChiTietDeThis { get; set; } = new();
    }
}