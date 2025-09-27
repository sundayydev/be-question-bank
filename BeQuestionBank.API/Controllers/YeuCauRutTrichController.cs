using BEQuestionBank.Core.Services;
using BeQuestionBank.Shared.DTOs.Common;
using BeQuestionBank.Shared.DTOs.YeuCauRutTrich;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
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
    // POST: api/YeuCauRutTrich/RutTrichDeThi
  
    [HttpPost("RutTrichDeThi")]
    [SwaggerOperation("Tạo yêu cầu rút trích và đề thi mới")]
    public async Task<IActionResult> CreateAndRutTrichDeThiAsync([FromBody] CreateYeuCauRutTrichDto dto)
    {
        if (dto == null || dto.MaNguoiDung == Guid.Empty || dto.MaMonHoc == Guid.Empty || 
            dto.MaTran == null)
        {
            return StatusCode(StatusCodes.Status400BadRequest,
                ApiResponseFactory.ValidationError<object>("Dữ liệu không hợp lệ, thiếu mã người dùng, mã môn học hoặc ma trận."));
        }

        try
        {
           

            // Tạo yêu cầu rút trích (không serialize ở đây)
            var (success, message, maYeuCau) = await _service.AddAsync(dto);
            if (!success)
            {
                return StatusCode(StatusCodes.Status400BadRequest,
                    ApiResponseFactory.ValidationError<object>(message));
            }
            // Sinh tên đề thi tự động (sau khi có mã yêu cầu)
            string tenDeThi = $"Đề thi {dto.MaMonHoc} - YC_{maYeuCau.ToString().Substring(0, 8)}";
            // Gọi DeThiService để rút trích đề thi
            var deThiService = HttpContext.RequestServices.GetService<DeThiService>();
            if (deThiService == null)
            {
                await _service.DeleteAsync(maYeuCau); // Xóa yêu cầu nếu không lấy được DeThiService
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponseFactory.ServerError("Không thể khởi tạo DeThiService."));
            }

            var (deThiSuccess, deThiMessage, maDeThi) = await deThiService.RutTrichDeThiAsync(maYeuCau, tenDeThi);
            if (!deThiSuccess)
            {
                // Nếu rút trích thất bại, xóa yêu cầu vừa tạo
                await _service.DeleteAsync(maYeuCau);
                return StatusCode(StatusCodes.Status400BadRequest,
                    ApiResponseFactory.ValidationError<object>(deThiMessage));
            }

            var responseData = new
            {
                MaYeuCau = maYeuCau,
                MaDeThi = maDeThi,
                MaNguoiDung = dto.MaNguoiDung,
                MaMonHoc = dto.MaMonHoc,
                TenDeThi = tenDeThi,
                NoiDungRutTrich = dto.NoiDungRutTrich,
                GhiChu = dto.GhiChu
            };

            return StatusCode(StatusCodes.Status201Created,
                ApiResponseFactory.Success(responseData, "Tạo yêu cầu rút trích và đề thi thành công!"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo yêu cầu rút trích và đề thi với mã người dùng {MaNguoiDung}", dto.MaNguoiDung);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponseFactory.ServerError($"Lỗi hệ thống: {ex.Message}"));
        }
    }
    /// <summary>
    /// Upload Excel để đọc Ma Trận
    /// </summary>
    /// <param name="file">Excel file</param>
    /// <param name="maMonHoc">Guid môn học</param>
    [HttpPost("upload")]
    public async Task<IActionResult> UploadExcelAsync(IFormFile file, [FromQuery] Guid maMonHoc)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Vui lòng chọn file Excel.");

        try
        {
            // Lưu file tạm
            var tempPath = Path.GetTempFileName();
            using (var stream = new FileStream(tempPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Gọi service đọc Excel
            var result = await _service.ReadMaTranFromExcelAsync(tempPath, maMonHoc);

            // Xoá file tạm sau khi xử lý
            System.IO.File.Delete(tempPath);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi đọc Excel Ma Trận.");
            return StatusCode(500, $"Lỗi: {ex.Message}");
        }
    }
}

