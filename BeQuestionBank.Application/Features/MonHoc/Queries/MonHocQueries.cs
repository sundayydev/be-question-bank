using BeQuestionBank.Domain.Interfaces.IRepositories;
using BeQuestionBank.Domain.Models;
using BeQuestionBank.Shared.DTOs.MonHoc;
using BeQuestionBank.Shared.DTOs.Pagination;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BeQuestionBank.Application.Features.MonHoc.Queries;

public record GetMonHocByIdQuery(Guid Id) : IRequest<BeQuestionBank.Domain.Models.MonHoc?>;

public class GetMonHocByIdQueryHandler(IMonHocRepository repository) : IRequestHandler<GetMonHocByIdQuery, BeQuestionBank.Domain.Models.MonHoc?>
{
    private readonly IMonHocRepository _repository = repository;

    public Task<BeQuestionBank.Domain.Models.MonHoc?> Handle(GetMonHocByIdQuery request, CancellationToken cancellationToken)
    {
        return _repository.GetByIdAsync(request.Id);
    }
}

public record GetAllMonHocQuery : IRequest<IEnumerable<BeQuestionBank.Domain.Models.MonHoc?>>;

public class GetAllMonHocQueryHandler(IMonHocRepository repository) : IRequestHandler<GetAllMonHocQuery, IEnumerable<BeQuestionBank.Domain.Models.MonHoc?>>
{
    private readonly IMonHocRepository _repository = repository;

    public Task<IEnumerable<BeQuestionBank.Domain.Models.MonHoc?>> Handle(GetAllMonHocQuery request, CancellationToken cancellationToken)
    {
        return _repository.GetAllAsync();
    }
}

public record GetMonHocsByMaKhoaQuery(Guid MaKhoa) : IRequest<IEnumerable<BeQuestionBank.Domain.Models.MonHoc?>>;

public class GetMonHocsByMaKhoaQueryHandler(IMonHocRepository repository) : IRequestHandler<GetMonHocsByMaKhoaQuery, IEnumerable<BeQuestionBank.Domain.Models.MonHoc?>>
{
    private readonly IMonHocRepository _repository = repository;

    public Task<IEnumerable<BeQuestionBank.Domain.Models.MonHoc?>> Handle(GetMonHocsByMaKhoaQuery request, CancellationToken cancellationToken)
    {
        return _repository.GetByMaKhoaAsync(request.MaKhoa);
    }
}

public record GetMonHocPagedQuery(
    int Page = 1,
    int PageSize = 10,
    Guid? MaKhoa = null,
    string? Search = null,
    string? Sort = null) : IRequest<PagedResult<MonHocDto>>;

public class GetMonHocPagedQueryHandler(IMonHocRepository repository) : IRequestHandler<GetMonHocPagedQuery, PagedResult<MonHocDto>>
{
    private readonly IMonHocRepository _repository = repository;

    public Task<PagedResult<MonHocDto>> Handle(GetMonHocPagedQuery request, CancellationToken cancellationToken)
    {
        return _repository.GetPagedMonHocsAsync(request.Page, request.PageSize, request.MaKhoa, request.Search, request.Sort);
    }
}

public record GetMonHocTrashedPagedQuery(int Page = 1, int PageSize = 10) : IRequest<PagedResult<MonHocDto>>;

public class GetMonHocTrashedPagedQueryHandler(IMonHocRepository repository) : IRequestHandler<GetMonHocTrashedPagedQuery, PagedResult<MonHocDto>>
{
    private readonly IMonHocRepository _repository = repository;

    public Task<PagedResult<MonHocDto>> Handle(GetMonHocTrashedPagedQuery request, CancellationToken cancellationToken)
    {
        return _repository.GetTrashedAsync(request.Page, request.PageSize);
    }
}
