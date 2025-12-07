using BeQuestionBank.Shared.DTOs.Common;
using BeQuestionBank.Shared.DTOs.CauHoi;
using BeQuestionBank.Shared.DTOs.File;
using BeQuestionBank.Shared.DTOs.Pagination;
using BeQuestionBank.Shared.Enums;
using BEQuestionBank.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BeQuestionBank.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class FileController : ControllerBase
{
    private readonly FileService _fileService;
    private readonly CauHoiService _cauHoiService;
    private readonly ILogger<FileController> _logger;

    public FileController(FileService fileService, CauHoiService cauHoiService, ILogger<FileController> logger)
    {
        _fileService = fileService;
        _cauHoiService = cauHoiService;
        _logger = logger;
    }

    /// <summary>
    /// Lấy danh sách file có phân trang
    /// </summary>
    [HttpGet]
    [SwaggerOperation("Lấy danh sách file có phân trang")]
    public async Task<IActionResult> GetFilesPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sort = null,
        [FromQuery] string? search = null,
        [FromQuery] int? loaiFile = null)
    {
        try
        {
            FileType? fileType = loaiFile.HasValue ? (FileType?)loaiFile.Value : null;
            var result = await _fileService.GetFilesPagedAsync(page, pageSize, sort, search, fileType);
            return Ok(ApiResponseFactory.Success(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách file");
            return StatusCode(500, ApiResponseFactory.ServerError(ex.Message));
        }
    }

    /// <summary>
    /// Xóa file
    /// </summary>
    [HttpDelete("{id}")]
    [SwaggerOperation("Xóa file")]
    public async Task<IActionResult> DeleteFile(Guid id)
    {
        try
        {
            if (id == Guid.Empty)
            {
                return BadRequest(ApiResponseFactory.ValidationError<bool>("ID không hợp lệ"));
            }

            var result = await _fileService.DeleteFileAsync(id);
            if (result)
            {
                return Ok(ApiResponseFactory.Success(true, "Xóa file thành công"));
            }
            else
            {
                return NotFound(ApiResponseFactory.NotFound<bool>("Không tìm thấy file"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Lỗi khi xóa file {id}");
            return StatusCode(500, ApiResponseFactory.ServerError(ex.Message));
        }
    }

    /// <summary>
    /// Lấy câu hỏi liên kết với file
    /// </summary>
    [HttpGet("{id}/cauhoi")]
    [SwaggerOperation("Lấy câu hỏi liên kết với file")]
    public async Task<IActionResult> GetCauHoiByFileId(Guid id)
    {
        try
        {
            if (id == Guid.Empty)
            {
                return BadRequest(ApiResponseFactory.ValidationError<object>("ID không hợp lệ"));
            }

            var cauHoiId = await _fileService.GetCauHoiIdByFileIdAsync(id);
            if (!cauHoiId.HasValue)
            {
                return NotFound(ApiResponseFactory.NotFound<object>("Không tìm thấy câu hỏi liên kết với file này"));
            }

            // Lấy chi tiết câu hỏi
            var cauHoiDto = await _cauHoiService.GetByIdAsync(cauHoiId.Value);
            if (cauHoiDto == null)
            {
                return NotFound(ApiResponseFactory.NotFound<CauHoiWithCauTraLoiDto>("Không tìm thấy câu hỏi"));
            }

            return Ok(ApiResponseFactory.Success(cauHoiDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Lỗi khi lấy câu hỏi liên kết với file {id}");
            return StatusCode(500, ApiResponseFactory.ServerError(ex.Message));
        }
    }
}

