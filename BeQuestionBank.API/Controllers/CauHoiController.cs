using BeQuestionBank.Shared.DTOs.CauHoi;
using BeQuestionBank.Shared.DTOs.Common;
using BEQuestionBank.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Swashbuckle.AspNetCore.Annotations;

namespace BeQuestionBank.API.Controllers
{
    [Route("api/[controller]")] 
    [ApiController]
   // [Authorize]
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
        [SwaggerOperation("Lấy tất cả câu hỏi")]
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
        [SwaggerOperation(
            Summary = "Lấy chi tiết câu hỏi",
            Description = "Lấy thông tin chi tiết của một câu hỏi theo ID."
        )]
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
        [SwaggerOperation(
            Summary = "Tạo câu hỏi đơn",
            Description = "Tạo một câu hỏi dạng trắc nghiệm đơn hoặc nhiều đáp án (Single/Multiple Choice)."
        )]
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
        [SwaggerOperation(
            Summary = "Tạo câu hỏi nhóm",
            Description = "Tạo một câu hỏi chùm gồm đoạn văn hoặc nội dung chính và nhiều câu hỏi con."
        )]

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
        [HttpPatch("{id}")]
        [SwaggerOperation(
            Summary = "Cập nhật câu hỏi",
            Description = "Cập nhật nội dung câu hỏi và danh sách câu trả lời theo ID."
        )]
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
        [SwaggerOperation(
            Summary = "Xóa câu hỏi",
            Description = "Xóa mềm (soft-delete) câu hỏi theo ID."
        )]

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
        [HttpGet("group-question")]
        [SwaggerOperation(
            Summary = "Lấy danh sách câu hỏi nhóm",
            Description = "Trả về toàn bộ câu hỏi thuộc dạng nhóm (Group Questions / Reading Passage)."
        )]

        public async Task<IActionResult> GetNhom()
        {
            try
            {
                var result = await _cauHoiService.GetCauHoiNhomAsync();
                return Ok(ApiResponseFactory.Success(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách câu hỏi");
                return StatusCode(500, ApiResponseFactory.ServerError(ex.Message));
            }
        }

        [HttpGet("pairing-question")]
        [SwaggerOperation(
            Summary = "Lấy danh sách câu hỏi ghép nối",
            Description = "Trả về danh sách câu hỏi dạng ghép nối (Matching/Pairing questions)."
        )]
        
        public async Task<IActionResult> GetGhepNoi()
        {
            try
            {
                var result = await _cauHoiService.GetCauHoiGhepNoiAsync();
                return Ok(ApiResponseFactory.Success(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách câu hỏi");
                return StatusCode(500, ApiResponseFactory.ServerError(ex.Message));
            };
        }
        // [HttpGet("dientu")]
        // public async Task<IActionResult> GetCauHoiDienTu()
        // {
        //     var data = await _cauHoiService.GetCauHoiDienTuAsync();
        //     return Ok(ApiResponseFactory.Success(data));
        // }
        [HttpGet("word-filling-questions")]
        [SwaggerOperation(
            Summary = "Lấy danh sách câu hỏi điền từ",
            Description = "Lấy toàn bộ câu hỏi dạng điền từ / fill-in-the-blank."
        )]

        public async Task<IActionResult> GetCauHoiDienTu()
        {
            try
            {
                var result = await _cauHoiService.GetCauHoiDienTuAsync();
                return Ok(ApiResponseFactory.Success(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách câu hỏi");
                return StatusCode(500, ApiResponseFactory.ServerError(ex.Message));
            }
        }
    }
}
