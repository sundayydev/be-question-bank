using BeQuestionBank.Shared.DTOs.Common;
using BeQuestionBank.Shared.DTOs.DeThi;
using BEQuestionBank.Shared.DTOs.DeThi;
using BeQuestionBank.Shared.DTOs.Pagination;
using BeQuestionBank.Shared.DTOs.YeuCauRutTrich;

namespace FEQuestionBank.Client.Services;

public interface IDeThiApiClient
{
    Task<ApiResponse<List<DeThiDto>>> GetAllAsync();
    Task<ApiResponse<PagedResult<DeThiDto>>> 
        GetPagedAsync(int page = 1, int pageSize = 10, string? sort = null, string? search = null);
    Task<ApiResponse<DeThiDto>> GetByIdAsync(Guid id);
    Task<ApiResponse<DeThiDto>> CreateAsync(CreateDeThiDto model);
    Task<ApiResponse<DeThiDto>> UpdateAsync(Guid id, UpdateDeThiDto model);
    Task<ApiResponse<string>> DeleteAsync(Guid id);
    Task<ApiResponse<List<DeThiDto>>> GetByMonHocAsync(Guid maMonHoc);
    Task<ApiResponse<DeThiWithChiTietAndCauTraLoiDto>> GetByIdWithChiTietAndCauTraLoiAsync(Guid id);
    Task<ApiResponse<CheckQuestionResultDto>> CheckQuestionsAsync(MaTranDto maTran, Guid maMonHoc);
    Task<byte[]> ExportAsync(Guid id, YeuCauXuatDeThiDto request);
    Task<byte[]> ExportEzpAsync(Guid id, string password = "matkhau123");
    Task<byte[]> ExportTuLuanWordAsync(Guid id, YeuCauXuatDeThiDto request);
}