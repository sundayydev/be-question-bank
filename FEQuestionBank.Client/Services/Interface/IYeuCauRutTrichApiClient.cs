using BeQuestionBank.Shared.DTOs.Common;
using BeQuestionBank.Shared.DTOs.Pagination;
using BeQuestionBank.Shared.DTOs.YeuCauRutTrich;

namespace FEQuestionBank.Client.Services;

public interface IYeuCauRutTrichApiClient
{
    /// <summary>
    /// Lấy tất cả yêu cầu rút trích (dùng cho thống kê)
    /// </summary>
    Task<ApiResponse<List<YeuCauRutTrichDto>>> GetAllAsync();

    /// <summary>
    /// Lấy yêu cầu theo ID
    /// </summary>
    Task<ApiResponse<YeuCauRutTrichDto>> GetByIdAsync(Guid id);

    /// <summary>
    /// Lấy danh sách yêu cầu theo người dùng
    /// </summary>
    Task<ApiResponse<List<YeuCauRutTrichDto>>> GetByMaNguoiDungAsync(Guid maNguoiDung);

    /// <summary>
    /// Lấy danh sách yêu cầu theo môn học
    /// </summary>
    Task<ApiResponse<List<YeuCauRutTrichDto>>> GetByMaMonHocAsync(Guid maMonHoc);

    /// <summary>
    /// Lấy danh sách yêu cầu theo trạng thái xử lý
    /// </summary>
    Task<ApiResponse<List<YeuCauRutTrichDto>>> GetByTrangThaiAsync(bool daXuLy);

    /// <summary>
    /// Lấy danh sách phân trang + tìm kiếm + sắp xếp + lọc trạng thái
    /// </summary>
    Task<ApiResponse<PagedResult<YeuCauRutTrichDto>>> GetPagedAsync(
        int page = 1,
        int limit = 10,
        string sort = "NgayYeuCau,desc",
        string? search = null,
        bool? daXuLy = null);

    /// <summary>
    /// Tạo yêu cầu rút trích mới
    /// </summary>
    Task<ApiResponse<object>> CreateAsync(CreateYeuCauRutTrichDto dto);

    /// <summary>
    /// Tạo yêu cầu + rút trích đề thi luôn
    /// </summary>
    Task<ApiResponse<object>> CreateAndRutTrichDeThiAsync(CreateYeuCauRutTrichDto dto);

    /// <summary>
    /// Cập nhật yêu cầu (chỉ dùng cho admin)
    /// </summary>
    Task<ApiResponse<object>> UpdateAsync(Guid id, YeuCauRutTrichDto dto);

    /// <summary>
    /// Xóa yêu cầu
    /// </summary>
    Task<ApiResponse<object>> DeleteAsync(Guid id);

    /// <summary>
    /// Upload file Excel để đọc ma trận
    /// </summary>
    //Task<ApiResponse<object>> UploadMaTranExcelAsync(IFormFile file, Guid maMonHoc);
}