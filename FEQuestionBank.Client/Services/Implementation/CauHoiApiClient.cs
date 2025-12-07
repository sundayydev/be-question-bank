using BeQuestionBank.Shared.DTOs.CauHoi;
using BeQuestionBank.Shared.DTOs.Common;
using FEQuestionBank.Client.Services.Interface;
using System.Net.Http.Json;
using BeQuestionBank.Shared.DTOs.CauHoi.Create;
using BeQuestionBank.Shared.DTOs.Pagination;

namespace FEQuestionBank.Client.Services
{
    public class CauHoiApiClient : BaseApiClient, ICauHoiApiClient
    {
        public CauHoiApiClient(HttpClient httpClient) : base(httpClient)
        {
        }

        public async Task<ApiResponse<IEnumerable<CauHoiDto>>> GetAllAsync()
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<IEnumerable<CauHoiDto>>>("/api/cauhoi");
            return response ?? ApiResponseFactory.Error<IEnumerable<CauHoiDto>>(500, "Lỗi kết nối");
        }

        public async Task<ApiResponse<CauHoiWithCauTraLoiDto>> GetByIdAsync(Guid id)
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<CauHoiWithCauTraLoiDto>>($"/api/cauhoi/{id}");
            return response ?? ApiResponseFactory.Error<CauHoiWithCauTraLoiDto>(500, "Lỗi kết nối");
        }

        // Sửa bool -> object
        public async Task<ApiResponse<object>> CreateSingleQuestionAsync(CreateCauHoiWithCauTraLoiDto request)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/cauhoi/single", request);
            return await response.Content.ReadFromJsonAsync<ApiResponse<object>>()
                   ?? ApiResponseFactory.Error<object>(500, "Lỗi kết nối");
        }

        public async Task<ApiResponse<object>> CreateEssayQuestionAsync(CreateCauHoiTuLuanDto request)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/cauhoi/essay", request);
            return await response.Content.ReadFromJsonAsync<ApiResponse<object>>()
                   ?? ApiResponseFactory.Error<object>(500, "Lỗi kết nối");
        }

        public async Task<ApiResponse<object>> CreateFillingQuestionAsync(CreateCauHoiDienTuDto request)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/cauhoi/fillblank", request);
            return await response.Content.ReadFromJsonAsync<ApiResponse<object>>()
                   ?? ApiResponseFactory.Error<object>(500, "Lỗi kết nối");
        }

        public async Task<ApiResponse<object>> CreatePairingQuestionAsync(CreateCauHoiGhepNoiDto request)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/cauhoi/pairing", request);
            return await response.Content.ReadFromJsonAsync<ApiResponse<object>>()
                   ?? ApiResponseFactory.Error<object>(500, "Lỗi kết nối");
        }

        public async Task<ApiResponse<object>> CreateMultipeChoiceQuestionAsync(CreateCauHoiMultipleChoiceDto request)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/cauhoi/multiplechoice", request);
            return await response.Content.ReadFromJsonAsync<ApiResponse<object>>()
                   ?? ApiResponseFactory.Error<object>(500, "Lỗi kết nối");
        }

        // Sửa bool -> object
        public async Task<ApiResponse<object>> CreateGroupQuestionAsync(CreateCauHoiNhomDto request)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/cauhoi/group", request);
            return await response.Content.ReadFromJsonAsync<ApiResponse<object>>()
                   ?? ApiResponseFactory.Error<object>(500, "Lỗi kết nối");
        }

        // Sửa bool -> object (nếu cần)
        public async Task<ApiResponse<object>> UpdateQuestionAsync(Guid id, UpdateCauHoiWithCauTraLoiDto request)
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/cauhoi/{id}", request);
            return await response.Content.ReadFromJsonAsync<ApiResponse<object>>()
                   ?? ApiResponseFactory.Error<object>(500, "Lỗi kết nối");
        }

        public async Task<ApiResponse<bool>> DeleteQuestionAsync(Guid id)
        {
            var response = await _httpClient.DeleteAsync($"/api/cauhoi/{id}");
            return await response.Content.ReadFromJsonAsync<ApiResponse<bool>>()
                   ?? ApiResponseFactory.Error<bool>(500, "Lỗi kết nối");
        }

        public Task<ApiResponse<PagedResult<CauHoiDto>>> GetEssaysPagedAsync(
            int page = 1,
            int pageSize = 10,
            string? sort = null,
            string? search = null,
            Guid? khoaId = null,
            Guid? monHocId = null,
            Guid? phanId = null)
        {
            return GetPagedAsync<CauHoiDto>(
                "api/cauhoi/essay/paged", // Đảm bảo endpoint đúng
                page: page,
                pageSize: pageSize,
                sort: sort,
                search: search,
                khoaId: khoaId,
                monHocId: monHocId,
                phanId: phanId);
        }

        public Task<ApiResponse<PagedResult<CauHoiDto>>> GetGroupsPagedAsync(int page = 1, int pageSize = 10,
            string? sort = null, string? search = null,
            Guid? khoaId = null, Guid? monHocId = null, Guid? phanId = null)
            => GetPagedAsync<CauHoiDto>("api/cauhoi/group/paged", page, pageSize, sort, search: search, khoaId,
                monHocId, phanId);

        public Task<ApiResponse<PagedResult<CauHoiDto>>> GetSinglesPagedAsync(int page = 1, int pageSize = 10,
            string? sort = null, string? search = null,
            Guid? khoaId = null, Guid? monHocId = null, Guid? phanId = null)
            => GetPagedAsync<CauHoiDto>("api/cauhoi/single/paged", page, pageSize, sort, search: search, khoaId,
                monHocId, phanId);

        public Task<ApiResponse<PagedResult<CauHoiDto>>> GetFillingsPagedAsync(int page = 1, int pageSize = 10,
            string? sort = null, string? search = null,
            Guid? khoaId = null, Guid? monHocId = null, Guid? phanId = null)
            => GetPagedAsync<CauHoiDto>("api/cauhoi/fillblank/paged", page, pageSize, sort, search: search, khoaId,
                monHocId, phanId);

        public Task<ApiResponse<PagedResult<CauHoiDto>>> GetMultipeChoicesPagedAsync(int page = 1, int pageSize = 10,
            string? sort = null, string? search = null,
            Guid? khoaId = null, Guid? monHocId = null, Guid? phanId = null)
            => GetPagedAsync<CauHoiDto>("api/cauhoi/multiplechoice/paged", page, pageSize, sort, search: search, khoaId,
                monHocId, phanId);


        public Task<ApiResponse<PagedResult<CauHoiDto>>> GetPairingsPagedAsync(int page = 1, int pageSize = 10,
            string? sort = null, string? search = null,
            Guid? khoaId = null, Guid? monHocId = null, Guid? phanId = null)
            => GetPagedAsync<CauHoiDto>("api/cauhoi/pairing/paged", page, pageSize, sort, search: search, khoaId,
                monHocId, phanId);


        // public Task<ApiResponse<PagedResult<CauHoiDto>>> GetSinglesPagedAsync(int page = 1, int pageSize = 10,
        //     string? sort = null, string? search = null)
        //     => GetPagedAsync<CauHoiDto>("api/cauhoi/single/paged", page, pageSize, sort, search: search);
    }
}