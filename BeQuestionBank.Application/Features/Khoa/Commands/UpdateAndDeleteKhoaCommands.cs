using BeQuestionBank.Domain.Exceptions;
using BeQuestionBank.Domain.Interfaces.IRepositories;
using BeQuestionBank.Domain.Models;
using FluentValidation;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BeQuestionBank.Application.Features.Khoa.Commands;

public class UpdateKhoaCommand : IRequest<BeQuestionBank.Domain.Models.Khoa>
{
    public Guid MaKhoa { get; set; }
    public string TenKhoa { get; set; } = string.Empty;
    public string? MoTa { get; set; }
    public bool? XoaTam { get; set; }
}

public class UpdateKhoaCommandValidator : AbstractValidator<UpdateKhoaCommand>
{
    public UpdateKhoaCommandValidator()
    {
        RuleFor(x => x.MaKhoa).NotEmpty();
        RuleFor(x => x.TenKhoa).NotEmpty().WithMessage("Tên khoa không được để trống");
    }
}

public class UpdateKhoaCommandHandler(IKhoaRepository repository) : IRequestHandler<UpdateKhoaCommand, BeQuestionBank.Domain.Models.Khoa>
{
    private readonly IKhoaRepository _repository = repository;

    public async Task<BeQuestionBank.Domain.Models.Khoa> Handle(UpdateKhoaCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByIdAsync(request.MaKhoa)
            ?? throw new NotFoundException($"Không tìm thấy Khoa với mã {request.MaKhoa}");

        existing.TenKhoa = request.TenKhoa;
        existing.MoTa = request.MoTa;
        if (request.XoaTam.HasValue)
        {
            existing.XoaTam = request.XoaTam.Value;
        }
        existing.NgayCapNhat = DateTime.UtcNow;

        await _repository.UpdateAsync(existing);
        return existing;
    }
}

public record DeleteKhoaCommand(Guid MaKhoa) : IRequest<bool>;

public class DeleteKhoaCommandHandler(IKhoaRepository repository) : IRequestHandler<DeleteKhoaCommand, bool>
{
    private readonly IKhoaRepository _repository = repository;

    public async Task<bool> Handle(DeleteKhoaCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByIdAsync(request.MaKhoa)
            ?? throw new NotFoundException($"Không tìm thấy Khoa với mã {request.MaKhoa}");

        await _repository.DeleteAsync(existing);
        return true;
    }
}
