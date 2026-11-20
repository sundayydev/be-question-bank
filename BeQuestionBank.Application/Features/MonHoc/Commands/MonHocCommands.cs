using BeQuestionBank.Domain.Exceptions;
using BeQuestionBank.Domain.Interfaces.IRepositories;
using BeQuestionBank.Domain.Models;
using FluentValidation;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BeQuestionBank.Application.Features.MonHoc.Commands;

public class CreateMonHocCommand : IRequest<BeQuestionBank.Domain.Models.MonHoc>
{
    public required string MaSoMonHoc { get; set; }
    public string TenMonHoc { get; set; } = string.Empty;
    public Guid MaKhoa { get; set; }
    public int? SoTinChi { get; set; }
    public bool? XoaTam { get; set; }
}

public class CreateMonHocCommandValidator : AbstractValidator<CreateMonHocCommand>
{
    public CreateMonHocCommandValidator()
    {
        RuleFor(x => x.MaSoMonHoc).NotEmpty().WithMessage("Mã số môn học không được để trống");
        RuleFor(x => x.TenMonHoc).NotEmpty().WithMessage("Tên môn học không được để trống");
        RuleFor(x => x.MaKhoa).NotEmpty().WithMessage("Mã khoa không được để trống");
    }
}

public class CreateMonHocCommandHandler(IMonHocRepository repository) : IRequestHandler<CreateMonHocCommand, BeQuestionBank.Domain.Models.MonHoc>
{
    private readonly IMonHocRepository _repository = repository;

    public async Task<BeQuestionBank.Domain.Models.MonHoc> Handle(CreateMonHocCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.FirstOrDefaultAsync(m => m.MaSoMonHoc == request.MaSoMonHoc);
        if (existing != null)
        {
            throw new ConflictException($"Môn học với mã số '{request.MaSoMonHoc}' đã tồn tại");
        }

        var entity = new BeQuestionBank.Domain.Models.MonHoc
        {
            MaMonHoc = Guid.NewGuid(),
            MaSoMonHoc = request.MaSoMonHoc,
            TenMonHoc = request.TenMonHoc,
            MaKhoa = request.MaKhoa,
            SoTinChi = request.SoTinChi ?? 0,
            XoaTam = request.XoaTam ?? false,
            NgayTao = DateTime.UtcNow
        };

        await _repository.AddAsync(entity);
        return entity;
    }
}

public class UpdateMonHocCommand : IRequest<BeQuestionBank.Domain.Models.MonHoc>
{
    public Guid MaMonHoc { get; set; }
    public string TenMonHoc { get; set; } = string.Empty;
    public string? MaSoMonHoc { get; set; }
    public Guid? MaKhoa { get; set; }
    public int? SoTinChi { get; set; }
    public bool? XoaTam { get; set; }
}

public class UpdateMonHocCommandHandler(IMonHocRepository repository) : IRequestHandler<UpdateMonHocCommand, BeQuestionBank.Domain.Models.MonHoc>
{
    private readonly IMonHocRepository _repository = repository;

    public async Task<BeQuestionBank.Domain.Models.MonHoc> Handle(UpdateMonHocCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByIdAsync(request.MaMonHoc)
            ?? throw new NotFoundException($"Không tìm thấy Môn học với mã {request.MaMonHoc}");

        if (!string.IsNullOrWhiteSpace(request.TenMonHoc))
            existing.TenMonHoc = request.TenMonHoc;
        if (!string.IsNullOrWhiteSpace(request.MaSoMonHoc))
            existing.MaSoMonHoc = request.MaSoMonHoc;
        if (request.MaKhoa.HasValue)
            existing.MaKhoa = request.MaKhoa.Value;
        if (request.SoTinChi.HasValue)
            existing.SoTinChi = request.SoTinChi.Value;
        if (request.XoaTam.HasValue)
            existing.XoaTam = request.XoaTam.Value;

        existing.NgayCapNhat = DateTime.UtcNow;

        await _repository.UpdateAsync(existing);
        return existing;
    }
}

public record DeleteMonHocCommand(Guid MaMonHoc) : IRequest<bool>;

public class DeleteMonHocCommandHandler(IMonHocRepository repository) : IRequestHandler<DeleteMonHocCommand, bool>
{
    private readonly IMonHocRepository _repository = repository;

    public async Task<bool> Handle(DeleteMonHocCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByIdAsync(request.MaMonHoc)
            ?? throw new NotFoundException($"Không tìm thấy Môn học với mã {request.MaMonHoc}");

        await _repository.DeleteAsync(existing);
        return true;
    }
}
