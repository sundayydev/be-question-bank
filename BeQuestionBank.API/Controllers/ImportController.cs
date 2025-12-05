using BeQuestionBank.Shared.DTOs.Common; 
using BeQuestionBank.Shared.DTOs.Import;
using BEQuestionBank.Core.Services;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;

namespace BeQuestionBank.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImportController : ControllerBase
    {
        private readonly ImportService _importService;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ImportController> _logger;

        public ImportController(
            ImportService importService, 
            IWebHostEnvironment env,
            ILogger<ImportController> logger)
        {
            _importService = importService;
            _env = env;
            _logger = logger;
        }

        /// <summary>
        /// Preview import câu hỏi từ file Word - Kiểm tra lỗi và validation trước khi import thực sự
        /// API này chỉ parse và validate, KHÔNG lưu vào database
        /// </summary>
        [HttpPost("preview-word")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(100_000_000)] // 100MB
        public async Task<IActionResult> PreviewImportFromWord([FromForm] PreviewRequestDto request)
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
                // Lấy hoặc tạo thư mục wwwroot
                string rootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

                if (!Directory.Exists(rootPath))
                {
                    Directory.CreateDirectory(rootPath);
                }

                // Đường dẫn folder chứa media (cho validation)
                string mediaFolderPath = Path.Combine(rootPath, "TempUploads");

                if (!Directory.Exists(mediaFolderPath))
                {
                    Directory.CreateDirectory(mediaFolderPath);
                }

                // Gọi Service để preview
                _logger.LogInformation($"Preview import file: {request.File.FileName}, Size: {request.File.Length} bytes");
                var result = await _importService.PreviewImportAsync(request.File, mediaFolderPath);

                if (result.Errors.Any() && result.TotalQuestionsFound == 0)
                {
                    // Lỗi nghiêm trọng - không parse được file
                    return BadRequest(ApiResponseFactory.ServerError(string.Join("\n", result.Errors)));
                }

                // Trả về kết quả preview
                var response = new
                {
                    summary = result.Summary,
                    totalFound = result.TotalQuestionsFound,
                    validCount = result.ValidCount,
                    invalidCount = result.InvalidCount,
                    hasErrors = result.HasErrors,
                    canImport = !result.HasErrors,
                    generalErrors = result.Errors,
                    questions = result.Questions.Select(q => new
                    {
                        questionNumber = q.QuestionNumber,
                        type = q.Type,
                        status = q.Status,
                        isValid = q.IsValid,
                        isGroup = q.IsGroup,
                        clo = q.CLO,
                        preview = q.ContentPreview,
                        answersCount = q.AnswersCount,
                        correctAnswersCount = q.CorrectAnswersCount,
                        subQuestionsCount = q.SubQuestionsCount,
                        features = new
                        {
                            hasImages = q.HasImages,
                            hasAudio = q.HasAudio,
                            hasLatex = q.HasLatex
                        },
                        errors = q.Errors,
                        warnings = q.Warnings,
                        subQuestions = q.SubQuestions.Select(sub => new
                        {
                            identifier = sub.Identifier,
                            isValid = sub.IsValid,
                            answersCount = sub.AnswersCount,
                            correctAnswersCount = sub.CorrectAnswersCount,
                            features = new
                            {
                                hasImages = sub.HasImages,
                                hasAudio = sub.HasAudio,
                                hasLatex = sub.HasLatex
                            },
                            errors = sub.Errors,
                            warnings = sub.Warnings
                        })
                    })
                };

                _logger.LogInformation($"Preview completed: {result.Summary}");
                
                if (result.HasErrors)
                {
                    return Ok(ApiResponseFactory.Success(response, 
                        $"⚠️ {result.Summary}. Vui lòng sửa các lỗi trước khi import."));
                }

                return Ok(ApiResponseFactory.Success(response, 
                    $"✓ {result.Summary}. File sẵn sàng để import!"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi hệ thống khi preview file Word");
                return StatusCode(500, ApiResponseFactory.ServerError($"Lỗi hệ thống: {ex.Message}"));
            }
        }

        /// <summary>
        /// Import câu hỏi từ file Word (.docx)
        /// Hỗ trợ:
        /// - LaTeX (chuyển đổi sang HTML với MathJax support)
        /// - Ảnh (lưu dưới dạng base64 nhúng trong nội dung)
        /// - Audio (lưu file vật lý và đường dẫn trong database)
        /// </summary>
        [HttpPost("word")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(100_000_000)] // 100MB
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
                // Lấy hoặc tạo thư mục wwwroot
                string rootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

                if (!Directory.Exists(rootPath))
                {
                    Directory.CreateDirectory(rootPath);
                    _logger.LogInformation($"Đã tạo thư mục wwwroot: {rootPath}");
                }

                // Đường dẫn folder chứa media gốc (TempUploads)
                string mediaFolderPath = Path.Combine(rootPath, "TempUploads");

                if (!Directory.Exists(mediaFolderPath))
                {
                    Directory.CreateDirectory(mediaFolderPath);
                    _logger.LogInformation($"Đã tạo thư mục TempUploads: {mediaFolderPath}");
                }

                // Gọi Service để xử lý import
                _logger.LogInformation($"Bắt đầu import file: {request.File.FileName}, Size: {request.File.Length} bytes");
                var result = await _importService.ImportQuestionsAsync(request.File, request.MaPhan, mediaFolderPath);

                if (result.Errors.Any())
                {
                    if (result.SuccessCount > 0)
                    {
                        // Import một phần thành công
                        return Ok(ApiResponseFactory.Success(result,
                            $"Đã import {result.SuccessCount} câu hỏi. Có {result.Errors.Count} lỗi: " + 
                            string.Join("; ", result.Errors.Take(3)) + 
                            (result.Errors.Count > 3 ? "..." : "")));
                    }
                    // Import hoàn toàn thất bại
                    return BadRequest(ApiResponseFactory.ServerError(string.Join("\n", result.Errors)));
                }

                _logger.LogInformation($"Import thành công {result.SuccessCount} câu hỏi");
                return Ok(ApiResponseFactory.Success(result, $"Import thành công {result.SuccessCount} câu hỏi!"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi hệ thống khi import file Word");
                return StatusCode(500, ApiResponseFactory.ServerError($"Lỗi hệ thống: {ex.Message}"));
            }
        }

        /// <summary>
        /// Preview import từ file ZIP - Kiểm tra lỗi trước khi import
        /// </summary>
        [HttpPost("preview-zip")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(200_000_000)] // 200MB
        public async Task<IActionResult> PreviewImportFromZip([FromForm] PreviewZipRequestDto request)
        {
            if (request.ZipFile == null || request.ZipFile.Length == 0)
            {
                return BadRequest(ApiResponseFactory.ServerError("Vui lòng upload file .zip hợp lệ."));
            }

            var extension = Path.GetExtension(request.ZipFile.FileName).ToLower();
            if (extension != ".zip")
            {
                return BadRequest(ApiResponseFactory.ServerError("Chỉ chấp nhận file định dạng .zip"));
            }

            string? extractPath = null;

            try
            {
                string rootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

                if (!Directory.Exists(rootPath))
                {
                    Directory.CreateDirectory(rootPath);
                }

                // Tạo thư mục tạm để giải nén
                extractPath = Path.Combine(rootPath, "TempExtract", Guid.NewGuid().ToString());
                Directory.CreateDirectory(extractPath);

                // Giải nén file ZIP
                _logger.LogInformation($"Preview: Giải nén file {request.ZipFile.FileName}");
                using (var stream = request.ZipFile.OpenReadStream())
                {
                    using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
                    {
                        archive.ExtractToDirectory(extractPath);
                    }
                }

                // Tìm file .docx trong thư mục giải nén
                var docxFiles = Directory.GetFiles(extractPath, "*.docx", SearchOption.AllDirectories);
                if (docxFiles.Length == 0)
                {
                    return BadRequest(ApiResponseFactory.ServerError("Không tìm thấy file .docx trong file ZIP."));
                }

                string docxFilePath = docxFiles[0];
                _logger.LogInformation($"Preview: Tìm thấy file Word: {Path.GetFileName(docxFilePath)}");

                // Tạo IFormFile từ file .docx
                using var docxStream = System.IO.File.OpenRead(docxFilePath);
                var memoryStream = new MemoryStream();
                await docxStream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                var formFile = new FormFile(memoryStream, 0, memoryStream.Length, "file", Path.GetFileName(docxFilePath))
                {
                    Headers = new HeaderDictionary(),
                    ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                };

                // Preview với đường dẫn media là thư mục đã giải nén
                var result = await _importService.PreviewImportAsync(formFile, extractPath);

                if (result.Errors.Any() && result.TotalQuestionsFound == 0)
                {
                    return BadRequest(ApiResponseFactory.ServerError(string.Join("\n", result.Errors)));
                }

                // Trả về kết quả tương tự như preview-word
                var response = new
                {
                    summary = result.Summary,
                    totalFound = result.TotalQuestionsFound,
                    validCount = result.ValidCount,
                    invalidCount = result.InvalidCount,
                    hasErrors = result.HasErrors,
                    canImport = !result.HasErrors,
                    generalErrors = result.Errors,
                    questions = result.Questions.Select(q => new
                    {
                        questionNumber = q.QuestionNumber,
                        type = q.Type,
                        status = q.Status,
                        isValid = q.IsValid,
                        isGroup = q.IsGroup,
                        clo = q.CLO,
                        preview = q.ContentPreview,
                        answersCount = q.AnswersCount,
                        correctAnswersCount = q.CorrectAnswersCount,
                        subQuestionsCount = q.SubQuestionsCount,
                        features = new
                        {
                            hasImages = q.HasImages,
                            hasAudio = q.HasAudio,
                            hasLatex = q.HasLatex
                        },
                        errors = q.Errors,
                        warnings = q.Warnings,
                        subQuestions = q.SubQuestions.Select(sub => new
                        {
                            identifier = sub.Identifier,
                            isValid = sub.IsValid,
                            answersCount = sub.AnswersCount,
                            correctAnswersCount = sub.CorrectAnswersCount,
                            features = new
                            {
                                hasImages = sub.HasImages,
                                hasAudio = sub.HasAudio,
                                hasLatex = sub.HasLatex
                            },
                            errors = sub.Errors,
                            warnings = sub.Warnings
                        })
                    })
                };

                _logger.LogInformation($"Preview ZIP completed: {result.Summary}");
                
                if (result.HasErrors)
                {
                    return Ok(ApiResponseFactory.Success(response, 
                        $"⚠️ {result.Summary}. Vui lòng sửa các lỗi trước khi import."));
                }

                return Ok(ApiResponseFactory.Success(response, 
                    $"✓ {result.Summary}. File sẵn sàng để import!"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi hệ thống khi preview file ZIP");
                return StatusCode(500, ApiResponseFactory.ServerError($"Lỗi hệ thống: {ex.Message}"));
            }
            finally
            {
                // Cleanup: Xóa thư mục tạm
                if (extractPath != null && Directory.Exists(extractPath))
                {
                    try
                    {
                        Directory.Delete(extractPath, true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Không thể xóa thư mục tạm: {extractPath}");
                    }
                }
            }
        }

        /// <summary>
        /// Import câu hỏi từ file ZIP chứa file Word và các file media (ảnh, audio)
        /// </summary>
        [HttpPost("word-with-media")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(200_000_000)] // 200MB
        public async Task<IActionResult> ImportFromWordWithMedia([FromForm] ImportZipRequestDto request)
        {
            if (request.ZipFile == null || request.ZipFile.Length == 0)
            {
                return BadRequest(ApiResponseFactory.ServerError("Vui lòng upload file .zip hợp lệ."));
            }

            var extension = Path.GetExtension(request.ZipFile.FileName).ToLower();
            if (extension != ".zip")
            {
                return BadRequest(ApiResponseFactory.ServerError("Chỉ chấp nhận file định dạng .zip"));
            }

            string? extractPath = null;

            try
            {
                // Lấy hoặc tạo thư mục wwwroot
                string rootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

                if (!Directory.Exists(rootPath))
                {
                    Directory.CreateDirectory(rootPath);
                }

                // Tạo thư mục tạm để giải nén
                extractPath = Path.Combine(rootPath, "TempExtract", Guid.NewGuid().ToString());
                Directory.CreateDirectory(extractPath);

                // Giải nén file ZIP
                _logger.LogInformation($"Giải nén file: {request.ZipFile.FileName}");
                using (var stream = request.ZipFile.OpenReadStream())
                {
                    using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
                    {
                        archive.ExtractToDirectory(extractPath);
                    }
                }

                // Tìm file .docx trong thư mục giải nén
                var docxFiles = Directory.GetFiles(extractPath, "*.docx", SearchOption.AllDirectories);
                if (docxFiles.Length == 0)
                {
                    return BadRequest(ApiResponseFactory.ServerError("Không tìm thấy file .docx trong file ZIP."));
                }

                string docxFilePath = docxFiles[0];
                _logger.LogInformation($"Tìm thấy file Word: {Path.GetFileName(docxFilePath)}");

                // Tạo IFormFile từ file .docx
                using var docxStream = System.IO.File.OpenRead(docxFilePath);
                var memoryStream = new MemoryStream();
                await docxStream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                var formFile = new FormFile(memoryStream, 0, memoryStream.Length, "file", Path.GetFileName(docxFilePath))
                {
                    Headers = new HeaderDictionary(),
                    ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                };

                // Import với đường dẫn media là thư mục đã giải nén
                var result = await _importService.ImportQuestionsAsync(formFile, request.MaPhan, extractPath);

                if (result.Errors.Any())
                {
                    if (result.SuccessCount > 0)
                    {
                        return Ok(ApiResponseFactory.Success(result,
                            $"Đã import {result.SuccessCount} câu hỏi. Có {result.Errors.Count} lỗi: " + 
                            string.Join("; ", result.Errors.Take(3)) + 
                            (result.Errors.Count > 3 ? "..." : "")));
                    }
                    return BadRequest(ApiResponseFactory.ServerError(string.Join("\n", result.Errors)));
                }

                _logger.LogInformation($"Import thành công {result.SuccessCount} câu hỏi từ ZIP");
                return Ok(ApiResponseFactory.Success(result, $"Import thành công {result.SuccessCount} câu hỏi!"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi hệ thống khi import file ZIP");
                return StatusCode(500, ApiResponseFactory.ServerError($"Lỗi hệ thống: {ex.Message}"));
            }
            finally
            {
                // Cleanup: Xóa thư mục tạm
                if (extractPath != null && Directory.Exists(extractPath))
                {
                    try
                    {
                        Directory.Delete(extractPath, true);
                        _logger.LogInformation($"Đã xóa thư mục tạm: {extractPath}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Không thể xóa thư mục tạm: {extractPath}");
                    }
                }
            }
        }

        /// <summary>
        /// Upload các file media (audio) riêng lẻ vào thư mục TempUploads
        /// Sử dụng trước khi import Word nếu file Word có tham chiếu đến các file media
        /// </summary>
        [HttpPost("upload-media")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadMedia([FromForm] List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest(ApiResponseFactory.ServerError("Vui lòng chọn ít nhất một file."));
            }

            try
            {
                string rootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                string uploadPath = Path.Combine(rootPath, "TempUploads");

                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                var uploadedFiles = new List<string>();

                foreach (var file in files)
                {
                    if (file.Length > 0)
                    {
                        // Giữ nguyên tên file hoặc tạo tên mới
                        string fileName = file.FileName;
                        string filePath = Path.Combine(uploadPath, fileName);

                        // Tạo thư mục con nếu fileName có chứa đường dẫn
                        var directory = Path.GetDirectoryName(filePath);
                        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        uploadedFiles.Add(fileName);
                        _logger.LogInformation($"Đã upload file: {fileName}");
                    }
                }

                return Ok(ApiResponseFactory.Success(new { Files = uploadedFiles, Count = uploadedFiles.Count }, 
                    $"Đã upload thành công {uploadedFiles.Count} file(s)."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi upload media files");
                return StatusCode(500, ApiResponseFactory.ServerError($"Lỗi hệ thống: {ex.Message}"));
            }
        }

        /// <summary>
        /// Xóa tất cả các file trong thư mục TempUploads (cleanup)
        /// </summary>
        [HttpDelete("clear-temp-uploads")]
        public IActionResult ClearTempUploads()
        {
            try
            {
                string rootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                string uploadPath = Path.Combine(rootPath, "TempUploads");

                if (Directory.Exists(uploadPath))
                {
                    Directory.Delete(uploadPath, true);
                    Directory.CreateDirectory(uploadPath);
                    _logger.LogInformation("Đã xóa thư mục TempUploads");
                }

                return Ok(ApiResponseFactory.Success("Đã xóa thành công thư mục tạm."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xóa TempUploads");
                return StatusCode(500, ApiResponseFactory.ServerError($"Lỗi hệ thống: {ex.Message}"));
            }
        }
    }

    /// <summary>
    /// DTO cho import từ file ZIP
    /// </summary>
    public class ImportZipRequestDto
    {
        public IFormFile ZipFile { get; set; } = null!;
        public Guid MaPhan { get; set; }
    }

    /// <summary>
    /// DTO cho preview file Word
    /// </summary>
    public class PreviewRequestDto
    {
        public IFormFile File { get; set; } = null!;
    }

    /// <summary>
    /// DTO cho preview file ZIP
    /// </summary>
    public class PreviewZipRequestDto
    {
        public IFormFile ZipFile { get; set; } = null!;
    }
}
