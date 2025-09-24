using BEQuestionBank.Core.Services;
using BeQuestionBank.Shared.DTOs.Common;
using BEQuestionBank.Shared.DTOs.DeThi;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BeQuestionBank.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DeThiController : ControllerBase
{
    private readonly DeThiService _service;
    private readonly ILogger<DeThiController> _logger;

    public DeThiController(DeThiService service, ILogger<DeThiController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // GET: api/DeThi/{id}
    [HttpGet("{id}")]
    [SwaggerOperation("Lấy Đề Thi theo ID")]
    public async Task<IActionResult> GetDeThiByIdAsync(Guid id)
    {
        try
        {
            if (id == Guid.Empty)
            {
                return StatusCode(StatusCodes.Status400BadRequest, ApiResponseFactory.ValidationError<object>("ID không hợp lệ."));
            }
            var deThi = await _service.GetBasicByIdAsync(id);
            if (deThi == null)
            {
                return StatusCode(StatusCodes.Status404NotFound, ApiResponseFactory.NotFound<object>("Không tìm thấy đề thi với ID đã cho."));
            }
            return StatusCode(StatusCodes.Status200OK, ApiResponseFactory.Success<object>(deThi, "Lấy đề thi thành công!"));
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ApiResponseFactory.ServerError($"Lỗi hệ thống: {ex.Message}"));
        }
    }

    // GET: api/DeThi
    [HttpGet]
    [SwaggerOperation("Lấy tất cả Đề Thi")]
    public async Task<IActionResult> GetAllDeThisAsync()
    {
        try
        {
            var deThis = await _service.GetAllBasicAsync();
            if (deThis == null || !deThis.Any())
            {
                return StatusCode(StatusCodes.Status404NotFound, ApiResponseFactory.NotFound<object>("Không tìm thấy đề thi nào."));
            }
            return StatusCode(StatusCodes.Status200OK, ApiResponseFactory.Success<object>(deThis, "Lấy danh sách đề thi thành công!"));
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ApiResponseFactory.ServerError($"Lỗi hệ thống: {ex.Message}"));
        }
    }

    // GET: api/DeThi/{id}/WithChiTiet
    [HttpGet("{id}/WithChiTiet")]
    [SwaggerOperation("Lấy Đề Thi với Chi Tiết theo ID")]
    public async Task<IActionResult> GetDeThiWithChiTietByIdAsync(Guid id)
    {
        try
        {
            if (id == Guid.Empty)
            {
                return StatusCode(StatusCodes.Status400BadRequest, ApiResponseFactory.ValidationError<object>("ID không hợp lệ."));
            }
            var deThi = await _service.GetByIdWithChiTietAsync(id);
            if (deThi == null)
            {
                return StatusCode(StatusCodes.Status404NotFound, ApiResponseFactory.NotFound<object>("Không tìm thấy đề thi với ID đã cho."));
            }
            return StatusCode(StatusCodes.Status200OK, ApiResponseFactory.Success<object>(deThi, "Lấy đề thi với chi tiết thành công!"));
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ApiResponseFactory.ServerError($"Lỗi hệ thống: {ex.Message}"));
        }
    }

    // GET: api/DeThi/{id}/WithChiTietAndCauTraLoi
    [HttpGet("{id}/WithChiTietAndCauTraLoi")]
    [SwaggerOperation("Lấy Đề Thi với Chi Tiết và Câu Trả Lời theo ID")]
    public async Task<IActionResult> GetDeThiWithChiTietAndCauTraLoiByIdAsync(Guid id)
    {
        try
        {
            if (id == Guid.Empty)
            {
                return StatusCode(StatusCodes.Status400BadRequest, ApiResponseFactory.ValidationError<object>("ID không hợp lệ."));
            }
            var deThi = await _service.GetByIdWithChiTietAndCauTraLoiAsync(id);
            if (deThi == null)
            {
                return StatusCode(StatusCodes.Status404NotFound, ApiResponseFactory.NotFound<object>("Không tìm thấy đề thi với ID đã cho."));
            }
            return StatusCode(StatusCodes.Status200OK, ApiResponseFactory.Success<object>(deThi, "Lấy đề thi với chi tiết và câu trả lời thành công!"));
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ApiResponseFactory.ServerError($"Lỗi hệ thống: {ex.Message}"));
        }
    }
    // GET: api/DeThi/MonHoc/{maMonHoc}
