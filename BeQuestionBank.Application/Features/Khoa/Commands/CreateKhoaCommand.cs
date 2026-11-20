using BeQuestionBank.Domain.Interfaces.IRepositories;
using BeQuestionBank.Domain.Models;
using FluentValidation;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BeQuestionBank.Application.Features.Khoa.Commands;

public class CreateKhoaCommand : IRequest<BeQuestionBank.Domain.Models.Khoa>
{
    public string? MaKhoa { get; set; }
    public string TenKhoa { get; set; } = string.Empty;
    public string? MoTa { get; set; }
    public bool? XoaTam { get; set; }
}

public class CreateKhoaCommandValidator : AbstractValidator<CreateKhoaCommand>
{
    public CreateKhoaCommandValidator()
    {
        RuleFor(x => x.TenKhoa).NotEmpty().WithMessage("Tên khoa không được để trống");
    }
}

public class CreateKhoaCommandHandler(IKhoaRepository repository) : IRequestHandler<CreateKhoaCommand, BeQuestionBank.Domain.Models.Khoa>
{
    private readonly IKhoaRepository _repository = repository;

    public async Task<BeQuestionBank.Domain.Models.Khoa> Handle(CreateKhoaCommand request, CancellationToken cancellationToken)
    {
        var entity = new BeQuestionBank.Domain.Models.Khoa
        {
            MaKhoa = Guid.TryParse(request.MaKhoa, out var g) ? g : Guid.NewGuid(),
            TenKhoa = request.TenKhoa,
            MoTa = request.MoTa,
            XoaTam = request.XoaTam ?? false,
            NgayTao = DateTime.UtcNow
        };

        await _repository.AddAsync(entity);
        return entity;
    }
}
