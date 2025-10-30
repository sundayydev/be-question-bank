using BeQuestionBank.Domain.Models;
using BeQuestionBank.Shared.DTOs.Common;
using BEQuestionBank.Core.Services;
using BeQuestionBank.Shared.DTOs.Pagination;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Linq;
using System.Threading.Tasks;
using BEQuestionBank.Shared.DTOs.user;

namespace BeQuestionBank.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class NguoiDungController(NguoiDungService service, ILogger<NguoiDungController> logger) : ControllerBase
{
    private readonly NguoiDungService _service = service;
    private readonly ILogger<NguoiDungController> _logger = logger;

    // GET: api/NguoiDung/{id}
    [HttpGet("{id}")]
    [SwaggerOperation("Tìm người dùng theo Mã Người Dùng")]
    public async Task<ActionResult<NguoiDungDto>> GetNguoiDungByIdAsync(string id)
    {
        try
        {
            _logger.LogInformation("Yêu cầu tìm người dùng theo mã: {MaNguoiDung}", id);

            var user = await _service.GetByIdAsync(Guid.Parse(id));
            if (user == null)
            {
                _logger.LogWarning("Không tìm thấy người dùng với mã: {MaNguoiDung}", id);
                return NotFound(ApiResponseFactory.NotFound<NguoiDung>(
                    $"Không tìm thấy người dùng với mã {id}",
                    new { MaNguoiDung = id }
                ));
            }

            var userDto = MapToDto(user);
            _logger.LogInformation("Tìm thấy người dùng: {HoTen}", user.HoTen);
            return Ok(ApiResponseFactory.Success(userDto, "Lấy thông tin người dùng thành công"));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Lỗi khi lấy thông tin người dùng theo mã: {MaNguoiDung}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponseFactory.ServerError("Đã xảy ra lỗi trong quá trình xử lý."));
        }
    }

    // GET: api/NguoiDung
    [HttpGet]
    [SwaggerOperation("Lấy danh sách tất cả người dùng")]
    public async Task<ActionResult<IEnumerable<NguoiDungDto>>> GetAllAsync()
    {
        var users = await _service.GetAllAsync();
        var result = users.Select(MapToDto);

        return Ok(ApiResponseFactory.Success(result, "Lấy danh sách người dùng thành công"));
    }

    // POST: api/NguoiDung
    [HttpPost]
    [SwaggerOperation("Thêm một người dùng mới")]
    public async Task<IActionResult> CreateNguoiDungAsync([FromBody] NguoiDungDto model)
    {
        try
        {
            _logger.LogInformation("Bắt đầu thêm người dùng. Tên đăng nhập: {TenDangNhap}", model.TenDangNhap);

            var existingUser = await _service.GetByUsernameAsync(model.TenDangNhap);
            if (existingUser != null)
            {
                if (existingUser.BiKhoa == true)
                {
                    _logger.LogInformation("Người dùng bị khóa được phát hiện: {TenDangNhap}", model.TenDangNhap);
                    return StatusCode(StatusCodes.Status409Conflict, new NguoiDungDto
                    {
                        MaNguoiDung = existingUser.MaNguoiDung,
                        TenDangNhap = existingUser.TenDangNhap,
                        BiKhoa = existingUser.BiKhoa
                    });
                }

                _logger.LogWarning("Tên đăng nhập {TenDangNhap} đã tồn tại.", model.TenDangNhap);
                return StatusCode(StatusCodes.Status409Conflict, $"Tên đăng nhập {model.TenDangNhap} đã tồn tại.");
            }

            var newUser = new NguoiDung
            {
                TenDangNhap = model.TenDangNhap,
                MatKhau = model.MatKhau, 
                HoTen = model.HoTen,
                Email = model.Email,
                VaiTro = model.VaiTro,
                BiKhoa = model.BiKhoa,
                MaKhoa = model.MaKhoa
            };

            var createdUser = await _service.CreateAsync(newUser);
            var userDto = MapToDto(createdUser);

            _logger.LogInformation("Thêm người dùng thành công: {TenDangNhap}", model.TenDangNhap);
            return StatusCode(StatusCodes.Status201Created, ApiResponseFactory.Created(userDto, "Thêm người dùng thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Có lỗi xảy ra khi thêm người dùng: {TenDangNhap}", model.TenDangNhap);
            return StatusCode(StatusCodes.Status500InternalServerError, ApiResponseFactory.ServerError("Đã xảy ra lỗi khi thêm người dùng."));
        }
    }

    // PATCH: api/NguoiDung/{id}
    [HttpPatch("{id}")]
    [SwaggerOperation("Cập nhật thông tin người dùng")]
    public async Task<IActionResult> UpdateNguoiDungAsync(string id, [FromBody] NguoiDungDto model)
    {
        try
        {
            _logger.LogInformation("Yêu cầu cập nhật người dùng có mã: {MaNguoiDung}", id);

            var existingUser = await _service.GetByIdAsync(Guid.Parse(id));
            if (existingUser == null)
            {
                _logger.LogWarning("Không tìm thấy người dùng với mã: {MaNguoiDung}", id);
                return StatusCode(StatusCodes.Status404NotFound,
                    ApiResponseFactory.NotFound<NguoiDung>($"Không tìm thấy người dùng với mã {id}", new { MaNguoiDung = id }));
            }

            existingUser.TenDangNhap = model.TenDangNhap;
            existingUser.HoTen = model.HoTen;
            existingUser.Email = model.Email;
            existingUser.VaiTro = model.VaiTro;
            existingUser.BiKhoa = model.BiKhoa;
            existingUser.MaKhoa = model.MaKhoa;
            if (!string.IsNullOrWhiteSpace(model.MatKhau))
            {
                existingUser.MatKhau = model.MatKhau; 
            }

            var updatedUser = await _service.UpdateAsync(existingUser.MaNguoiDung, existingUser);
            var userDto = MapToDto(updatedUser);

            _logger.LogInformation("Cập nhật thành công người dùng: {TenDangNhap}", updatedUser.TenDangNhap);
            return Ok(ApiResponseFactory.Updated(userDto, "Cập nhật người dùng thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật người dùng có mã: {MaNguoiDung}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, ApiResponseFactory.ServerError());
        }
    }

    // DELETE: api/NguoiDung/{id}
    [HttpDelete("{id}")]
    [SwaggerOperation("Xóa người dùng (xóa cứng)")]
    public async Task<IActionResult> DeleteNguoiDungAsync(string id)
    {
        try
        {
            _logger.LogInformation("Yêu cầu xóa người dùng có mã: {MaNguoiDung}", id);

            var success = await _service.DeleteAsync(Guid.Parse(id));
            if (!success)
            {
                _logger.LogWarning("Không tìm thấy người dùng với mã: {MaNguoiDung}", id);
                return StatusCode(StatusCodes.Status404NotFound,
                    ApiResponseFactory.NotFound<NguoiDung>($"Không tìm thấy người dùng với mã {id}", new { MaNguoiDung = id }));
            }

            _logger.LogInformation("Đã xóa người dùng có mã: {MaNguoiDung}", id);
            return Ok(ApiResponseFactory.Success($"Đã xóa người dùng với mã {id} thành công."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xóa người dùng với mã: {MaNguoiDung}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, ApiResponseFactory.ServerError("Đã xảy ra lỗi khi xóa người dùng."));
        }
    }

    // PATCH: api/NguoiDung/{id}/Khoa
    [HttpPatch("{id}/Khoa")]
    [SwaggerOperation("Khóa tài khoản người dùng")]
    public async Task<IActionResult> LockUser(string id)
    {
        var success = await _service.SetUserLockStateAsync(Guid.Parse(id), true);
        if (!success)
        {
            return StatusCode(StatusCodes.Status404NotFound,
                ApiResponseFactory.NotFound<NguoiDung>($"Không tìm thấy người dùng với mã {id}"));
        }
        return Ok(ApiResponseFactory.Success($"Đã khóa tài khoản thành công"));
    }

    // PATCH: api/NguoiDung/{id}/MoKhoa
    [HttpPatch("{id}/MoKhoa")]
    [SwaggerOperation("Mở khóa tài khoản người dùng")]
    public async Task<IActionResult> UnlockUser(string id)
    {
        var success = await _service.SetUserLockStateAsync(Guid.Parse(id), false);
        if (!success)
        {
            return StatusCode(StatusCodes.Status404NotFound,
                ApiResponseFactory.NotFound<NguoiDung>($"Không tìm thấy người dùng với mã {id}"));
        }
        return Ok(ApiResponseFactory.Success($"Đã mở khóa tài khoản thành công"));
    }

    // GET: api/NguoiDung/paged
    [HttpGet("paged")]
    [SwaggerOperation("Lấy danh sách người dùng có phân trang, lọc, sắp xếp")]
    public async Task<IActionResult> GetPagedAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sort = null,
        [FromQuery] string? filter = null,
        [FromQuery] bool? biKhoa = null)
    {
        try
        {
            var query = (await _service.GetAllAsync()).AsQueryable();

            // Lọc theo trạng thái khóa
            if (biKhoa.HasValue)
            {
                query = query.Where(u => u.BiKhoa == biKhoa.Value);
            }

            // Lọc theo từ khóa
            if (!string.IsNullOrWhiteSpace(filter))
            {
                query = query.Where(u =>
                    u.TenDangNhap.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    u.HoTen.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    u.Email.Contains(filter, StringComparison.OrdinalIgnoreCase)
                );
            }

            // Sắp xếp
            if (!string.IsNullOrWhiteSpace(sort))
            {
                var parts = sort.Split(',');
                var column = parts[0].Trim();
                var direction = parts.Length > 1 ? parts[1].Trim() : "asc";

                query = (column, direction) switch
                {
                    ("TenDangNhap", "asc") => query.OrderBy(u => u.TenDangNhap),
                    ("TenDangNhap", "desc") => query.OrderByDescending(u => u.TenDangNhap),
                    ("HoTen", "asc") => query.OrderBy(u => u.HoTen),
                    ("HoTen", "desc") => query.OrderByDescending(u => u.HoTen),
                    ("Email", "asc") => query.OrderBy(u => u.Email),
                    ("Email", "desc") => query.OrderByDescending(u => u.Email),
                    _ => query.OrderBy(u => u.HoTen)
                };
            }
            else
            {
                query = query.OrderBy(u => u.HoTen);
            }

            var totalCount = query.Count();

            var items = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(MapToDto)
                .ToList();

            var result = new PagedResult<NguoiDungDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return Ok(ApiResponseFactory.Success(result, "Lấy danh sách người dùng phân trang thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách người dùng phân trang");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponseFactory.ServerError("Đã xảy ra lỗi khi xử lý."));
        }
    }

    // Helper: Map NguoiDung -> NguoiDungDto
    private static NguoiDungDto MapToDto(NguoiDung user)
    {
        return new NguoiDungDto
        {
            MaNguoiDung = user.MaNguoiDung,
            TenDangNhap = user.TenDangNhap,
            HoTen = user.HoTen,
            Email = user.Email,
            VaiTro = user.VaiTro,
            BiKhoa = user.BiKhoa,
            MaKhoa = user.MaKhoa,
            NgayTao = user.NgayTao,
            NgayCapNhat = user.NgayCapNhat,
            NgayDangNhapCuoi = user.NgayDangNhapCuoi
        };
    }
}