[HttpGet("MonHoc/{maMonHoc}")]
[SwaggerOperation("Lấy danh sách đề thi theo mã môn học")]
public async Task<IActionResult> GetByMaMonHocAsync(Guid maMonHoc)
{
    try
    {
        if (maMonHoc == Guid.Empty)
        {
            return StatusCode(StatusCodes.Status400BadRequest,
                ApiResponseFactory.ValidationError<object>("Mã môn học không hợp lệ."));
        }

        var deThis = await _service.GetByMaMonHocAsync(maMonHoc);
        if (deThis == null || !deThis.Any())
        {
            return StatusCode(StatusCodes.Status404NotFound,
                ApiResponseFactory.NotFound<object>($"Không tìm thấy đề thi nào với mã môn học: {maMonHoc}"));
        }

        return StatusCode(StatusCodes.Status200OK,
            ApiResponseFactory.Success<object>(deThis, "Lấy danh sách đề thi thành công!"));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Lỗi khi lấy danh sách đề thi theo mã môn học: {maMonHoc}", maMonHoc);
        return StatusCode(StatusCodes.Status500InternalServerError,
            ApiResponseFactory.ServerError($"Lỗi hệ thống: {ex.Message}"));
    }
}

// GET: api/DeThi/Approved
[HttpGet("Approved")]
[SwaggerOperation("Lấy danh sách đề thi đã được duyệt")]
public async Task<IActionResult> GetApprovedDeThisAsync()
{
    try
    {
        var deThis = await _service.GetApprovedDeThisAsync();
        if (deThis == null || !deThis.Any())
        {
            return StatusCode(StatusCodes.Status404NotFound,
                ApiResponseFactory.NotFound<object>("Không tìm thấy đề thi đã được duyệt."));
        }

        return StatusCode(StatusCodes.Status200OK,
            ApiResponseFactory.Success<object>(deThis, "Lấy danh sách đề thi đã được duyệt thành công!"));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Lỗi khi lấy danh sách đề thi đã được duyệt.");
        return StatusCode(StatusCodes.Status500InternalServerError,
            ApiResponseFactory.ServerError($"Lỗi hệ thống: {ex.Message}"));
    }
}

   
    // POST: api/DeThi
    [HttpPost]
    [SwaggerOperation("Thêm Đề Thi mới")]
    public async Task<IActionResult> AddDeThi([FromBody] CreateDeThiDto deThiCreateDto)
    {
        if (deThiCreateDto == null)
        {
            return StatusCode(StatusCodes.Status400BadRequest, 
                ApiResponseFactory.ValidationError<object>("Dữ liệu không hợp lệ."));
        }
        try
        {
            var (success, message, maDeThi) = await _service.AddAsync(deThiCreateDto);
            if (!success)
            {
                return StatusCode(StatusCodes.Status400BadRequest, 
                    ApiResponseFactory.ValidationError<object>(message));
            }

            // ✅ Tạo response object mới có cả ID
            var responseData = new
            {
                MaDeThi = maDeThi,
                deThiCreateDto.MaMonHoc,
                deThiCreateDto.TenDeThi,
                deThiCreateDto.DaDuyet,
                deThiCreateDto.ChiTietDeThis
            };

            return StatusCode(StatusCodes.Status201Created, 
                ApiResponseFactory.Success(responseData, "Thêm đề thi thành công!"));
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, 
                ApiResponseFactory.ServerError($"Lỗi hệ thống: {ex.Message}"));
        }
    }


    // PATCH: api/DeThi/{id}
    [HttpPatch("{id}")]
    [SwaggerOperation("Cập nhật Đề Thi")]
    public async Task<IActionResult> UpdateDeThi(Guid id, [FromBody] UpdateDeThiDto deThiUpdateDto)
    {
        if (deThiUpdateDto == null || id == Guid.Empty)
        {
            return StatusCode(StatusCodes.Status400BadRequest, ApiResponseFactory.ValidationError<object>("Dữ liệu không hợp lệ hoặc ID không hợp lệ."));
        }
        try
        {
            var (success, message) = await _service.UpdateAsync(id, deThiUpdateDto);
            if (!success)
            {
                return StatusCode(StatusCodes.Status400BadRequest, ApiResponseFactory.ValidationError<object>(message));
            }

            return StatusCode(StatusCodes.Status200OK, ApiResponseFactory.Success<object>(deThiUpdateDto, "Cập nhật đề thi thành công!"));
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ApiResponseFactory.ServerError($"Lỗi hệ thống: {ex.Message}"));
        }
    }

    // DELETE: api/DeThi/{id}
    [HttpDelete("{id}")]
    [SwaggerOperation("Xóa Đề Thi")]
    public async Task<IActionResult> DeleteDeThi(Guid id)
    {
        if (id == Guid.Empty)
        {
            return StatusCode(StatusCodes.Status400BadRequest, ApiResponseFactory.ValidationError<object>("ID không hợp lệ."));
        }
        try
        {
            var (success, message) = await _service.DeleteAsync(id);
            if (!success)
            {
                return StatusCode(StatusCodes.Status404NotFound, ApiResponseFactory.NotFound<object>(message));
            }
            return StatusCode(StatusCodes.Status200OK, ApiResponseFactory.Success<object>("Xóa đề thi thành công!"));
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ApiResponseFactory.ServerError($"Lỗi hệ thống: {ex.Message}"));
        }
    }
}