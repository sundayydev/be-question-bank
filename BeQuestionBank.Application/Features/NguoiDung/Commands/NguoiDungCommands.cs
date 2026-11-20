using BeQuestionBank.Domain.Enums;
using BeQuestionBank.Domain.Exceptions;
using BeQuestionBank.Domain.Interfaces.IRepositories;
using BEQuestionBank.Domain.Interfaces.Repo;
using BeQuestionBank.Domain.Models;
using FluentValidation;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using ValidationException = BeQuestionBank.Domain.Exceptions.ValidationException;

namespace BeQuestionBank.Application.Features.NguoiDung.Commands;

public class CreateNguoiDungCommand : IRequest<Domain.Models.NguoiDung>
{
    public required string TenDangNhap { get; set; }
    public required string MatKhau { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public string? Email { get; set; }
    public EnumRole VaiTro { get; set; } = EnumRole.User;
    public Guid? MaKhoa { get; set; }
    public bool BiKhoa { get; set; } = false;
}

public class CreateNguoiDungCommandValidator : AbstractValidator<CreateNguoiDungCommand>
{
    public CreateNguoiDungCommandValidator()
    {
        RuleFor(x => x.TenDangNhap).NotEmpty().WithMessage("Tên đăng nhập không được để trống");
        RuleFor(x => x.MatKhau).NotEmpty().MinimumLength(6).WithMessage("Mật khẩu phải có ít nhất 6 ký tự");
    }
}

public class CreateNguoiDungCommandHandler(INguoiDungRepository repository) 
    : IRequestHandler<CreateNguoiDungCommand, Domain.Models.NguoiDung>
{
    private readonly INguoiDungRepository _repository = repository;

    public async Task<Domain.Models.NguoiDung> Handle(CreateNguoiDungCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByUsernameAsync(request.TenDangNhap);
        if (existing != null)
        {
            throw new ConflictException($"Tên đăng nhập '{request.TenDangNhap}' đã tồn tại");
        }

        var entity = new Domain.Models.NguoiDung
        {
            MaNguoiDung = Guid.NewGuid(),
            TenDangNhap = request.TenDangNhap,
            MatKhau = BCrypt.Net.BCrypt.HashPassword(request.MatKhau),
            HoTen = request.HoTen,
            Email = request.Email,
            VaiTro = request.VaiTro,
            MaKhoa = request.MaKhoa,
            BiKhoa = request.BiKhoa,
            NgayTao = DateTime.UtcNow
        };

        await _repository.AddAsync(entity);
        return entity;
    }
}

public class UpdateNguoiDungCommand : IRequest<Domain.Models.NguoiDung>
{
    public Guid MaNguoiDung { get; set; }
    public string? HoTen { get; set; }
    public string? Email { get; set; }
    public EnumRole? VaiTro { get; set; }
    public Guid? MaKhoa { get; set; }
    public bool? BiKhoa { get; set; }
}

public class UpdateNguoiDungCommandHandler(INguoiDungRepository repository)
    : IRequestHandler<UpdateNguoiDungCommand, Domain.Models.NguoiDung>
{
    private readonly INguoiDungRepository _repository = repository;

    public async Task<Domain.Models.NguoiDung> Handle(UpdateNguoiDungCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByIdAsync(request.MaNguoiDung)
            ?? throw new NotFoundException($"Không tìm thấy Người dùng với mã {request.MaNguoiDung}");

        if (request.HoTen != null) existing.HoTen = request.HoTen;
        if (request.Email != null) existing.Email = request.Email;
        if (request.VaiTro.HasValue) existing.VaiTro = request.VaiTro.Value;
        if (request.MaKhoa.HasValue) existing.MaKhoa = request.MaKhoa.Value;
        if (request.BiKhoa.HasValue) existing.BiKhoa = request.BiKhoa.Value;

        existing.NgayCapNhat = DateTime.UtcNow;

        await _repository.UpdateAsync(existing);
        return existing;
    }
}

public class ChangePasswordCommand : IRequest<bool>
{
    public Guid MaNguoiDung { get; set; }
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class ChangePasswordCommandHandler(INguoiDungRepository repository)
    : IRequestHandler<ChangePasswordCommand, bool>
{
    private readonly INguoiDungRepository _repository = repository;

    public async Task<bool> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(request.MaNguoiDung)
            ?? throw new NotFoundException($"Không tìm thấy Người dùng với mã {request.MaNguoiDung}");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.MatKhau))
        {
            throw new ValidationException("Mật khẩu hiện tại không chính xác");
        }

        user.MatKhau = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.NgayCapNhat = DateTime.UtcNow;
        await _repository.UpdateAsync(user);
        return true;
    }
}

public record DeleteNguoiDungCommand(Guid MaNguoiDung) : IRequest<bool>;

public class DeleteNguoiDungCommandHandler(INguoiDungRepository repository) : IRequestHandler<DeleteNguoiDungCommand, bool>
{
    private readonly INguoiDungRepository _repository = repository;

    public async Task<bool> Handle(DeleteNguoiDungCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByIdAsync(request.MaNguoiDung)
            ?? throw new NotFoundException($"Không tìm thấy Người dùng với mã {request.MaNguoiDung}");

        await _repository.DeleteAsync(existing);
        return true;
    }
}
