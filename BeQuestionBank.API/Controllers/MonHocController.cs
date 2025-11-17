using BeQuestionBank.Domain.Models;
using BeQuestionBank.Shared.DTOs.Common;
using BeQuestionBank.Shared.DTOs.MonHoc;
using BEQuestionBank.Core.Services;
using BeQuestionBank.Shared.DTOs.Pagination;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BeQuestionBank.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MonHocController(MonHocService service, ILogger<MonHocController> logger) : ControllerBase
{
    private readonly MonHocService _service = service;
    private readonly ILogger<MonHocController> _logger = logger;

    // GET: api/MonHoc/{id}
    [HttpGet("{id}")]
    [SwaggerOperation("Tìm môn học theo mã")]
    public async Task<ActionResult<MonHoc>> GetByIdAsync(string id)
    {
        try
        {
            var monHoc = await _service.GetMonHocByIdAsync(Guid.Parse(id));
            if (monHoc == null)
                return StatusCode(StatusCodes.Status404NotFound, ApiResponseFactory.NotFound<Object>($"Không tìm thấy môn học với mã {id}"));

            return StatusCode(StatusCodes.Status200OK, monHoc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tìm môn học");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiResponseFactory.ServerError());
        }
    }

    // GET: api/MonHoc
    [HttpGet]
    [SwaggerOperation("Lấy danh sách tất cả môn học")]
    public async Task<ActionResult<MonHoc>> GetAllAsync()
    {
        var list = await _service.GetAllMonHocsAsync();
        if (list == null || !list.Any())
            return StatusCode(StatusCodes.Status404NotFound, ApiResponseFactory.NotFound<Object>("Không có môn học nào trong hệ thống"));
        return StatusCode(StatusCodes.Status200OK, ApiResponseFactory.Success<Object>(list, "Lấy danh sách môn học thành công"));
    }

    // POST: api/MonHoc
    [HttpGet("khoa/{maKhoa}")]
    [SwaggerOperation("Lấy danh sách môn học theo mã khoa")]
    public async Task<ActionResult<ApiResponse<List<MonHocDto>>>> GetByMaKhoaAsync(string maKhoa)
    {
        try
        {
            if (!Guid.TryParse(maKhoa, out var khoaId))
            {
                _logger.LogError($"Mã khoa {maKhoa} không đúng định dạng GUID");
                return StatusCode(StatusCodes.Status400BadRequest, ApiResponseFactory.ValidationError<object>("Mã khoa không đúng định dạng GUID."));
            }

            var list = await _service.GetMonHocsByMaKhoaAsync(khoaId);
            if (list == null || !list.Any())
            {
                _logger.LogInformation($"Không tìm thấy môn học nào cho mã khoa {maKhoa}");
                return StatusCode(StatusCodes.Status404NotFound, ApiResponseFactory.NotFound<object>($"Không tìm thấy môn học nào cho mã khoa {maKhoa}"));
            }

            var monHocDtos = list
                .Where(m => m.XoaTam == false) // Chỉ lấy các môn chưa bị xóa tạm
                .Select(m => new MonHocDto
                {
                    MaMonHoc = m.MaMonHoc,
                    MaSoMonHoc = m.MaSoMonHoc,
                    TenMonHoc = m.TenMonHoc,
                    MaKhoa = m.MaKhoa,
                    XoaTam = m.XoaTam
                })
                .ToList();

            _logger.LogInformation($"Số môn học tìm thấy cho MaKhoa {maKhoa}: {monHocDtos.Count}");
            return StatusCode(StatusCodes.Status200OK, ApiResponseFactory.Success<List<MonHocDto>>(monHocDtos, "Lấy danh sách môn học theo mã khoa thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách môn học theo mã khoa {maKhoa}", maKhoa);
            return StatusCode(StatusCodes.Status500InternalServerError, ApiResponseFactory.ServerError($"Lỗi: {ex.Message}"));
        }
    }

    // POST: api/MonHoc
    [HttpPost]
    [SwaggerOperation("Thêm mới môn học")]
    public async Task<IActionResult> CreateAsync([FromBody] CreateMonHocDto model)
    {
        try
        {
            var monHoc = new MonHoc
            {
                MaSoMonHoc = model.MaSoMonHoc,
                TenMonHoc = model.TenMonHoc,
                MaKhoa = model.MaKhoa,
                XoaTam = model.XoaTam ?? false
            };

            await _service.AddMonHocAsync(monHoc);
            return StatusCode(StatusCodes.Status201Created, ApiResponseFactory.Success<Object>(model, "Thêm môn học mới thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi thêm môn học");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiResponseFactory.ServerError());
        }
    }

    // PATCH: api/MonHoc
    [HttpPatch("{id}")]
    [SwaggerOperation("Cập nhật thông tin môn học")]
    public async Task<IActionResult> UpdateAsync(string id, [FromBody] UpdateMonHocDto model)
    {
        try
        {
            var monHoc = await _service.GetMonHocByIdAsync(Guid.Parse(id));
            if (monHoc == null)
                return StatusCode(StatusCodes.Status404NotFound, ApiResponseFactory.NotFound<object>($"Không tìm thấy môn học với mã {id}"));

            monHoc.TenMonHoc = model.TenMonHoc;
            monHoc.MaKhoa = model.MaKhoa;
            monHoc.XoaTam = model.XoaTam ?? monHoc.XoaTam;

            await _service.UpdateMonHocAsync(monHoc);

            return StatusCode(StatusCodes.Status200OK, new MonHocDto
            {
                MaSoMonHoc = monHoc.MaSoMonHoc,
                MaMonHoc = monHoc.MaMonHoc,
                TenMonHoc = monHoc.TenMonHoc,
                MaKhoa = monHoc.MaKhoa,
                XoaTam = monHoc.XoaTam
            });
        }
        catch (FormatException)
        {
            return StatusCode(StatusCodes.Status400BadRequest, ApiResponseFactory.ValidationError<object>("ID không đúng định dạng GUID."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật môn học với mã {MaMonHoc}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, ApiResponseFactory.ServerError($"Lỗi khi cập nhật môn học: {ex.Message}"));
        }
    }

    // DELETE: api/MonHoc/{id}
    [HttpDelete("{id}")]
    [SwaggerOperation("Xóa môn học")]
    public async Task<IActionResult> DeleteAsync(string id)
    {
        var monHoc = await _service.GetMonHocByIdAsync(Guid.Parse(id));
        if (monHoc == null)
            return StatusCode(StatusCodes.Status404NotFound, ApiResponseFactory.NotFound<Object>($"Không tìm thấy môn học với mã {id}"));

        await _service.DeleteMonHocAsync(monHoc);
        return StatusCode(StatusCodes.Status200OK, ApiResponseFactory.Success($"Đã xóa môn học có mã {id}"));
    }

    // PATCH: api/MonHoc/{id}/XoaTam
    [HttpPatch("{id}/XoaTam")]
    [SwaggerOperation("Xóa tạm thời môn học")]
    public async Task<IActionResult> SoftDelete(string id)
    {
        var monHoc = await _service.GetMonHocByIdAsync(Guid.Parse(id));
        if (monHoc == null)
            return StatusCode(StatusCodes.Status404NotFound, ApiResponseFactory.NotFound<Object>($"Không tìm thấy môn học với mã {id}"));

        monHoc.XoaTam = true;
        await _service.UpdateMonHocAsync(monHoc);
        return StatusCode(StatusCodes.Status200OK, ApiResponseFactory.Success($"Đã xóa tạm thời môn học: {monHoc.TenMonHoc}"));
    }

    // PATCH: api/MonHoc/{id}/KhoiPhuc
    [HttpPatch("{id}/KhoiPhuc")]
    [SwaggerOperation("Khôi phục môn học đã xóa tạm")]
    public async Task<IActionResult> Restore(string id)
    {
        var monHoc = await _service.GetMonHocByIdAsync(Guid.Parse(id));
        if (monHoc == null)
            return StatusCode(StatusCodes.Status404NotFound, ApiResponseFactory.NotFound<Object>($"Không tìm thấy môn học với mã {id}"));

        monHoc.XoaTam = false;
        await _service.UpdateMonHocAsync(monHoc);
        return StatusCode(StatusCodes.Status200OK, ApiResponseFactory.Success($"Đã khôi phục môn học: {monHoc.TenMonHoc}"));
    }
    [HttpGet("paged")]
[SwaggerOperation("Lấy danh sách Môn học có phân trang, filter, sort")]
public async Task<IActionResult> GetPagedAsync(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] string? sort = null,
    [FromQuery] string? search = null)
{
    try
    {
        var query = await _service.GetAllMonHocsAsync();
        
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(m => 
                m.TenMonHoc.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                m.MaSoMonHoc.Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        // Sorting
        if (!string.IsNullOrWhiteSpace(sort))
        {
            var parts = sort.Split(',');
            var column = parts[0];
            var direction = parts.Length > 1 ? parts[1] : "asc";

            query = column switch
            {
                "TenMonHoc" when direction == "asc" => query.OrderBy(m => m.TenMonHoc),
                "TenMonHoc" when direction == "desc" => query.OrderByDescending(m => m.TenMonHoc),
                _ => query.OrderBy(m => m.TenMonHoc)
            };
        }

        var totalCount = query.Count();

        var items = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new MonHocDto
            {
                MaMonHoc = m.MaMonHoc,
                MaSoMonHoc = m.MaSoMonHoc,
                TenMonHoc = m.TenMonHoc,
                MaKhoa = m.MaKhoa,
                XoaTam = m.XoaTam
            })
            .ToList();

        var result = new PagedResult<MonHocDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        return Ok(ApiResponseFactory.Success(result, "Lấy danh sách môn học có phân trang thành công"));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Lỗi khi lấy danh sách Môn học có phân trang");
        return StatusCode(StatusCodes.Status500InternalServerError,
            ApiResponseFactory.ServerError("Đã xảy ra lỗi khi xử lý."));
    }
}
}

