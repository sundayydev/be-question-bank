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
using FileEntity = BeQuestionBank.Domain.Models.File;

namespace BEQuestionBank.Core.Services
{
    public class ImportService
    {
        private const bool USE_IMAGE_AS_BASE64 = true; 
        private readonly ICauHoiRepository _cauHoiRepository;
        private readonly IFileRepository _fileRepository;
        private readonly ILogger<ImportService> _logger;
        private const int BatchSize = 50;
        private readonly string _storagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

        // Regex Patterns
        private readonly string _breakTag = "[<br>]";
        private readonly string _startGroupTag = "[<sg>]";
        private readonly string _endGroupContentTag = "[<egc>]";
        private readonly string _endGroupTag = "[</sg>]";

        // Regex nhận diện câu hỏi: (CLO1), (<1>), (1) - có thể có loại câu hỏi [<MN>] sau đó
        // Pattern cải thiện: xử lý spacing linh hoạt, có thể dính liền hoặc có khoảng trắng
        // Pattern: (CLO1) [<MN>] Nội dung..., (<1>) [<TN>] Nội dung..., (1) [<TL>] Nội dung...
        // Lưu ý: (<1>) A. answer là đáp án điền từ, KHÔNG phải câu hỏi mới
        // Cải thiện: cho phép khoảng trắng tùy ý hoặc không có giữa các tag
        private readonly Regex _questionPattern = new Regex(@"^(\(CLO\s*(\d+)\)|\(\s*\d+\s*\)|\(<\s*(\d+)\s*>\)(?!\s*[A-D]\.))", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        
        // Regex nhận diện loại câu hỏi: [<TL>], [<NH>], [<TN>], [<MN>], [<GN>], [<DT>]
        // Cải thiện: xử lý spacing linh hoạt trong tag
        private readonly Regex _questionTypePattern = new Regex(@"\[\s*<\s*(?<type>TL|NH|TN|MN|GN|DT)\s*>\s*\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Regex nhận diện CLO: (CLO1) - cải thiện để xử lý spacing
        private readonly Regex _cloPattern = new Regex(@"\(CLO\s*(\d+)\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        
        // Regex để xóa prefix khỏi nội dung câu hỏi: (CLO1), (<1>), (1), [<MN>], [<TN>]...
        // Cải thiện: xử lý spacing linh hoạt
        private readonly Regex _cleanPrefixPattern = new Regex(@"^(\(CLO\s*\d+\s*\)\s*|\(<\s*\d+\s*>\s*\)\s*|\(\s*\d+\s*\)\s*)?(\[\s*<\s*(?:TL|NH|TN|MN|GN|DT)\s*>\s*\]\s*)?", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Regex nhận diện đáp án: A. B. C. D. (ở đầu dòng) - cải thiện để xử lý spacing
        private readonly Regex _answerPattern = new Regex(@"^\s*[A-D]\s*\.", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Regex nhận diện đáp án điền từ: (<1>) A. answer (cho câu hỏi điền từ)
        // Pattern linh hoạt: có thể có hoặc không có space giữa (<1>) và A.
        // Cải thiện: xử lý spacing linh hoạt trong (<1>)
        private readonly Regex _fillInAnswerPattern = new Regex(@"^\(<\s*(\d+)\s*>\)\s*[A-D]\s*\.", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        
        // Regex nhận diện pattern (<1>) đơn giản (để kiểm tra context) - cải thiện spacing
        private readonly Regex _fillInPlaceholderPattern = new Regex(@"^\(<\s*(\d+)\s*>\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Regex nhận diện placeholder trong nội dung câu hỏi: {<1>}, {<2>}, etc.
        private readonly Regex _placeholderPattern = new Regex(@"\{<(\d+)>\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Regex Audio: [<audio>]path/to/file.mp3[</audio>]
        private readonly Regex _audioPattern = new Regex(@"\[<audio>\](.*?)\[</audio>\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Regex LaTeX: $...$ for inline and $$...$$ for display math
        private readonly Regex _latexInlinePattern = new Regex(@"\$([^\$]+)\$", RegexOptions.Compiled);
        private readonly Regex _latexDisplayPattern = new Regex(@"\$\$([^\$]+)\$\$", RegexOptions.Compiled);

        // Regex LaTeX with \(...\) and \[...\]
        private readonly Regex _latexInlineParenPattern = new Regex(@"\\\((.+?)\\\)", RegexOptions.Compiled);
        private readonly Regex _latexDisplayBracketPattern = new Regex(@"\\\[(.+?)\\\]", RegexOptions.Compiled | RegexOptions.Singleline);

        public ImportService(ICauHoiRepository cauHoiRepository, IFileRepository fileRepository, ILogger<ImportService> logger)
        {
            _cauHoiRepository = cauHoiRepository ?? throw new ArgumentNullException(nameof(cauHoiRepository));
            _fileRepository = fileRepository ?? throw new ArgumentNullException(nameof(fileRepository));
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

                    // Bỏ qua dòng trống hoàn toàn (trừ khi có ảnh hoặc audio)
                    if (string.IsNullOrWhiteSpace(textRaw))
                    {
                        // Kiểm tra xem có ảnh hoặc audio không
                        bool hasImage = para.Descendants<DocumentFormat.OpenXml.Drawing.Blip>().Any() ||
                                       para.Descendants<DocumentFormat.OpenXml.Vml.ImageData>().Any();
                        if (!hasImage)
                        {
                            continue; // Bỏ qua dòng trống
                        }
                    }

                    _logger.LogDebug($"Đang xử lý dòng: '{textRaw.Substring(0, Math.Min(50, textRaw.Length))}...' | inGroup: {inGroup} | currentQuestion: {currentQuestion != null} | currentGroup: {currentGroup != null}");

                    // --- 1. XỬ LÝ CÁC THẺ CẤU TRÚC (TAGS) ---

                    // Bắt đầu nhóm
                    if (textRaw.Equals(_startGroupTag, StringComparison.OrdinalIgnoreCase))
                    {
                        // Lưu câu hỏi lẻ trước đó (nếu có và chưa được lưu bởi [<br>])
                        if (currentQuestion != null && !inGroup)
                        {
                            questions.Add(currentQuestion);
                            _logger.LogDebug($"Đã lưu câu hỏi đơn trước khi bắt đầu nhóm. Tổng số câu hỏi hiện tại: {questions.Count}");
                            currentQuestion = null;
                        }

                        // Nếu đang trong nhóm (trường hợp có [<sg>] mà chưa đóng nhóm trước), lưu nhóm trước đó
                        if (inGroup && currentGroup != null)
                        {
                            // Lưu câu hỏi con cuối cùng nếu có
                            if (currentQuestion != null)
                            {
                                currentGroup.CauHoiCon.Add(currentQuestion);
                                currentQuestion = null;
                            }
                            questions.Add(currentGroup);
                            _logger.LogWarning($"Phát hiện [<sg>] mới nhưng nhóm trước chưa đóng! Đã lưu nhóm trước với {currentGroup.CauHoiCon.Count} câu hỏi con. Tổng số câu hỏi: {questions.Count}");
                        }

                        inGroup = true;
                        inGroupContent = true; // Bắt đầu phần nội dung dẫn
                        currentGroup = new QuestionData
                        {
                            MaCauHoi = Guid.NewGuid(),
                            IsGroup = true,
                            NoiDungNhom = "",
                            CauHoiCon = new List<QuestionData>(),
                            LoaiCauHoi = "NH" // Nhóm câu hỏi
                        };
                        _logger.LogDebug($"Bắt đầu nhóm câu hỏi mới (số nhóm thứ {questions.Count(q => q.IsGroup) + 1}). Tổng số câu hỏi hiện tại: {questions.Count}");
                        continue;
                    }

                    // Kết thúc phần dẫn nhóm -> chuyển sang phần câu hỏi con
                    // Lưu ý: Sau [<egc>], vẫn tiếp tục thêm nội dung vào nhóm cho đến khi gặp câu hỏi con
                    if (textRaw.Equals(_endGroupContentTag, StringComparison.OrdinalIgnoreCase) && inGroup)
                    {
                        inGroupContent = false;
                        continue;
                    }

                    // --- 2.5. XỬ LÝ NỘI DUNG SAU [<egc>] (tiếp tục thêm vào nội dung nhóm cho đến khi gặp câu hỏi con) ---
                    if (inGroup && !inGroupContent && currentGroup != null)
                    {
                        // Kiểm tra xem có tag loại câu hỏi không: [<DT>], [<NH>], [<GN>], etc.
                        var typeMatch = _questionTypePattern.Match(textRaw);
                        if (typeMatch.Success)
                        {
                            string extractedType = typeMatch.Groups["type"].Value.ToUpper();
                            // Nếu gặp [<DT>], luôn set thành DT (ưu tiên DT)
                            // Nếu gặp [<GN>], set thành GN (Ghép nối)
                            // Nếu gặp [<TL>], set thành TL (Tự luận)
                            // Nếu gặp [<NH>], chỉ set nếu chưa có loại câu hỏi hoặc đang là NH
                            if (extractedType == "DT")
                            {
                                currentGroup.LoaiCauHoi = "DT";
                            }
                            else if (extractedType == "GN")
                            {
                                currentGroup.LoaiCauHoi = "GN";
                            }
                            else if (extractedType == "TL")
                            {
                                currentGroup.LoaiCauHoi = "TL";
                            }
                            else if (extractedType == "NH" && 
                                     (string.IsNullOrEmpty(currentGroup.LoaiCauHoi) || currentGroup.LoaiCauHoi == "NH"))
                            {
                                currentGroup.LoaiCauHoi = "NH";
                            }
                        }
                        
                        // Kiểm tra xem đây có phải là câu hỏi con không (nếu có thì dừng thêm nội dung vào nhóm)
                        bool isQuestionStart = _questionPattern.IsMatch(textRaw);
                        bool isAnswer = _answerPattern.IsMatch(textRaw);
                        bool isFillInAnswer = _fillInAnswerPattern.IsMatch(textRaw);
                        
                        // Nếu không phải là câu hỏi con, đáp án, hoặc đáp án điền từ, tiếp tục thêm vào nội dung nhóm
                        if (!isQuestionStart && !isAnswer && !isFillInAnswer && !textRaw.Equals(_breakTag, StringComparison.OrdinalIgnoreCase))
                        {
                            // Kiểm tra xem có audio tag trong dòng này không (trước khi xử lý HTML)
                            bool hasAudioTag = _audioPattern.IsMatch(textRaw);
                            
                            // Gộp HTML vào nội dung nhóm (nội dung sau [<egc>])
                            string html = ParagraphToHtml(para, doc, sourceMediaFolder, null, ref currentGroup);
                            // Xóa tag loại câu hỏi khỏi nội dung nhóm: [<DT>], [<TN>], etc. (với spacing linh hoạt)
                            html = _questionTypePattern.Replace(html, "").Trim();
                            // Xóa tag [<egc>] nếu có trong nội dung (với spacing linh hoạt)
                            html = Regex.Replace(html, @"\[\s*<\s*egc\s*>\s*\]", "", RegexOptions.IgnoreCase);
                            // Xóa tag [</egc>] nếu có trong nội dung (với spacing linh hoạt) - tag kết thúc nội dung nhóm
                            html = Regex.Replace(html, @"\[\s*</\s*egc\s*>\s*\]", "", RegexOptions.IgnoreCase);
                            // Xóa tag kết thúc nhóm [</sg>] nếu có trong nội dung (với spacing linh hoạt)
                            html = Regex.Replace(html, @"\[\s*</\s*sg\s*>\s*\]", "", RegexOptions.IgnoreCase);
                            // Xóa các tag CLO và số thứ tự khỏi nội dung nhóm nếu có
                            html = _cloPattern.Replace(html, "").Trim();
                            html = Regex.Replace(html, @"\(\s*\d+\s*\)", "", RegexOptions.IgnoreCase);
                            html = Regex.Replace(html, @"\(<\s*\d+\s*>\s*\)", "", RegexOptions.IgnoreCase);
                            
                            // Xóa span trống trước khi gộp vào nội dung nhóm (nhưng giữ lại audio player)
                            html = RemoveEmptySpans(html);
                            
                            // Thêm khoảng trắng nếu cần và gộp vào nội dung nhóm
                            // Luôn thêm nếu có nội dung hoặc có audio/ảnh
                            if (!string.IsNullOrWhiteSpace(html) || html.Contains("<img") || html.Contains("audio-player-wrapper") || hasAudioTag)
                            {
                                if (!string.IsNullOrWhiteSpace(currentGroup.NoiDungNhom))
                                {
                                    currentGroup.NoiDungNhom += " " + html;
                                }
                                else
                                {
                                    currentGroup.NoiDungNhom = html;
                                }
                            }
                            continue;
                        }
                    }

                    // Thẻ ngắt câu hỏi [<br>] -> Kết thúc câu hỏi hiện tại
                    // Xử lý TRƯỚC khi xử lý kết thúc nhóm để đảm bảo câu hỏi được lưu đúng
                    if (textRaw.Equals(_breakTag, StringComparison.OrdinalIgnoreCase))
                    {
                        if (currentQuestion != null)
                        {
                            if (inGroup && currentGroup != null)
                            {
                                currentGroup.CauHoiCon.Add(currentQuestion);
                                _logger.LogDebug($"Đã lưu câu hỏi con vào nhóm. Số câu hỏi con hiện tại: {currentGroup.CauHoiCon.Count}");
                            }
                            else
                            {
                                questions.Add(currentQuestion);
                                _logger.LogDebug($"Đã lưu câu hỏi đơn. Tổng số câu hỏi hiện tại: {questions.Count}");
                            }
                            currentQuestion = null;
                        }
                        continue;
                    }

                    // Kết thúc nhóm
                    if (textRaw.Equals(_endGroupTag, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!inGroup)
                        {
                            _logger.LogWarning($"Gặp [</sg>] nhưng không đang trong nhóm! Bỏ qua.");
                            continue;
                        }

                        // Lưu câu hỏi con cuối cùng của nhóm (nếu chưa được lưu bởi [<br>])
                        if (currentQuestion != null && currentGroup != null)
                        {
                            currentGroup.CauHoiCon.Add(currentQuestion);
                            _logger.LogDebug($"Đã lưu câu hỏi con cuối cùng vào nhóm trước khi đóng. Số câu hỏi con: {currentGroup.CauHoiCon.Count}");
                            currentQuestion = null;
                        }

                        if (currentGroup != null)
                        {
                            // Xóa span trống cuối cùng trong nội dung nhóm
                            if (!string.IsNullOrEmpty(currentGroup.NoiDungNhom))
                            {
                                currentGroup.NoiDungNhom = RemoveEmptySpans(currentGroup.NoiDungNhom);
                            }
                            
                            questions.Add(currentGroup);
                            _logger.LogDebug($"Đã lưu nhóm câu hỏi với {currentGroup.CauHoiCon.Count} câu hỏi con. Tổng số câu hỏi hiện tại: {questions.Count}");
                        }

                        // Reset
                        currentGroup = null;
                        inGroup = false;
                        inGroupContent = false;
                        _logger.LogDebug($"Đã reset trạng thái nhóm. inGroup: {inGroup}, currentGroup: {currentGroup != null}");
                        continue;
                    }

                    // --- 2. XỬ LÝ NỘI DUNG DẪN CỦA NHÓM ---
                    if (inGroup && inGroupContent && currentGroup != null)
                    {
                        // Extract CLO từ dòng (nếu có)
                        EnumCLO? clo = ExtractCLO(textRaw);
                        if (clo.HasValue && currentGroup.CLO == null)
                        {
                            currentGroup.CLO = clo;
                            _logger.LogDebug($"Đã extract CLO {clo} cho nhóm câu hỏi");
                        }
                        
                        // Kiểm tra xem có tag loại câu hỏi trong nội dung nhóm không: [<DT>], [<TN>], [<MN>], [<NH>], [<GN>], etc.
                        var typeMatch = _questionTypePattern.Match(textRaw);
                        if (typeMatch.Success)
                        {
                            string extractedType = typeMatch.Groups["type"].Value.ToUpper();
                            // Nếu gặp [<DT>], luôn set thành DT (ưu tiên DT)
                            // Nếu gặp [<GN>], set thành GN (Ghép nối)
                            // Nếu gặp [<TL>], set thành TL (Tự luận)
                            // Nếu gặp [<NH>], xác định là câu hỏi nhóm (NH)
                            // Nếu chưa có loại câu hỏi, set loại được extract
                            if (extractedType == "DT")
                            {
                                currentGroup.LoaiCauHoi = "DT";
                            }
                            else if (extractedType == "GN")
                            {
                                currentGroup.LoaiCauHoi = "GN";
                            }
                            else if (extractedType == "TL")
                            {
                                currentGroup.LoaiCauHoi = "TL";
                            }
                            else if (extractedType == "NH" || string.IsNullOrEmpty(currentGroup.LoaiCauHoi))
                            {
                                currentGroup.LoaiCauHoi = extractedType;
                            }
                        }
                        
                        // Kiểm tra xem có audio tag trong dòng này không (trước khi xử lý HTML)
                        bool hasAudioTag = _audioPattern.IsMatch(textRaw);
                        
                        // Gộp HTML vào nội dung nhóm (bao gồm cả nội dung trước [<egc>])
                        string html = ParagraphToHtml(para, doc, sourceMediaFolder, null, ref currentGroup);
                        // Xóa tag loại câu hỏi khỏi nội dung nhóm: [<DT>], [<TN>], etc. (với spacing linh hoạt)
                        html = _questionTypePattern.Replace(html, "").Trim();
                        // Xóa tag kết thúc nhóm [</sg>] nếu có trong nội dung (với spacing linh hoạt)
                        html = Regex.Replace(html, @"\[\s*</\s*sg\s*>\s*\]", "", RegexOptions.IgnoreCase);
                        // Xóa tag [<egc>] nếu có trong nội dung (với spacing linh hoạt)
                        html = Regex.Replace(html, @"\[\s*<\s*egc\s*>\s*\]", "", RegexOptions.IgnoreCase);
                        // Xóa tag [</egc>] nếu có trong nội dung (với spacing linh hoạt) - tag kết thúc nội dung nhóm
                        html = Regex.Replace(html, @"\[\s*</\s*egc\s*>\s*\]", "", RegexOptions.IgnoreCase);
                        // Xóa các tag CLO và số thứ tự khỏi nội dung nhóm nếu có
                        html = _cloPattern.Replace(html, "").Trim();
                        html = Regex.Replace(html, @"\(\s*\d+\s*\)", "", RegexOptions.IgnoreCase);
                        html = Regex.Replace(html, @"\(<\s*\d+\s*>\s*\)", "", RegexOptions.IgnoreCase);
                        
                        // Xóa span trống trước khi gộp vào nội dung nhóm
                        html = RemoveEmptySpans(html);
                        
                        // Thêm khoảng trắng nếu cần và gộp vào nội dung nhóm
                        // Luôn thêm nếu có nội dung hoặc có audio/ảnh
                        if (currentGroup != null)
                        {
                            if (!string.IsNullOrWhiteSpace(html) || html.Contains("<img") || html.Contains("audio-player-wrapper") || hasAudioTag)
                            {
                                if (!string.IsNullOrWhiteSpace(currentGroup.NoiDungNhom))
                                {
                                    currentGroup.NoiDungNhom += " " + html;
                                }
                                else
                                {
                                    currentGroup.NoiDungNhom = html;
                                }
                            }
                        }
                        continue;
                    }

                    // --- 3. XỬ LÝ ĐÁP ÁN ĐIỀN TỪ (DT) VÀ GHÉP NỐI (GN) TRƯỚC (để tránh nhận diện nhầm (<1>) là câu hỏi mới) ---
                    // Kiểm tra đáp án điền từ: (<1>) A. answer (cho DT)
                    // Kiểm tra câu hỏi ghép nối: (<1>) Câu hỏi... A. Đáp án (cho GN)
                    // Phải kiểm tra TRƯỚC khi kiểm tra câu hỏi mới để tránh nhận diện nhầm
                    bool hasFillInPlaceholder = _fillInPlaceholderPattern.IsMatch(textRaw);
                    bool isDTContext = (currentGroup != null && currentGroup.LoaiCauHoi == "DT") || 
                                      (currentQuestion != null && currentQuestion.LoaiCauHoi == "DT");
                    bool isGNContext = (currentGroup != null && currentGroup.LoaiCauHoi == "GN");
                    
                    // Nếu có pattern (<1>) và đang trong context DT, coi là đáp án điền từ
                    if (hasFillInPlaceholder && isDTContext && currentGroup != null && currentGroup.LoaiCauHoi == "DT")
                    {
                        // Trích xuất số placeholder từ (<1>)
                        var placeholderMatch = _fillInPlaceholderPattern.Match(textRaw);
                        if (placeholderMatch.Success)
                        {
                            string placeholderNumber = placeholderMatch.Groups[1].Value;
                            
                            // Kiểm tra xem có phải format đáp án điền từ: (<1>) A. answer (trên cùng dòng)
                            bool isFillInAnswer = _fillInAnswerPattern.IsMatch(textRaw);
                            
                            if (isFillInAnswer)
                            {
                                // Format: (<1>) A. answer trên cùng dòng
                                // Tạo một câu hỏi con mới cho mỗi placeholder
                                // Nội dung câu hỏi con là (1) thay vì (<1>)
                                currentQuestion = new QuestionData
                                {
                                    MaCauHoi = Guid.NewGuid(),
                                    MaCauHoiCha = currentGroup.MaCauHoi,
                                    NoiDung = $"({placeholderNumber})", // Lưu (1) thay vì (<1>)
                                    LoaiCauHoi = "DT",
                                    CLO = null // Câu hỏi con DT không có CLO, chỉ nhóm mới có
                                };
                                currentGroup.CauHoiCon.Add(currentQuestion);
                                
                                // Xử lý đáp án điền từ
                                ProcessFillInAnswerParagraph(para, doc, sourceMediaFolder, currentQuestion, currentGroup, placeholderNumber);
                                
                                // Reset currentQuestion để đáp án tiếp theo tạo câu hỏi con mới
                                currentQuestion = null;
                                continue;
                            }
                            else
                            {
                                // Format: (<1>) trên một dòng riêng, đáp án A. answer sẽ ở dòng tiếp theo
                                // Tạo câu hỏi con mới và chờ đáp án ở dòng tiếp theo
                                // Nội dung câu hỏi con là (1) thay vì (<1>)
                                currentQuestion = new QuestionData
                                {
                                    MaCauHoi = Guid.NewGuid(),
                                    MaCauHoiCha = currentGroup.MaCauHoi,
                                    NoiDung = $"({placeholderNumber})", // Lưu (1) thay vì (<1>)
                                    LoaiCauHoi = "DT",
                                    CLO = null // Câu hỏi con DT không có CLO, chỉ nhóm mới có
                                };
                                currentGroup.CauHoiCon.Add(currentQuestion);
                                
                                // Đáp án sẽ được xử lý ở phần xử lý đáp án thông thường
                                // ThuTu sẽ được set dựa vào vị trí của câu hỏi con trong nhóm
                                continue;
                            }
                        }
                    }
                    
                    // Nếu có pattern (<1>) và đang trong context GN (Ghép nối), coi là câu hỏi con
                    // Format GN: (<1>) Câu hỏi... (câu hỏi và đáp án có thể trên cùng dòng hoặc khác dòng)
                    // Trong GN, (<1>) là câu hỏi con, không phải đáp án điền từ
                    // Đáp án sẽ được xử lý ở phần xử lý đáp án thông thường (A., B., C., D.)
                    // Không cần xử lý đặc biệt ở đây, để logic câu hỏi mới xử lý

                    // --- 4. XỬ LÝ CÂU HỎI (ĐƠN HOẶC CON) ---

                    // Kiểm tra xem đây có phải dòng bắt đầu câu hỏi mới không? (VD: (CLO1), (<1>)...)
                    // Lưu ý: (<1>) đã được xử lý ở trên nếu là đáp án điền từ
                    bool isNewQuestion = _questionPattern.IsMatch(textRaw);

                    if (isNewQuestion)
                    {
                        // Nếu đang trong nhóm DT và đã kết thúc phần nội dung nhóm ([</egc>]),
                        // thì không tạo câu hỏi mới nữa, chỉ xử lý đáp án điền từ
                        if (inGroup && !inGroupContent && currentGroup != null && currentGroup.LoaiCauHoi == "DT")
                        {
                            // Bỏ qua, đáp án điền từ đã được xử lý ở trên
                            continue;
                        }
                        
                        // Nếu đang trong nhóm GN và gặp (<1>), đây là câu hỏi con ghép nối
                        // Cho phép tạo câu hỏi con bình thường (không bỏ qua)
                        
                        // Nếu đang có câu hỏi dở dang -> lưu lại
                        if (currentQuestion != null)
                        {
                            if (inGroup && currentGroup != null)
                                currentGroup.CauHoiCon.Add(currentQuestion);
                            else
                                questions.Add(currentQuestion);
                        }

                        // Tạo câu hỏi mới
                        if (inGroup && currentGroup != null)
                        {
                            // Xử lý câu hỏi con trong nhóm
                            currentQuestion = ProcessGroupSubQuestion(para, doc, sourceMediaFolder, textRaw, currentGroup);
                        }
                        else
                        {
                            // Xử lý câu hỏi đơn lẻ
                            EnumCLO? clo = ExtractCLO(textRaw);
                            string questionType = ExtractQuestionType(textRaw);
                            
                            // Tạo câu hỏi trước để audio có thể được lưu vào
                            currentQuestion = new QuestionData
                            {
                                MaCauHoi = Guid.NewGuid(),
                                CLO = clo,
                                MaCauHoiCha = null,
                                NoiDung = "",
                                LoaiCauHoi = questionType
                            };
                            
                            // Kiểm tra xem nội dung có chứa placeholder {<1>} không (câu hỏi điền từ)
                            QuestionData? tempGroup = null;
                            // Gọi ParagraphToHtml với currentQuestion để audio được lưu vào câu hỏi
                            string contentHtml = ParagraphToHtml(para, doc, sourceMediaFolder, currentQuestion, ref tempGroup);
                            if (_placeholderPattern.IsMatch(contentHtml))
                            {
                                currentQuestion.LoaiCauHoi = "DT";
                            }

                            // Lấy nội dung HTML của dòng tiêu đề này (bao gồm cả ảnh/audio nếu có)
                            // Xóa prefix (loại câu hỏi, CLO, số thứ tự) khỏi nội dung
                            currentQuestion.NoiDung = CleanQuestionContent(contentHtml);
                        }
                        continue;
                    }

                    // --- 5. XỬ LÝ ĐÁP ÁN THÔNG THƯỜNG ---
                    
                    // Nếu đã có câu hỏi và dòng này bắt đầu bằng A. B. C. D. (đáp án thông thường)
                    if (currentQuestion != null && _answerPattern.IsMatch(textRaw))
                    {
                        // Kiểm tra xem có phải câu hỏi điền từ (DT) trong nhóm không
                        // Nếu có, xử lý đáp án điền từ với ThuTu = số thứ tự câu hỏi con
                        if (currentQuestion.LoaiCauHoi == "DT" && 
                            currentGroup != null && 
                            currentGroup.LoaiCauHoi == "DT" &&
                            currentQuestion.Answers.Count == 0) // Chưa có đáp án
                        {
                            // Tìm số thứ tự của câu hỏi con trong nhóm (dựa vào vị trí trong CauHoiCon)
                            int questionIndex = currentGroup.CauHoiCon.IndexOf(currentQuestion);
                            if (questionIndex >= 0)
                            {
                                string placeholderNumber = (questionIndex + 1).ToString();
                                ProcessFillInAnswerParagraph(para, doc, sourceMediaFolder, currentQuestion, currentGroup, placeholderNumber);
                                continue;
                            }
                        }
                        
                        ProcessAnswerParagraph(para, doc, sourceMediaFolder, currentQuestion, currentGroup);
                        continue;
                    }

                    // --- 5. NỘI DUNG BỔ SUNG CHO CÂU HỎI HIỆN TẠI ---
                    if (currentQuestion != null)
                    {
                        // Kiểm tra xem có audio tag trong dòng này không (trước khi xử lý HTML)
                        bool hasAudioTag = _audioPattern.IsMatch(textRaw);
                        
                        // Bỏ qua dòng trống hoàn toàn (trừ khi có audio hoặc ảnh)
                        string html = ParagraphToHtml(para, doc, sourceMediaFolder, currentQuestion, ref currentGroup);
                        // Xóa tag kết thúc nhóm [</sg>] nếu có trong nội dung
                        html = Regex.Replace(html, @"\[</sg>\]", "", RegexOptions.IgnoreCase);
                        
                        // Thêm nội dung nếu không rỗng hoặc có ảnh/audio
                        if (!string.IsNullOrWhiteSpace(textRaw) || html.Contains("<img") || html.Contains("audio-player-wrapper") || hasAudioTag)
                        {
                            currentQuestion.NoiDung += "<br/>" + html;
                            
                            // Nếu nội dung có chứa placeholder {<1>}, đây là câu hỏi điền từ
                            // CHỈ áp dụng nếu câu hỏi không phải là câu hỏi con trong nhóm NH (vì trong nhóm NH, placeholder chỉ có trong nội dung nhóm)
                            bool isSubQuestionInNHGroup = currentGroup != null && 
                                                          currentGroup.LoaiCauHoi == "NH" && 
                                                          currentQuestion.MaCauHoiCha.HasValue;
                            
                            if (!isSubQuestionInNHGroup && _placeholderPattern.IsMatch(currentQuestion.NoiDung) && currentQuestion.LoaiCauHoi != "DT")
                            {
                                currentQuestion.LoaiCauHoi = "DT";
                            }
                        }
                    }
                }

                // Kết thúc vòng lặp, check câu hỏi cuối cùng
                if (currentQuestion != null)
                {
                    if (inGroup && currentGroup != null)
                    {
                        currentGroup.CauHoiCon.Add(currentQuestion);
                        _logger.LogDebug($"Cuối vòng lặp: Đã lưu câu hỏi con cuối cùng vào nhóm. Số câu hỏi con: {currentGroup.CauHoiCon.Count}");
                    }
                    else
                    {
                        questions.Add(currentQuestion);
                        _logger.LogDebug($"Cuối vòng lặp: Đã lưu câu hỏi đơn cuối cùng. Tổng số câu hỏi: {questions.Count}");
                    }
                }
                // Check nhóm cuối cùng (trường hợp thiếu thẻ đóng </sg>)
                if (inGroup && currentGroup != null && !questions.Contains(currentGroup))
                {
                    questions.Add(currentGroup);
                    _logger.LogDebug($"Cuối vòng lặp: Đã lưu nhóm cuối cùng (thiếu thẻ đóng). Tổng số câu hỏi: {questions.Count}");
                }

                _logger.LogInformation($"Kết thúc parse: Tổng số câu hỏi đã parse: {questions.Count}");
                foreach (var q in questions)
                {
                    if (q.IsGroup)
                    {
                        _logger.LogInformation($"  - Nhóm câu hỏi với {q.CauHoiCon.Count} câu hỏi con");
                    }
                    else
                    {
                        _logger.LogInformation($"  - Câu hỏi đơn: {q.NoiDung.Substring(0, Math.Min(50, q.NoiDung.Length))}...");
                    }
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

        /// <summary>
        /// Trích xuất loại câu hỏi từ text dựa trên prefix
        /// TL: Tự luận, NH: Nhóm, TN: Trắc nghiệm 1 đáp án, MN: Nhiều đáp án, GN: Ghép nối, DT: Điền từ
        /// </summary>
        private string ExtractQuestionType(string text)
        {
            var match = _questionTypePattern.Match(text);
            if (match.Success)
            {
                return match.Groups["type"].Value.ToUpper();
            }
            // Mặc định là TN (Trắc nghiệm) nếu không có prefix
            return "TN";
        }

        /// <summary>
        /// Chuyển đổi loại câu hỏi viết tắt sang tên đầy đủ
        /// </summary>
        private string GetQuestionTypeName(string? typeCode)
        {
            if (string.IsNullOrWhiteSpace(typeCode))
                return "Câu hỏi con"; // Câu hỏi con trong nhóm NH không có loại
            
            return typeCode.ToUpper() switch
            {
                "TL" => "Tự luận",
                "NH" => "Nhóm",
                "TN" => "Trắc nghiệm",
                "MN" => "Nhiều đáp án",
                "GN" => "Ghép nối",
                "DT" => "Điền từ",
                _ => "Trắc nghiệm"
            };
        }

        /// <summary>
        /// Xóa các prefix (loại câu hỏi, CLO, số thứ tự) khỏi nội dung câu hỏi
        /// VD: "(TN)(CLO1) Câu hỏi..." -> "Câu hỏi..."
        /// Cũng xóa các tag [] và CLO thừa trong nội dung
        /// Xử lý cả trường hợp tag bị tách do formatting HTML
        /// Cải thiện: Xóa triệt để các tag rác mà không làm hỏng định dạng HTML (đậm, nghiêng, ảnh)
        /// </summary>
        private string CleanQuestionContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return content;
            
            // Bước 1: Xóa prefix ở đầu chuỗi (CLO, số thứ tự, loại câu hỏi)
            content = _cleanPrefixPattern.Replace(content, "").TrimStart();
            
            // Bước 2: Xử lý các tag bị tách do formatting HTML
            // Strategy: Tạm thời thay thế HTML tags bằng placeholder, xóa pattern, rồi khôi phục
            var htmlTagPlaceholders = new Dictionary<string, string>();
            int placeholderIndex = 0;
            
            // Thay thế tất cả HTML tags bằng placeholder
            string contentWithPlaceholders = Regex.Replace(content, @"<[^>]+>", match =>
            {
                string placeholder = $"__HTML_TAG_{placeholderIndex}__";
                htmlTagPlaceholders[placeholder] = match.Value;
                placeholderIndex++;
                return placeholder;
            });
            
            // Bước 3: Xóa các pattern rác trong content đã thay thế (không có HTML tags)
            // Xóa tag loại câu hỏi: [<TN>], [<MN>], etc. (với spacing linh hoạt)
            contentWithPlaceholders = Regex.Replace(contentWithPlaceholders, @"\[\s*<\s*(?:TL|NH|TN|MN|GN|DT)\s*>\s*\]", "", RegexOptions.IgnoreCase);
            
            // Xóa các CLO: (CLO1), (CLO2), etc. (với spacing linh hoạt)
            contentWithPlaceholders = Regex.Replace(contentWithPlaceholders, @"\(CLO\s*\d+\s*\)", "", RegexOptions.IgnoreCase);
            
            // Xóa số thứ tự: (1), (2), (<1>), (<2>), etc. (với spacing linh hoạt)
            contentWithPlaceholders = Regex.Replace(contentWithPlaceholders, @"\(\s*\d+\s*\)", "", RegexOptions.IgnoreCase);
            contentWithPlaceholders = Regex.Replace(contentWithPlaceholders, @"\(<\s*\d+\s*>\s*\)", "", RegexOptions.IgnoreCase);
            
            // Xóa các dấu ngoặc vuông trống: [], [ ]
            contentWithPlaceholders = Regex.Replace(contentWithPlaceholders, @"\[\s*\]", "", RegexOptions.IgnoreCase);
            
            // Xóa tag kết thúc nhóm: [</sg>]
            contentWithPlaceholders = Regex.Replace(contentWithPlaceholders, @"\[\s*</\s*sg\s*>\s*\]", "", RegexOptions.IgnoreCase);
            
            // Bước 4: Khôi phục HTML tags
            content = contentWithPlaceholders;
            foreach (var kvp in htmlTagPlaceholders.OrderByDescending(x => x.Key.Length))
            {
                content = content.Replace(kvp.Key, kvp.Value);
            }
            
            // Bước 5: Xóa lại các pattern có thể còn sót (fallback - xử lý trường hợp không bị tách)
            // Xóa tag loại câu hỏi bình thường (không bị tách)
            content = _questionTypePattern.Replace(content, "").Trim();
            
            // Xóa các CLO trong nội dung
            content = _cloPattern.Replace(content, "").Trim();
            
            // Xóa số thứ tự còn sót
            content = Regex.Replace(content, @"\(\s*\d+\s*\)", "", RegexOptions.IgnoreCase);
            content = Regex.Replace(content, @"\(<\s*\d+\s*>\s*\)", "", RegexOptions.IgnoreCase);
            
            // Xóa các dấu ngoặc vuông trống
            content = Regex.Replace(content, @"\[\s*\]", "", RegexOptions.IgnoreCase);
            
            // Xóa tag kết thúc nhóm
            content = Regex.Replace(content, @"\[\s*</\s*sg\s*>\s*\]", "", RegexOptions.IgnoreCase);
            
            // Bước 6: Xóa các tag HTML rỗng còn sót lại (nhưng giữ lại các tag quan trọng)
            // Xóa các tag formatting rỗng: <b></b>, <i></i>, <u></u>
            content = Regex.Replace(content, @"<b>\s*</b>", "", RegexOptions.IgnoreCase);
            content = Regex.Replace(content, @"<i>\s*</i>", "", RegexOptions.IgnoreCase);
            content = Regex.Replace(content, @"<u>\s*</u>", "", RegexOptions.IgnoreCase);
            
            // Xóa span rỗng (nhưng KHÔNG xóa nếu chứa img, audio, span con, hoặc text)
            // Pattern: <span...> chỉ có whitespace </span> (không chứa img, audio, span con, hoặc text)
            content = Regex.Replace(content, @"<span[^>]*>\s*(?!.*(?:<img|<audio|<span|\[<|math-inline|math-display))</span>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            
            // Xóa các span rỗng (trừ span chứa math-display hoặc math-inline)
            content = Regex.Replace(content, @"<span[^>]*>\s*(?!.*(?:math-display|math-inline))</span>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            
            // Bước 7: LaTeX đã được convert trong ParagraphToHtml, không cần convert lại ở đây
            // (Đã được xử lý trong ParagraphToHtml trước khi gọi CleanQuestionContent)
            
            // Bước 8: Làm sạch khoảng trắng thừa (nhưng giữ lại khoảng trắng trong nội dung)
            // Xóa khoảng trắng thừa giữa các tag HTML (nhưng không xóa trong text)
            content = Regex.Replace(content, @">\s+<", "><", RegexOptions.Compiled);
            
            // Xóa khoảng trắng ở đầu và cuối
            content = content.Trim();
            
            return content;
        }

        /// <summary>
        /// Xử lý đáp án cho câu hỏi Trắc nghiệm (TN) - KHÔNG SỬA GÌ HÀM NÀY
        /// Logic đã hoạt động đúng, tách riêng để không bị ảnh hưởng khi sửa các loại câu hỏi khác
        /// </summary>
        private void ProcessTNAnswerParagraph(Paragraph para, WordprocessingDocument doc, string sourceMediaFolder,
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

            // Duyệt Runs để check style - chỉ check các run trực tiếp trong paragraph
            foreach (var run in para.Elements<Run>())
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

            // Clean nội dung: Xóa "A. B. C. D." khỏi HTML (có thể nằm trong span hoặc thẻ <u>)
            string cleanHtml = fullHtml;
            // Xóa "A. " ở đầu chuỗi nếu có
            cleanHtml = Regex.Replace(cleanHtml, @"^[A-D]\.\s*", "", RegexOptions.IgnoreCase);
            // Xóa thẻ <u>A.</u> trong span: <span class='text-content'><u>B.</u></span>
            cleanHtml = Regex.Replace(cleanHtml, @"(<span[^>]*>)\s*<u>[A-D]\.</u>\s*", "$1", RegexOptions.IgnoreCase);
            cleanHtml = Regex.Replace(cleanHtml, @"(<span[^>]*>)\s*<b>[A-D]\.</b>\s*", "$1", RegexOptions.IgnoreCase);
            cleanHtml = Regex.Replace(cleanHtml, @"(<span[^>]*>)\s*<i>[A-D]\.</i>\s*", "$1", RegexOptions.IgnoreCase);
            // Xóa "A. " trong thẻ span đầu tiên: <span class='text-content'>A. </span> hoặc <span class='text-content'>A. content</span>
            cleanHtml = Regex.Replace(cleanHtml, @"(<span[^>]*>)\s*[A-D]\.\s+", "$1", RegexOptions.IgnoreCase);
            // Xóa thẻ <u>A.</u> hoặc <b>A.</b> hoặc <i>A.</i> ở bất kỳ đâu (không trong span)
            cleanHtml = Regex.Replace(cleanHtml, @"<u>[A-D]\.</u>\s*", "", RegexOptions.IgnoreCase);
            cleanHtml = Regex.Replace(cleanHtml, @"<b>[A-D]\.</b>\s*", "", RegexOptions.IgnoreCase);
            cleanHtml = Regex.Replace(cleanHtml, @"<i>[A-D]\.</i>\s*", "", RegexOptions.IgnoreCase);
            // Xóa thẻ span rỗng sau khi xóa A. B. C. D.
            cleanHtml = Regex.Replace(cleanHtml, @"<span[^>]*>\s*</span>", "", RegexOptions.IgnoreCase);

            currentQuestion.Answers.Add(new AnswerData
            {
                NoiDung = cleanHtml,
                ThuTu = thuTu,
                LaDapAn = isCorrect,
                HoanVi = isShuffle,
                MaCauHoi = currentQuestion.MaCauHoi
            });
        }

        /// <summary>
        /// Xử lý câu hỏi con trong nhóm (NH, DT, GN)
        /// Xử lý các câu hỏi con nằm giữa [<egc>] và [</sg>]
        /// </summary>
        private QuestionData ProcessGroupSubQuestion(Paragraph para, WordprocessingDocument doc, string sourceMediaFolder,
            string textRaw, QuestionData currentGroup)
        {
            EnumCLO? clo = ExtractCLO(textRaw);
            string? questionType = null; // Mặc định là null cho câu hỏi con trong nhóm NH
            
            // Extract loại câu hỏi từ tag [<TN>], [<MN>], etc. trong câu hỏi con
            var typeMatch = _questionTypePattern.Match(textRaw);
            if (typeMatch.Success)
            {
                string extractedType = typeMatch.Groups["type"].Value.ToUpper();
                // Câu hỏi con trong nhóm NH có thể là TN hoặc MN
                    // Cho phép tất cả loại câu hỏi: TN, MN, DT, GN, TL (nếu có tag rõ ràng)
                questionType = extractedType;
            }
            
            // Nếu nhóm có loại câu hỏi, kế thừa loại từ nhóm (nếu chưa có loại từ tag)
            if (string.IsNullOrEmpty(questionType) && !string.IsNullOrEmpty(currentGroup.LoaiCauHoi))
            {
                // Nếu nhóm là DT (Điền từ), câu hỏi con cũng là DT
                if (currentGroup.LoaiCauHoi == "DT")
                {
                    questionType = "DT";
                }
                // Nếu nhóm là GN (Ghép nối), câu hỏi con cũng là GN
                else if (currentGroup.LoaiCauHoi == "GN")
                {
                    questionType = "GN";
                }
                // Nếu nhóm là NH (Nhóm), câu hỏi con mặc định là TN nếu không có tag
                else if (currentGroup.LoaiCauHoi == "NH" && string.IsNullOrEmpty(questionType))
                {
                    questionType = "TN"; // Mặc định là TN cho câu hỏi con trong nhóm NH
                }
                // Nếu nhóm là TL (Tự luận), câu hỏi con cũng là TL
                else if (currentGroup.LoaiCauHoi == "TL")
                {
                    questionType = "TL";
                }
            }
            
            // Kiểm tra xem nội dung có chứa placeholder {<1>} không (câu hỏi điền từ)
            // CHỈ kiểm tra nếu nhóm KHÔNG phải là NH (vì trong nhóm NH, placeholder chỉ có trong nội dung nhóm, không phải trong câu hỏi con)
            QuestionData? tempGroup = currentGroup;
            string contentHtml = ParagraphToHtml(para, doc, sourceMediaFolder, null, ref tempGroup);
            
            // Chỉ kiểm tra placeholder nếu nhóm là DT, không kiểm tra cho nhóm NH hoặc GN
            if (currentGroup.LoaiCauHoi != "NH" && currentGroup.LoaiCauHoi != "GN" && _placeholderPattern.IsMatch(contentHtml))
            {
                questionType = "DT";
            }
            
            var subQuestion = new QuestionData
            {
                MaCauHoi = Guid.NewGuid(),
                // Câu hỏi con trong nhóm DT, GN, NH, và TL không có CLO, chỉ nhóm mới có
                CLO = (currentGroup.LoaiCauHoi == "DT" || currentGroup.LoaiCauHoi == "GN" || currentGroup.LoaiCauHoi == "NH" || currentGroup.LoaiCauHoi == "TL") ? null : (clo ?? currentGroup.CLO),
                MaCauHoiCha = currentGroup.MaCauHoi,
                NoiDung = "",
                LoaiCauHoi = questionType // null cho nhóm NH, "DT" cho nhóm DT, "GN" cho nhóm GN, "TL" cho nhóm TL
            };

            // Lấy nội dung HTML của dòng tiêu đề này (bao gồm cả ảnh/audio nếu có)
            // Xóa prefix (loại câu hỏi, CLO, số thứ tự) khỏi nội dung
            subQuestion.NoiDung = CleanQuestionContent(contentHtml);
            
            return subQuestion;
        }

        /// <summary>
        /// Xử lý nội dung dẫn của nhóm câu hỏi (NH)
        /// Xử lý phần nội dung giữa [<sg>] và [<egc>]
        /// </summary>
        private void ProcessGroupContentParagraph(Paragraph para, WordprocessingDocument doc, string sourceMediaFolder,
            string textRaw, QuestionData currentGroup)
        {
            // Kiểm tra xem có tag loại câu hỏi trong nội dung nhóm không: [<DT>], [<TN>], [<MN>], [<NH>], etc.
            var typeMatch = _questionTypePattern.Match(textRaw);
            if (typeMatch.Success)
            {
                string extractedType = typeMatch.Groups["type"].Value.ToUpper();
                // Nếu gặp [<DT>], luôn set thành DT (ưu tiên DT)
                // Nếu gặp [<NH>], xác định là câu hỏi nhóm (NH)
                // Nếu chưa có loại câu hỏi, set loại được extract
                if (extractedType == "DT")
                {
                    currentGroup.LoaiCauHoi = "DT";
                }
                else if (extractedType == "NH" || string.IsNullOrEmpty(currentGroup.LoaiCauHoi))
                {
                    currentGroup.LoaiCauHoi = extractedType;
                }
            }
            
            // Gộp HTML vào nội dung nhóm
            QuestionData? tempGroup = currentGroup;
            string html = ParagraphToHtml(para, doc, sourceMediaFolder, null, ref tempGroup);
            
            // Xóa tag loại câu hỏi khỏi nội dung nhóm: [<DT>], [<TN>], etc.
            html = _questionTypePattern.Replace(html, "").Trim();
            
            // Xóa các tag CLO và số thứ tự khỏi nội dung nhóm nếu có
            html = _cloPattern.Replace(html, "").Trim();
            html = Regex.Replace(html, @"^\(\d+\)\s*", "", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"^\(<\d+>\)\s*", "", RegexOptions.IgnoreCase);
            
            // Xóa các dấu ngoặc vuông trống [] hoặc [ ]
            html = Regex.Replace(html, @"\[\s*\]", "", RegexOptions.IgnoreCase);
            
            // Xóa tag kết thúc nhóm [</sg>] nếu có trong nội dung
            html = Regex.Replace(html, @"\[</sg>\]", "", RegexOptions.IgnoreCase);
            
            // Thêm khoảng trắng nếu cần và gộp vào nội dung nhóm
            if (!string.IsNullOrWhiteSpace(currentGroup.NoiDungNhom) && !string.IsNullOrWhiteSpace(html))
            {
                currentGroup.NoiDungNhom += " " + html;
            }
            else
            {
                currentGroup.NoiDungNhom += html;
            }
        }

        /// <summary>
        /// Xử lý đáp án cho các loại câu hỏi (routing function)
        /// </summary>
        private void ProcessAnswerParagraph(Paragraph para, WordprocessingDocument doc, string sourceMediaFolder,
            QuestionData currentQuestion, QuestionData? currentGroup)
        {
            // Nếu là câu hỏi TN (Trắc nghiệm), dùng hàm riêng đã được tách ra
            if (currentQuestion.LoaiCauHoi?.ToUpper() == "TN")
            {
                ProcessTNAnswerParagraph(para, doc, sourceMediaFolder, currentQuestion, currentGroup);
                return;
            }

            // Các loại câu hỏi khác có thể thêm logic ở đây
            // Hiện tại mặc định dùng logic TN cho các loại khác
            ProcessTNAnswerParagraph(para, doc, sourceMediaFolder, currentQuestion, currentGroup);
        }

        /// <summary>
        /// Xử lý đáp án cho câu hỏi điền từ: (<1>) A. answer
        /// Tìm placeholder {<1>} trong nội dung và thay thế bằng đáp án đúng
        /// </summary>
        private void ProcessFillInAnswerParagraph(Paragraph para, WordprocessingDocument doc, string sourceMediaFolder,
            QuestionData currentQuestion, QuestionData? currentGroup, string placeholderNumber)
        {
            string fullHtml = ParagraphToHtml(para, doc, sourceMediaFolder, currentQuestion, ref currentGroup);
            string fullText = para.InnerText.Trim();

            string answerHtml;
            // Kiểm tra xem có format (<1>) A. answer trên cùng dòng không
            if (_fillInAnswerPattern.IsMatch(fullText))
            {
                // Format: (<1>) A. answer trên cùng dòng
                // Trích xuất HTML của đáp án (bỏ phần (<1>) A.)
                answerHtml = Regex.Replace(fullHtml, @"^\(<(\d+)>\)\s*[A-D]\.\s*", "", RegexOptions.IgnoreCase).Trim();
            }
            else
            {
                // Format: A. answer (không có (<1>) trên cùng dòng)
                // Xóa "A." ở đầu và xóa (<1>) nếu có trong HTML
                answerHtml = fullHtml;
                // Xóa (<1>) nếu có trong HTML (trường hợp (<1>) ở dòng trước)
                answerHtml = Regex.Replace(answerHtml, @"\(<(\d+)>\)\s*", "", RegexOptions.IgnoreCase);
            }
            // Xóa pattern (<1>), (<2>), (<3>) khỏi HTML (có thể nằm trong span)
            // Xóa (<1>) ở đầu hoặc trong span: <span class='text-content'>(<3>) hiếu</span>
            // Lặp lại để đảm bảo xóa hết
            bool changed = true;
            int maxIterations = 5;
            int iteration = 0;
            while (changed && iteration < maxIterations)
            {
                iteration++;
                string previousHtml = answerHtml;
                answerHtml = Regex.Replace(answerHtml, @"\(<(\d+)>\)\s*", "", RegexOptions.IgnoreCase);
                changed = (answerHtml != previousHtml);
            }
            
            // Xóa "A. B. C. D." khỏi HTML nếu còn sót lại (có thể nằm trong span hoặc thẻ <u>)
            answerHtml = Regex.Replace(answerHtml, @"^[A-D]\.\s*", "", RegexOptions.IgnoreCase);
            // Xóa thẻ <u>A.</u> trong span: <span class='text-content'><u>B.</u></span>
            answerHtml = Regex.Replace(answerHtml, @"(<span[^>]*>)\s*<u>[A-D]\.</u>\s*", "$1", RegexOptions.IgnoreCase);
            answerHtml = Regex.Replace(answerHtml, @"(<span[^>]*>)\s*<b>[A-D]\.</b>\s*", "$1", RegexOptions.IgnoreCase);
            answerHtml = Regex.Replace(answerHtml, @"(<span[^>]*>)\s*<i>[A-D]\.</i>\s*", "$1", RegexOptions.IgnoreCase);
            // Xóa "A. " trong thẻ span
            answerHtml = Regex.Replace(answerHtml, @"(<span[^>]*>)\s*[A-D]\.\s+", "$1", RegexOptions.IgnoreCase);
            // Xóa thẻ <u>A.</u> hoặc <b>A.</b> hoặc <i>A.</i> ở bất kỳ đâu (không trong span)
            answerHtml = Regex.Replace(answerHtml, @"<u>[A-D]\.</u>\s*", "", RegexOptions.IgnoreCase);
            answerHtml = Regex.Replace(answerHtml, @"<b>[A-D]\.</b>\s*", "", RegexOptions.IgnoreCase);
            answerHtml = Regex.Replace(answerHtml, @"<i>[A-D]\.</i>\s*", "", RegexOptions.IgnoreCase);
            // Xóa thẻ span rỗng
            answerHtml = Regex.Replace(answerHtml, @"<span[^>]*>\s*</span>", "", RegexOptions.IgnoreCase).Trim();

            // Kiểm tra đúng/sai (Gạch chân) và hoán vị (In nghiêng)
            // Mặc định: đáp án điền từ là đúng và không hoán vị
            bool isCorrect = true; // Mặc định là đúng
            bool isShuffle = false; // Câu hỏi điền từ không hoán vị

            // Duyệt Runs để check style - chỉ check các run trực tiếp trong paragraph
            // Nếu có gạch chân, giữ nguyên isCorrect = true
            // Nếu có in nghiêng, set isShuffle = false (không hoán vị)
            foreach (var run in para.Elements<Run>())
            {
                if (run.RunProperties != null)
                {
                    // Gạch chân không ảnh hưởng (mặc định đã là đúng)
                    // In nghiêng = không hoán vị (đã set mặc định)
                    if (run.RunProperties.Italic != null)
                        isShuffle = false;
                }
            }

            // Câu hỏi con điền từ không có nội dung, chỉ có đáp án
            // Không cần thay thế placeholder vì câu hỏi con không có nội dung

            // Lưu thông tin đáp án (dùng placeholder number làm thứ tự)
            if (int.TryParse(placeholderNumber, out int placeholderIndex))
            {
                // Lưu đáp án vào câu hỏi hiện tại
                currentQuestion.Answers.Add(new AnswerData
                {
                    NoiDung = answerHtml,
                    ThuTu = placeholderIndex, // Dùng số placeholder làm thứ tự
                    LaDapAn = isCorrect,
                    HoanVi = isShuffle,
                    MaCauHoi = currentQuestion.MaCauHoi
                });
            }
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
                    // Không convert LaTeX ở đây - sẽ convert sau khi merge các span
                    // Apply text formatting
                    if (run.RunProperties != null)
                    {
                        if (run.RunProperties.Bold != null) text = $"<b>{text}</b>";
                        if (run.RunProperties.Italic != null) text = $"<i>{text}</i>";
                        if (run.RunProperties.Underline != null) text = $"<u>{text}</u>";
                        if (run.RunProperties.Color != null)
                            text = $"<span style='color:#{run.RunProperties.Color.Val}'>{text}</span>";
                    }
                    // Wrap text trong span để đảm bảo tất cả nội dung đều nằm trong HTML tags
                    sb.Append($"<span class='text-content'>{text}</span>");
                }

                // XỬ LÝ ẢNH TRONG RUN
                var imagesHtml = ExtractImagesFromRun(run, doc, ref currentQuestion, ref currentGroup);
                sb.Append(imagesHtml);
            }

            string html = sb.ToString();
            
            // Bước 1: Merge các span liền kề có chứa LaTeX delimiters ($$ hoặc $) để tránh LaTeX bị tách
            // Pattern: tìm các span liền kề có chứa $$ hoặc $ và merge chúng lại
            // Ví dụ: <span class='text-content'>$$</span><span class='text-content'>3x^2</span><span class='text-content'>$$</span>
            // -> <span class='text-content'>$$3x^2$$</span>
            html = MergeLatexSpans(html);
            
            // Bước 2: Convert LaTeX trên toàn bộ HTML (sau khi đã merge)
            html = ConvertLatexToHtml(html);
            
            // Xóa các tag HTML rỗng (như <b></b>, <i></i>, etc.) sau khi xử lý
            // Lưu ý: KHÔNG xóa span rỗng nếu nó có thể chứa ảnh hoặc nội dung quan trọng
            html = Regex.Replace(html, @"<b>\s*</b>", "", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<i>\s*</i>", "", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<u>\s*</u>", "", RegexOptions.IgnoreCase);
            
            // Xóa span rỗng (không chứa nội dung, img, audio, hoặc các phần tử khác)
            // Lặp lại cho đến khi không còn span rỗng nào
            bool changed = true;
            int maxIterations = 10;
            int iteration = 0;
            while (changed && iteration < maxIterations)
            {
                iteration++;
                string previousHtml = html;
                // Pattern: <span class='text-content'> chỉ có whitespace </span> (không chứa img, audio, span con, hoặc text)
                html = Regex.Replace(html, @"<span[^>]*class=['""]text-content['""][^>]*>\s*(?!.*(?:<img|<audio|<span|\[<|math-inline|math-display))</span>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                // Xóa span rỗng không có class (nếu có)
                html = Regex.Replace(html, @"<span[^>]*>\s*(?!.*(?:<img|<audio|<span|\[<|math-inline|math-display|text-content))</span>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                changed = (html != previousHtml);
            }

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
                        <span class='audio-player-wrapper'>
                            <audio controls>
                                <source src='/media/{savedFileName}' type='audio/mpeg'>
                                Your browser does not support the audio element.
                            </audio>
                        </span>";
                    audioSb.Append(audioPlayerHtml);
                }
                else
                {
                    // Lưu thông tin file audio không tìm thấy để cảnh báo trong validation
                    if (currentQuestion != null)
                    {
                        currentQuestion.MissingAudioFiles.Add(audioFileName);
                    }
                    else if (currentGroup != null)
                    {
                        currentGroup.MissingAudioFiles.Add(audioFileName);
                    }
                }

                // Xóa thẻ [<audio>] cũ khỏi text hiển thị
                html = html.Replace(match.Value, "");
            }

            // Ghép Audio vào cuối đoạn văn
            return html + audioSb.ToString();
        }

        /// <summary>
        /// Xóa các span trống khỏi HTML
        /// </summary>
        private string RemoveEmptySpans(string html)
        {
            if (string.IsNullOrEmpty(html))
                return html;

            // Lặp lại cho đến khi không còn span trống nào
            bool changed = true;
            int maxIterations = 10;
            int iteration = 0;
            while (changed && iteration < maxIterations)
            {
                iteration++;
                string previousHtml = html;
                
                // Xóa span class='text-content' rỗng (chỉ có whitespace hoặc rỗng hoàn toàn)
                // Pattern: <span class='text-content'> chỉ có whitespace </span>
                html = Regex.Replace(html, @"<span[^>]*class=['""]text-content['""][^>]*>\s*</span>", "", RegexOptions.IgnoreCase);
                
                // Xóa span rỗng không có class (nếu có)
                html = Regex.Replace(html, @"<span[^>]*>\s*</span>", "", RegexOptions.IgnoreCase);
                
                changed = (html != previousHtml);
            }

            // Xóa khoảng trắng thừa giữa các tag
            html = Regex.Replace(html, @">\s+<", "><", RegexOptions.Compiled);
            
            return html.Trim();
        }

        /// <summary>
        /// Merge các span liền kề có chứa LaTeX delimiters để tránh LaTeX bị tách
        /// </summary>
        private string MergeLatexSpans(string html)
        {
            if (string.IsNullOrEmpty(html))
                return html;

            // Merge tất cả các span liền kề có class='text-content' lại với nhau
            // Điều này đảm bảo LaTeX không bị tách thành nhiều span
            // Lặp lại cho đến khi không còn span nào có thể merge được
            bool changed = true;
            int maxIterations = 10;
            int iteration = 0;

            while (changed && iteration < maxIterations)
            {
                iteration++;
                string previousHtml = html;
                
                // Merge các span liền kề có class='text-content'
                // Pattern: </span><span class='text-content'> -> merge nội dung vào span trước
                html = Regex.Replace(html,
                    @"(<span[^>]*class=['""]text-content['""][^>]*>)(.*?)</span>\s*<span[^>]*class=['""]text-content['""][^>]*>(.*?)</span>",
                    match =>
                    {
                        string content1 = match.Groups[2].Value;
                        string content2 = match.Groups[3].Value;
                        return $@"{match.Groups[1].Value}{content1}{content2}</span>";
                    },
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);
                
                changed = (html != previousHtml);
            }

            return html;
        }

        /// <summary>
        /// Chuyển đổi LaTeX sang HTML với MathJax/KaTeX support
        /// </summary>
        private string ConvertLatexToHtml(string content)
        {
            if (string.IsNullOrEmpty(content))
                return content;

            // Xử lý display math ($$...$$) trước
            content = _latexDisplayPattern.Replace(content, match =>
            {
                string latex = match.Groups[1].Value.Trim();
                return $@"<span class='math-display'>\[{latex}\]</span>";
            });

            // Xử lý display math với \[...\] - chỉ convert nếu chưa được wrap trong span math-display
            content = _latexDisplayBracketPattern.Replace(content, match =>
            {
                // Kiểm tra xem có nằm trong <span class='math-display'> không bằng cách kiểm tra context
                int matchIndex = match.Index;
                string beforeMatch = content.Substring(Math.Max(0, matchIndex - 150), Math.Min(150, matchIndex));
                
                // Đếm số lượng <span class='math-display'> và </span> trước match
                int openMathSpans = Regex.Matches(beforeMatch,
                    @"<span[^>]*class=['""]math-display['""][^>]*>", RegexOptions.IgnoreCase).Count;
                int closeSpans = Regex.Matches(beforeMatch,
                    @"</span>", RegexOptions.IgnoreCase).Count;
                
                // Nếu có <span class='math-display'> chưa đóng, đã được convert rồi
                if (openMathSpans > closeSpans)
                {
                    return match.Value; // Đã được convert rồi, giữ nguyên
                }
                
                string latex = match.Groups[1].Value.Trim();
                return $@"<span class='math-display'>\[{latex}\]</span>";
            });

            // Xử lý inline math với \(...\) trước
            // Chỉ convert nếu chưa được wrap trong span math-inline
            content = _latexInlineParenPattern.Replace(content, match =>
            {
                int matchIndex = match.Index;
                string beforeMatch = content.Substring(Math.Max(0, matchIndex - 150), Math.Min(150, matchIndex));
                
                int openMathSpans = Regex.Matches(beforeMatch, @"<span[^>]*class=['""]math-(?:inline|display)['""][^>]*>", RegexOptions.IgnoreCase).Count;
                int closeSpans = Regex.Matches(beforeMatch, @"</span>", RegexOptions.IgnoreCase).Count;
                
                if (openMathSpans > closeSpans)
                {
                    return match.Value; // Đã được convert rồi, giữ nguyên
                }
                
                string latex = match.Groups[1].Value.Trim();
                return $@"<span class='math-inline'>\({latex}\)</span>";
            });

            // Xử lý inline math ($...$) - chỉ convert nếu chưa được wrap
            content = _latexInlinePattern.Replace(content, match =>
            {
                int matchIndex = match.Index;
                string beforeMatch = content.Substring(Math.Max(0, matchIndex - 150), Math.Min(150, matchIndex));

                int openMathSpans = Regex.Matches(beforeMatch,
                    @"<span[^>]*class=['""]math-(?:inline|display)['""][^>]*>", RegexOptions.IgnoreCase).Count;
                int closeSpans = Regex.Matches(beforeMatch,
                    @"</span>", RegexOptions.IgnoreCase).Count;
                
                if (openMathSpans > closeSpans)
                {
                    return match.Value; // Đã được convert rồi, giữ nguyên
                }
                
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

                // Đọc bytes ảnh - đảm bảo đọc hết tất cả bytes
                using var memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);
                byte[] imageBytes = memoryStream.ToArray();
                string contentType = imagePart.ContentType;

                // LƯU ẢNH DƯỚI DẠNG BASE64 NHÚNG TRỰC TIẾP VÀO HTML
                // Wrap ảnh trong span để dễ styling và tránh bị xóa nhầm
                string base64 = Convert.ToBase64String(imageBytes);
                sb.Append($"<span class='image-wrapper'><img src=\"data:{contentType};base64,{base64}\" style=\"max-width:100%; height:auto; display:block; margin: 10px 0;\" /></span>");

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
                // relativePath VD: audio/47-49.mp3 hoặc 0.mp3
                // Normalize đường dẫn (thay / thành \)
                string normalizedPath = relativePath.Replace("/", Path.DirectorySeparatorChar.ToString());
                
                // Thử tìm file theo đường dẫn trực tiếp trước
                string sourcePath = Path.Combine(sourceMediaFolder, normalizedPath);
                
                // Nếu không tìm thấy, tìm trong tất cả các folder con
                if (!File.Exists(sourcePath))
                {
                    string fileName = Path.GetFileName(normalizedPath);
                    string? foundPath = null;
                    
                    // Tìm file trong tất cả các folder con (recursive search)
                    if (Directory.Exists(sourceMediaFolder))
                    {
                        var allFiles = Directory.GetFiles(sourceMediaFolder, fileName, SearchOption.AllDirectories);
                        if (allFiles.Length > 0)
                        {
                            foundPath = allFiles[0];
                            _logger.LogInformation($"Tìm thấy file audio trong folder con: {foundPath} (từ {relativePath})");
                        }
                    }
                    
                    if (foundPath != null)
                    {
                        sourcePath = foundPath;
                    }
                    else
                    {
                        // Nếu vẫn không tìm thấy, log cảnh báo chi tiết
                        _logger.LogWarning($"Không tìm thấy file audio: {relativePath}. " +
                            $"Đã tìm trong: {sourceMediaFolder} và tất cả các folder con. " +
                            $"Vui lòng đảm bảo file audio nằm trong cùng folder với file Word hoặc trong folder con.");
                        return string.Empty;
                    }
                }

                // Copy file audio
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi copy audio file: {RelativePath}", relativePath);
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
                        LoaiCauHoi = qData.LoaiCauHoi ?? "NH", // Sử dụng loại câu hỏi từ QuestionData (NH - Nhóm)
                        CLO = qData.CLO, // Lưu CLO cho nhóm
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

            // Kiểm tra duplicate MaCauHoi trước khi lưu
            var duplicateGuids = entities
                .GroupBy(e => e.MaCauHoi)
                .Where(g => g.Count() > 1)
                .Select(g => new { MaCauHoi = g.Key, Count = g.Count() })
                .ToList();

            if (duplicateGuids.Any())
            {
                foreach (var dup in duplicateGuids)
                {
                    var duplicateEntities = entities.Where(e => e.MaCauHoi == dup.MaCauHoi).ToList();
                    _logger.LogError($"Phát hiện duplicate MaCauHoi: {dup.MaCauHoi} xuất hiện {dup.Count} lần");
                    foreach (var ent in duplicateEntities)
                    {
                        _logger.LogError($"  - Entity: MaSoCauHoi={ent.MaSoCauHoi}, LoaiCauHoi={ent.LoaiCauHoi}, MaCauHoiCha={ent.MaCauHoiCha}, NoiDung={ent.NoiDung?.Substring(0, Math.Min(50, ent.NoiDung?.Length ?? 0))}...");
                    }
                }
                result.Errors.Add($"Lỗi: Có {duplicateGuids.Count} MaCauHoi bị trùng lặp. Vui lòng kiểm tra lại logic tạo câu hỏi.");
                return;
            }

            // Kiểm tra duplicate trong QuestionData trước khi tạo entities
            foreach (var qData in questions)
            {
                if (qData.IsGroup)
                {
                    var duplicateSubQuestions = qData.CauHoiCon
                        .GroupBy(q => q.MaCauHoi)
                        .Where(g => g.Count() > 1)
                        .Select(g => g.Key)
                        .ToList();
                    
                    if (duplicateSubQuestions.Any())
                    {
                        _logger.LogError($"Phát hiện duplicate câu hỏi con trong nhóm {qData.MaCauHoi}: {string.Join(", ", duplicateSubQuestions)}");
                        result.Errors.Add($"Lỗi: Nhóm câu hỏi có câu hỏi con bị trùng lặp MaCauHoi.");
                        return;
                    }
                }
            }

            // Lưu Batch
            try
            {
                await _cauHoiRepository.AddRangeAsync(entities);
                result.SuccessCount = entities.Count;
                
                // Lưu Files (audio, ảnh) vào database
                var fileEntities = new List<FileEntity>();
                foreach (var qData in questions)
                {
                    // Lưu files của nhóm
                    if (qData.IsGroup && qData.Files.Any())
                    {
                        var groupEntity = entities.FirstOrDefault(e => e.MaCauHoi == qData.MaCauHoi);
                        if (groupEntity != null)
                        {
                            foreach (var fileData in qData.Files)
                            {
                                fileEntities.Add(new FileEntity
                                {
                                    MaFile = Guid.NewGuid(),
                                    MaCauHoi = groupEntity.MaCauHoi,
                                    TenFile = fileData.FileName,
                                    LoaiFile = fileData.FileType,
                                    MaCauTraLoi = null
                                });
                            }
                        }
                    }
                    
                    // Lưu files của câu hỏi đơn
                    if (!qData.IsGroup && qData.Files.Any())
                    {
                        var questionEntity = entities.FirstOrDefault(e => e.MaCauHoi == qData.MaCauHoi);
                        if (questionEntity != null)
                        {
                            foreach (var fileData in qData.Files)
                            {
                                fileEntities.Add(new FileEntity
                                {
                                    MaFile = Guid.NewGuid(),
                                    MaCauHoi = questionEntity.MaCauHoi,
                                    TenFile = fileData.FileName,
                                    LoaiFile = fileData.FileType,
                                    MaCauTraLoi = null
                                });
                            }
                        }
                    }
                    
                    // Lưu files của câu hỏi con
                    if (qData.IsGroup)
                    {
                        foreach (var subQ in qData.CauHoiCon)
                        {
                            if (subQ.Files.Any())
                            {
                                var subEntity = entities.FirstOrDefault(e => e.MaCauHoi == subQ.MaCauHoi);
                                if (subEntity != null)
                                {
                                    foreach (var fileData in subQ.Files)
                                    {
                                        fileEntities.Add(new FileEntity
                                        {
                                            MaFile = Guid.NewGuid(),
                                            MaCauHoi = subEntity.MaCauHoi,
                                            TenFile = fileData.FileName,
                                            LoaiFile = fileData.FileType,
                                            MaCauTraLoi = null
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
                
                // Lưu files vào database
                if (fileEntities.Any())
                {
                    foreach (var fileEntity in fileEntities)
                    {
                        await _fileRepository.AddAsync(fileEntity);
                    }
                    _logger.LogInformation($"Đã lưu {fileEntities.Count} file vào database");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi lưu câu hỏi vào database");
                result.Errors.Add($"Lỗi lưu database: {ex.Message}");
            }
        }

        private CauHoi CreateCauHoiEntity(QuestionData dto, Guid maPhan, int maSo)
        {
            // Xác định có hoán vị hay không dựa trên loại câu hỏi
            // TL (Tự luận), GN (Ghép nối), DT (Điền từ) thường không hoán vị
            bool hoanVi = dto.LoaiCauHoi?.ToUpper() switch
            {
                "TL" => false, // Tự luận không hoán vị
                "GN" => false, // Ghép nối không hoán vị
                "DT" => false, // Điền từ không hoán vị
                _ => true      // TN, MN, NH có thể hoán vị
            };

            var cauHoi = new CauHoi
            {
                MaCauHoi = dto.MaCauHoi,
                MaPhan = maPhan,
                NoiDung = dto.NoiDung,
                LoaiCauHoi = dto.LoaiCauHoi ?? "TN", // Sử dụng loại câu hỏi từ QuestionData
                MaSoCauHoi = maSo,
                CLO = dto.CLO,
                NgayTao = DateTime.UtcNow,
                HoanVi = hoanVi,
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
                    
                    // Thống kê theo loại câu hỏi
                    string loaiKey = GetQuestionTypeName(question.LoaiCauHoi);
                    if (result.StatsByType.ContainsKey(loaiKey))
                        result.StatsByType[loaiKey]++;
                    else
                        result.StatsByType[loaiKey] = 1;
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
                IsValid = true,
                LoaiCauHoi = question.LoaiCauHoi,
                LoaiCauHoiDisplay = GetQuestionTypeName(question.LoaiCauHoi)
            };

            // Validate nội dung
            if (question.IsGroup)
            {
                if (string.IsNullOrWhiteSpace(question.NoiDungNhom))
                {
                    result.Errors.Add("Nhóm câu hỏi không có nội dung dẫn");
                    result.IsValid = false;
                }
                else
                {
                    // Kiểm tra audio trong nội dung nhóm (NoiDungNhom)
                    // Kiểm tra nhiều format: <audio>, audio-player-wrapper, <source>, [<audio>], và Files
                    string noiDungNhom = question.NoiDungNhom ?? string.Empty;
                    string noiDungNhomLower = noiDungNhom.ToLower();
                    
                    // Kiểm tra bằng regex để tìm pattern audio (case-insensitive)
                    bool hasAudioInContent = Regex.IsMatch(noiDungNhom, @"<audio[^>]*>", RegexOptions.IgnoreCase) ||
                                           Regex.IsMatch(noiDungNhom, @"audio-player-wrapper", RegexOptions.IgnoreCase) ||
                                           Regex.IsMatch(noiDungNhom, @"<source[^>]*src\s*=\s*['""]/media/", RegexOptions.IgnoreCase) ||
                                           Regex.IsMatch(noiDungNhom, @"/media/[^'""\s>]+\.(mp3|wav|ogg|m4a)", RegexOptions.IgnoreCase) ||
                                           _audioPattern.IsMatch(noiDungNhom);
                    
                    bool hasAudioInFiles = question.Files != null && question.Files.Any(f => f.FileType == FileType.Audio);
                    
                    if (hasAudioInContent || hasAudioInFiles)
                    {
                        result.HasAudio = true;
                    }
                    
                    // Kiểm tra ảnh trong nội dung nhóm
                    if (question.NoiDungNhom.Contains("<img", StringComparison.OrdinalIgnoreCase))
                    {
                        result.HasImages = true;
                    }
                    
                    // Kiểm tra LaTeX trong nội dung nhóm
                    if (question.NoiDungNhom.Contains("$") || question.NoiDungNhom.Contains("\\(") || question.NoiDungNhom.Contains("\\["))
                    {
                        result.HasLatex = true;
                    }
                    
                    // Cảnh báo nếu có file audio không tìm thấy
                    if (question.MissingAudioFiles != null && question.MissingAudioFiles.Any())
                    {
                        foreach (var missingFile in question.MissingAudioFiles)
                        {
                            result.Warnings.Add($"Không tìm thấy file audio: {missingFile}. " +
                                $"Vui lòng đảm bảo file audio nằm trong cùng folder với file Word hoặc trong folder con. " +
                                $"Cấu trúc ZIP đúng: [TênFolder]/file.docx và [TênFolder]/audio/file.mp3 (hoặc file.mp3 trực tiếp trong [TênFolder])");
                        }
                    }
                }

                // Nhóm câu hỏi cần có câu hỏi con (trừ TL có thể không có câu hỏi con nếu chỉ có nội dung nhóm)
                // TL có thể có câu hỏi con hoặc chỉ có nội dung nhóm
                if (question.CauHoiCon.Count == 0)
                {
                    // TL có thể không có câu hỏi con nếu chỉ có nội dung nhóm
                    if (question.LoaiCauHoi != "TL")
                    {
                        result.Errors.Add("Nhóm câu hỏi không có câu hỏi con nào");
                        result.IsValid = false;
                    }
                }
                else
                {
                    result.SubQuestionsCount = question.CauHoiCon.Count;
                    
                    // Validate từng câu hỏi con
                    int subQuestionIndex = 0;
                    foreach (var subQ in question.CauHoiCon)
                    {
                        subQuestionIndex++;
                        var subValidation = ValidateQuestionContent(subQ, $"{questionNumber}.{subQuestionIndex}", question);
                        result.SubQuestions.Add(subValidation);
                        
                        if (!subValidation.IsValid)
                            result.IsValid = false;
                        
                        // Nếu câu hỏi con có audio, ảnh hoặc LaTeX, cập nhật vào kết quả nhóm
                        if (subValidation.HasAudio)
                            result.HasAudio = true;
                        if (subValidation.HasImages)
                            result.HasImages = true;
                        if (subValidation.HasLatex)
                            result.HasLatex = true;
                    }
                }
            }
            else
            {
                // Validate câu hỏi đơn
                var validation = ValidateQuestionContent(question, questionNumber.ToString(), null);
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

        private QuestionContentValidation ValidateQuestionContent(QuestionData question, string identifier, QuestionData? parentQuestion = null)
        {
            var result = new QuestionContentValidation
            {
                Identifier = identifier,
                IsValid = true,
                ContentPreview = GetContentPreview(question),
                LoaiCauHoi = question.LoaiCauHoi,
                LoaiCauHoiDisplay = GetQuestionTypeName(question.LoaiCauHoi)
            };

            // Lấy loại câu hỏi để sử dụng trong validation
            // Câu hỏi con trong nhóm NH có thể có LoaiCauHoi là TN hoặc MN (từ tag [<TN>] hoặc [<MN>])
            // Nếu không có loại, mặc định là "TN" cho validation
            string loaiCauHoi = question.LoaiCauHoi?.ToUpper() ?? "TN";

            // 1. Validate nội dung câu hỏi
            // Câu hỏi điền từ (DT) trong nhóm không cần nội dung (nội dung nằm ở nhóm)
            if (string.IsNullOrWhiteSpace(question.NoiDung))
            {
                if (loaiCauHoi != "DT")
            {
                result.Errors.Add($"Câu {identifier}: Không có nội dung câu hỏi");
                result.IsValid = false;
                }
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

            // 2. Validate đáp án (khác nhau theo loại câu hỏi)
            result.AnswersCount = question.Answers.Count;

            // Kiểm tra xem có phải là câu hỏi con trong nhóm TL không
            bool isSubQuestionInTLGroup = parentQuestion != null && 
                                          parentQuestion.LoaiCauHoi == "TL" && 
                                          question.MaCauHoiCha.HasValue;

            // TL (Tự luận) không cần đáp án (bao gồm cả câu hỏi con trong nhóm TL)
            if (loaiCauHoi == "TL" || isSubQuestionInTLGroup)
            {
                if (question.Answers.Count > 0)
                {
                    result.Warnings.Add($"Câu {identifier} (Tự luận): Có {question.Answers.Count} đáp án (thường không cần)");
                }
            }
            else
            {
                // Các loại khác cần có đáp án
                if (question.Answers.Count == 0)
                {
                    result.Errors.Add($"Câu {identifier} ({GetQuestionTypeName(loaiCauHoi)}): Không có đáp án nào");
                    result.IsValid = false;
                }
                else
                {
                    // Check số lượng đáp án
                    // TN (Trắc nghiệm) chỉ cần 1 đáp án là đủ, không cần warning
                    // MN (Nhiều đáp án) cần ít nhất 2 đáp án (sẽ check ở dưới)
                    if (question.Answers.Count < 2 && loaiCauHoi != "DT" && loaiCauHoi != "GN" && loaiCauHoi != "TN")
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
                    else if (loaiCauHoi == "TN" && correctAnswers.Count > 1)
                    {
                        // TN (Trắc nghiệm) chỉ có 1 đáp án đúng
                        result.Warnings.Add($"Câu {identifier} (Trắc nghiệm): Có {correctAnswers.Count} đáp án đúng, nên chỉ có 1");
                    }
                    else if (loaiCauHoi == "MN" && correctAnswers.Count < 2)
                    {
                        // MN (Nhiều đáp án) cần ít nhất 2 đáp án đúng
                        result.Warnings.Add($"Câu {identifier} (Nhiều đáp án): Chỉ có {correctAnswers.Count} đáp án đúng, nên có ít nhất 2");
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

                    // Check thứ tự đáp án (chỉ cho TN, MN)
                    if (loaiCauHoi == "TN" || loaiCauHoi == "MN")
                    {
                        var expectedOrder = question.Answers.OrderBy(a => a.ThuTu).Select(a => a.ThuTu).ToList();
                        var actualOrder = Enumerable.Range(1, question.Answers.Count).ToList();
                        if (!expectedOrder.SequenceEqual(actualOrder))
                        {
                            result.Warnings.Add($"Câu {identifier}: Thứ tự đáp án không liên tục (có thể thiếu đáp án)");
                        }
                    }
                }
            }

            // 3. Validate CLO
            // Bỏ qua cảnh báo CLO cho câu hỏi con trong nhóm DT, GN, NH, và TL vì CLO được kế thừa từ câu hỏi cha
            bool isSubQuestionInDTGroup = parentQuestion != null && 
                                          parentQuestion.LoaiCauHoi == "DT" && 
                                          question.MaCauHoiCha.HasValue;
            bool isSubQuestionInGNGroup = parentQuestion != null && 
                                          parentQuestion.LoaiCauHoi == "GN" && 
                                          question.MaCauHoiCha.HasValue;
            bool isSubQuestionInNHGroup = parentQuestion != null && 
                                          parentQuestion.LoaiCauHoi == "NH" && 
                                          question.MaCauHoiCha.HasValue;
            // isSubQuestionInTLGroup đã được định nghĩa ở trên (dòng 1783)
            
            if (question.CLO == null && !isSubQuestionInDTGroup && !isSubQuestionInGNGroup && !isSubQuestionInNHGroup && !isSubQuestionInTLGroup)
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
            
            // 5. Cảnh báo nếu có file audio không tìm thấy
            if (question.MissingAudioFiles != null && question.MissingAudioFiles.Any())
            {
                foreach (var missingFile in question.MissingAudioFiles)
                {
                    result.Warnings.Add($"Câu {identifier}: Không tìm thấy file audio '{missingFile}'. " +
                        $"Vui lòng đảm bảo file audio nằm trong cùng folder với file Word hoặc trong folder con. " +
                        $"Cấu trúc ZIP đúng: [TênFolder]/file.docx và [TênFolder]/audio/file.mp3 (hoặc file.mp3 trực tiếp trong [TênFolder])");
                }
            }

            return result;
        }

        private string GetContentPreview(QuestionData question)
        {
            string content = question.IsGroup ? question.NoiDungNhom : question.NoiDung;
            
            if (string.IsNullOrWhiteSpace(content))
                return string.Empty;

            // Giữ lại HTML để có thể render trên web (đặc biệt là LaTeX)
            // Chỉ làm sạch các khoảng trắng thừa và giới hạn độ dài
            // Không xóa HTML tags vì cần để render LaTeX, ảnh, audio, etc.
            
            // Bước 1: Xóa các span rỗng (không chứa nội dung, chỉ có whitespace)
            // Pattern: <span class='text-content'></span> hoặc <span class='text-content'> </span>
            // Lặp lại cho đến khi không còn span rỗng nào
            bool changed = true;
            int maxIterations = 10;
            int iteration = 0;
            while (changed && iteration < maxIterations)
            {
                iteration++;
                string previousContent = content;
                content = Regex.Replace(content, @"<span[^>]*class=['""]text-content['""][^>]*>\s*</span>", "", RegexOptions.IgnoreCase);
                changed = (content != previousContent);
            }
            
            // Bước 2: Loại bỏ các khoảng trắng thừa giữa các tags (nhưng giữ lại trong nội dung)
            content = Regex.Replace(content, @">\s+<", "><", RegexOptions.Compiled);
            
            // Bước 3: Loại bỏ các khoảng trắng ở đầu và cuối
            content = content.Trim();

            // Bước 4: Giới hạn độ dài: nếu quá dài, cắt ở vị trí an toàn (không cắt giữa tag)
            if (content.Length > 200)
            {
                // Tìm vị trí cắt an toàn (sau tag đóng hoặc trước tag mở)
                int cutPosition = 200;
                
                // Tìm tag đóng gần nhất trước vị trí cắt
                int lastCloseTag = content.LastIndexOf('>', cutPosition);
                if (lastCloseTag > 100) // Đảm bảo không cắt quá sớm
                {
                    cutPosition = lastCloseTag + 1;
                }
                else
                {
                    // Nếu không tìm thấy tag đóng, tìm tag mở gần nhất
                    int lastOpenTag = content.LastIndexOf('<', cutPosition);
                    if (lastOpenTag > 100)
                    {
                        cutPosition = lastOpenTag;
                    }
                }
                
                content = content.Substring(0, cutPosition) + "...";
            }

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
            
            /// <summary>
            /// Thống kê số lượng theo loại câu hỏi
            /// </summary>
            public Dictionary<string, int> StatsByType { get; set; } = new Dictionary<string, int>();

            public bool HasErrors => Errors.Any() || InvalidCount > 0;
            public string Summary => HasErrors 
                ? $"Tìm thấy {TotalQuestionsFound} câu hỏi: {ValidCount} hợp lệ, {InvalidCount} có lỗi"
                : $"Tìm thấy {TotalQuestionsFound} câu hỏi, tất cả đều hợp lệ";
            
            /// <summary>
            /// Chi tiết thống kê theo loại câu hỏi
            /// </summary>
            public string TypeSummary => StatsByType.Any() 
                ? string.Join(", ", StatsByType.Select(kv => $"{kv.Key}: {kv.Value}"))
                : "";
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
            
            /// <summary>
            /// Loại câu hỏi: TL, NH, TN, MN, GN, DT
            /// </summary>
            public string? LoaiCauHoi { get; set; }
            
            /// <summary>
            /// Tên đầy đủ của loại câu hỏi
            /// </summary>
            public string? LoaiCauHoiDisplay { get; set; }

            public string Status => IsValid ? "Hợp lệ" : "Có lỗi";
            public string Type => LoaiCauHoiDisplay ?? (IsGroup ? "Nhóm" : "Đơn");
        }

        public class QuestionContentValidation
        {
            public string Identifier { get; set; } = string.Empty;
            public bool IsValid { get; set; }
            public string ContentPreview { get; set; } = string.Empty;
            public int AnswersCount { get; set; }
            public int CorrectAnswersCount { get; set; }
            public bool HasImages { get; set; }
            public bool HasAudio { get; set; }
            public bool HasLatex { get; set; }
            public List<string> Errors { get; set; } = new List<string>();
            public List<string> Warnings { get; set; } = new List<string>();
            
            /// <summary>
            /// Loại câu hỏi: TL, NH, TN, MN, GN, DT
            /// </summary>
            public string? LoaiCauHoi { get; set; }
            
            /// <summary>
            /// Tên đầy đủ của loại câu hỏi
            /// </summary>
            public string? LoaiCauHoiDisplay { get; set; }
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
            
            /// <summary>
            /// Loại câu hỏi: TL (Tự luận), NH (Nhóm), TN (Trắc nghiệm), MN (Nhiều đáp án), GN (Ghép nối), DT (Điền từ)
            /// null cho câu hỏi con trong nhóm NH
            /// </summary>
            public string? LoaiCauHoi { get; set; } = "TN";

            public List<AnswerData> Answers { get; set; } = new List<AnswerData>();
            public List<FileData> Files { get; set; } = new List<FileData>();
            
            // Lưu danh sách file audio không tìm thấy để cảnh báo trong validation
            public List<string> MissingAudioFiles { get; set; } = new List<string>();
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
