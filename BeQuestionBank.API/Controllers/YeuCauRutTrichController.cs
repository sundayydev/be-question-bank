using BEQuestionBank.Core.Services;
using BeQuestionBank.Shared.DTOs.Common;
using BeQuestionBank.Shared.DTOs.YeuCauRutTrich;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BeQuestionBank.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class YeuCauRutTrichController : ControllerBase
{
    private readonly YeuCauRutTrichService _service;
    private readonly ILogger<YeuCauRutTrichController> _logger;

    public YeuCauRutTrichController(YeuCauRutTrichService service, ILogger<YeuCauRutTrichController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // GET: api/YeuCauRutTrich/{id}
    [HttpGet("{id}")]
    [SwaggerOperation("Lấy yêu cầu rút trích theo ID")]
    public async Task<IActionResult> GetByIdAsync(Guid id)
    {
        try
        {
            if (id == Guid.Empty)
                return StatusCode(StatusCodes.Status400BadRequest,
                    ApiResponseFactory.ValidationError<object>("ID không hợp lệ."));

            var yeuCau = await _service.GetBasicByIdAsync(id);
            if (yeuCau == null)
                return StatusCode(StatusCodes.Status404NotFound,
                    ApiResponseFactory.NotFound<object>("Không tìm thấy yêu cầu rút trích."));

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseFactory.Success<object>(yeuCau, "Lấy yêu cầu rút trích thành công!"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy yêu cầu rút trích theo id {id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponseFactory.ServerError($"Lỗi hệ thống: {ex.Message}"));
        }
    }

    // GET: api/YeuCauRutTrich
    [HttpGet]
    [SwaggerOperation("Lấy tất cả yêu cầu rút trích")]
    public async Task<IActionResult> GetAllAsync()
    {
        try
        {
            var yeuCaus = await _service.GetAllBasicAsync();
            if (yeuCaus == null || !yeuCaus.Any())
                return StatusCode(StatusCodes.Status404NotFound,
                    ApiResponseFactory.NotFound<object>("Không có yêu cầu rút trích nào."));

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseFactory.Success<object>(yeuCaus, "Lấy danh sách yêu cầu rút trích thành công!"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy tất cả yêu cầu rút trích.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponseFactory.ServerError($"Lỗi hệ thống: {ex.Message}"));
        }
    }

    // GET: api/YeuCauRutTrich/NguoiDung/{maNguoiDung}
    [HttpGet("NguoiDung/{maNguoiDung}")]
    [SwaggerOperation("Lấy yêu cầu rút trích theo mã người dùng")]
    public async Task<IActionResult> GetByMaNguoiDungAsync(Guid maNguoiDung)
    {
        try
        {
            if (maNguoiDung == Guid.Empty)
                return StatusCode(StatusCodes.Status400BadRequest,
                    ApiResponseFactory.ValidationError<object>("Mã người dùng không hợp lệ."));

            var yeuCaus = await _service.GetByMaNguoiDungAsync(maNguoiDung);
            if (yeuCaus == null || !yeuCaus.Any())
                return StatusCode(StatusCodes.Status404NotFound,
                    ApiResponseFactory.NotFound<object>("Không có yêu cầu rút trích nào cho người dùng này."));

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseFactory.Success<object>(yeuCaus, "Lấy danh sách yêu cầu rút trích theo người dùng thành công!"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy yêu cầu rút trích theo mã người dùng {maNguoiDung}", maNguoiDung);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponseFactory.ServerError($"Lỗi hệ thống: {ex.Message}"));
        }
    }

    // GET: api/YeuCauRutTrich/MonHoc/{maMonHoc}
    [HttpGet("MonHoc/{maMonHoc}")]
    [SwaggerOperation("Lấy yêu cầu rút trích theo mã môn học")]
    public async Task<IActionResult> GetByMaMonHocAsync(Guid maMonHoc)
    {
        try
        {
            if (maMonHoc == Guid.Empty)
                return StatusCode(StatusCodes.Status400BadRequest,
                    ApiResponseFactory.ValidationError<object>("Mã môn học không hợp lệ."));

            var yeuCaus = await _service.GetByMaMonHocAsync(maMonHoc);
            if (yeuCaus == null || !yeuCaus.Any())
                return StatusCode(StatusCodes.Status404NotFound,
                    ApiResponseFactory.NotFound<object>("Không có yêu cầu rút trích nào cho môn học này."));

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseFactory.Success<object>(yeuCaus, "Lấy danh sách yêu cầu rút trích theo môn học thành công!"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy yêu cầu rút trích theo mã môn học {maMonHoc}", maMonHoc);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponseFactory.ServerError($"Lỗi hệ thống: {ex.Message}"));
        }
    }

    // GET: api/YeuCauRutTrich/TrangThai/{daXuLy}
    [HttpGet("TrangThai/{daXuLy}")]
    [SwaggerOperation("Lấy yêu cầu rút trích theo trạng thái xử lý")]
    public async Task<IActionResult> GetByTrangThaiAsync(bool daXuLy)
    {
        try
        {
            var yeuCaus = await _service.GetByTrangThaiAsync(daXuLy);
            if (yeuCaus == null || !yeuCaus.Any())
                return StatusCode(StatusCodes.Status404NotFound,
                    ApiResponseFactory.NotFound<object>("Không có yêu cầu rút trích nào với trạng thái này."));

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseFactory.Success<object>(yeuCaus, "Lấy danh sách yêu cầu rút trích theo trạng thái thành công!"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy yêu cầu rút trích theo trạng thái {daXuLy}", daXuLy);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponseFactory.ServerError($"Lỗi hệ thống: {ex.Message}"));
        }
    }
    
     [HttpPost]
    [SwaggerOperation("Thêm yêu cầu rút trích mới")]
    public async Task<IActionResult> AddAsync([FromBody] CreateYeuCauRutTrichDto dto)
    {
        if (dto == null)
            return StatusCode(StatusCodes.Status400BadRequest,
                ApiResponseFactory.ValidationError<object>("Dữ liệu không hợp lệ."));

        try
        {
            var (success, message, maYeuCau) = await _service.AddAsync(dto);
            if (!success)
                return StatusCode(StatusCodes.Status400BadRequest,
                    ApiResponseFactory.ValidationError<object>(message));

            var responseData = new
            {
                MaYeuCau = maYeuCau,
                dto.MaNguoiDung,
                dto.MaMonHoc,
                dto.NoiDungRutTrich,
                dto.GhiChu
            };

            return StatusCode(StatusCodes.Status201Created,
                ApiResponseFactory.Success(responseData, "Thêm yêu cầu rút trích thành công!"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi thêm yêu cầu rút trích.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponseFactory.ServerError($"Lỗi hệ thống: {ex.Message}"));
        }
    }

    // PATCH: api/YeuCauRutTrich/{id}
    [HttpPatch("{id}")]
    [SwaggerOperation("Cập nhật yêu cầu rút trích")]
    public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] YeuCauRutTrichDto dto)
    {
        if (dto == null || id == Guid.Empty)
            return StatusCode(StatusCodes.Status400BadRequest,
                ApiResponseFactory.ValidationError<object>("Dữ liệu không hợp lệ hoặc ID không hợp lệ."));

        try
        {
            var (success, message) = await _service.UpdateAsync(id, dto);
            if (!success)
                return StatusCode(StatusCodes.Status400BadRequest,
                    ApiResponseFactory.ValidationError<object>(message));

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseFactory.Success<object>(dto, "Cập nhật yêu cầu rút trích thành công!"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật yêu cầu rút trích {id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponseFactory.ServerError($"Lỗi hệ thống: {ex.Message}"));
        }
    }

    // DELETE: api/YeuCauRutTrich/{id}
    [HttpDelete("{id}")]
    [SwaggerOperation("Xóa yêu cầu rút trích")]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        if (id == Guid.Empty)
            return StatusCode(StatusCodes.Status400BadRequest,
                ApiResponseFactory.ValidationError<object>("ID không hợp lệ."));

        try
        {
            var (success, message) = await _service.DeleteAsync(id);
            if (!success)
                return StatusCode(StatusCodes.Status404NotFound,
                    ApiResponseFactory.NotFound<object>(message));

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseFactory.Success<object>("Xóa yêu cầu rút trích thành công!"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xóa yêu cầu rút trích {id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponseFactory.ServerError($"Lỗi hệ thống: {ex.Message}"));
        }
    }
}
