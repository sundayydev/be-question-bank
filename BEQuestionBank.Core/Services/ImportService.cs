using BeQuestionBank.Domain.Interfaces.IRepositories;
using BeQuestionBank.Domain.Models;
using BeQuestionBank.Shared.DTOs.File;
using BeQuestionBank.Shared.Enums;
using BEQuestionBank.Domain.Interfaces; // Hoặc namespace repository của bạn
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
        private const bool USE_IMAGE_AS_BASE64 = true; // Set true để lưu ảnh dạng Base64 vào DB
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
            // Nếu upload zip thì cần giải nén trước, ở đây giả sử mediaFolderPath là đường dẫn folder chứa file media
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
                        // Truyền ref currentQuestion để hàm xử lý file đính kèm gắn vào câu hỏi này
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
                    // (Ví dụ: Dòng text xuống dòng, hoặc ảnh minh họa nằm riêng 1 dòng)
                    if (currentQuestion != null)
                    {
                        // Bỏ qua dòng trống hoàn toàn
                        string html = ParagraphToHtml(para, doc, sourceMediaFolder, currentQuestion, ref currentGroup);
                        if (!string.IsNullOrWhiteSpace(textRaw) || html.Contains("<img") || html.Contains("<audio"))
                        {
                            currentQuestion.NoiDung += "<br/>" + html;
                        }
                    }
                    // Nếu không thuộc câu hỏi nào và đang ở ngoài nhóm (rác hoặc intro), có thể bỏ qua
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
            // Lưu ý: pass 'currentQuestion' để ảnh/audio trong đáp án được gắn vào FileData của câu hỏi
            string fullHtml = ParagraphToHtml(para, doc, sourceMediaFolder, currentQuestion, ref currentGroup);
            string fullText = para.InnerText.Trim();

            // Xác định đáp án nào (A, B, C, D)
            char dapAnChar = fullText.ToUpper()[0]; // Lấy ký tự đầu tiên
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
                    // Check in nghiêng -> KHÔNG hoán vị (cố định) - Theo logic bạn yêu cầu ở prompt trước
                    // Hoặc ngược lại tùy logic dự án: "In nghiêng là có hoán vị". Ở đây tôi để: In nghiêng => Cố định (False)
                    if (run.RunProperties.Italic != null)
                        isShuffle = false;
                }
            }

            // Clean nội dung: Xóa "A." ở đầu chuỗi HTML
            // Cách đơn giản: Regex replace kí tự đầu tiên của text trong HTML
            // Tuy nhiên HTML phức tạp. Cách an toàn nhất cho hiển thị là để nguyên hoặc replace text thuần.
            // Ở đây ta dùng Regex replace text pattern A. ở đầu
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
            var audioSb = new StringBuilder(); // Chứa HTML audio player

            foreach (var run in para.Elements<Run>())
            {
                var text = run.InnerText;
                if (!string.IsNullOrEmpty(text))
                {
                    // Clean các tag hệ thống ra khỏi text hiển thị (nếu muốn)
                    // text = text.Replace("[<br>]", "").Replace("[<egc>]", ""); 
                    // Tùy nhu cầu, ở đây giữ nguyên style text

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

            // --- XỬ LÝ AUDIO (Regex trên toàn bộ HTML của đoạn văn) ---
            var matches = _audioPattern.Matches(html);
            foreach (Match match in matches)
            {
                string audioFileName = match.Groups[1].Value.Trim(); // VD: audio/47-49.mp3

                // Xử lý copy file audio
                string savedFileName = ProcessAudioFile(audioFileName, sourceMediaFolder, currentQuestion, currentGroup);

                if (!string.IsNullOrEmpty(savedFileName))
                {
                    // Tạo HTML Player
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

                if (USE_IMAGE_AS_BASE64)
                {
                    string base64 = Convert.ToBase64String(imageBytes);
                    sb.Append($"<img src=\"data:{contentType};base64,{base64}\" style=\"max-width:100%; height:auto; display:block; margin: 10px 0;\" />");
                }
                else
                {
                    // Lưu ra file vật lý (wwwroot/images)
                    string fileName = $"{Guid.NewGuid()}{GetExtension(contentType)}";
                    string savePath = Path.Combine(_storagePath, "images", fileName);

                    if (!Directory.Exists(Path.GetDirectoryName(savePath)))
                        Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);

                    File.WriteAllBytes(savePath, imageBytes);

                    // Thêm thông tin FileData vào câu hỏi/nhóm để lưu DB (nếu cần)
                    var fileData = new FileData { FileName = fileName, FileType = FileType.Image };
                    if (question != null) question.Files.Add(fileData);
                    else if (group != null) group.Files.Add(fileData);

                    sb.Append($"<img src=\"/images/{fileName}\" style=\"max-width:100%;\" />");
                }
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
                // sourceMediaFolder: Đường dẫn folder chứa file này bên ngoài (do user upload hoặc cấu hình)

                string sourcePath = Path.Combine(sourceMediaFolder, relativePath.Replace("/", Path.DirectorySeparatorChar.ToString()));

                if (File.Exists(sourcePath))
                {
                    string newFileName = $"{Guid.NewGuid()}{Path.GetExtension(sourcePath)}";
                    string destFolder = Path.Combine(_storagePath, "media"); // wwwroot/media

                    if (!Directory.Exists(destFolder)) Directory.CreateDirectory(destFolder);

                    string destPath = Path.Combine(destFolder, newFileName);
                    File.Copy(sourcePath, destPath, true);

                    // Thêm FileData vào DTO để lưu DB
                    var fileData = new FileData
                    {
                        FileName = newFileName,
                        FileType = FileType.Audio,
                        // Nếu muốn lưu binary vào DB thì đọc bytes tại đây
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
            // Cần implement hàm lấy MaxMaSo trong Repo, hoặc query đơn giản
            // currentMaxMaSo = await _cauHoiRepository.GetMaxMaSoCauHoiAsync();

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
                        LoaiCauHoi = "Nhom", // Hoặc "Group"
                        MaSoCauHoi = currentMaxMaSo,
                        NgayTao = DateTime.UtcNow,
                        MaCauHoiCha = null, // Nhóm không có cha
                        SoCauHoiCon = qData.CauHoiCon.Count,
                        // Map Files nếu có
                    };
                    entities.Add(groupEntity);

                    // 2. Lưu các câu hỏi con
                    foreach (var subQ in qData.CauHoiCon)
                    {
                        currentMaxMaSo++; // Tăng mã số cho câu con
                        var subEntity = CreateCauHoiEntity(subQ, maPhan, currentMaxMaSo);
                        subEntity.MaCauHoiCha = groupEntity.MaCauHoi; // Gắn cha
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
            await _cauHoiRepository.AddRangeAsync(entities);
            result.SuccessCount = entities.Count;
        }

        private CauHoi CreateCauHoiEntity(QuestionData dto, Guid maPhan, int maSo)
        {
            return new CauHoi
            {
                MaCauHoi = dto.MaCauHoi,
                MaPhan = maPhan,
                NoiDung = dto.NoiDung,
                LoaiCauHoi = "TracNghiem",
                MaSoCauHoi = maSo,
                CLO = dto.CLO,
                NgayTao = DateTime.UtcNow,
                HoanVi = true,
                MaCauHoiCha = dto.MaCauHoiCha, // Có thể null hoặc ID nhóm

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

                // Map Files (nếu cần lưu bảng File riêng)
                // ...
            };
        }

        // Đặt đoạn này ở cuối file WordImportService.cs, TRƯỚC dấu đóng ngoặc nhọn } của namespace

        // --- CÁC CLASS DTO HỖ TRỢ ---

        public class ImportResult
        {
            public int SuccessCount { get; set; }
            public List<string> Errors { get; set; } = new List<string>();
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
            public Guid? MaCauHoi { get; set; } // Để map ID sau khi lưu DB
        }
    }
}