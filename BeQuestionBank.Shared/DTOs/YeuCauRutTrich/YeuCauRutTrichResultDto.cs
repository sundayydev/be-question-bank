namespace BeQuestionBank.Shared.DTOs.YeuCauRutTrich
{
    public class YeuCauRutTrichResultDto
    {
        // Thông tin yêu cầu rút trích
        public Guid MaYeuCau { get; set; }
        public Guid MaNguoiDung { get; set; }
        public Guid MaMonHoc { get; set; }
        public string? NoiDungRutTrich { get; set; }
        public string? GhiChu { get; set; }
        public DateTime? NgayYeuCau { get; set; }
        public DateTime? NgayXuLy { get; set; }
        public bool? DaXuLy { get; set; }

        // Thông tin đề thi vừa rút trích
        public Guid MaDeThi { get; set; }
        public string TenDeThi { get; set; } = string.Empty;
    }
}