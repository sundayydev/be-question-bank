using BeQuestionBank.Domain.Interfaces.IRepositories;
using BeQuestionBank.Domain.Models;
using BeQuestionBank.Shared.DTOs.File;
using BeQuestionBank.Shared.Enums;
using BEQuestionBank.Domain.Interfaces;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using File = System.IO.File;

namespace BEQuestionBank.Core.Services
{
    public class ImportService
    {
        private const bool USE_IMAGE_AS_BASE64 = true; 
        private readonly ICauHoiRepository _cauHoiRepository;
        private readonly ILogger<ImportService> _logger;
        private const int BatchSize = 50;
        private readonly string _storagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

        // Regex Patterns
        private readonly string _breakTag = "[<br>]";
        private readonly string _startGroupTag = "[<sg>]";
        private readonly string _endGroupContentTag = "[<egc>]";
        private readonly string _endGroupTag = "[</sg>]";

        // Regex nhận diện câu hỏi: (CLO1), (<1>), (1)
        private readonly Regex _questionPattern = new Regex(@"^(\(<(\d+)>\)|\(CLO(\d+)\)|\(\d+\))", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Regex nhận diện CLO: (CLO1)
        private readonly Regex _cloPattern = new Regex(@"\(CLO(\d+)\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Regex nhận diện đáp án: A. B. C. D. (ở đầu dòng)
        private readonly Regex _answerPattern = new Regex(@"^[A-D]\.", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Regex Audio: [<audio>]path/to/file.mp3[</audio>]
        private readonly Regex _audioPattern = new Regex(@"\[<audio>\](.*?)\[</audio>\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Regex LaTeX: $...$ for inline and $$...$$ for display math
        private readonly Regex _latexInlinePattern = new Regex(@"\$([^\$]+)\$", RegexOptions.Compiled);
        private readonly Regex _latexDisplayPattern = new Regex(@"\$\$([^\$]+)\$\$", RegexOptions.Compiled);

        // Regex LaTeX with \(...\) and \[...\]
        private readonly Regex _latexInlineParenPattern = new Regex(@"\\\((.+?)\\\)", RegexOptions.Compiled);
        private readonly Regex _latexDisplayBracketPattern = new Regex(@"\\\[(.+?)\\\]", RegexOptions.Compiled | RegexOptions.Singleline);

        public ImportService(ICauHoiRepository cauHoiRepository, ILogger<ImportService> logger)
        {
            _cauHoiRepository = cauHoiRepository ?? throw new ArgumentNullException(nameof(cauHoiRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ImportResult> ImportQuestionsAsync(IFormFile wordFile, Guid maPhan, string? mediaFolderPath)
        {
            var result = new ImportResult();

            // 1. Validate File
            if (wordFile == null || wordFile.Length == 0)
            {
                result.Errors.Add("File không được để trống.");
                return result;
            }
            if (!wordFile.FileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            {
                result.Errors.Add("Chỉ hỗ trợ định dạng .docx.");
                return result;
            }

            // 2. Parse File Word
            try
            {
                var questions = await ParseWordFileAsync(wordFile, mediaFolderPath);

                if (questions.Any())
                {
                    await SaveQuestionsToDatabaseAsync(questions, maPhan, result);
                }
                else
                {
                    result.Errors.Add("Không tìm thấy câu hỏi nào trong file.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi import file Word");
                result.Errors.Add($"Lỗi xử lý file: {ex.Message}");
            }

            return result;
        }

        public async Task<List<QuestionData>> ParseWordFileAsync(IFormFile wordFile, string? mediaFolderPath)
        {
            // Folder chứa ảnh/audio gốc (nếu import file có kèm folder media bên ngoài)
            string sourceMediaFolder = mediaFolderPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp_media");

            var questions = new List<QuestionData>();

            // Tạo file tạm để OpenXml đọc
            string tempFile = Path.GetTempFileName();
            using (var stream = File.Create(tempFile))
            {
                await wordFile.CopyToAsync(stream);
            }

            using (var doc = WordprocessingDocument.Open(tempFile, false))
            {
                var body = doc.MainDocumentPart?.Document.Body;
                if (body == null) return questions;

                // Các biến trạng thái (State Machine)
                QuestionData? currentGroup = null;      // Đang xử lý nhóm nào
                QuestionData? currentQuestion = null;   // Đang xử lý câu hỏi nào (đơn hoặc con)

                bool inGroup = false;           // Đang trong thẻ <sg>...</sg>
                bool inGroupContent = false;    // Đang trong phần dẫn nhập (<sg>...<egc>)

                foreach (var para in body.Elements<Paragraph>())
                {
                    string textRaw = para.InnerText.Trim();

                    // --- 1. XỬ LÝ CÁC THẺ CẤU TRÚC (TAGS) ---

                    // Bắt đầu nhóm
                    if (textRaw.Equals(_startGroupTag, StringComparison.OrdinalIgnoreCase))
                    {
                        // Lưu câu hỏi lẻ trước đó (nếu có)
                        if (currentQuestion != null && !inGroup)
                        {
                            questions.Add(currentQuestion);
                            currentQuestion = null;
                        }

                        inGroup = true;
                        inGroupContent = true; // Bắt đầu phần nội dung dẫn
                        currentGroup = new QuestionData
                        {
                            MaCauHoi = Guid.NewGuid(),
                            IsGroup = true,
                            NoiDungNhom = "",
                            CauHoiCon = new List<QuestionData>()
                        };
                        continue;
                    }

                    // Kết thúc phần dẫn nhóm -> chuyển sang phần câu hỏi con
                    if (textRaw.Equals(_endGroupContentTag, StringComparison.OrdinalIgnoreCase) && inGroup)
                    {
                        inGroupContent = false;
                        continue;
                    }

                    // Kết thúc nhóm
                    if (textRaw.Equals(_endGroupTag, StringComparison.OrdinalIgnoreCase) && inGroup)
                    {
                        // Lưu câu hỏi con cuối cùng của nhóm
                        if (currentQuestion != null && currentGroup != null)
                        {
                            currentGroup.CauHoiCon.Add(currentQuestion);
                            currentQuestion = null;
                        }

                        if (currentGroup != null) questions.Add(currentGroup);

                        // Reset
                        currentGroup = null;
                        inGroup = false;
                        inGroupContent = false;
                        continue;
                    }

                    // Thẻ ngắt câu hỏi [<br>] -> Kết thúc câu hỏi hiện tại
                    if (textRaw.Equals(_breakTag, StringComparison.OrdinalIgnoreCase))
                    {
                        if (currentQuestion != null)
                        {
                            if (inGroup && currentGroup != null)
                            {
                                currentGroup.CauHoiCon.Add(currentQuestion);
                            }
                            else
                            {
                                questions.Add(currentQuestion);
                            }
                            currentQuestion = null;
                        }
                        continue;
                    }

                    // --- 2. XỬ LÝ NỘI DUNG DẪN CỦA NHÓM ---
                    if (inGroup && inGroupContent && currentGroup != null)
                    {
                        // Gộp HTML vào nội dung nhóm
                        string html = ParagraphToHtml(para, doc, sourceMediaFolder, null, ref currentGroup);
                        currentGroup.NoiDungNhom += html;
                        continue;
                    }

                    // --- 3. XỬ LÝ CÂU HỎI (ĐƠN HOẶC CON) ---

                    // Kiểm tra xem đây có phải dòng bắt đầu câu hỏi mới không? (VD: (CLO1), (<1>)...)
                    bool isNewQuestion = _questionPattern.IsMatch(textRaw);

                    if (isNewQuestion)
                    {
                        // Nếu đang có câu hỏi dở dang -> lưu lại
                        if (currentQuestion != null)
                        {
                            if (inGroup && currentGroup != null)
                                currentGroup.CauHoiCon.Add(currentQuestion);
                            else
                                questions.Add(currentQuestion);
                        }

                        // Tạo câu hỏi mới
                        EnumCLO? clo = ExtractCLO(textRaw);
                        currentQuestion = new QuestionData
                        {
                            MaCauHoi = Guid.NewGuid(),
                            CLO = clo,
                            MaCauHoiCha = inGroup && currentGroup != null ? currentGroup.MaCauHoi : null,
                            NoiDung = ""
                        };

                        // Lấy nội dung HTML của dòng tiêu đề này (bao gồm cả ảnh/audio nếu có)
                        currentQuestion.NoiDung = ParagraphToHtml(para, doc, sourceMediaFolder, currentQuestion, ref currentGroup);
                        continue;
                    }

                    // --- 4. XỬ LÝ ĐÁP ÁN ---
                    // Nếu đã có câu hỏi và dòng này bắt đầu bằng A. B. C. D.
                    if (currentQuestion != null && _answerPattern.IsMatch(textRaw))
                    {
                        ProcessAnswerParagraph(para, doc, sourceMediaFolder, currentQuestion, currentGroup);
                        continue;
                    }

                    // --- 5. NỘI DUNG BỔ SUNG CHO CÂU HỎI HIỆN TẠI ---
                    if (currentQuestion != null)
                    {
                        // Bỏ qua dòng trống hoàn toàn
                        string html = ParagraphToHtml(para, doc, sourceMediaFolder, currentQuestion, ref currentGroup);
                        if (!string.IsNullOrWhiteSpace(textRaw) || html.Contains("<img") || html.Contains("<audio"))
                        {
                            currentQuestion.NoiDung += "<br/>" + html;
                        }
                    }
                }

                // Kết thúc vòng lặp, check câu hỏi cuối cùng
                if (currentQuestion != null)
                {
                    if (inGroup && currentGroup != null)
                        currentGroup.CauHoiCon.Add(currentQuestion);
                    else
                        questions.Add(currentQuestion);
                }
                // Check nhóm cuối cùng (trường hợp thiếu thẻ đóng </sg>)
                if (inGroup && currentGroup != null && !questions.Contains(currentGroup))
                {
                    questions.Add(currentGroup);
                }
            }

            // Xóa file tạm
            if (File.Exists(tempFile)) File.Delete(tempFile);

            return questions;
        }

        // --- CÁC HÀM XỬ LÝ CHI TIẾT ---

        private EnumCLO? ExtractCLO(string text)
        {
            var match = _cloPattern.Match(text);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int val))
            {
                return (EnumCLO)val;
            }
            return null;
        }

        private void ProcessAnswerParagraph(Paragraph para, WordprocessingDocument doc, string sourceMediaFolder,
            QuestionData currentQuestion, QuestionData? currentGroup)
        {
            // Convert cả đoạn văn đáp án sang HTML trước để giữ định dạng (đậm, nghiêng, ảnh...)
            string fullHtml = ParagraphToHtml(para, doc, sourceMediaFolder, currentQuestion, ref currentGroup);
            string fullText = para.InnerText.Trim();

            // Xác định đáp án nào (A, B, C, D)
            char dapAnChar = fullText.ToUpper()[0];
            int thuTu = dapAnChar - 'A' + 1;

            // Kiểm tra đúng/sai (Gạch chân) và hoán vị (In nghiêng)
            bool isCorrect = false;
            bool isShuffle = true; // Mặc định là có hoán vị

            // Duyệt Runs để check style
            foreach (var run in para.Descendants<Run>())
            {
                if (run.RunProperties != null)
                {
                    // Check gạch chân -> Đúng
                    if (run.RunProperties.Underline != null && run.RunProperties.Underline.Val != UnderlineValues.None)
                        isCorrect = true;
                    // Check in nghiêng -> KHÔNG hoán vị (cố định)
                    if (run.RunProperties.Italic != null)
                        isShuffle = false;
                }
            }

            // Clean nội dung: Xóa "A." ở đầu chuỗi HTML
            string cleanHtml = Regex.Replace(fullHtml, @"^[A-D]\.\s*", "", RegexOptions.IgnoreCase);

            currentQuestion.Answers.Add(new AnswerData
            {
                NoiDung = cleanHtml,
                ThuTu = thuTu,
                LaDapAn = isCorrect,
                HoanVi = isShuffle,
                MaCauHoi = currentQuestion.MaCauHoi
            });
        }

        private string ParagraphToHtml(Paragraph para, WordprocessingDocument doc, string sourceMediaFolder,
            QuestionData? currentQuestion, ref QuestionData? currentGroup)
        {
            var sb = new StringBuilder();
            var audioSb = new StringBuilder();

            foreach (var run in para.Elements<Run>())
            {
                var text = run.InnerText;
                if (!string.IsNullOrEmpty(text))
                {
                    // Apply text formatting
                    if (run.RunProperties != null)
                    {
                        if (run.RunProperties.Bold != null) text = $"<b>{text}</b>";
                        if (run.RunProperties.Italic != null) text = $"<i>{text}</i>";
                        if (run.RunProperties.Underline != null) text = $"<u>{text}</u>";
                        if (run.RunProperties.Color != null)
                            text = $"<span style='color:#{run.RunProperties.Color.Val}'>{text}</span>";
                    }
                    sb.Append(text);
                }

                // XỬ LÝ ẢNH TRONG RUN
                var imagesHtml = ExtractImagesFromRun(run, doc, ref currentQuestion, ref currentGroup);
                sb.Append(imagesHtml);
            }

            string html = sb.ToString();

            // --- XỬ LÝ LATEX - CHUYỂN ĐỔI SANG HTML ---
            html = ConvertLatexToHtml(html);

            // --- XỬ LÝ AUDIO (Regex trên toàn bộ HTML của đoạn văn) ---
            var matches = _audioPattern.Matches(html);
            foreach (Match match in matches)
            {
                string audioFileName = match.Groups[1].Value.Trim();

                // Xử lý copy file audio
                string savedFileName = ProcessAudioFile(audioFileName, sourceMediaFolder, currentQuestion, currentGroup);

                if (!string.IsNullOrEmpty(savedFileName))
                {
                    // Tạo HTML Player - Lưu đường dẫn file
                    string audioPlayerHtml = $@"
                        <div class='audio-player-wrapper'>
                            <audio controls>
                                <source src='/media/{savedFileName}' type='audio/mpeg'>
                                Your browser does not support the audio element.
                            </audio>
                        </div>";
                    audioSb.Append(audioPlayerHtml);
                }

                // Xóa thẻ [<audio>] cũ khỏi text hiển thị
                html = html.Replace(match.Value, "");
            }

            // Ghép Audio vào cuối đoạn văn
            return html + audioSb.ToString();
        }

        /// <summary>
        /// Chuyển đổi LaTeX sang HTML với MathJax/KaTeX support
        /// </summary>
        private string ConvertLatexToHtml(string content)
        {
            if (string.IsNullOrEmpty(content))
                return content;

            // Xử lý display math trước ($$...$$)
            content = _latexDisplayPattern.Replace(content, match =>
            {
                string latex = match.Groups[1].Value.Trim();
                // Wrap với delimiters cho MathJax
                return $@"<div class='math-display'>\[{latex}\]</div>";
            });

            // Xử lý display math với \[...\]
            content = _latexDisplayBracketPattern.Replace(content, match =>
            {
                string latex = match.Groups[1].Value.Trim();
                return $@"<div class='math-display'>\[{latex}\]</div>";
            });

            // Xử lý inline math ($...$)
            content = _latexInlinePattern.Replace(content, match =>
            {
                string latex = match.Groups[1].Value.Trim();
                // Wrap với delimiters cho MathJax
                return $@"<span class='math-inline'>\({latex}\)</span>";
            });

            // Xử lý inline math với \(...\)
            content = _latexInlineParenPattern.Replace(content, match =>
            {
                string latex = match.Groups[1].Value.Trim();
                return $@"<span class='math-inline'>\({latex}\)</span>";
            });

            return content;
        }

        private string ExtractImagesFromRun(Run run, WordprocessingDocument doc, ref QuestionData? question, ref QuestionData? group)
        {
            var sb = new StringBuilder();
            var mainPart = doc.MainDocumentPart;

            // 1. Lấy Blip (Drawing)
            var blips = run.Descendants<DocumentFormat.OpenXml.Drawing.Blip>();
            foreach (var blip in blips)
            {
                if (blip?.Embed?.Value != null)
                {
                    ProcessImagePart(blip.Embed.Value, mainPart, ref question, ref group, sb);
                }
            }

            // 2. Lấy VML Shape (Legacy)
            var shapes = run.Descendants<DocumentFormat.OpenXml.Vml.ImageData>();
            foreach (var imgData in shapes)
            {
                if (imgData?.RelationshipId?.Value != null)
                {
                    ProcessImagePart(imgData.RelationshipId.Value, mainPart, ref question, ref group, sb);
                }
            }

            return sb.ToString();
        }

        private void ProcessImagePart(string rId, MainDocumentPart mainPart, ref QuestionData? question, ref QuestionData? group, StringBuilder sb)
        {
            try
            {
                var imagePart = (ImagePart)mainPart.GetPartById(rId);
                using var stream = imagePart.GetStream();

                // Đọc bytes ảnh
                byte[] imageBytes = new byte[stream.Length];
                stream.Read(imageBytes, 0, imageBytes.Length);
                string contentType = imagePart.ContentType;

                // LƯU ẢNH DƯỚI DẠNG BASE64 NHÚNG TRỰC TIẾP VÀO HTML
                    string base64 = Convert.ToBase64String(imageBytes);
                    sb.Append($"<img src=\"data:{contentType};base64,{base64}\" style=\"max-width:100%; height:auto; display:block; margin: 10px 0;\" />");

                // Không cần lưu vào bảng File riêng vì đã nhúng trong NoiDung
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xử lý ảnh trong Word");
            }
        }

        private string ProcessAudioFile(string relativePath, string sourceMediaFolder, QuestionData? question, QuestionData? group)
        {
            try
            {
                // relativePath VD: audio/47-49.mp3
                string sourcePath = Path.Combine(sourceMediaFolder, relativePath.Replace("/", Path.DirectorySeparatorChar.ToString()));

                if (File.Exists(sourcePath))
                {
                    string newFileName = $"{Guid.NewGuid()}{Path.GetExtension(sourcePath)}";
                    string destFolder = Path.Combine(_storagePath, "media");

                    if (!Directory.Exists(destFolder)) Directory.CreateDirectory(destFolder);

                    string destPath = Path.Combine(destFolder, newFileName);
                    File.Copy(sourcePath, destPath, true);

                    // Lưu thông tin file audio vào database
                    var fileData = new FileData
                    {
                        FileName = newFileName,
                        FileType = FileType.Audio,
                        FilePath = $"/media/{newFileName}" // Lưu đường dẫn
                    };

                    if (question != null) question.Files.Add(fileData);
                    else if (group != null) group.Files.Add(fileData);

                    return newFileName;
                }
                else
                {
                    _logger.LogWarning($"Không tìm thấy file audio: {sourcePath}");
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi copy audio file");
                return string.Empty;
            }
        }

        private string GetExtension(string contentType)
        {
            return contentType switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/gif" => ".gif",
                _ => ".png"
            };
        }

        // --- HÀM LƯU DATABASE ---
        public async Task SaveQuestionsToDatabaseAsync(List<QuestionData> questions, Guid maPhan, ImportResult result)
        {
            // Lấy mã số lớn nhất hiện tại
            int currentMaxMaSo = 0;
            try
            {
                var existingQuestions = await _cauHoiRepository.GetByMaPhanAsync(maPhan);
                if (existingQuestions != null && existingQuestions.Any())
                {
                    currentMaxMaSo = existingQuestions.Max(q => q.MaSoCauHoi);
                }
            }
            catch
            {
                currentMaxMaSo = 0;
            }

            var entities = new List<CauHoi>();

            foreach (var qData in questions)
            {
                currentMaxMaSo++;

                if (qData.IsGroup)
                {
                    // 1. Lưu câu hỏi nhóm (Cha)
                    var groupEntity = new CauHoi
                    {
                        MaCauHoi = qData.MaCauHoi,
                        MaPhan = maPhan,
                        NoiDung = qData.NoiDungNhom,
                        LoaiCauHoi = "Nhom",
                        MaSoCauHoi = currentMaxMaSo,
                        NgayTao = DateTime.UtcNow,
                        MaCauHoiCha = null,
                        SoCauHoiCon = qData.CauHoiCon.Count,
                        TrangThai = true,
                        HoanVi = false // Nhóm không hoán vị
                    };
                    entities.Add(groupEntity);

                    // 2. Lưu các câu hỏi con
                    foreach (var subQ in qData.CauHoiCon)
                    {
                        currentMaxMaSo++;
                        var subEntity = CreateCauHoiEntity(subQ, maPhan, currentMaxMaSo);
                        subEntity.MaCauHoiCha = groupEntity.MaCauHoi;
                        entities.Add(subEntity);
                    }
                }
                else
                {
                    // Lưu câu hỏi đơn
                    var entity = CreateCauHoiEntity(qData, maPhan, currentMaxMaSo);
                    entities.Add(entity);
                }
            }

            // Lưu Batch
            try
            {
            await _cauHoiRepository.AddRangeAsync(entities);
            result.SuccessCount = entities.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi lưu câu hỏi vào database");
                result.Errors.Add($"Lỗi lưu database: {ex.Message}");
            }
        }

        private CauHoi CreateCauHoiEntity(QuestionData dto, Guid maPhan, int maSo)
        {
            var cauHoi = new CauHoi
            {
                MaCauHoi = dto.MaCauHoi,
                MaPhan = maPhan,
                NoiDung = dto.NoiDung,
                LoaiCauHoi = "TracNghiem",
                MaSoCauHoi = maSo,
                CLO = dto.CLO,
                NgayTao = DateTime.UtcNow,
                HoanVi = true,
                TrangThai = true,
                MaCauHoiCha = dto.MaCauHoiCha,
                SoLanDuocThi = 0,
                SoLanDung = 0,

                // Map Câu Trả Lời
                CauTraLois = dto.Answers.Select(a => new CauTraLoi
                {
                    MaCauTraLoi = Guid.NewGuid(),
                    MaCauHoi = dto.MaCauHoi,
                    NoiDung = a.NoiDung,
                    ThuTu = a.ThuTu,
                    LaDapAn = a.LaDapAn,
                    HoanVi = a.HoanVi
                }).ToList()
            };

            return cauHoi;
        }

        // --- PREVIEW IMPORT (VALIDATION ONLY) ---
        
        public async Task<ImportPreviewResult> PreviewImportAsync(IFormFile wordFile, string? mediaFolderPath)
        {
            var result = new ImportPreviewResult();

            // 1. Validate File
            if (wordFile == null || wordFile.Length == 0)
            {
                result.Errors.Add("File không được để trống.");
                return result;
            }
            if (!wordFile.FileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            {
                result.Errors.Add("Chỉ hỗ trợ định dạng .docx.");
                return result;
            }

            // 2. Parse File Word
            try
            {
                var questions = await ParseWordFileAsync(wordFile, mediaFolderPath);
                result.TotalQuestionsFound = questions.Count;

                // 3. Validate từng câu hỏi
                int questionNumber = 0;
                foreach (var question in questions)
                {
                    questionNumber++;
                    var validationResult = ValidateQuestion(question, questionNumber);
                    result.Questions.Add(validationResult);

                    if (validationResult.IsValid)
                        result.ValidCount++;
                    else
                        result.InvalidCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi preview import file Word");
                result.Errors.Add($"Lỗi xử lý file: {ex.Message}");
            }

            return result;
        }

        private QuestionValidationResult ValidateQuestion(QuestionData question, int questionNumber)
        {
            var result = new QuestionValidationResult
            {
                QuestionNumber = questionNumber,
                IsGroup = question.IsGroup,
                CLO = question.CLO?.ToString(),
                ContentPreview = GetContentPreview(question),
                IsValid = true
            };

            // Validate nội dung
            if (question.IsGroup)
            {
                if (string.IsNullOrWhiteSpace(question.NoiDungNhom))
                {
                    result.Errors.Add("Nhóm câu hỏi không có nội dung dẫn");
                    result.IsValid = false;
                }

                if (question.CauHoiCon.Count == 0)
                {
                    result.Errors.Add("Nhóm câu hỏi không có câu hỏi con nào");
                    result.IsValid = false;
                }
                else
                {
                    result.SubQuestionsCount = question.CauHoiCon.Count;
                    
                    // Validate từng câu hỏi con
                    int subQuestionIndex = 0;
                    foreach (var subQ in question.CauHoiCon)
                    {
                        subQuestionIndex++;
                        var subValidation = ValidateQuestionContent(subQ, $"{questionNumber}.{subQuestionIndex}");
                        result.SubQuestions.Add(subValidation);
                        
                        if (!subValidation.IsValid)
                            result.IsValid = false;
                    }
                }
            }
            else
            {
                // Validate câu hỏi đơn
                var validation = ValidateQuestionContent(question, questionNumber.ToString());
                result.Errors.AddRange(validation.Errors);
                result.Warnings.AddRange(validation.Warnings);
                result.AnswersCount = validation.AnswersCount;
                result.CorrectAnswersCount = validation.CorrectAnswersCount;
                result.HasImages = validation.HasImages;
                result.HasAudio = validation.HasAudio;
                result.HasLatex = validation.HasLatex;
                
                if (!validation.IsValid)
                    result.IsValid = false;
            }

            return result;
        }

        private QuestionContentValidation ValidateQuestionContent(QuestionData question, string identifier)
        {
            var result = new QuestionContentValidation
            {
                Identifier = identifier,
                IsValid = true
            };

            // 1. Validate nội dung câu hỏi
            if (string.IsNullOrWhiteSpace(question.NoiDung))
            {
                result.Errors.Add($"Câu {identifier}: Không có nội dung câu hỏi");
                result.IsValid = false;
            }
            else
            {
                // Check for LaTeX
                if (question.NoiDung.Contains("$") || question.NoiDung.Contains("\\(") || question.NoiDung.Contains("\\["))
                    result.HasLatex = true;

                // Check for images
                if (question.NoiDung.Contains("<img"))
                    result.HasImages = true;

                // Check for audio
                if (question.NoiDung.Contains("<audio"))
                    result.HasAudio = true;
            }

            // 2. Validate đáp án
            result.AnswersCount = question.Answers.Count;

            if (question.Answers.Count == 0)
            {
                result.Errors.Add($"Câu {identifier}: Không có đáp án nào");
                result.IsValid = false;
            }
            else
            {
                // Check số lượng đáp án
                if (question.Answers.Count < 2)
                {
                    result.Warnings.Add($"Câu {identifier}: Chỉ có {question.Answers.Count} đáp án (khuyến nghị ít nhất 2)");
                }

                // Check đáp án đúng
                var correctAnswers = question.Answers.Where(a => a.LaDapAn).ToList();
                result.CorrectAnswersCount = correctAnswers.Count;

                if (correctAnswers.Count == 0)
                {
                    result.Errors.Add($"Câu {identifier}: Không có đáp án đúng nào (cần gạch chân đáp án đúng)");
                    result.IsValid = false;
                }
                else if (correctAnswers.Count > 1)
                {
                    result.Warnings.Add($"Câu {identifier}: Có {correctAnswers.Count} đáp án đúng (câu hỏi nhiều đáp án đúng)");
                }

                // Check nội dung đáp án
                for (int i = 0; i < question.Answers.Count; i++)
                {
                    var answer = question.Answers[i];
                    if (string.IsNullOrWhiteSpace(answer.NoiDung))
                    {
                        result.Errors.Add($"Câu {identifier}: Đáp án {(char)('A' + i)} không có nội dung");
                        result.IsValid = false;
                    }
                }

                // Check thứ tự đáp án
                var expectedOrder = question.Answers.OrderBy(a => a.ThuTu).Select(a => a.ThuTu).ToList();
                var actualOrder = Enumerable.Range(1, question.Answers.Count).ToList();
                if (!expectedOrder.SequenceEqual(actualOrder))
                {
                    result.Warnings.Add($"Câu {identifier}: Thứ tự đáp án không liên tục (có thể thiếu đáp án)");
                }
            }

            // 3. Validate CLO
            if (question.CLO == null)
            {
                result.Warnings.Add($"Câu {identifier}: Không có CLO (Course Learning Outcome)");
            }

            // 4. Validate files
            if (question.Files.Any(f => f.FileType == FileType.Audio))
            {
                result.HasAudio = true;
                foreach (var audioFile in question.Files.Where(f => f.FileType == FileType.Audio))
                {
                    // Check if audio file exists (if path is available)
                    if (string.IsNullOrEmpty(audioFile.FileName))
                    {
                        result.Warnings.Add($"Câu {identifier}: File audio không có tên file");
                    }
                }
            }

            return result;
        }

        private string GetContentPreview(QuestionData question)
        {
            string content = question.IsGroup ? question.NoiDungNhom : question.NoiDung;
            
            // Remove HTML tags for preview
            content = System.Text.RegularExpressions.Regex.Replace(content, @"<[^>]+>", " ");
            content = System.Text.RegularExpressions.Regex.Replace(content, @"\s+", " ");
            content = content.Trim();

            // Limit length
            if (content.Length > 100)
                content = content.Substring(0, 100) + "...";

            return content;
        }

        // --- CÁC CLASS DTO HỖ TRỢ ---

        public class ImportResult
        {
            public int SuccessCount { get; set; }
            public List<string> Errors { get; set; } = new List<string>();
        }

        public class ImportPreviewResult
        {
            public int TotalQuestionsFound { get; set; }
            public int ValidCount { get; set; }
            public int InvalidCount { get; set; }
            public List<string> Errors { get; set; } = new List<string>();
            public List<QuestionValidationResult> Questions { get; set; } = new List<QuestionValidationResult>();

            public bool HasErrors => Errors.Any() || InvalidCount > 0;
            public string Summary => HasErrors 
                ? $"Tìm thấy {TotalQuestionsFound} câu hỏi: {ValidCount} hợp lệ, {InvalidCount} có lỗi"
                : $"Tìm thấy {TotalQuestionsFound} câu hỏi, tất cả đều hợp lệ";
        }

        public class QuestionValidationResult
        {
            public int QuestionNumber { get; set; }
            public bool IsGroup { get; set; }
            public string? CLO { get; set; }
            public string ContentPreview { get; set; } = string.Empty;
            public bool IsValid { get; set; }
            public int AnswersCount { get; set; }
            public int CorrectAnswersCount { get; set; }
            public int SubQuestionsCount { get; set; }
            public bool HasImages { get; set; }
            public bool HasAudio { get; set; }
            public bool HasLatex { get; set; }
            public List<string> Errors { get; set; } = new List<string>();
            public List<string> Warnings { get; set; } = new List<string>();
            public List<QuestionContentValidation> SubQuestions { get; set; } = new List<QuestionContentValidation>();

            public string Status => IsValid ? "✓ Hợp lệ" : "✗ Có lỗi";
            public string Type => IsGroup ? "Nhóm" : "Đơn";
        }

        public class QuestionContentValidation
        {
            public string Identifier { get; set; } = string.Empty;
            public bool IsValid { get; set; }
            public int AnswersCount { get; set; }
            public int CorrectAnswersCount { get; set; }
            public bool HasImages { get; set; }
            public bool HasAudio { get; set; }
            public bool HasLatex { get; set; }
            public List<string> Errors { get; set; } = new List<string>();
            public List<string> Warnings { get; set; } = new List<string>();
        }

        public class QuestionData
        {
            public Guid MaCauHoi { get; set; }
            public string NoiDung { get; set; } = string.Empty;

            // Dùng cho câu hỏi nhóm
            public bool IsGroup { get; set; }
            public string NoiDungNhom { get; set; } = string.Empty;
            public List<QuestionData> CauHoiCon { get; set; } = new List<QuestionData>();

            public Guid? MaCauHoiCha { get; set; }
            public EnumCLO? CLO { get; set; }

            public List<AnswerData> Answers { get; set; } = new List<AnswerData>();
            public List<FileData> Files { get; set; } = new List<FileData>();
        }

        public class AnswerData
        {
            public Guid MaCauHoi { get; set; }
            public string NoiDung { get; set; } = string.Empty;
            public int ThuTu { get; set; }
            public bool LaDapAn { get; set; }
            public bool HoanVi { get; set; } = true;
        }

        public class FileData
        {
            public string FileName { get; set; } = string.Empty;
            public FileType FileType { get; set; } = FileType.Image;
            public Guid? MaCauHoi { get; set; }
            public string? FilePath { get; set; } // Đường dẫn file (cho Audio)
        }
    }
}
