using BeQuestionBank.Domain.Interfaces.IRepositories;
using BeQuestionBank.Domain.Models;
using BeQuestionBank.Shared.DTOs.CauHoi;
using BeQuestionBank.Shared.DTOs.Pagination;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BeQuestionBank.Application.Features.CauHoi.Queries;

public record GetCauHoiByIdQuery(Guid Id) : IRequest<BeQuestionBank.Domain.Models.CauHoi?>;

public class GetCauHoiByIdQueryHandler(ICauHoiRepository repository) : IRequestHandler<GetCauHoiByIdQuery, BeQuestionBank.Domain.Models.CauHoi?>
{
    private readonly ICauHoiRepository _repository = repository;

    public Task<BeQuestionBank.Domain.Models.CauHoi?> Handle(GetCauHoiByIdQuery request, CancellationToken cancellationToken)
    {
        return _repository.GetByIdWithAnswersAsync(request.Id);
    }
}

public record GetCauHoiPagedQuery(
    string Type,
    int Page = 1,
    int PageSize = 10,
    string? Sort = null,
    string? Search = null,
    Guid? KhoaId = null,
    Guid? MonHocId = null,
    Guid? PhanId = null) : IRequest<PagedResult<CauHoiDto>>;

public class GetCauHoiPagedQueryHandler(ICauHoiRepository repository) : IRequestHandler<GetCauHoiPagedQuery, PagedResult<CauHoiDto>>
{
    private readonly ICauHoiRepository _repository = repository;

    public Task<PagedResult<CauHoiDto>> Handle(GetCauHoiPagedQuery request, CancellationToken cancellationToken)
    {
        return request.Type?.ToUpper() switch
        {
            "ESSAY" or "TL" => _repository.GetEssayPagedAsync(request.Page, request.PageSize, request.Sort, request.Search, request.KhoaId, request.MonHocId, request.PhanId),
            "GROUP" or "NHOM" => _repository.GetGroupPagedAsync(request.Page, request.PageSize, request.Sort, request.Search, request.KhoaId, request.MonHocId, request.PhanId),
            "FILL" or "DIENTU" => _repository.GetFillBlankPagedAsync(request.Page, request.PageSize, request.Sort, request.Search, request.KhoaId, request.MonHocId, request.PhanId),
            "PAIR" or "GHEPNOI" => _repository.GetPairingPagedAsync(request.Page, request.PageSize, request.Sort, request.Search, request.KhoaId, request.MonHocId, request.PhanId),
            _ => _repository.GetMultipleChoicePagedAsync(request.Page, request.PageSize, request.Sort, request.Search, request.KhoaId, request.MonHocId, request.PhanId)
        };
    }
}
