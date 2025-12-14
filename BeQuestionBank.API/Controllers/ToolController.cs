using BEQuestionBank.Core.Services;
using BeQuestionBank.Shared.DTOs.Tool;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using BeQuestionBank.Shared.DTOs.Common;

namespace BeQuestionBank.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ToolController : ControllerBase
    {
        private readonly ToolService _service;

        public ToolController(ToolService service)
        {
            _service = service;
        }

        [HttpPost("upload-image")]
        public IActionResult UploadImage([FromForm] UploadImageRequest request)
        {
            var file = request.File; // lấy từ request

            if (file == null || file.Length == 0)
                return BadRequest(ApiResponseFactory.ValidationError<bool>("Chưa chọn file"));

            try
            {
                using var ms = new MemoryStream();
                file.CopyTo(ms);
                byte[] fileBytes = ms.ToArray();

                string base64Src = _service.ConvertToBase64(fileBytes, file.ContentType);
                string html = _service.BuildImageHtml(base64Src);

                var data = new
                {
                    Base64 = base64Src,
                    Html = html,
                    Message = "Lấy dữ liệu thành công"
                };

                return Ok(ApiResponseFactory.Success(data));
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponseFactory.ServerError("Đã xảy ra lỗi trong quá trình xử lý."));
            }
        }

        /// <summary>
        /// Chuyển đổi nội dung chứa LaTeX thành HTML có hỗ trợ MathJax
        /// </summary>
        [HttpPost("convert-latex")]
        public IActionResult ConvertLatex([FromBody] ConvertLatexRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Content))
            {
                return BadRequest(ApiResponseFactory.ValidationError<string>("Nội dung không được để trống"));
            }

            try
            {
                string resultHtml;

                if (request.MergeSpans)
                {
                    // Dùng hàm tiện ích kết hợp cả convert LaTeX + merge spans
                    resultHtml = _service.ProcessContentWithLatexAndMergeSpans(request.Content);
                }
                else
                {
                    // Chỉ convert LaTeX
                    resultHtml = _service.ConvertLatexToHtml(request.Content);
                }

                var data = new
                {
                    OriginalContent = request.Content,
                    Html = resultHtml,
                    Message = "Chuyển đổi LaTeX thành công"
                };

                return Ok(ApiResponseFactory.Success(data));
            }
            catch (Exception ex)
            {
                // Có thể log exception ở đây nếu có logger
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponseFactory.ServerError("Đã xảy ra lỗi khi chuyển đổi LaTeX."));
            }
        }
    }
}