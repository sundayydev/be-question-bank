using BeQuestionBank.Domain.Enums;
using BEQuestionBank.Domain.Interfaces.Repo;
using BeQuestionBank.Domain.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BeQuestionBank.Application.Features.NguoiDung.Queries;

public record GetNguoiDungByIdQuery(Guid Id) : IRequest<Domain.Models.NguoiDung?>;

public class GetNguoiDungByIdQueryHandler(INguoiDungRepository repository) 
    : IRequestHandler<GetNguoiDungByIdQuery, Domain.Models.NguoiDung?>
{
    private readonly INguoiDungRepository _repository = repository;

    public Task<Domain.Models.NguoiDung?> Handle(GetNguoiDungByIdQuery request, CancellationToken cancellationToken)
    {
        return _repository.GetByIdAsync(request.Id)!;
    }
}

public record GetNguoiDungByUsernameQuery(string Username) : IRequest<Domain.Models.NguoiDung?>;

public class GetNguoiDungByUsernameQueryHandler(INguoiDungRepository repository)
    : IRequestHandler<GetNguoiDungByUsernameQuery, Domain.Models.NguoiDung?>
{
    private readonly INguoiDungRepository _repository = repository;

    public Task<Domain.Models.NguoiDung?> Handle(GetNguoiDungByUsernameQuery request, CancellationToken cancellationToken)
    {
        return _repository.GetByUsernameAsync(request.Username)!;
    }
}

public record GetAllNguoiDungQuery : IRequest<IEnumerable<Domain.Models.NguoiDung>>;

public class GetAllNguoiDungQueryHandler(INguoiDungRepository repository)
    : IRequestHandler<GetAllNguoiDungQuery, IEnumerable<Domain.Models.NguoiDung>>
{
    private readonly INguoiDungRepository _repository = repository;

    public Task<IEnumerable<Domain.Models.NguoiDung>> Handle(GetAllNguoiDungQuery request, CancellationToken cancellationToken)
    {
        return _repository.GetAllAsync();
    }
}

public record GetNguoiDungByRoleQuery(EnumRole Role) : IRequest<IEnumerable<Domain.Models.NguoiDung>>;

public class GetNguoiDungByRoleQueryHandler(INguoiDungRepository repository)
    : IRequestHandler<GetNguoiDungByRoleQuery, IEnumerable<Domain.Models.NguoiDung>>
{
    private readonly INguoiDungRepository _repository = repository;

    public Task<IEnumerable<Domain.Models.NguoiDung>> Handle(GetNguoiDungByRoleQuery request, CancellationToken cancellationToken)
    {
        return _repository.GetByVaiTroAsync(request.Role);
    }
}
