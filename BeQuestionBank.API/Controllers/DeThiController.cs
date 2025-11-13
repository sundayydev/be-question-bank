using BEQuestionBank.Core.Services;
using BeQuestionBank.Shared.DTOs.Common;
using BEQuestionBank.Shared.DTOs.DeThi;
using BeQuestionBank.Shared.DTOs.Khoa;
using BEQuestionBank.Shared.DTOs.MaTran;
using BeQuestionBank.Shared.DTOs.MonHoc;
using BeQuestionBank.Shared.DTOs.Pagination;
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
    [SwaggerOperation("Lấy danh sách Khoa có phân trang, filter, sort")]
    public async Task<IActionResult> GetPagedAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sort = null,
        [FromQuery] string? filter = null)
    {
        try
        {
            var query = await _service.GetAllBasicAsync(); 

            // Filtering
            if (!string.IsNullOrWhiteSpace(filter))
            {
                query = query.Where(k =>
                    k.TenDeThi.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    (k.TenMonHoc != null && k.TenMonHoc.Contains(filter, StringComparison.OrdinalIgnoreCase))
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

}