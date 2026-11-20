using BeQuestionBank.Application.Common.Interfaces.Services;
using BeQuestionBank.Domain.Enums;
using BeQuestionBank.Shared.DTOs.File;
using BeQuestionBank.Shared.DTOs.Pagination;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BeQuestionBank.Application.Features.File.Queries;

public record GetFilesPagedQuery(
    int Page = 1,
    int PageSize = 10,
    string? Sort = null,
    string? Search = null,
    FileType? FileType = null) : IRequest<PagedResult<FileDto>>;

public class GetFilesPagedQueryHandler(IFileStorageService fileStorageService) 
    : IRequestHandler<GetFilesPagedQuery, PagedResult<FileDto>>
{
    private readonly IFileStorageService _fileStorageService = fileStorageService;

    public Task<PagedResult<FileDto>> Handle(GetFilesPagedQuery request, CancellationToken cancellationToken)
    {
        return _fileStorageService.GetFilesPagedAsync(request.Page, request.PageSize, request.Sort, request.Search, request.FileType);
    }
}
