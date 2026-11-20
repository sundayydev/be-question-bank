using BeQuestionBank.Domain.Interfaces.IRepositories;
using BeQuestionBank.Domain.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BeQuestionBank.Application.Features.DeThi.Queries;

public record GetDeThiByIdQuery(Guid Id) : IRequest<BeQuestionBank.Domain.Models.DeThi?>;

public class GetDeThiByIdQueryHandler(IDeThiRepository repository) : IRequestHandler<GetDeThiByIdQuery, BeQuestionBank.Domain.Models.DeThi?>
{
    private readonly IDeThiRepository _repository = repository;

    public Task<BeQuestionBank.Domain.Models.DeThi?> Handle(GetDeThiByIdQuery request, CancellationToken cancellationToken)
    {
        return _repository.GetByIdAsync(request.Id);
    }
}

public record GetAllDeThiQuery : IRequest<IEnumerable<BeQuestionBank.Domain.Models.DeThi>>;

public class GetAllDeThiQueryHandler(IDeThiRepository repository) : IRequestHandler<GetAllDeThiQuery, IEnumerable<BeQuestionBank.Domain.Models.DeThi>>
{
    private readonly IDeThiRepository _repository = repository;

    public Task<IEnumerable<BeQuestionBank.Domain.Models.DeThi>> Handle(GetAllDeThiQuery request, CancellationToken cancellationToken)
    {
        return _repository.GetAllAsync();
    }
}

public record GetDeThiByMonHocQuery(Guid MaMonHoc) : IRequest<IEnumerable<BeQuestionBank.Domain.Models.DeThi>>;

public class GetDeThiByMonHocQueryHandler(IDeThiRepository repository) : IRequestHandler<GetDeThiByMonHocQuery, IEnumerable<BeQuestionBank.Domain.Models.DeThi>>
{
    private readonly IDeThiRepository _repository = repository;

    public Task<IEnumerable<BeQuestionBank.Domain.Models.DeThi>> Handle(GetDeThiByMonHocQuery request, CancellationToken cancellationToken)
    {
        return _repository.GetByMaMonHocAsync(request.MaMonHoc);
    }
}
