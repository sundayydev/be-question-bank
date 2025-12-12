using BeQuestionBank.Shared.DTOs.CauHoi;
using BeQuestionBank.Shared.DTOs.CauHoi.Create;
using BeQuestionBank.Shared.DTOs.CauHoi.TuLuan;
using BeQuestionBank.Shared.DTOs.Common;
using BeQuestionBank.Shared.DTOs.Khoa;
using BeQuestionBank.Shared.DTOs.Pagination;

namespace FEQuestionBank.Client.Services.Interface
{
    public interface ICauHoiApiClient
    {
        Task<ApiResponse<IEnumerable<CauHoiDto>>> GetAllAsync();
        Task<ApiResponse<CauHoiWithCauTraLoiDto>> GetByIdAsync(Guid id);
        Task<ApiResponse<object>> CreateSingleQuestionAsync(CreateCauHoiWithCauTraLoiDto request);
        Task<ApiResponse<object>> CreateFillingQuestionAsync(CreateCauHoiDienTuDto request);
        Task<ApiResponse<object>> CreatePairingQuestionAsync(CreateCauHoiGhepNoiDto request);
        Task<ApiResponse<object>> CreateEssayQuestionAsync(CreateCauHoiTuLuanDto request);
        Task<ApiResponse<object>> CreateMultipeChoiceQuestionAsync(CreateCauHoiMultipleChoiceDto request);

        Task<ApiResponse<object>> CreateGroupQuestionAsync(CreateCauHoiNhomDto request);
        Task<ApiResponse<object>> UpdateMultipleChoiceQuestionAsync(Guid id, UpdateCauHoiWithCauTraLoiDto request);
        Task<ApiResponse<object>> UpdateGroupQuestionAsync(Guid id, UpdateCauHoiNhomDto request);
        Task<ApiResponse<object>> UpdateQuestionAsync(Guid id, UpdateCauHoiWithCauTraLoiDto request);
        Task<ApiResponse<object>> UpdateEssayQuestionAsync(Guid id, UpdateCauHoiTuLuanDto request);
        Task<ApiResponse<object>> UpdateDienTuQuestionAsync(Guid id, UpdateDienTuQuestionDto request);
        Task<ApiResponse<object>> UpdateGhepNoiQuestionAsync(Guid id, UpdateCauHoiNhomDto request);
        Task<ApiResponse<object>> UpdateSingleQuestionAsync(Guid id, UpdateCauHoiWithCauTraLoiDto request);

        Task<ApiResponse<bool>> DeleteQuestionAsync(Guid id);
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
