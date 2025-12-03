using BeQuestionBank.Shared.DTOs.CauHoi;
using BeQuestionBank.Shared.DTOs.Common;

namespace FEQuestionBank.Client.Services.Interface
{
    public interface ICauHoiApiClient
    {
        Task<ApiResponse<IEnumerable<CauHoiDto>>> GetAllAsync();
        Task<ApiResponse<CauHoiWithCauTraLoiDto>> GetByIdAsync(Guid id);

        // Đổi bool -> object
        Task<ApiResponse<object>> CreateSingleQuestionAsync(CreateCauHoiWithCauTraLoiDto request);

        // Đổi bool -> object
        Task<ApiResponse<object>> CreateGroupQuestionAsync(CreateCauHoiNhomDto request); // Lưu ý DTO này

        // Đổi bool -> object (nếu Update controller cũng trả về object)
        Task<ApiResponse<object>> UpdateQuestionAsync(Guid id, UpdateCauHoiWithCauTraLoiDto request);

        Task<ApiResponse<bool>> DeleteQuestionAsync(Guid id); // Delete thường trả về bool hoặc null, kiểm tra lại Controller
    }
}
