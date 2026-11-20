using BeQuestionBank.Application.Common.Interfaces.Services;
using BeQuestionBank.Domain.Exceptions;
using BeQuestionBank.Domain.Interfaces.IRepositories;
using BeQuestionBank.Domain.Models;
using FluentValidation;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BeQuestionBank.Application.Features.DeThi.Commands;

public class CreateDeThiCommand : IRequest<BeQuestionBank.Domain.Models.DeThi>
{
    public Guid MaMonHoc { get; set; }
    public string TenDeThi { get; set; } = string.Empty;
    public int? SoCauHoi { get; set; }
    public bool DaDuyet { get; set; }
}

public class CreateDeThiCommandValidator : AbstractValidator<CreateDeThiCommand>
{
    public CreateDeThiCommandValidator()
    {
        RuleFor(x => x.MaMonHoc).NotEmpty().WithMessage("Mã môn học không được để trống");
        RuleFor(x => x.TenDeThi).NotEmpty().WithMessage("Tên đề thi không được để trống");
    }
}

public class CreateDeThiCommandHandler(IDeThiRepository repository) 
    : IRequestHandler<CreateDeThiCommand, BeQuestionBank.Domain.Models.DeThi>
{
    private readonly IDeThiRepository _repository = repository;

    public async Task<BeQuestionBank.Domain.Models.DeThi> Handle(CreateDeThiCommand request, CancellationToken cancellationToken)
    {
        var entity = new BeQuestionBank.Domain.Models.DeThi
        {
            MaDeThi = Guid.NewGuid(),
            MaMonHoc = request.MaMonHoc,
            TenDeThi = request.TenDeThi,
            SoCauHoi = request.SoCauHoi,
            DaDuyet = request.DaDuyet,
            NgayTao = DateTime.UtcNow
        };

        await _repository.AddAsync(entity);
        return entity;
    }
}

public class UpdateDeThiCommand : IRequest<BeQuestionBank.Domain.Models.DeThi>
{
    public Guid MaDeThi { get; set; }
    public string TenDeThi { get; set; } = string.Empty;
    public bool? DaDuyet { get; set; }
    public int? SoCauHoi { get; set; }
}

public class UpdateDeThiCommandHandler(IDeThiRepository repository) 
    : IRequestHandler<UpdateDeThiCommand, BeQuestionBank.Domain.Models.DeThi>
{
    private readonly IDeThiRepository _repository = repository;

    public async Task<BeQuestionBank.Domain.Models.DeThi> Handle(UpdateDeThiCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByIdAsync(request.MaDeThi)
            ?? throw new NotFoundException($"Không tìm thấy Đề thi với mã {request.MaDeThi}");

        if (!string.IsNullOrWhiteSpace(request.TenDeThi)) existing.TenDeThi = request.TenDeThi;
        if (request.DaDuyet.HasValue) existing.DaDuyet = request.DaDuyet.Value;
        if (request.SoCauHoi.HasValue) existing.SoCauHoi = request.SoCauHoi.Value;

        existing.NgayCapNhat = DateTime.UtcNow;
        await _repository.UpdateAsync(existing);
        return existing;
    }
}

public record DeleteDeThiCommand(Guid MaDeThi) : IRequest<bool>;

public class DeleteDeThiCommandHandler(IDeThiRepository repository) : IRequestHandler<DeleteDeThiCommand, bool>
{
    private readonly IDeThiRepository _repository = repository;

    public async Task<bool> Handle(DeleteDeThiCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByIdAsync(request.MaDeThi)
            ?? throw new NotFoundException($"Không tìm thấy Đề thi với mã {request.MaDeThi}");

        await _repository.DeleteAsync(existing);
        return true;
    }
}

public record ExportDeThiCommand(Guid MaDeThi, bool WithPassword = false) 
    : IRequest<(bool Success, string Message, byte[]? FileContent, string FileName)>;

public class ExportDeThiCommandHandler(IExportService exportService) 
    : IRequestHandler<ExportDeThiCommand, (bool Success, string Message, byte[]? FileContent, string FileName)>
{
    private readonly IExportService _exportService = exportService;

    public Task<(bool Success, string Message, byte[]? FileContent, string FileName)> Handle(
        ExportDeThiCommand request, CancellationToken cancellationToken)
    {
        return request.WithPassword
            ? _exportService.ExportDeThiToEzpFileWithPasswordAsync(request.MaDeThi)
            : _exportService.ExportDeThiToEzpFileAsync(request.MaDeThi);
    }
}
