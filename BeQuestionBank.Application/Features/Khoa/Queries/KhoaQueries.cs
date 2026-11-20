using BeQuestionBank.Domain.Interfaces.IRepositories;
using BeQuestionBank.Domain.Models;
using BeQuestionBank.Shared.DTOs.Khoa;
using BeQuestionBank.Shared.DTOs.Pagination;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BeQuestionBank.Application.Features.Khoa.Queries;

public record GetKhoaByIdQuery(Guid Id) : IRequest<BeQuestionBank.Domain.Models.Khoa?>;

public class GetKhoaByIdQueryHandler(IKhoaRepository repository) : IRequestHandler<GetKhoaByIdQuery, BeQuestionBank.Domain.Models.Khoa?>
{
    private readonly IKhoaRepository _repository = repository;

    public Task<BeQuestionBank.Domain.Models.Khoa?> Handle(GetKhoaByIdQuery request, CancellationToken cancellationToken)
    {
        return _repository.GetByIdAsync(request.Id);
    }
}

public record GetAllKhoaQuery : IRequest<IEnumerable<BeQuestionBank.Domain.Models.Khoa>>;

public class GetAllKhoaQueryHandler(IKhoaRepository repository) : IRequestHandler<GetAllKhoaQuery, IEnumerable<BeQuestionBank.Domain.Models.Khoa>>
{
    private readonly IKhoaRepository _repository = repository;

    public Task<IEnumerable<BeQuestionBank.Domain.Models.Khoa>> Handle(GetAllKhoaQuery request, CancellationToken cancellationToken)
    {
        return _repository.GetAllAsync();
    }
}

public record GetKhoaPagedQuery(int Page = 1, int PageSize = 10, string? Search = null, string? Sort = null) 
    : IRequest<PagedResult<KhoaDto>>;

public class GetKhoaPagedQueryHandler(IKhoaRepository repository) : IRequestHandler<GetKhoaPagedQuery, PagedResult<KhoaDto>>
{
    private readonly IKhoaRepository _repository = repository;

    public Task<PagedResult<KhoaDto>> Handle(GetKhoaPagedQuery request, CancellationToken cancellationToken)
    {
        return _repository.GetPagedKhoasAsync(request.Page, request.PageSize, request.Search, request.Sort);
    }
}

public record GetKhoaTrashedPagedQuery(int Page = 1, int PageSize = 10) : IRequest<PagedResult<KhoaDto>>;

public class GetKhoaTrashedPagedQueryHandler(IKhoaRepository repository) : IRequestHandler<GetKhoaTrashedPagedQuery, PagedResult<KhoaDto>>
{
    private readonly IKhoaRepository _repository = repository;

    public Task<PagedResult<KhoaDto>> Handle(GetKhoaTrashedPagedQuery request, CancellationToken cancellationToken)
    {
        return _repository.GetTrashedAsync(request.Page, request.PageSize);
    }
}
