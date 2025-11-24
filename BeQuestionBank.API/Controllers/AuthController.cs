using BeQuestionBank.Shared.DTOs.NguoiDung;
using BEQuestionBank.Core.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BEQuestionBank.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Đăng nhập và nhận access token + refresh token
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.LoginAsync(request);
        
        if (result == null)
            return Unauthorized(new { Message = "Tên đăng nhập hoặc mật khẩu không đúng, hoặc tài khoản bị khóa." });

        return Ok(result);
    }

    /// <summary>
    /// Đăng ký người dùng mới
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _authService.RegisterAsync(request);
        if (user == null)
            return Conflict(new { Message = "Tên đăng nhập đã tồn tại." });

        return CreatedAtAction(nameof(Register), new { user.MaNguoiDung }, new { Message = "Đăng ký thành công." });
    }

    /// <summary>
    /// Làm mới access token bằng refresh token
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.RefreshTokenAsync(request);
        if (result == null)
            return Unauthorized(new { Message = "Refresh token không hợp lệ hoặc tài khoản bị khóa." });

        return Ok(result);
    }

    /// <summary>
    /// Đăng xuất (xóa refresh token)
    /// </summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return BadRequest(new { Message = "UserId không hợp lệ." });

        var success = await _authService.LogoutAsync(userId);
        if (!success)
            return NotFound(new { Message = "Không tìm thấy refresh token để xóa." });

        return Ok(new { Message = "Đăng xuất thành công." });
    }
}