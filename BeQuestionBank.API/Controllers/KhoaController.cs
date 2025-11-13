using BeQuestionBank.Domain.Models;
using BeQuestionBank.Shared.DTOs.Common;
using BeQuestionBank.Shared.DTOs.Khoa;
using BeQuestionBank.Shared.DTOs.MonHoc;
using BEQuestionBank.Core.Services;
using BeQuestionBank.Shared.DTOs.Pagination;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BeQuestionBank.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class KhoaController(KhoaService service, ILogger<KhoaController> logger) : ControllerBase
{
    private readonly KhoaService _service = service;
    private readonly ILogger<KhoaController> _logger = logger;

    // GET: api/Khoa/{id}
    [HttpGet("{id}")]
    [SwaggerOperation("Tìm Khoa theo Mã Khoa")]
    public async Task<ActionResult<Khoa>> GetKhoaByIdAsync(string id)
    {
        try
        {
            _logger.LogInformation("Yêu cầu tìm Khoa theo mã: {MaKhoa}", id);

            var khoa = await _service.GetKhoaByIdAsync(Guid.Parse(id));
            if (khoa == null)
            {
                _logger.LogWarning("Không tìm thấy Khoa với mã: {MaKhoa}", id);
                return NotFound(ApiResponseFactory.NotFound<Khoa>(
                  $"Không tìm thấy Khoa với mã {id}",
                  new { MaKhoa = id }
                 ));
            }

            _logger.LogInformation("Tìm thấy Khoa: {TenKhoa}", khoa.TenKhoa);
            return Ok(ApiResponseFactory.Success(khoa, "Lấy dữ liệu thành công"));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Lỗi khi lấy thông tin Khoa theo mã: {MaKhoa}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                ApiResponseFactory.ServerError("Đã xảy ra lỗi trong quá trình xử lý."));
        }
    }

    // GET: api/Khoa
    [HttpGet]
    [SwaggerOperation("Lấy danh sách tất cả Khoa")]
    public async Task<ActionResult<KhoaDto>> GetAllAsync()
    {
        var list = await _service.GetAllKhoasAsync(); // Khoa có Include DanhSachMonHoc

        var result = list.Select(k => new KhoaDto
        {
            MaKhoa = k.MaKhoa,
            TenKhoa = k.TenKhoa,
            XoaTam = k.XoaTam,
            MoTa = k.MoTa,
            NgayTao = k.NgayTao,
            NgayCapNhat = k.NgayCapNhat,
            DanhSachMonHoc = k.MonHocs?.Select(m => new MonHocDto
            {
                MaSoMonHoc = m.MaSoMonHoc,
                MaMonHoc = m.MaMonHoc,
                TenMonHoc = m.TenMonHoc,
                SoTinChi = m.SoTinChi
            }).ToList() ?? new()
        });

        return Ok(ApiResponseFactory.Success(result, "Lấy danh sách Khoa thành công"));
    }

    //POST: api/Khoa
    [HttpPost]
    [SwaggerOperation("Thêm một Khoa")]
    public async Task<IActionResult> CreateKhoaAsync([FromBody] CreateKhoaDto model)
    {
        try
        {
            _logger.LogInformation("Bắt đầu thêm Khoa. Tên: {TenKhoa}", model.TenKhoa);

            var existingKhoa = await _service.GetKhoaByTenKhoaAsync(model.TenKhoa);
            if (existingKhoa != null)
            {
                if (existingKhoa.XoaTam == true)
                {
                    _logger.LogInformation("Khoa tạm xóa được phát hiện: {TenKhoa}", existingKhoa.TenKhoa);
                    return StatusCode(StatusCodes.Status409Conflict, new KhoaDto
                    {
                        MaKhoa = existingKhoa.MaKhoa,
                        TenKhoa = existingKhoa.TenKhoa,
                        XoaTam = existingKhoa.XoaTam
                    });
                }

                _logger.LogWarning("Khoa với tên {TenKhoa} đã tồn tại.", model.TenKhoa);
                return StatusCode(StatusCodes.Status409Conflict, $"Khoa với tên {model.TenKhoa} đã tồn tại.");
            }

            var newKhoa = new Khoa
            {
                TenKhoa = model.TenKhoa,
                XoaTam = model.XoaTam ?? false
            };

            await _service.AddKhoaAsync(newKhoa);
            _logger.LogInformation("Thêm Khoa thành công: {TenKhoa}", model.TenKhoa);

            return StatusCode(StatusCodes.Status201Created, ApiResponseFactory.Created(new KhoaDto
            {
                MaKhoa = newKhoa.MaKhoa,
                TenKhoa = newKhoa.TenKhoa,
                XoaTam = newKhoa.XoaTam
            }, "Thêm Khoa thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Có lỗi xảy ra khi thêm Khoa: {TenKhoa}", model.TenKhoa);
            return StatusCode(StatusCodes.Status500InternalServerError, ApiResponseFactory.ServerError("Đã xảy ra lỗi khi thêm Khoa."));
        }
    }

    //PATCH: api/Khoa/{id}
    [HttpPatch("{id}")]
    [SwaggerOperation(Summary = "Cập nhật thông tin Khoa")]
    public async Task<IActionResult> UpdateKhoaAsync(string id, [FromBody] UpdateKhoaDto model)
    {
        try
        {
            _logger.LogInformation("Yêu cầu cập nhật Khoa có mã: {MaKhoa}", id);

            var existingKhoa = await _service.GetKhoaByIdAsync(Guid.Parse(id));
            if (existingKhoa == null)
            {
                _logger.LogWarning("Không tìm thấy Khoa với mã: {MaKhoa}", id);
                return StatusCode(StatusCodes.Status404NotFound, ApiResponseFactory.NotFound<Khoa>($"Không tìm thấy Khoa với mã {id}", new {MaKhoa = id}));
            }

            existingKhoa.TenKhoa = model.TenKhoa;
            existingKhoa.XoaTam = model.XoaTam ?? existingKhoa.XoaTam;

            await _service.UpdateKhoaAsync(existingKhoa);

            var khoaDto = new KhoaDto
            {
                MaKhoa = existingKhoa.MaKhoa,
                TenKhoa = existingKhoa.TenKhoa,
                XoaTam = existingKhoa.XoaTam
            };

            _logger.LogInformation("Cập nhật thành công Khoa: {TenKhoa}", existingKhoa.TenKhoa);

            return StatusCode(StatusCodes.Status200OK, ApiResponseFactory.Updated(khoaDto, "Cập nhật Khoa thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật Khoa có mã: {MaKhoa}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, ApiResponseFactory.ServerError());
        }
    }

    // DELETE: api/Khoa/{id}
    [HttpDelete("{id}")]
    [SwaggerOperation(Summary = "Xóa Khoa (xóa cứng)")]
    public async Task<IActionResult> DeleteKhoaAsync(string id)
    {
        try
        {
            _logger.LogInformation("Yêu cầu xóa Khoa có mã: {MaKhoa}", id);

            var existingKhoa = await _service.GetKhoaByIdAsync(Guid.Parse(id));
            if (existingKhoa == null)
            {
                _logger.LogWarning("Không tìm thấy Khoa với mã: {MaKhoa}", id);
                return StatusCode(StatusCodes.Status404NotFound, ApiResponseFactory.NotFound<Khoa>($"Không tìm thấy Khoa với mã {id}", new { MaKhoa = id }));
            }

            await _service.DeleteKhoaAsync(existingKhoa);
            _logger.LogInformation("Đã xóa Khoa có mã: {MaKhoa}", id);

            return StatusCode(StatusCodes.Status200OK, ApiResponseFactory.Success($"Đã xóa Khoa với mã {id} thành công."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xóa Khoa với mã: {MaKhoa}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, ApiResponseFactory.ServerError("Đã xảy ra lỗi khi xóa Khoa."));
        }
    }

    // PATCH: api/Khoa/{id}/XoaTam
    [HttpPatch("{id}/XoaTam")]
    [SwaggerOperation(Summary = "Xóa Khoa tạm thời")]
    public async Task<IActionResult> SoftDelete(string id)
    {
        var existingKhoa = await _service.GetKhoaByIdAsync(Guid.Parse(id));
        if (existingKhoa == null)
        {
            return StatusCode(StatusCodes.Status404NotFound, ApiResponseFactory.NotFound<Khoa>($"Không tìm thấy Khoa với mã {id}"));
        }
        existingKhoa.XoaTam = true;
        await _service.UpdateKhoaAsync(existingKhoa);
        return StatusCode(StatusCodes.Status200OK, ApiResponseFactory.Success($"Đã xóa tạm {existingKhoa.TenKhoa} thành công"));
    }

    // PATCH: api/Khoa/{id}/KhoiPhuc
    [HttpPatch("{id}/KhoiPhuc")]
    [SwaggerOperation(Summary = "Khôi phục Khoa đã xóa tạm")]
    public async Task<IActionResult> Restore(string id)
    {
        var existingKhoa = await _service.GetKhoaByIdAsync(Guid.Parse(id));
        if (existingKhoa == null)
        {
            return StatusCode(StatusCodes.Status404NotFound, ApiResponseFactory.NotFound<Khoa>($"Không tìm thấy Khoa với mã {id}"));
        }
        existingKhoa.XoaTam = false;
        await _service.UpdateKhoaAsync(existingKhoa);
        return StatusCode(StatusCodes.Status200OK, ApiResponseFactory.Success($"Đã khôi phục {existingKhoa.TenKhoa} thành công"));
    }
    
    // GET: api/Khoa/paged
    [HttpGet("paged")]
    [SwaggerOperation("Lấy danh sách Khoa có phân trang, filter, sort")]
    public async Task<IActionResult> GetPagedAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sort = null,
        [FromQuery] string? search = null)
    {
        try
        {
            var query = await _service.GetAllKhoasAsync(); // Trả về IQueryable hoặc List<Khoa>

            // Filtering
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(k => k.TenKhoa.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            // Sorting
            if (!string.IsNullOrWhiteSpace(sort))
            {
                var parts = sort.Split(',');
                var column = parts[0];
                var direction = parts.Length > 1 ? parts[1] : "asc";

                query = column switch
                {
                    "TenKhoa" when direction == "asc" => query.OrderBy(k => k.TenKhoa),
                    "TenKhoa" when direction == "desc" => query.OrderByDescending(k => k.TenKhoa),
                    _ => query.OrderBy(k => k.TenKhoa)
                };
            }

            var totalCount = query.Count();

            var items = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(k => new KhoaDto
                {
                    MaKhoa = k.MaKhoa,
                    TenKhoa = k.TenKhoa,
                    XoaTam = k.XoaTam,
                    MoTa = k.MoTa,
                    DanhSachMonHoc = k.MonHocs?.Select(m => new MonHocDto
                    {
                        MaSoMonHoc = m.MaSoMonHoc,
                        MaMonHoc = m.MaMonHoc,
                        TenMonHoc = m.TenMonHoc,
                        SoTinChi = m.SoTinChi
                    }).ToList() ?? new()
                })
                .ToList();

            var result = new PagedResult<KhoaDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return Ok(ApiResponseFactory.Success(result, "Lấy danh sách Khoa có phân trang thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách Khoa có phân trang");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponseFactory.ServerError("Đã xảy ra lỗi khi xử lý."));
        }
    }
}
