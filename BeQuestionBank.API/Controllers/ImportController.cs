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
        private readonly string _rootPath;
        private readonly string _tempUploadsPath;
        private readonly string _tempExtractPath;

        public ImportController(
            ImportService importService, 
            IWebHostEnvironment env,
            ILogger<ImportController> logger)
        {
            _importService = importService ?? throw new ArgumentNullException(nameof(importService));
            _env = env ?? throw new ArgumentNullException(nameof(env));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Initialize paths
            _rootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            _tempUploadsPath = Path.Combine(_rootPath, "TempUploads");
            _tempExtractPath = Path.Combine(_rootPath, "TempExtract");
            
            EnsureDirectoriesExist();
        }

        // --- MAIN PUBLIC METHODS ---

        /// <summary>
        /// Preview import câu hỏi từ file Word - Kiểm tra lỗi và validation trước khi import thực sự
        /// API này chỉ parse và validate, KHÔNG lưu vào database
        /// </summary>
        [HttpPost("preview-word")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(100_000_000)] // 100MB
        public async Task<IActionResult> PreviewImportFromWord([FromForm] PreviewRequestDto request)
        {
            // 1. Validate File
            var validationResult = ValidateWordFile(request.File);
            if (validationResult != null)
            {
                return validationResult;
            }

            try
            {
                // 2. Call Service to preview
                _logger.LogInformation($"Preview import file: {request.File.FileName}, Size: {request.File.Length} bytes");
                var result = await _importService.PreviewImportAsync(request.File, _tempUploadsPath);

                // 3. Handle errors
                if (result.Errors.Any() && result.TotalQuestionsFound == 0)
                {
                    return BadRequest(ApiResponseFactory.ServerError(string.Join("\n", result.Errors)));
                }

                // 4. Build response
                var response = BuildPreviewResponse(result);

                _logger.LogInformation($"Preview completed: {result.Summary}. {result.TypeSummary}");
                
                // 5. Return result
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
            // 1. Validate File
            var validationResult = ValidateWordFile(request.File);
            if (validationResult != null)
            {
                return validationResult;
            }

            try
            {
                // 2. Call Service to import
                _logger.LogInformation($"Bắt đầu import file: {request.File.FileName}, Size: {request.File.Length} bytes");
                var result = await _importService.ImportQuestionsAsync(request.File, request.MaPhan, _tempUploadsPath);

                // 3. Handle result
                return HandleImportResult(result);
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
            // 1. Validate File
            var validationResult = ValidateZipFile(request.ZipFile);
            if (validationResult != null)
            {
                return validationResult;
            }

            string? extractPath = null;

            try
            {
                // 2. Extract ZIP file
                extractPath = await ExtractZipFileAsync(request.ZipFile, "Preview");

                // 3. Find .docx file in extracted folder
                var docxFile = FindDocxFileInDirectory(extractPath);
                if (docxFile == null)
                {
                    return BadRequest(ApiResponseFactory.ServerError("Không tìm thấy file .docx trong file ZIP."));
                }

                // 4. Create IFormFile from .docx
                var formFile = await CreateFormFileFromPathAsync(docxFile);

                // 5. Call Service to preview
                _logger.LogInformation($"Preview: Tìm thấy file Word: {Path.GetFileName(docxFile)}");
                var result = await _importService.PreviewImportAsync(formFile, extractPath);

                // 6. Handle errors
                if (result.Errors.Any() && result.TotalQuestionsFound == 0)
                {
                    return BadRequest(ApiResponseFactory.ServerError(string.Join("\n", result.Errors)));
                }

                // 7. Build and return response
                var response = BuildPreviewResponse(result);
                _logger.LogInformation($"Preview ZIP completed: {result.Summary}. {result.TypeSummary}");
                
                if (result.HasErrors)
                {
                    return Ok(ApiResponseFactory.Success(response, 
                        $"{result.Summary}. Vui lòng sửa các lỗi trước khi import."));
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
                CleanupTempDirectory(extractPath);
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
            // 1. Validate File
            var validationResult = ValidateZipFile(request.ZipFile);
            if (validationResult != null)
            {
                return validationResult;
            }

            string? extractPath = null;

            try
            {
                // 2. Extract ZIP file
                extractPath = await ExtractZipFileAsync(request.ZipFile, "Import");

                // 3. Find .docx file in extracted folder
                var docxFile = FindDocxFileInDirectory(extractPath);
                if (docxFile == null)
                {
                    return BadRequest(ApiResponseFactory.ServerError("Không tìm thấy file .docx trong file ZIP."));
                }

                // 4. Create IFormFile from .docx
                var formFile = await CreateFormFileFromPathAsync(docxFile);

                // 5. Call Service to import
                _logger.LogInformation($"Tìm thấy file Word: {Path.GetFileName(docxFile)}");
                var result = await _importService.ImportQuestionsAsync(formFile, request.MaPhan, extractPath);

                // 6. Handle result
                return HandleImportResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi hệ thống khi import file ZIP");
                return StatusCode(500, ApiResponseFactory.ServerError($"Lỗi hệ thống: {ex.Message}"));
            }
            finally
            {
                // Cleanup: Xóa thư mục tạm
                CleanupTempDirectory(extractPath);
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
                var uploadedFiles = await SaveMediaFilesAsync(files);
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
                if (Directory.Exists(_tempUploadsPath))
                {
                    Directory.Delete(_tempUploadsPath, true);
                    Directory.CreateDirectory(_tempUploadsPath);
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

        // --- HELPER METHODS ---

        /// <summary>
        /// Đảm bảo các thư mục cần thiết tồn tại
        /// Tạo wwwroot, TempUploads, và TempExtract nếu chưa có
        /// </summary>
        private void EnsureDirectoriesExist()
        {
            if (!Directory.Exists(_rootPath))
            {
                Directory.CreateDirectory(_rootPath);
                _logger.LogInformation($"Đã tạo thư mục wwwroot: {_rootPath}");
            }

            if (!Directory.Exists(_tempUploadsPath))
            {
                Directory.CreateDirectory(_tempUploadsPath);
                _logger.LogInformation($"Đã tạo thư mục TempUploads: {_tempUploadsPath}");
            }

            if (!Directory.Exists(_tempExtractPath))
            {
                Directory.CreateDirectory(_tempExtractPath);
            }
        }

        /// <summary>
        /// Validate file Word (.docx)
        /// Kiểm tra file không null, có kích thước > 0, và có extension .docx
        /// </summary>
        private IActionResult? ValidateWordFile(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(ApiResponseFactory.ServerError("Vui lòng upload file .docx hợp lệ."));
            }

            var extension = Path.GetExtension(file.FileName).ToLower();
            if (extension != ".docx")
            {
                return BadRequest(ApiResponseFactory.ServerError("Chỉ chấp nhận file định dạng .docx"));
            }

            return null;
        }

        /// <summary>
        /// Validate file ZIP
        /// Kiểm tra file không null, có kích thước > 0, và có extension .zip
        /// </summary>
        private IActionResult? ValidateZipFile(IFormFile? zipFile)
        {
            if (zipFile == null || zipFile.Length == 0)
            {
                return BadRequest(ApiResponseFactory.ServerError("Vui lòng upload file .zip hợp lệ."));
            }

            var extension = Path.GetExtension(zipFile.FileName).ToLower();
            if (extension != ".zip")
            {
                return BadRequest(ApiResponseFactory.ServerError("Chỉ chấp nhận file định dạng .zip"));
            }

            return null;
        }

        /// <summary>
        /// Xử lý kết quả import và trả về response phù hợp
        /// </summary>
        private IActionResult HandleImportResult(ImportService.ImportResult result)
        {
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

        /// <summary>
        /// Xây dựng response cho preview
        /// Chuyển đổi ImportPreviewResult từ service sang format response cho API
        /// Bao gồm thông tin summary, validation results, và chi tiết từng câu hỏi
        /// </summary>
        private object BuildPreviewResponse(ImportService.ImportPreviewResult result)
        {
            return new
            {
                summary = result.Summary,
                typeSummary = result.TypeSummary,
                totalFound = result.TotalQuestionsFound,
                validCount = result.ValidCount,
                invalidCount = result.InvalidCount,
                hasErrors = result.HasErrors,
                canImport = !result.HasErrors,
                generalErrors = result.Errors,
                statsByType = result.StatsByType,
                questions = result.Questions.Select(q => new
                {
                    questionNumber = q.QuestionNumber,
                    type = q.Type,
                    loaiCauHoi = q.LoaiCauHoi,
                    loaiCauHoiDisplay = q.LoaiCauHoiDisplay,
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
                        loaiCauHoi = sub.LoaiCauHoi,
                        loaiCauHoiDisplay = sub.LoaiCauHoiDisplay,
                        preview = sub.ContentPreview,
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
        }

        /// <summary>
        /// Giải nén file ZIP vào thư mục tạm
        /// </summary>
        private Task<string> ExtractZipFileAsync(IFormFile zipFile, string operation)
        {
            string extractPath = Path.Combine(_tempExtractPath, Guid.NewGuid().ToString());
            Directory.CreateDirectory(extractPath);

            _logger.LogInformation($"{operation}: Giải nén file {zipFile.FileName}");
            using (var stream = zipFile.OpenReadStream())
            {
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
                {
                    archive.ExtractToDirectory(extractPath);
                }
            }

            return Task.FromResult(extractPath);
        }

        /// <summary>
        /// Tìm file .docx trong thư mục đã giải nén
        /// </summary>
        private string? FindDocxFileInDirectory(string directory)
        {
            var docxFiles = Directory.GetFiles(directory, "*.docx", SearchOption.AllDirectories);
            return docxFiles.Length > 0 ? docxFiles[0] : null;
        }

        /// <summary>
        /// Tạo IFormFile từ đường dẫn file .docx
        /// </summary>
        private async Task<IFormFile> CreateFormFileFromPathAsync(string filePath)
        {
            using var docxStream = System.IO.File.OpenRead(filePath);
            var memoryStream = new MemoryStream();
            await docxStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            return new FormFile(memoryStream, 0, memoryStream.Length, "file", Path.GetFileName(filePath))
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
            };
        }

        /// <summary>
        /// Lưu các file media vào thư mục TempUploads
        /// </summary>
        private async Task<List<string>> SaveMediaFilesAsync(List<IFormFile> files)
        {
            var uploadedFiles = new List<string>();

            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    string fileName = file.FileName;
                    string filePath = Path.Combine(_tempUploadsPath, fileName);

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

            return uploadedFiles;
        }

        /// <summary>
        /// Xóa thư mục tạm (cleanup)
        /// </summary>
        private void CleanupTempDirectory(string? directoryPath)
        {
            if (directoryPath != null && Directory.Exists(directoryPath))
            {
                try
                {
                    Directory.Delete(directoryPath, true);
                    _logger.LogInformation($"Đã xóa thư mục tạm: {directoryPath}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Không thể xóa thư mục tạm: {directoryPath}");
                }
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
