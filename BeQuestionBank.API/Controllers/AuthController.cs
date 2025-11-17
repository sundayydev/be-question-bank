using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using BE_CIRRO.Core.Services;
using BE_CIRRO.Shared.DTOs.Auth;
using BE_CIRRO.Shared.DTOs;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using BeQuestionBank.Shared.DTOs.Common;
using BEQuestionBank.Shared.DTOs.user;

namespace BE_CIRRO.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(AuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        // POST: /api/auth/register - Đăng ký user mới
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ApiResponseFactory.ValidationError<object>("Dữ liệu đầu vào không hợp lệ"));

                var user = await _authService.RegisterAsync(dto);
                if (user == null)
                    return BadRequest(ApiResponseFactory.ValidationError<object>("Tên đăng nhập đã tồn tại"));

                return StatusCode(StatusCodes.Status201Created,
                    ApiResponseFactory.Created(user, "Đăng ký tài khoản thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration");
                return StatusCode(500, ApiResponseFactory.ServerError("Lỗi khi đăng ký tài khoản: " + ex.Message));
            }
        }

        // POST: /api/auth/login - Đăng nhập và trả về JWT token
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ApiResponseFactory.ValidationError<object>("Dữ liệu đầu vào không hợp lệ"));

                var tokenDto = await _authService.LoginAsync(dto);
                if (tokenDto == null)
                    return Unauthorized(ApiResponseFactory.Unauthorized("Tên đăng nhập hoặc mật khẩu không đúng"));

                return Ok(ApiResponseFactory.Success(tokenDto, "Đăng nhập thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user login");
                return StatusCode(500, ApiResponseFactory.ServerError("Lỗi khi đăng nhập: " + ex.Message));
            }
        }

        // GET: /api/auth/me - Lấy thông tin user hiện tại từ JWT
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMe()
        {
            try
            {
                // Lấy user ID từ JWT token
                // Đảm bảo bạn dùng using System.Security.Claims;
                var userIdClaim = User.FindFirstValue("UserId");
                if (string.IsNullOrEmpty(userIdClaim))
                    return Unauthorized(ApiResponseFactory.Unauthorized("Token không hợp lệ"));

                var user = await _authService.GetCurrentUserAsync(userIdClaim);
                if (user == null)
                    return NotFound(ApiResponseFactory.NotFound<NguoiDungDto>("Không tìm thấy thông tin user"));

                return Ok(ApiResponseFactory.Success(user, "Lấy thông tin user thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user info");
                return StatusCode(500, ApiResponseFactory.ServerError("Lỗi khi lấy thông tin user: " + ex.Message));
            }
        }

        // POST: /api/auth/refresh - Làm mới access token
        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto dto)
        {
            try
            {
                if (string.IsNullOrEmpty(dto.RefreshToken))
                    return BadRequest(ApiResponseFactory.ValidationError<object>("Refresh token không được để trống"));

                var newTokenDto = await _authService.RefreshTokenAsync(dto.RefreshToken);
                if (newTokenDto == null)
                    return Unauthorized(ApiResponseFactory.Unauthorized("Refresh token không hợp lệ"));

                return Ok(ApiResponseFactory.Success(newTokenDto, "Làm mới token thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return StatusCode(500, ApiResponseFactory.ServerError("Lỗi khi làm mới token: " + ex.Message));
            }
        }

        // POST: /api/auth/logout - Đăng xuất
        [HttpPost("logout")]
        [Authorize] // Bạn có thể giữ [Authorize] hoặc không, tùy logic
        public async Task<IActionResult> Logout([FromBody] RefreshTokenDto dto) // <-- Thay đổi: Nhận từ body cho chuẩn REST
        {
            try
            {
                // Lấy refresh token từ body sẽ tốt hơn là từ header/query
                if (string.IsNullOrEmpty(dto.RefreshToken))
                    return BadRequest(ApiResponseFactory.ValidationError<object>("Refresh token không được để trống"));

                var success = await _authService.LogoutAsync(dto.RefreshToken);
                if (!success)
                    return BadRequest(ApiResponseFactory.ValidationError<object>("Refresh token không hợp lệ hoặc đã hết hạn"));

                return Ok(ApiResponseFactory.Success<object>(null, "Đăng xuất thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, ApiResponseFactory.ServerError("Lỗi khi đăng xuất: " + ex.Message));
            }
        }

        // POST: /api/auth/change-password - Đổi mật khẩu
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ApiResponseFactory.ValidationError<object>("Dữ liệu đầu vào không hợp lệ"));

                var userIdClaim = User.FindFirstValue("UserId"); // <-- Dùng FindFirstValue
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                    return Unauthorized(ApiResponseFactory.Unauthorized("Token không hợp lệ"));

                var success = await _authService.ChangePasswordAsync(userId, dto.MatKhauHienTai, dto.MatKhauMoi);
                if (!success)
                    return BadRequest(ApiResponseFactory.ValidationError<object>("Mật khẩu hiện tại không đúng"));

                return Ok(ApiResponseFactory.Success<object>(null, "Đổi mật khẩu thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                return StatusCode(500, ApiResponseFactory.ServerError("Lỗi khi đổi mật khẩu: " + ex.Message));
            }
        }

        /* // GET: /api/auth/debug/refresh-tokens - Debug endpoint để kiểm tra refresh tokens
        // [ĐÃ XÓA] Endpoint này không còn an toàn khi dùng Redis vì nó
        // yêu cầu quét toàn bộ keys, gây ảnh hưởng nghiêm trọng đến hiệu năng.
        [HttpGet("debug/refresh-tokens")]
        public IActionResult GetRefreshTokens()
        {
             return StatusCode(501, ApiResponseFactory.ServerError("Endpoint không được hỗ trợ."));
        }
        */


        // GET: /api/auth/debug/refresh-token/{token} - Debug endpoint để kiểm tra refresh token cụ thể
        // [SỬA] Chuyển sang async Task và dùng await
        [HttpGet("debug/refresh-token/{token}")]
        public async Task<IActionResult> CheckRefreshToken(string token)
        {
            try
            {
                // Thêm 'await' vì hàm service giờ là async
                var isValid = await _authService.IsRefreshTokenValid(token);
                var info = await _authService.GetRefreshTokenInfo(token);

                return Ok(ApiResponseFactory.Success(new
                {
                    IsValid = isValid,
                    Info = info,
                    Token = token.Length > 10 ? token.Substring(0, 10) + "..." : token
                }, "Thông tin refresh token"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking refresh token");
                return StatusCode(500, ApiResponseFactory.ServerError("Lỗi khi kiểm tra refresh token: " + ex.Message));
            }
        }
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ApiResponseFactory.ValidationError<object>("Dữ liệu đầu vào không hợp lệ"));

                var success = await _authService.SendOtpAsync(dto);
                if (!success)
                    return BadRequest(ApiResponseFactory.ValidationError<object>("Email không tồn tại hoặc không hợp lệ"));

                return Ok(ApiResponseFactory.Success<object>(null, "Mã OTP đã được gửi đến email"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi OTP");
                return StatusCode(500, ApiResponseFactory.ServerError("Lỗi khi gửi OTP: " + ex.Message));
            }
        }

        // POST: /api/auth/verify-otp - Xác nhận OTP
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ApiResponseFactory.ValidationError<object>("Dữ liệu đầu vào không hợp lệ"));

                var success = await _authService.VerifyOtpAsync(dto);
                if (!success)
                    return BadRequest(ApiResponseFactory.ValidationError<object>("Mã OTP không hợp lệ hoặc đã hết hạn"));

                return Ok(ApiResponseFactory.Success<object>(null, "Xác nhận OTP thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xác nhận OTP");
                return StatusCode(500, ApiResponseFactory.ServerError("Lỗi khi xác nhận OTP: " + ex.Message));
            }
        }

        // POST: /api/auth/reset-password - Đặt lại mật khẩu
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ApiResponseFactory.ValidationError<object>("Dữ liệu đầu vào không hợp lệ"));

                var success = await _authService.ResetPasswordAsync(dto);
                if (!success)
                    return BadRequest(ApiResponseFactory.ValidationError<object>("Mã OTP hoặc email không hợp lệ"));

                return Ok(ApiResponseFactory.Success<object>(null, "Đặt lại mật khẩu thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đặt lại mật khẩu");
                return StatusCode(500, ApiResponseFactory.ServerError("Lỗi khi đặt lại mật khẩu: " + ex.Message));
            }
        }
    }
}