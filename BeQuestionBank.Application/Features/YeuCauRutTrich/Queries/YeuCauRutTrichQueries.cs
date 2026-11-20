using BeQuestionBank.Domain.Interfaces.IRepositories;
using BeQuestionBank.Domain.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BeQuestionBank.Application.Features.YeuCauRutTrich.Queries;

public record GetYeuCauRutTrichByIdQuery(Guid Id) : IRequest<Domain.Models.YeuCauRutTrich?>;

public class GetYeuCauRutTrichByIdQueryHandler(IYeuCauRutTrichRepository repository) 
    : IRequestHandler<GetYeuCauRutTrichByIdQuery, Domain.Models.YeuCauRutTrich?>
{
    private readonly IYeuCauRutTrichRepository _repository = repository;

    public Task<Domain.Models.YeuCauRutTrich?> Handle(GetYeuCauRutTrichByIdQuery request, CancellationToken cancellationToken)
    {
        return _repository.GetByIdAsync(request.Id);
    }
}

public record GetAllYeuCauRutTrichQuery : IRequest<IEnumerable<Domain.Models.YeuCauRutTrich>>;

public class GetAllYeuCauRutTrichQueryHandler(IYeuCauRutTrichRepository repository) 
    : IRequestHandler<GetAllYeuCauRutTrichQuery, IEnumerable<Domain.Models.YeuCauRutTrich>>
{
    private readonly IYeuCauRutTrichRepository _repository = repository;

    public Task<IEnumerable<Domain.Models.YeuCauRutTrich>> Handle(GetAllYeuCauRutTrichQuery request, CancellationToken cancellationToken)
    {
        return _repository.GetAllAsync();
    }
}

public record GetYeuCauRutTrichByNguoiDungQuery(Guid MaNguoiDung) : IRequest<IEnumerable<Domain.Models.YeuCauRutTrich>>;

public class GetYeuCauRutTrichByNguoiDungQueryHandler(IYeuCauRutTrichRepository repository) 
    : IRequestHandler<GetYeuCauRutTrichByNguoiDungQuery, IEnumerable<Domain.Models.YeuCauRutTrich>>
{
    private readonly IYeuCauRutTrichRepository _repository = repository;

    public Task<IEnumerable<Domain.Models.YeuCauRutTrich>> Handle(GetYeuCauRutTrichByNguoiDungQuery request, CancellationToken cancellationToken)
    {
        return _repository.GetByMaNguoiDungAsync(request.MaNguoiDung);
    }
}
