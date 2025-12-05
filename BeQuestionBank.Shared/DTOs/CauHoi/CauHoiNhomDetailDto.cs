using BeQuestionBank.Shared.Enums;
using BeQuestionBank.Shared.DTOs.CauTraLoi;
using System;
using System.Collections.Generic;

namespace BeQuestionBank.Shared.DTOs.CauHoi
{
    /// <summary>
    /// DTO chi tiết câu hỏi nhóm (Group Question) bao gồm nội dung đoạn văn cha và danh sách câu hỏi con
    /// </summary>
    public class CauHoiNhomDetailDto
    {
        // Thông tin câu hỏi cha (đoạn văn / ngữ cảnh)
        public Guid MaCauHoi { get; set; }
        public Guid MaPhan { get; set; }
        public string? TenPhan { get; set; }
        public int MaSoCauHoi { get; set; }
        public string? NoiDung { get; set; } // Nội dung đoạn văn
        public bool HoanVi { get; set; }
        public short CapDo { get; set; }
        public int SoCauHoiCon { get; set; }
        public DateTime? NgayTao { get; set; }
        public DateTime? NgayCapNhat { get; set; }
        public bool XoaTam { get; set; }
        public EnumCLO? CLO { get; set; }
        public string? LoaiCauHoi { get; set; }

        // Danh sách câu hỏi con với câu trả lời
        public List<CauHoiWithCauTraLoiDto> CauHoiCons { get; set; } = new List<CauHoiWithCauTraLoiDto>();
    }
}
