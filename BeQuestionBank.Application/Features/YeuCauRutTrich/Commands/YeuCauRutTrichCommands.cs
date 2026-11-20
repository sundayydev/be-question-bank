using BeQuestionBank.Domain.Exceptions;
using BeQuestionBank.Domain.Interfaces.IRepositories;
using BeQuestionBank.Domain.Models;
using FluentValidation;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BeQuestionBank.Application.Features.YeuCauRutTrich.Commands;

public class CreateYeuCauRutTrichCommand : IRequest<Domain.Models.YeuCauRutTrich>
{
    public Guid MaNguoiDung { get; set; }
    public Guid MaMonHoc { get; set; }
    public string? NoiDungRutTrich { get; set; }
    public string? GhiChu { get; set; }
    public string? MaTran { get; set; }
}

public class CreateYeuCauRutTrichCommandValidator : AbstractValidator<CreateYeuCauRutTrichCommand>
{
    public CreateYeuCauRutTrichCommandValidator()
    {
        RuleFor(x => x.MaNguoiDung).NotEmpty().WithMessage("Mã người dùng không được để trống");
        RuleFor(x => x.MaMonHoc).NotEmpty().WithMessage("Mã môn học không được để trống");
    }
}

public class CreateYeuCauRutTrichCommandHandler(IYeuCauRutTrichRepository repository) 
    : IRequestHandler<CreateYeuCauRutTrichCommand, Domain.Models.YeuCauRutTrich>
{
    private readonly IYeuCauRutTrichRepository _repository = repository;

    public async Task<Domain.Models.YeuCauRutTrich> Handle(CreateYeuCauRutTrichCommand request, CancellationToken cancellationToken)
    {
        var entity = new Domain.Models.YeuCauRutTrich
        {
            MaYeuCau = Guid.NewGuid(),
            MaNguoiDung = request.MaNguoiDung,
            MaMonHoc = request.MaMonHoc,
            NoiDungRutTrich = request.NoiDungRutTrich,
            GhiChu = request.GhiChu,
            MaTran = request.MaTran,
            NgayYeuCau = DateTime.UtcNow,
            DaXuLy = false
        };

        await _repository.AddAsync(entity);
        return entity;
    }
}

public record DeleteYeuCauRutTrichCommand(Guid MaYeuCau) : IRequest<bool>;

public class DeleteYeuCauRutTrichCommandHandler(IYeuCauRutTrichRepository repository) 
    : IRequestHandler<DeleteYeuCauRutTrichCommand, bool>
{
    private readonly IYeuCauRutTrichRepository _repository = repository;

    public async Task<bool> Handle(DeleteYeuCauRutTrichCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByIdAsync(request.MaYeuCau)
            ?? throw new NotFoundException($"Không tìm thấy Yêu cầu rút trích với mã {request.MaYeuCau}");

        await _repository.DeleteAsync(existing);
        return true;
    }
}
