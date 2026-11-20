using BE_CIRRO.Shared.DTOs.Auth;
using BeQuestionBank.Application.Common.Interfaces.Services;
using BeQuestionBank.Domain.Enums;
using BeQuestionBank.Domain.Exceptions;
using BEQuestionBank.Domain.Interfaces.Repo;
using BeQuestionBank.Domain.Models;
using FluentValidation;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using ValidationException = BeQuestionBank.Domain.Exceptions.ValidationException;

namespace BeQuestionBank.Application.Features.Auth.Commands;

public class LoginCommand : IRequest<TokenDto>
{
    public required string TenDangNhap { get; set; }
    public required string MatKhau { get; set; }
}

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.TenDangNhap).NotEmpty().WithMessage("Tên đăng nhập không được để trống");
        RuleFor(x => x.MatKhau).NotEmpty().WithMessage("Mật khẩu không được để trống");
    }
}

public class LoginCommandHandler(
    INguoiDungRepository userRepository,
    IJwtService jwtService,
    IRedisCacheService redisService) : IRequestHandler<LoginCommand, TokenDto>
{
    private readonly INguoiDungRepository _userRepository = userRepository;
    private readonly IJwtService _jwtService = jwtService;
    private readonly IRedisCacheService _redisService = redisService;

    public async Task<TokenDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByUsernameAsync(request.TenDangNhap)
            ?? throw new ValidationException("Tên đăng nhập hoặc mật khẩu không chính xác");

        if (!BCrypt.Net.BCrypt.Verify(request.MatKhau, user.MatKhau))
        {
            throw new ValidationException("Tên đăng nhập hoặc mật khẩu không chính xác");
        }

        if (user.BiKhoa)
        {
            throw new ValidationException("Tài khoản đã bị khóa");
        }

        user.NgayDangNhapCuoi = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken(user.MaNguoiDung.ToString(), user.TenDangNhap);

        await _redisService.SetRefreshTokenAsync(user.MaNguoiDung.ToString(), refreshToken, TimeSpan.FromDays(7));

        return new TokenDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            HanSuDung = DateTime.UtcNow.AddMinutes(60),
            LoaiToken = "Bearer"
        };
    }
}

public class RegisterCommand : IRequest<Domain.Models.NguoiDung>
{
    public required string TenDangNhap { get; set; }
    public required string MatKhau { get; set; }
    public string? Email { get; set; }
    public string VaiTro { get; set; } = "User";
}

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.TenDangNhap).NotEmpty().WithMessage("Tên đăng nhập không được để trống");
        RuleFor(x => x.MatKhau).NotEmpty().MinimumLength(6).WithMessage("Mật khẩu phải có ít nhất 6 ký tự");
    }
}

public class RegisterCommandHandler(INguoiDungRepository userRepository) : IRequestHandler<RegisterCommand, Domain.Models.NguoiDung>
{
    private readonly INguoiDungRepository _userRepository = userRepository;

    public async Task<Domain.Models.NguoiDung> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var existing = await _userRepository.GetByUsernameAsync(request.TenDangNhap);
        if (existing != null)
        {
            throw new ConflictException($"Tên đăng nhập '{request.TenDangNhap}' đã tồn tại");
        }

        var vaiTro = Enum.TryParse<EnumRole>(request.VaiTro, true, out var r) ? r : EnumRole.User;

        var user = new Domain.Models.NguoiDung
        {
            MaNguoiDung = Guid.NewGuid(),
            TenDangNhap = request.TenDangNhap,
            MatKhau = BCrypt.Net.BCrypt.HashPassword(request.MatKhau),
            Email = request.Email,
            VaiTro = vaiTro,
            BiKhoa = false,
            NgayTao = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user);
        return user;
    }
}

public class RefreshTokenCommand : IRequest<TokenDto>
{
    public required string RefreshToken { get; set; }
}

public class RefreshTokenCommandHandler(
    INguoiDungRepository userRepository,
    IJwtService jwtService,
    IRedisCacheService redisService) : IRequestHandler<RefreshTokenCommand, TokenDto>
{
    private readonly INguoiDungRepository _userRepository = userRepository;
    private readonly IJwtService _jwtService = jwtService;
    private readonly IRedisCacheService _redisService = redisService;

    public async Task<TokenDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var userIdStr = await _redisService.FindUserIdByTokenAsync(request.RefreshToken);
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            throw new ValidationException("Refresh token không hợp lệ hoặc đã hết hạn");
        }

        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new NotFoundException("Không tìm thấy thông tin người dùng từ refresh token");

        if (user.BiKhoa)
        {
            throw new ValidationException("Tài khoản đã bị khóa");
        }

        await _redisService.RevokeTokenAsync(user.MaNguoiDung.ToString());

        var newAccessToken = _jwtService.GenerateAccessToken(user);
        var newRefreshToken = _jwtService.GenerateRefreshToken(user.MaNguoiDung.ToString(), user.TenDangNhap);

        await _redisService.SetRefreshTokenAsync(user.MaNguoiDung.ToString(), newRefreshToken, TimeSpan.FromDays(7));

        return new TokenDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            HanSuDung = DateTime.UtcNow.AddMinutes(60),
            LoaiToken = "Bearer"
        };
    }
}

public record RevokeTokenCommand(string RefreshToken) : IRequest<bool>;

public class RevokeTokenCommandHandler(IRedisCacheService redisService) : IRequestHandler<RevokeTokenCommand, bool>
{
    private readonly IRedisCacheService _redisService = redisService;

    public async Task<bool> Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        var userId = await _redisService.FindUserIdByTokenAsync(request.RefreshToken);
        if (userId != null)
        {
            return await _redisService.RevokeTokenAsync(userId);
        }
        return false;
    }
}
