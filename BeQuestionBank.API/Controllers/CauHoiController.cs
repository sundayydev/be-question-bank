using BeQuestionBank.Shared.DTOs.CauHoi;
using BeQuestionBank.Shared.DTOs.Common;
using BEQuestionBank.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BeQuestionBank.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CauHoiController : ControllerBase
    {
        private readonly CauHoiService _cauHoiService;
        private readonly ILogger<CauHoiController> _logger;

        public CauHoiController(CauHoiService cauHoiService, ILogger<CauHoiController> logger)
        {
            _cauHoiService = cauHoiService;
            _logger = logger;
        }

        /// <summary>
        /// Lấy tất cả câu hỏi (Thường dùng cho trang danh sách)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                // Lưu ý: Bạn cần bổ sung hàm GetAllAsync vào Service nếu chưa có
                var result = await _cauHoiService.GetAllAsync();
                return Ok(ApiResponseFactory.Success(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách câu hỏi");
                return StatusCode(500, ApiResponseFactory.ServerError(ex.Message));
            }
        }

        /// <summary>
        /// Lấy chi tiết câu hỏi theo ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var result = await _cauHoiService.GetByIdAsync(id);
                if (result == null)
                    return NotFound(ApiResponseFactory.NotFound<object>("Không tìm thấy câu hỏi"));

                return Ok(ApiResponseFactory.Success(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy câu hỏi {id}");
                return StatusCode(500, ApiResponseFactory.ServerError(ex.Message));
            }
        }

        /// <summary>
        /// Tạo câu hỏi đơn (Single Choice / Multiple Choice đơn lẻ)
        /// </summary>
        [HttpPost("single")]
        public async Task<IActionResult> CreateSingle([FromBody] CreateCauHoiWithCauTraLoiDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponseFactory.ValidationError<object>("Dữ liệu không hợp lệ", ModelState));

            try
            {
                var userId = GetCurrentUserId();
                var result = await _cauHoiService.CreateSingleQuestionAsync(request, userId);

                if (result != null)
                    return Ok(ApiResponseFactory.Success(result, "Tạo câu hỏi đơn thành công"));

                return BadRequest(ApiResponseFactory.Error<object>(400, "Tạo thất bại"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo câu hỏi đơn");
                return StatusCode(500, ApiResponseFactory.ServerError(ex.Message));
            }
        }

        /// <summary>
        /// Tạo câu hỏi nhóm (Câu hỏi chùm / Reading passage)
        /// </summary>
        [HttpPost("group")]
        public async Task<IActionResult> CreateGroup([FromBody] CreateCauHoiNhomDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponseFactory.ValidationError<object>("Dữ liệu không hợp lệ", ModelState));

            try
            {
                var userId = GetCurrentUserId();
                var result = await _cauHoiService.CreateGroupQuestionAsync(request, userId);

                if (result != null)
                    return Ok(ApiResponseFactory.Success(result, "Tạo câu hỏi nhóm thành công"));

                return BadRequest(ApiResponseFactory.Error<object>(400, "Tạo thất bại"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo câu hỏi nhóm");
                return StatusCode(500, ApiResponseFactory.ServerError(ex.Message));
            }
        }

        /// <summary>
        /// Cập nhật câu hỏi
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCauHoiWithCauTraLoiDto request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _cauHoiService.UpdateAsync(id, request, userId);

                if (result)
                    return Ok(ApiResponseFactory.Success<object>(null, "Cập nhật thành công"));

                return BadRequest(ApiResponseFactory.Error<object>(400, "Cập nhật thất bại hoặc không tìm thấy ID"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi cập nhật câu hỏi {id}");
                return StatusCode(500, ApiResponseFactory.ServerError(ex.Message));
            }
        }

        /// <summary>
        /// Xóa câu hỏi (Xóa mềm - Soft Delete)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var result = await _cauHoiService.DeleteAsync(id);
                if (result)
                    return Ok(ApiResponseFactory.Deleted());

                return NotFound(ApiResponseFactory.NotFound<object>("Không tìm thấy câu hỏi để xóa"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi xóa câu hỏi {id}");
                return StatusCode(500, ApiResponseFactory.ServerError(ex.Message));
            }
        }

        // Helper để lấy ID người dùng từ Token JWT
        private Guid GetCurrentUserId()
        {
            var idClaim = User.FindFirst("Id") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            if (idClaim != null && Guid.TryParse(idClaim.Value, out Guid userId))
            {
                return userId;
            }
            return Guid.Empty; // Hoặc throw exception nếu bắt buộc phải có user
        }
    }
}
