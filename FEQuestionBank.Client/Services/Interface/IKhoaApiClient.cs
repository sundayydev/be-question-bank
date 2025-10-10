﻿using BeQuestionBank.Shared.DTOs.Common;
using BeQuestionBank.Shared.DTOs.Khoa;
using BeQuestionBank.Shared.DTOs.Pagination;

namespace FEQuestionBank.Client.Services
{
    public interface IKhoaApiClient
    {
        Task<ApiResponse<List<KhoaDto>>> GetAllKhoasAsync();
        Task<ApiResponse<PagedResult<KhoaDto>>> GetKhoasAsync(int page = 1, int limit = 10, string? sort = null, string? filter = null);
        Task<ApiResponse<KhoaDto>> CreateKhoaAsync(CreateKhoaDto model);
        Task<ApiResponse<KhoaDto>> UpdateKhoaAsync(string id, UpdateKhoaDto model);
        Task<ApiResponse<string>> DeleteKhoaAsync(string id);
    }
}