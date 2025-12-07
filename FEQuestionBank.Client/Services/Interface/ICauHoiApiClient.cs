using BeQuestionBank.Shared.DTOs.CauHoi;
using BeQuestionBank.Shared.DTOs.CauHoi.Create;
using BeQuestionBank.Shared.DTOs.Common;
using BeQuestionBank.Shared.DTOs.Khoa;
using BeQuestionBank.Shared.DTOs.Pagination;

namespace FEQuestionBank.Client.Services.Interface
{
    public interface ICauHoiApiClient
    {
        Task<ApiResponse<IEnumerable<CauHoiDto>>> GetAllAsync();
        Task<ApiResponse<CauHoiWithCauTraLoiDto>> GetByIdAsync(Guid id);

        // Đổi bool -> object
        Task<ApiResponse<object>> CreateSingleQuestionAsync(CreateCauHoiWithCauTraLoiDto request);
        Task<ApiResponse<object>> CreateFillingQuestionAsync(CreateCauHoiDienTuDto request);

        // Task<ApiResponse<CauHoiDto>> CreateMultipeChoiceQuestionAsync(
        //     CreateCauHoiMultipleChoiceDto dto,
        //     Guid userId);
        Task<ApiResponse<object>> CreatePairingQuestionAsync(CreateCauHoiGhepNoiDto request);
        Task<ApiResponse<object>> CreateEssayQuestionAsync(CreateCauHoiTuLuanDto request);
        Task<ApiResponse<object>> CreateMultipeChoiceQuestionAsync(CreateCauHoiMultipleChoiceDto request);

        // Đổi bool -> object
        Task<ApiResponse<object>> CreateGroupQuestionAsync(CreateCauHoiNhomDto request); // Lưu ý DTO này

        // Đổi bool -> object (nếu Update controller cũng trả về object)
        Task<ApiResponse<object>> UpdateQuestionAsync(Guid id, UpdateCauHoiWithCauTraLoiDto request);

        Task<ApiResponse<bool>> DeleteQuestionAsync(Guid id);

        // Task<ApiResponse<PagedResult<CauHoiDto>>> GetEssaysPagedAsync
        //     (int page = 1, int pageSize = 10, string? sort = null, string? search = null);
        Task<ApiResponse<PagedResult<CauHoiDto>>> GetEssaysPagedAsync(
            int page = 1,
            int pageSize = 10,
            string? sort = null,
            string? search = null,
            Guid? khoaId = null,
            Guid? monHocId = null,
            Guid? phanId = null);

        Task<ApiResponse<PagedResult<CauHoiDto>>> GetGroupsPagedAsync(
            int page = 1,
            int pageSize = 10,
            string? sort = null,
            string? search = null,
            Guid? khoaId = null,
            Guid? monHocId = null,
            Guid? phanId = null);

        Task<ApiResponse<PagedResult<CauHoiDto>>> GetSinglesPagedAsync(
            int page = 1,
            int pageSize = 10,
            string? sort = null,
            string? search = null,
            Guid? khoaId = null,
            Guid? monHocId = null,
            Guid? phanId = null
        );

        Task<ApiResponse<PagedResult<CauHoiDto>>> GetFillingsPagedAsync
        (int page = 1,
            int pageSize = 10,
            string? sort = null,
            string? search = null,
            Guid? khoaId = null,
            Guid? monHocId = null,
            Guid? phanId = null
        );

        Task<ApiResponse<PagedResult<CauHoiDto>>> GetMultipeChoicesPagedAsync
        (int page = 1,
            int pageSize = 10,
            string? sort = null,
            string? search = null,
            Guid? khoaId = null,
            Guid? monHocId = null,
            Guid? phanId = null
        );

        Task<ApiResponse<PagedResult<CauHoiDto>>> GetPairingsPagedAsync
        (int page = 1,
            int pageSize = 10,
            string? sort = null,
            string? search = null,
            Guid? khoaId = null,
            Guid? monHocId = null,
            Guid? phanId = null
        );
    }
}
