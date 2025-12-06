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
        /// Tạo câu hỏi điền từ (Fill in the blank) - DT
        /// </summary>
        [HttpPost("dientu")]
        [SwaggerOperation(
            Summary = "Tạo câu hỏi điền từ",
            Description = "Tạo câu hỏi dạng điền từ, các đáp án đều đúng và thứ tự cực kỳ quan trọng."
        )]
        public async Task<IActionResult> CreateDienTu([FromBody] CreateCauHoiDienTuDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponseFactory.ValidationError<object>("Dữ liệu không hợp lệ", ModelState));

            try
            {
                var userId = GetCurrentUserId();
                var result = await _cauHoiService.CreateCauHoiDienTuAsync(request, userId);

                if (result != null)
                    return Ok(ApiResponseFactory.Success(result, "Tạo câu hỏi điền từ thành công"));

                return BadRequest(ApiResponseFactory.Error<object>(400, "Tạo thất bại"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo câu hỏi điền từ");
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

        /// <summary>
        /// Thống kê số lượng câu hỏi theo loại
        /// </summary>
        [HttpGet("statistics")]
        [SwaggerOperation(
            Summary = "Thống kê câu hỏi",
            Description = "Lấy thống kê số lượng câu hỏi theo loại, môn học, trạng thái."
        )]
        public async Task<IActionResult> GetStatistics(
            [FromQuery] Guid? khoaId = null,
            [FromQuery] Guid? monHocId = null,
            [FromQuery] Guid? phanId = null)
        {
            try
            {
                var result = await _cauHoiService.GetStatisticsAsync(khoaId, monHocId, phanId);
                return Ok(ApiResponseFactory.Success(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy thống kê câu hỏi");
                return StatusCode(500, ApiResponseFactory.ServerError(ex.Message));
            }
        }

        /// <summary>
        /// Lấy các loại câu hỏi có sẵn
        /// </summary>
        [HttpGet("question-types")]
        [SwaggerOperation(
            Summary = "Lấy danh sách loại câu hỏi",
            Description = "Lấy danh sách các loại câu hỏi có trong hệ thống."
        )]
        public async Task<IActionResult> GetQuestionTypes()
        {
            try
            {
                var result = await _cauHoiService.GetQuestionTypesAsync();
                return Ok(ApiResponseFactory.Success(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách loại câu hỏi");
                return StatusCode(500, ApiResponseFactory.ServerError(ex.Message));
            }
        }

        /// <summary>
        /// Lấy danh sách câu hỏi nhóm (có phân trang và lọc)
        /// </summary>
        [HttpGet("group-questions")]
        [SwaggerOperation(
            Summary = "Lấy danh sách câu hỏi nhóm",
            Description =
                "Trả về danh sách câu hỏi thuộc dạng nhóm với hỗ trợ phân trang, tìm kiếm và lọc theo các tiêu chí."
        )]
        public async Task<IActionResult> GetGroupQuestions(
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? keyword = null,
            [FromQuery] Guid? khoaId = null,
            [FromQuery] Guid? monHocId = null,
            [FromQuery] Guid? phanId = null)
        {
            try
            {
                var result = await _cauHoiService.GetCauHoiNhomAsync(
                    pageIndex,
                    pageSize,
                    keyword,
                    khoaId,
                    monHocId,
                    phanId);

                return Ok(ApiResponseFactory.Success(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách câu hỏi nhóm");
                return StatusCode(500, ApiResponseFactory.ServerError(ex.Message));
            }
        }

        /// <summary>
        /// Lấy chi tiết câu hỏi nhóm kèm tất cả câu hỏi con
        /// </summary>
        [HttpGet("group-questions/{id}")]
        [SwaggerOperation(
            Summary = "Lấy chi tiết câu hỏi nhóm",
            Description =
                "Lấy thông tin chi tiết của một câu hỏi nhóm bao gồm nội dung đoạn văn và tất cả các câu hỏi con với câu trả lời."
        )]
        public async Task<IActionResult> GetGroupQuestionById(Guid id)
        {
            try
            {
                var result = await _cauHoiService.GetGroupQuestionDetailAsync(id);

                if (result == null)
                    return NotFound(ApiResponseFactory.NotFound<object>("Không tìm thấy câu hỏi nhóm"));

                return Ok(ApiResponseFactory.Success(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy chi tiết câu hỏi nhóm {id}");
                return StatusCode(500, ApiResponseFactory.ServerError(ex.Message));
            }
        }

        /// <summary>
        /// Lấy danh sách câu hỏi con của một câu hỏi nhóm
        /// </summary>
        [HttpGet("group-questions/{parentId}/children")]
        [SwaggerOperation(
            Summary = "Lấy danh sách câu hỏi con",
            Description = "Lấy tất cả các câu hỏi con thuộc về một câu hỏi nhóm (parent question) cụ thể."
        )]
        public async Task<IActionResult> GetChildQuestions(Guid parentId)
        {
            try
            {
                var result = await _cauHoiService.GetChildQuestionsByParentIdAsync(parentId);

                return Ok(ApiResponseFactory.Success(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy danh sách câu hỏi con của câu hỏi nhóm {parentId}");
                return StatusCode(500, ApiResponseFactory.ServerError(ex.Message));
            }
        }

        /// <summary>
        /// Lấy danh sách câu hỏi ghép nối
        /// </summary>
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
            }

            ;
        }

        // [HttpGet("dientu")]
        // public async Task<IActionResult> GetCauHoiDienTu()
        // {
        //     var data = await _cauHoiService.GetCauHoiDienTuAsync();
        //     return Ok(ApiResponseFactory.Success(data));
        // }
        /// <summary>
        /// Lấy danh sách câu hỏi ddienf từ
        /// </summary>
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

        /// <summary>
        /// Lấy danh sách câu hỏi muitichoi
        /// </summary>
        [HttpGet("multiplechoice")]
        [SwaggerOperation(
            Summary = "Lấy tất cả câu hỏi Multiple Choice",
            Description = "Trả về danh sách câu hỏi MN cùng các đáp án"
        )]
        public async Task<IActionResult> GetMultipleChoice()
        {
            try
            {
                var result = await _cauHoiService.GetMultipleChoiceQuestionsAsync();
                return Ok(ApiResponseFactory.Success(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách câu hỏi Multiple Choice");
                return StatusCode(500, ApiResponseFactory.ServerError(ex.Message));
            }
        }


      

        /// <summary>
        /// Tạo câu hỏi ghép nối (Matching) - GN
        /// </summary>
        [HttpPost("ghepnoi")]
        [SwaggerOperation(
            Summary = "Tạo câu hỏi ghép nối",
            Description = "Tạo một nhóm câu hỏi ghép nối (có câu cha làm tiêu đề và các cặp Trái - Phải)."
        )]
        public async Task<IActionResult> CreateGhepNoi([FromBody] CreateCauHoiGhepNoiDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponseFactory.ValidationError<object>("Dữ liệu không hợp lệ", ModelState));

            try
            {
                var userId = GetCurrentUserId();
                var result = await _cauHoiService.CreateGhepNoiQuestionAsync(request, userId);

                if (result != null)
                    return Ok(ApiResponseFactory.Success(result, "Tạo câu hỏi ghép nối thành công"));

                return BadRequest(ApiResponseFactory.Error<object>(400, "Tạo thất bại"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo câu hỏi ghép nối");
                return StatusCode(500, ApiResponseFactory.ServerError(ex.Message));
            }
        }

        // <summary>
        /// Tạo câu hỏi Multiple Choice (MN)
        /// </summary>
        [HttpPost("multiplechoice")]
        [SwaggerOperation(
            Summary = "Tạo câu hỏi MN",
            Description = "Tạo 1 câu hỏi Multiple Choice với ít nhất 3 đáp án và ít nhất 2 đáp án đúng."
        )]
        public async Task<IActionResult> CreateMultipleChoice([FromBody] CreateCauHoiMultipleChoiceDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponseFactory.ValidationError<object>("Dữ liệu không hợp lệ", ModelState));

            try
            {
                var userId = GetCurrentUserId();

                var result = await _cauHoiService.CreateMultipleChoiceQuestionAsync(request, userId);

                if (result != null)
                    return Ok(ApiResponseFactory.Success(result, "Tạo câu hỏi Multiple Choice thành công"));

                return BadRequest(ApiResponseFactory.Error<object>(400, "Tạo câu hỏi thất bại"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo câu hỏi Multiple Choice");
                return StatusCode(500, ApiResponseFactory.ServerError(ex.Message));
            }
        }
    }
}