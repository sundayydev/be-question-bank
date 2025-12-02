using BeQuestionBank.Shared.DTOs.Common; // Giả sử bạn có ApiResponseFactory ở đây
using BeQuestionBank.Shared.DTOs.Import;
using BEQuestionBank.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace BeQuestionBank.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImportController : ControllerBase
    {
        private readonly ImportService _importService;
        private readonly IWebHostEnvironment _env;

        public ImportController(ImportService importService, IWebHostEnvironment env)
        {
            _importService = importService;
            _env = env;
        }

        [HttpPost("word")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ImportFromWord([FromForm] ImportRequestDto request)
        {
            if (request.File == null || request.File.Length == 0)
            {
                return BadRequest(ApiResponseFactory.ServerError("Vui lòng upload file .docx hợp lệ."));
            }

            var extension = Path.GetExtension(request.File.FileName).ToLower();
            if (extension != ".docx")
            {
                return BadRequest(ApiResponseFactory.ServerError("Chỉ chấp nhận file định dạng .docx"));
            }

            try
            {
                // --- SỬA LỖI TẠI ĐÂY ---
                // Kiểm tra nếu WebRootPath null thì lấy thư mục hiện tại + wwwroot
                string rootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

                // Tạo thư mục nếu chưa có
                if (!Directory.Exists(rootPath))
                {
                    Directory.CreateDirectory(rootPath);
                }

                // Đường dẫn folder chứa media gốc (TempUploads)
                string mediaFolderPath = Path.Combine(rootPath, "TempUploads");

                // Tạo thư mục TempUploads nếu chưa có để tránh lỗi tiếp theo
                if (!Directory.Exists(mediaFolderPath))
                {
                    Directory.CreateDirectory(mediaFolderPath);
                }

                // Gọi Service
                var result = await _importService.ImportQuestionsAsync(request.File, request.MaPhan, mediaFolderPath);

                if (result.Errors.Any())
                {
                    if (result.SuccessCount > 0)
                    {
                        return Ok(ApiResponseFactory.Success(result,
                            $"Đã import {result.SuccessCount} câu. Có {result.Errors.Count} lỗi: " + string.Join("; ", result.Errors.Take(3)) + "..."));
                    }
                    return BadRequest(ApiResponseFactory.ServerError(string.Join("\n", result.Errors)));
                }

                return Ok(ApiResponseFactory.Success(result, $"Import thành công {result.SuccessCount} câu hỏi!"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseFactory.ServerError($"Lỗi hệ thống: {ex.Message}"));
            }
        }
    }
}