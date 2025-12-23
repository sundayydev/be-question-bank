using BeQuestionBank.Shared.DTOs.Common;
using BeQuestionBank.Shared.DTOs.DeThi;
using BeQuestionBank.Shared.DTOs.Khoa;
using BeQuestionBank.Shared.DTOs.MonHoc;
using BeQuestionBank.Shared.DTOs.Pagination;
using BEQuestionBank.Core.Services;
using BEQuestionBank.Shared.DTOs.DeThi;
using BEQuestionBank.Shared.DTOs.MaTran;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BeQuestionBank.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DeThiController : ControllerBase
{
    private readonly DeThiExportForStudentService _exportService;
    private readonly DeThiService _service;
    private readonly DeThiTuLuanExportService _tuLuanExportService;
    private readonly DeThiExportService _ezpExportService;
    private readonly ILogger<DeThiController> _logger;

    public DeThiController(DeThiService service,
        DeThiTuLuanExportService tuLuanExportService, 
        DeThiExportForStudentService exportService,
        DeThiExportService ezpExportService,
        ILogger<DeThiController> logger)
    {
        _service = service;
        _exportService = exportService;
        _tuLuanExportService = tuLuanExportService;
        _ezpExportService = ezpExportService;
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

    // POST: api/YeuCauRutTrich/CheckQuestions
    [HttpPost("CheckQuestions")]
    [SwaggerOperation("Kiểm tra xem có đủ câu hỏi theo ma trận hay không")]
    public async Task<IActionResult> CheckQuestionsAsync([FromBody] MaTranDto maTran, [FromQuery] Guid maMonHoc)
    {
        if (maTran == null || maMonHoc == Guid.Empty)
        {
            return StatusCode(StatusCodes.Status400BadRequest,
                ApiResponseFactory.ValidationError<object>("Thiếu dữ liệu ma trận hoặc mã môn học."));
        }

        try
        {
            var (success, message, available) = await _service.CheckAvailableQuestionsAsync(maTran, maMonHoc);

            if (!success)
            {
                return StatusCode(StatusCodes.Status400BadRequest,
                    ApiResponseFactory.ValidationError<object>(
                        $"Không đủ câu hỏi. Tổng yêu cầu: {maTran.TotalQuestions}, Có: {available}. Chi tiết: {message}"
                    ));
            }

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseFactory.Success(new
                {
                    TongYeuCau = maTran.TotalQuestions,
                    SoCauHoiCo = available
                }, "Đủ câu hỏi để rút trích."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi kiểm tra câu hỏi cho môn học {MaMonHoc}", maMonHoc);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponseFactory.ServerError($"Lỗi hệ thống: {ex.Message}"));
        }
    }
    // GET: api/Khoa/paged
    [HttpGet("paged")]
    [SwaggerOperation("Lấy danh sách Khoa có phân trang, sort")]
    public async Task<IActionResult> GetPagedAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sort = null,
        [FromQuery] string? search = null)
    {
        try
        {
            var query = await _service.GetAllBasicAsync();

            // Filtering
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(k =>
                    k.TenDeThi.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (k.TenMonHoc != null && k.TenMonHoc.Contains(search, StringComparison.OrdinalIgnoreCase))
                );
            }


            // Sorting
            if (!string.IsNullOrWhiteSpace(sort))
            {
                var parts = sort.Split(',');
                var column = parts[0];
                var direction = parts.Length > 1 ? parts[1] : "asc";

                query = column switch
                {
                    "TenDeThi" when direction == "asc" => query.OrderBy(k => k.TenDeThi),
                    "TenDeThi" when direction == "desc" => query.OrderByDescending(k => k.TenDeThi),
                    "NgayTao" when direction == "asc" => query.OrderBy(k => k.NgayTao),
                    "NgayTao" when direction == "desc" => query.OrderByDescending(k => k.NgayTao),
                    _ => query.OrderBy(k => k.TenDeThi)
                };
            }

            var totalCount = query.Count();

            var items = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new DeThiDto
                {
                    MaDeThi = d.MaDeThi,
                    MaMonHoc = d.MaMonHoc,
                    TenDeThi = d.TenDeThi,
                    DaDuyet = d.DaDuyet,
                    SoCauHoi = d.SoCauHoi,
                    NgayTao = d.NgayTao,
                    TenMonHoc = d.TenMonHoc,
                    TenKhoa = d.TenKhoa,
                    NgayCapNhap = d.NgayCapNhap
                })
                .ToList();


            var result = new PagedResult<DeThiDto>
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
    [HttpPost("{id}/export")]
    [SwaggerOperation(Summary = "Xuất đề thi Word/PDF cho sinh viên", Description = "Hỗ trợ hoán vị đáp án và in kèm đáp án. Format: 'word' hoặc 'pdf'.")]
    [SwaggerResponse(200, "File đề thi", typeof(FileResult))]
    [SwaggerResponse(404, "Không tìm thấy đề thi")]
    [SwaggerResponse(500, "Lỗi server")]
    public async Task<IActionResult> ExportDeThi(Guid id, [FromBody] YeuCauXuatDeThiDto? request = null)
    {
        if (id == Guid.Empty)
            return BadRequest(ApiResponseFactory.ValidationError<object>("ID đề thi không hợp lệ."));

        request ??= new YeuCauXuatDeThiDto();
        request.MaDeThi = id;

        try
        {
            byte[] fileBytes;
            string contentType;
            string extension;

            if (request.Format?.Equals("pdf", StringComparison.OrdinalIgnoreCase) == true)
            {
                fileBytes = await _exportService.ExportPdfAsync(request);
                contentType = "application/pdf";
                extension = "pdf";
            }
            else
            {
                fileBytes = await _exportService.ExportWordAsync(request);
                contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                extension = "docx";
            }

            var fileName = $"DeThi_{id:N}_{(request.IncludeDapAn ? "CoDapAn" : "")}_{DateTime.Now:yyyyMMdd}.{extension}";
            return File(fileBytes, contentType, fileName);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponseFactory.NotFound<object>("Không tìm thấy đề thi"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xuất đề thi {MaDeThi}", id);
            return StatusCode(500, ApiResponseFactory.ServerError("Không thể xuất đề thi: " + ex.Message));
        }

    }
    [HttpGet("{id}/export-ezp")]
    [SwaggerOperation(Summary = "Export đề thi ra file .ezp (JSON)", Description = "Export đề thi với đầy đủ thông tin câu hỏi và đáp án. Tự động mã hóa nếu được bật trong config.")]
    [SwaggerResponse(200, "File .ezp", typeof(FileResult))]
    [SwaggerResponse(404, "Không tìm thấy đề thi")]
    [SwaggerResponse(500, "Lỗi server")]
    public async Task<IActionResult> ExportDeThiEzp(Guid id)
    {
        try
        {
            if (id == Guid.Empty)
            {
                return BadRequest(ApiResponseFactory.ValidationError<object>("ID đề thi không hợp lệ."));
            }

            var (success, message, fileContent, fileName) = await _ezpExportService.ExportDeThiToEzpFileWithPasswordAsync(id);

            if (!success || fileContent == null)
            {
                return NotFound(ApiResponseFactory.NotFound<object>(message));
            }

            return File(fileContent, "application/json", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi export đề thi {MaDeThi} ra file .ezp", id);
            return StatusCode(500, ApiResponseFactory.ServerError($"Không thể export đề thi: {ex.Message}"));
        }
    }

    // GET: api/DeThi/{id}/export-json
    [HttpGet("{id}/export-json")]
    [SwaggerOperation(Summary = "Export đề thi ra JSON string", Description = "Lấy nội dung JSON của đề thi để xem trước hoặc kiểm tra")]
    [SwaggerResponse(200, "JSON content")]
    [SwaggerResponse(404, "Không tìm thấy đề thi")]
    public async Task<IActionResult> ExportDeThiJson(Guid id, [FromQuery] bool indented = true)
    {
        try
        {
            if (id == Guid.Empty)
            {
                return BadRequest(ApiResponseFactory.ValidationError<object>("ID đề thi không hợp lệ."));
            }

            var (success, message, jsonContent) = await _ezpExportService.ExportDeThiToJsonStringAsync(id, indented);

            if (!success || string.IsNullOrEmpty(jsonContent))
            {
                return NotFound(ApiResponseFactory.NotFound<object>(message));
            }

            return Ok(ApiResponseFactory.Success(new { JsonContent = jsonContent }, "Export JSON thành công"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi export đề thi {MaDeThi} ra JSON", id);
            return StatusCode(500, ApiResponseFactory.ServerError($"Không thể export JSON: {ex.Message}"));
        }
    }
     [HttpPost("decrypt-ezp")]
    [SwaggerOperation(Summary = "Giải mã file EZP để xem nội dung", Description = "Upload file EZP để giải mã và xem nội dung JSON bên trong. Dùng để test/debug.")]
    [SwaggerResponse(200, "Nội dung JSON đã giải mã")]
    [SwaggerResponse(400, "Lỗi giải mã")]
    public IActionResult DecryptEzpFile(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(ApiResponseFactory.ValidationError<object>("File rỗng hoặc không hợp lệ."));
            }

            // Đọc file content
            using var memoryStream = new MemoryStream();
            file.CopyTo(memoryStream);
            byte[] fileBytes = memoryStream.ToArray();

            // Giải mã
            var (success, message, decryptedJson) = _ezpExportService.DecryptEzpFileFromBytes(fileBytes);

            if (!success)
            {
                return BadRequest(ApiResponseFactory.ValidationError<object>(message));
            }

            // Trả về JSON đã giải mã
            return Ok(ApiResponseFactory.Success(new 
            { 
                Message = message,
                JsonContent = decryptedJson,
                FileSize = fileBytes.Length,
                IsEncrypted = message.Contains("Giải mã thành công")
            }, "Xử lý file thành công!"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi giải mã file EZP");
            return StatusCode(500, ApiResponseFactory.ServerError($"Lỗi: {ex.Message}"));
        }
    }

    [HttpPost("{id}/export-tuluan-word")]
    [SwaggerOperation("Xuất đề thi tự luận ra file Word")]
    public async Task<IActionResult> ExportTuLuanWord(Guid id, [FromBody] YeuCauXuatDeThiDto request)
    {
        request.MaDeThi = id;
        var result = await _tuLuanExportService.ExportTuLuanToWordAsync(request);

        if (!result.Success)
            return BadRequest(result.Message);

        return File(
            result.FileStream!,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            result.FileName
        );
    }
}