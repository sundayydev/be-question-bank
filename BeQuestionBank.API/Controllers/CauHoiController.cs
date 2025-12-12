using BeQuestionBank.Shared.DTOs.CauHoi;
using BeQuestionBank.Shared.DTOs.CauHoi.Create;
using BeQuestionBank.Shared.DTOs.CauHoi.TuLuan;
using BeQuestionBank.Shared.DTOs.Common;
using BEQuestionBank.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

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
        [HttpPost("fillblank")]
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
        /// Tạo câu hỏi tự luận
        /// </summary>
        [HttpPost("essay")]
        [SwaggerOperation(
            Summary = "Tạo câu hỏi Tự luận TL"
        )]
        public async Task<IActionResult> CreateTuLuan([FromBody] CreateCauHoiTuLuanDto request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _cauHoiService.CreateEssayQuestionAsync(request, userId);
                return Ok(ApiResponseFactory.Success(result, "Tạo câu hỏi tự luận thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tạo câu hỏi tự luận");
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

        /// <summary>
        /// Tạo câu hỏi ghép nối (Matching) - GN
        /// </summary>
        [HttpPost("pairing")]
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
            if (!ModelState.IsValid)
                return BadRequest(ApiResponseFactory.ValidationError<object>("Dữ liệu không hợp lệ", ModelState));

            try
            {
                var userId = GetCurrentUserId();

                var updatedQuestion = await _cauHoiService.UpdateAsync(id, request, userId);

                if (updatedQuestion != null)
                    return Ok(
                        ApiResponseFactory.Success(updatedQuestion, "Cập nhật câu hỏi Multiple Choice thành công"));

                return NotFound(
                    ApiResponseFactory.Error<object>(404, "Không tìm thấy câu hỏi hoặc không phải loại MN"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponseFactory.Error<object?>(400, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi cập nhật câu hỏi Multiple Choice {id}: {ex.Message}");
                if (ex.InnerException != null)
                    _logger.LogError(ex.InnerException, $"Inner: {ex.InnerException.Message}");

                return StatusCode(500,
                    ApiResponseFactory.ServerError(ex.Message + " | Inner: " +
                                                   (ex.InnerException?.Message ?? "No inner")));
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
            var idClaim = User.FindFirst("sub")
                          ?? User.FindFirst("id")
                          ?? User.FindFirst(ClaimTypes.NameIdentifier);
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
        /// Lấy tất cả câu hỏi tự luận (không phân trang)
        /// </summary>
        [HttpGet("essay")]
        public async Task<IActionResult> GetAllTuLuan()
        {
            try
            {
                var result = await _cauHoiService.GetAllEssayQuestionsAsync();
                return Ok(ApiResponseFactory.Success(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi lấy danh sách câu hỏi tự luận");
                return StatusCode(500, ApiResponseFactory.ServerError(ex.Message));
            }
        }


        /// <summary>
        /// Lấy danh sách câu hỏi TỰ LUẬN (TL) - có phân trang, tìm kiếm, lọc
        /// </summary>
        [HttpGet("essay/paged")]
        [SwaggerOperation(Summary = "Danh sách câu hỏi tự luận (phân trang)",
            Description = "Lọc theo khoa, môn, phần, từ khóa")]
        public async Task<IActionResult> GetEssayPagedAsync(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? sort = null,
            [FromQuery] string? search = null,
            [FromQuery] Guid? khoaId = null,
            [FromQuery] Guid? monHocId = null,
            [FromQuery] Guid? phanId = null)
        {
            var result =
                await _cauHoiService.GetEssayPagedAsync(page, pageSize, sort, search, khoaId, monHocId, phanId);
            return Ok(ApiResponseFactory.Success(result, "Lấy danh sách câu hỏi tự luận thành công"));
        }

        /// <summary>
        /// Lấy danh sách câu hỏi NHÓM (Reading / Chùm) - có phân trang, tìm kiếm
        /// </summary>
        [HttpGet("group/paged")]
        [SwaggerOperation(Summary = "Danh sách câu hỏi nhóm (phân trang)",
            Description = "Câu hỏi có đoạn văn + nhiều câu con")]
        public async Task<IActionResult> GetGroupPagedAsync(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? sort = null,
            [FromQuery] string? search = null,
            [FromQuery] Guid? khoaId = null,
            [FromQuery] Guid? monHocId = null,
            [FromQuery] Guid? phanId = null)
        {
            var result =
                await _cauHoiService.GetGroupPagedAsync(page, pageSize, sort, search, khoaId, monHocId, phanId);
            return Ok(ApiResponseFactory.Success(result, "Lấy danh sách câu hỏi nhóm thành công"));
        }

        /// <summary>
        /// Lấy danh sách câu hỏi ĐƠN (Single Choice) - có phân trang, tìm kiếm
        /// </summary>
        [HttpGet("single/paged")]
        [SwaggerOperation(Summary = "Danh sách câu hỏi đơn (phân trang)",
            Description = "Câu trắc nghiệm chọn 1 đáp án đúng")]
        public async Task<IActionResult> GetSinglePagedAsync(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? sort = null,
            [FromQuery] string? search = null,
            [FromQuery] Guid? khoaId = null,
            [FromQuery] Guid? monHocId = null,
            [FromQuery] Guid? phanId = null)
        {
            var result =
                await _cauHoiService.GetSinglePagedAsync(page, pageSize, sort, search, khoaId, monHocId, phanId);
            return Ok(ApiResponseFactory.Success(result, "Lấy danh sách câu hỏi đơn thành công"));
        }

        /// <summary>
        /// Lấy danh sách câu hỏi ĐIỀN TỪ (Fill in the blank) - có phân trang
        /// </summary>
        [HttpGet("fillblank/paged")]
        [SwaggerOperation(Summary = "Danh sách câu hỏi điền từ (phân trang)")]
        public async Task<IActionResult> GetFillBlankPagedAsync(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? sort = null,
            [FromQuery] string? search = null,
            [FromQuery] Guid? khoaId = null,
            [FromQuery] Guid? monHocId = null,
            [FromQuery] Guid? phanId = null)
        {
            var result =
                await _cauHoiService.GetFillBlankPagedAsync(page, pageSize, sort, search, khoaId, monHocId, phanId);
            return Ok(ApiResponseFactory.Success(result, "Lấy danh sách câu hỏi điền từ thành công"));
        }

        /// <summary>
        /// Lấy danh sách câu hỏi GHÉP NỐI (Matching) - có phân trang
        /// </summary>
        [HttpGet("pairing/paged")]
        [SwaggerOperation(Summary = "Danh sách câu hỏi ghép nối (phân trang)")]
        public async Task<IActionResult> GetPairingPagedAsync(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? sort = null,
            [FromQuery] string? search = null,
            [FromQuery] Guid? khoaId = null,
            [FromQuery] Guid? monHocId = null,
            [FromQuery] Guid? phanId = null)
        {
            var result =
                await _cauHoiService.GetPairingPagedAsync(page, pageSize, sort, search, khoaId, monHocId, phanId);
            return Ok(ApiResponseFactory.Success(result, "Lấy danh sách câu hỏi ghép nối thành công"));
        }

        /// <summary>
        /// Lấy danh sách câu hỏi GHÉP NỐI (Matching) - có phân trang
        /// </summary>
        [HttpGet("multiplechoice/paged")]
        [SwaggerOperation(Summary = "Danh sách câu hỏi ghép nối (phân trang)")]
        public async Task<IActionResult> GetMultipleChoicePagedAsync(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? sort = null,
            [FromQuery] string? search = null,
            [FromQuery] Guid? khoaId = null,
            [FromQuery] Guid? monHocId = null,
            [FromQuery] Guid? phanId = null)
        {
            var result =
                await _cauHoiService.GetMultipleChoicePagedAsync(page, pageSize, sort, search, khoaId, monHocId,
                    phanId);
            return Ok(ApiResponseFactory.Success(result, "Lấy danh sách câu hỏi ghép nối thành công"));
        }

        [HttpPatch("group/{id}")]
        public async Task<IActionResult> UpdateGroup(Guid id, [FromBody] UpdateCauHoiNhomDto request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var updated = await _cauHoiService.UpdateGroupQuestionAsync(id, request, userId);

                if (updated != null)
                    return Ok(ApiResponseFactory.Success(updated, "Cập nhật câu hỏi nhóm thành công"));

                return NotFound(ApiResponseFactory.Error<object>(404, "Cập nhật thất bại hoặc không tìm thấy ID"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật câu hỏi nhóm");
                return StatusCode(500, ApiResponseFactory.ServerError(ex.Message));
            }
        }

        /// <summary>
        /// Cập nhật câu hỏi Multiple Choice (MN)
        /// </summary>
        [HttpPatch("multiplechoice/{id}")]
        [SwaggerOperation(
            Summary = "Cập nhật câu hỏi Multiple Choice",
            Description = "Cập nhật câu hỏi MN (ít nhất 3 đáp án, ít nhất 2 đáp án đúng)"
        )]
        public async Task<IActionResult> UpdateMultipleChoice(Guid id, [FromBody] UpdateCauHoiWithCauTraLoiDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponseFactory.ValidationError<object>("Dữ liệu không hợp lệ", ModelState));

            try
            {
                var userId = GetCurrentUserId();

                var updatedQuestion = await _cauHoiService.UpdateMultipleChoiceQuestionAsync(id, request, userId);

                if (updatedQuestion != null)
                    return Ok(
                        ApiResponseFactory.Success(updatedQuestion, "Cập nhật câu hỏi Multiple Choice thành công"));

                return NotFound(
                    ApiResponseFactory.Error<object>(404, "Không tìm thấy câu hỏi hoặc không phải loại MN"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponseFactory.Error<object?>(400, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi cập nhật câu hỏi Multiple Choice {id}: {ex.Message}");
                if (ex.InnerException != null)
                    _logger.LogError(ex.InnerException, $"Inner: {ex.InnerException.Message}");

                return StatusCode(500,
                    ApiResponseFactory.ServerError(ex.Message + " | Inner: " +
                                                   (ex.InnerException?.Message ?? "No inner")));
            }
        }

        /// <summary>
        /// Cập nhật câu hỏi Tu kuab
        /// </summary>
        [HttpPatch("essay/{id}")]
        [SwaggerOperation(
            Summary = "Cập nhật câu hỏi Tự LUẬN",
            Description = "Cập nhật câu hỏi Tự luận"
        )]
        public async Task<IActionResult> UpdateEssay(Guid id, [FromBody] UpdateCauHoiTuLuanDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponseFactory.ValidationError<object>("Dữ liệu không hợp lệ", ModelState));

            try
            {
                var userId = GetCurrentUserId();

                var updatedQuestion = await _cauHoiService.UpdateEssayQuestionAsync(id, request, userId);

                if (updatedQuestion != null)
                    return Ok(
                        ApiResponseFactory.Success(updatedQuestion, "Cập nhật câu hỏi tự luận thành công"));

                return NotFound(
                    ApiResponseFactory.Error<object>(404, "Không tìm thấy câu hỏi hoặc không phải loại TL"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponseFactory.Error<object?>(400, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi cập nhật câu hỏi tự luận  {id}: {ex.Message}");
                if (ex.InnerException != null)
                    _logger.LogError(ex.InnerException, $"Inner: {ex.InnerException.Message}");

                return StatusCode(500,
                    ApiResponseFactory.ServerError(ex.Message + " | Inner: " +
                                                   (ex.InnerException?.Message ?? "No inner")));
            }
        }

        /// <summary>
        /// Cập nhật câu hỏi dein từ
        /// </summary>
        [HttpPatch("Fillblank/{id}")]
        [SwaggerOperation(
            Summary = "Cập nhật câu hỏi dien từ",
            Description = "Cập nhật câu hỏi Tự luận"
        )]
        public async Task<IActionResult> UpdateFillblank(Guid id, [FromBody] UpdateDienTuQuestionDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponseFactory.ValidationError<object>("Dữ liệu không hợp lệ", ModelState));

            try
            {
                var userId = GetCurrentUserId();

                var updatedQuestion = await _cauHoiService.UpdateDienTuQuestionAsync(id, request, userId);

                if (updatedQuestion != null)
                    return Ok(
                        ApiResponseFactory.Success(updatedQuestion, "Cập nhật câu hỏi tự luận thành công"));

                return NotFound(
                    ApiResponseFactory.Error<object>(404, "Không tìm thấy câu hỏi hoặc không phải loại TL"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponseFactory.Error<object?>(400, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi cập nhật câu hỏi tự luận  {id}: {ex.Message}");
                if (ex.InnerException != null)
                    _logger.LogError(ex.InnerException, $"Inner: {ex.InnerException.Message}");

                return StatusCode(500,
                    ApiResponseFactory.ServerError(ex.Message + " | Inner: " +
                                                   (ex.InnerException?.Message ?? "No inner")));
            }
        }

        /// <summary>
        /// Cập nhật câu hỏi Tu kuab
        /// </summary>
        [HttpPatch("Pairing/{id}")]
        [SwaggerOperation(
            Summary = "Cập nhật câu hỏi Tự LUẬN",
            Description = "Cập nhật câu hỏi Tự luận"
        )]
        public async Task<IActionResult> UpdatePairing(Guid id, [FromBody] UpdateCauHoiNhomDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponseFactory.ValidationError<object>("Dữ liệu không hợp lệ", ModelState));

            try
            {
                var userId = GetCurrentUserId();

                var updatedQuestion = await _cauHoiService.UpdateGhepNoiQuestionAsync(id, request, userId);

                if (updatedQuestion != null)
                    return Ok(
                        ApiResponseFactory.Success(updatedQuestion, "Cập nhật câu hỏi tự luận thành công"));

                return NotFound(
                    ApiResponseFactory.Error<object>(404, "Không tìm thấy câu hỏi hoặc không phải loại TL"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponseFactory.Error<object?>(400, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi cập nhật câu hỏi tự luận  {id}: {ex.Message}");
                if (ex.InnerException != null)
                    _logger.LogError(ex.InnerException, $"Inner: {ex.InnerException.Message}");

                return StatusCode(500,
                    ApiResponseFactory.ServerError(ex.Message + " | Inner: " +
                                                   (ex.InnerException?.Message ?? "No inner")));
            }
        }
    }
}