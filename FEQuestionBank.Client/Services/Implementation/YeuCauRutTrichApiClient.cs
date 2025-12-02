using System.Net.Http.Json;
using BeQuestionBank.Shared.DTOs.Common;
using BeQuestionBank.Shared.DTOs.Pagination;
using BeQuestionBank.Shared.DTOs.YeuCauRutTrich;

namespace FEQuestionBank.Client.Services;

public class YeuCauRutTrichApiClient : BaseApiClient, IYeuCauRutTrichApiClient
{
    public YeuCauRutTrichApiClient(HttpClient httpClient) : base(httpClient)
    {
    }

    public Task<ApiResponse<List<YeuCauRutTrichDto>>> GetAllAsync()
        => GetListAsync<YeuCauRutTrichDto>("api/yeucauruttrich");

    public Task<ApiResponse<YeuCauRutTrichDto>> GetByIdAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<ApiResponse<List<YeuCauRutTrichDto>>> GetByMaNguoiDungAsync(Guid maNguoiDung)
    {
        throw new NotImplementedException();
    }

    public Task<ApiResponse<List<YeuCauRutTrichDto>>> GetByMaMonHocAsync(Guid maMonHoc)
    {
        throw new NotImplementedException();
    }

    public Task<ApiResponse<List<YeuCauRutTrichDto>>> GetByTrangThaiAsync(bool daXuLy)
    {
        throw new NotImplementedException();
    }

    public Task<ApiResponse<PagedResult<YeuCauRutTrichDto>>> GetYeuCauPagedAsync(
        int page = 1,
        int limit = 10,
        string sort = "NgayYeuCau,desc",
        string? search = null,
        bool? daXuLy = null)
    {
        return GetPagedAsync<YeuCauRutTrichDto>(
            url: "api/YeuCauRutTrich/paged",
            page: page,
            pageSize: limit,
            sort: sort,
            search: search,
            daXuLy: daXuLy
        );
    }

    public async Task<ApiResponse<YeuCauRutTrichDto>> CreateAsync(CreateYeuCauRutTrichDto dto)
    {
        var res = await _httpClient.PostAsJsonAsync("api/yeucauruttrich/RutTrichDeThi", dto);
        return await res.Content.ReadFromJsonAsync<ApiResponse<YeuCauRutTrichDto>>() 
               ?? new ApiResponse<YeuCauRutTrichDto>(500, "Error");
    }

    public Task<ApiResponse<YeuCauRutTrichResultDto>> CreateAndRutTrichDeThiAsync(CreateYeuCauRutTrichDto dto)
    {
        throw new NotImplementedException();
    }


    // public async Task<ApiResponse<YeuCauRutTrichResultDto>> CreateAndRutTrichDeThiAsync(CreateYeuCauRutTrichDto dto)
    // {
    //     
    //     var response = await _httpClient.PostAsJsonAsync("api/yeucauruttrich/RutTrichDeThi", dto);
    //     var content = await response.Content.ReadAsStringAsync();
    //
    //     if (!response.IsSuccessStatusCode)
    //     {
    //         return new ApiResponse<YeuCauRutTrichResultDto>
    //         {
    //             StatusCode = (int)response.StatusCode,
    //             Message = content
    //         };
    //     }
    //
    //     var data = await response.Content.ReadFromJsonAsync<YeuCauRutTrichResultDto>();
    //     return new ApiResponse<YeuCauRutTrichResultDto>
    //     {
    //         StatusCode = 201,
    //         Message = "Tạo thành công",
    //         Data = data
    //     };
    // }

    public Task<ApiResponse<object>> UpdateAsync(Guid id, YeuCauRutTrichDto dto)
    {
        throw new NotImplementedException();
    }

    public Task<ApiResponse<object>> DeleteAsync(Guid id)
    {
        throw new NotImplementedException();
    }
}