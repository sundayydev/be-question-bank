using BeQuestionBank.Domain.Exceptions;
using BeQuestionBank.Domain.Interfaces.IRepositories;
using BeQuestionBank.Domain.Models;
using FluentValidation;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BeQuestionBank.Application.Features.Phan.Commands;

public class CreatePhanCommand : IRequest<Domain.Models.Phan>
{
    public Guid MaMonHoc { get; set; }
    public required string TenPhan { get; set; }
    public string? NoiDung { get; set; }
    public int? ThuTu { get; set; }
    public int SoLuongCauHoi { get; set; }
    public Guid? MaPhanCha { get; set; }
    public bool LaCauHoiNhom { get; set; }
    public bool? XoaTam { get; set; }
    public int? MaSoPhan { get; set; }
}

public class CreatePhanCommandValidator : AbstractValidator<CreatePhanCommand>
{
    public CreatePhanCommandValidator()
    {
        RuleFor(x => x.MaMonHoc).NotEmpty().WithMessage("Mã môn học là bắt buộc");
        RuleFor(x => x.TenPhan).NotEmpty().WithMessage("Tên phần là bắt buộc");
    }
}

public class CreatePhanCommandHandler(IPhanRepository repository) : IRequestHandler<CreatePhanCommand, Domain.Models.Phan>
{
    private readonly IPhanRepository _repository = repository;

    public async Task<Domain.Models.Phan> Handle(CreatePhanCommand request, CancellationToken cancellationToken)
    {
        var entity = new Domain.Models.Phan
        {
            MaPhan = Guid.NewGuid(),
            MaMonHoc = request.MaMonHoc,
            TenPhan = request.TenPhan,
            NoiDung = request.NoiDung,
            ThuTu = request.ThuTu ?? 0,
            SoLuongCauHoi = request.SoLuongCauHoi,
            MaPhanCha = request.MaPhanCha,
            LaCauHoiNhom = request.LaCauHoiNhom,
            XoaTam = request.XoaTam ?? false,
            MaSoPhan = request.MaSoPhan ?? 0,
            NgayTao = DateTime.UtcNow
        };

        await _repository.AddAsync(entity);
        return entity;
    }
}

public class UpdatePhanCommand : IRequest<Domain.Models.Phan>
{
    public Guid MaPhan { get; set; }
    public required string TenPhan { get; set; }
    public string? NoiDung { get; set; }
    public int? ThuTu { get; set; }
    public int? SoLuongCauHoi { get; set; }
    public Guid? MaPhanCha { get; set; }
    public bool? LaCauHoiNhom { get; set; }
    public bool? XoaTam { get; set; }
    public int? MaSoPhan { get; set; }
}

public class UpdatePhanCommandHandler(IPhanRepository repository) : IRequestHandler<UpdatePhanCommand, Domain.Models.Phan>
{
    private readonly IPhanRepository _repository = repository;

    public async Task<Domain.Models.Phan> Handle(UpdatePhanCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByIdAsync(request.MaPhan)
            ?? throw new NotFoundException($"Không tìm thấy Phần với mã {request.MaPhan}");

        existing.TenPhan = request.TenPhan;
        if (request.NoiDung != null) existing.NoiDung = request.NoiDung;
        if (request.ThuTu.HasValue) existing.ThuTu = request.ThuTu.Value;
        if (request.SoLuongCauHoi.HasValue) existing.SoLuongCauHoi = request.SoLuongCauHoi.Value;
        if (request.MaPhanCha.HasValue) existing.MaPhanCha = request.MaPhanCha.Value;
        if (request.LaCauHoiNhom.HasValue) existing.LaCauHoiNhom = request.LaCauHoiNhom.Value;
        if (request.XoaTam.HasValue) existing.XoaTam = request.XoaTam.Value;
        if (request.MaSoPhan.HasValue) existing.MaSoPhan = request.MaSoPhan.Value;

        existing.NgayCapNhat = DateTime.UtcNow;

        await _repository.UpdateAsync(existing);
        return existing;
    }
}

public record DeletePhanCommand(Guid MaPhan) : IRequest<bool>;

public class DeletePhanCommandHandler(IPhanRepository repository) : IRequestHandler<DeletePhanCommand, bool>
{
    private readonly IPhanRepository _repository = repository;

    public async Task<bool> Handle(DeletePhanCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByIdAsync(request.MaPhan)
            ?? throw new NotFoundException($"Không tìm thấy Phần với mã {request.MaPhan}");

        await _repository.DeleteAsync(existing);
        return true;
    }
}
