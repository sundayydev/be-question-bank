using BeQuestionBank.Domain.Enums;
using BeQuestionBank.Domain.Exceptions;
using BeQuestionBank.Domain.Interfaces.IRepositories;
using BeQuestionBank.Domain.Models;
using FluentValidation;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BeQuestionBank.Application.Features.CauHoi.Commands;

public class CreateCauHoiCommand : IRequest<BeQuestionBank.Domain.Models.CauHoi>
{
    public Guid MaPhan { get; set; }
    public string? NoiDung { get; set; }
    public bool HoanVi { get; set; }
    public short CapDo { get; set; }
    public EnumCLO? CLO { get; set; }
    public string? LoaiCauHoi { get; set; }
    public Guid? NguoiTao { get; set; }
    public List<CreateCauTraLoiItemDto>? CauTraLois { get; set; }
}

public class CreateCauTraLoiItemDto
{
    public string? NoiDung { get; set; }
    public bool LaDapAn { get; set; }
    public bool HoanVi { get; set; }
    public int ThuTu { get; set; }
}

public class CreateCauHoiCommandValidator : AbstractValidator<CreateCauHoiCommand>
{
    public CreateCauHoiCommandValidator()
    {
        RuleFor(x => x.MaPhan).NotEmpty().WithMessage("Mã phần không được để trống");
        RuleFor(x => x.NoiDung).NotEmpty().WithMessage("Nội dung câu hỏi không được để trống");
    }
}

public class CreateCauHoiCommandHandler(ICauHoiRepository repository) 
    : IRequestHandler<CreateCauHoiCommand, BeQuestionBank.Domain.Models.CauHoi>
{
    private readonly ICauHoiRepository _repository = repository;

    public async Task<BeQuestionBank.Domain.Models.CauHoi> Handle(CreateCauHoiCommand request, CancellationToken cancellationToken)
    {
        var nextMaSo = await _repository.GetNextMaSoCauHoiAsync(request.MaPhan);

        var entity = new BeQuestionBank.Domain.Models.CauHoi
        {
            MaCauHoi = Guid.NewGuid(),
            MaPhan = request.MaPhan,
            MaSoCauHoi = nextMaSo,
            NoiDung = request.NoiDung,
            HoanVi = request.HoanVi,
            CapDo = request.CapDo,
            CLO = request.CLO,
            LoaiCauHoi = request.LoaiCauHoi ?? "TN",
            NguoiTao = request.NguoiTao,
            TrangThai = true,
            XoaTam = false,
            NgayTao = DateTime.UtcNow
        };

        if (request.CauTraLois != null && request.CauTraLois.Count > 0)
        {
            int order = 1;
            foreach (var a in request.CauTraLois)
            {
                entity.CauTraLois.Add(new CauTraLoi
                {
                    MaCauTraLoi = Guid.NewGuid(),
                    MaCauHoi = entity.MaCauHoi,
                    NoiDung = a.NoiDung,
                    LaDapAn = a.LaDapAn,
                    HoanVi = a.HoanVi,
                    ThuTu = a.ThuTu > 0 ? a.ThuTu : order++
                });
            }
            return await _repository.AddWithAnswersAsync(entity);
        }

        await _repository.AddAsync(entity);
        return entity;
    }
}

public class UpdateCauHoiCommand : IRequest<BeQuestionBank.Domain.Models.CauHoi>
{
    public Guid MaCauHoi { get; set; }
    public string? NoiDung { get; set; }
    public bool? HoanVi { get; set; }
    public short? CapDo { get; set; }
    public EnumCLO? CLO { get; set; }
    public bool? TrangThai { get; set; }
    public bool? XoaTam { get; set; }
}

public class UpdateCauHoiCommandHandler(ICauHoiRepository repository) 
    : IRequestHandler<UpdateCauHoiCommand, BeQuestionBank.Domain.Models.CauHoi>
{
    private readonly ICauHoiRepository _repository = repository;

    public async Task<BeQuestionBank.Domain.Models.CauHoi> Handle(UpdateCauHoiCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByIdAsync(request.MaCauHoi)
            ?? throw new NotFoundException($"Không tìm thấy Câu hỏi với mã {request.MaCauHoi}");

        if (request.NoiDung != null) existing.NoiDung = request.NoiDung;
        if (request.HoanVi.HasValue) existing.HoanVi = request.HoanVi.Value;
        if (request.CapDo.HasValue) existing.CapDo = request.CapDo.Value;
        if (request.CLO.HasValue) existing.CLO = request.CLO.Value;
        if (request.TrangThai.HasValue) existing.TrangThai = request.TrangThai.Value;
        if (request.XoaTam.HasValue) existing.XoaTam = request.XoaTam.Value;

        existing.NgayCapNhat = DateTime.UtcNow;
        await _repository.UpdateAsync(existing);
        return existing;
    }
}

public record DeleteCauHoiCommand(Guid MaCauHoi) : IRequest<bool>;

public class DeleteCauHoiCommandHandler(ICauHoiRepository repository) : IRequestHandler<DeleteCauHoiCommand, bool>
{
    private readonly ICauHoiRepository _repository = repository;

    public async Task<bool> Handle(DeleteCauHoiCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByIdAsync(request.MaCauHoi)
            ?? throw new NotFoundException($"Không tìm thấy Câu hỏi với mã {request.MaCauHoi}");

        await _repository.DeleteAsync(existing);
        return true;
    }
}
