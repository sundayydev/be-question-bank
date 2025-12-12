using BeQuestionBank.Shared.Enums;
using BeQuestionBank.Shared.DTOs.CauTraLoi;
using System;
using System.Collections.Generic;

namespace BeQuestionBank.Shared.DTOs.CauHoi
{
    /// <summary>
    /// DTO chi tiết câu hỏi nhóm (Group Question) bao gồm nội dung đoạn văn cha và danh sách câu hỏi con
    /// </summary>
    public class CauHoiNhomDetailDto : CauHoiDto
    {
        public List<CauHoiWithCauTraLoiDto> CauHoiCons { get; set; } = new List<CauHoiWithCauTraLoiDto>();
    }
}
