using BeQuestionBank.Shared.DTOs.Common;
using Microsoft.AspNetCore.Components.Forms;

namespace FEQuestionBank.Client.Services.Interface
{
    public interface IImportApiClient
    {
        /// <summary>
        /// Preview import từ file Word - Validation only
        /// </summary>
        Task<ApiResponse<PreviewImportResult>> PreviewWordAsync(IBrowserFile file);

        /// <summary>
        /// Preview import từ file ZIP - Validation only
        /// </summary>
        Task<ApiResponse<PreviewImportResult>> PreviewZipAsync(IBrowserFile zipFile);

        /// <summary>
        /// Import thực sự từ file Word
        /// </summary>
        Task<ApiResponse<ImportResult>> ImportWordAsync(IBrowserFile file, Guid maPhan);

        /// <summary>
        /// Import từ file ZIP (Word + Media)
        /// </summary>
        Task<ApiResponse<ImportResult>> ImportZipAsync(IBrowserFile zipFile, Guid maPhan);

        /// <summary>
        /// Upload media files riêng lẻ
        /// </summary>
        Task<ApiResponse<UploadMediaResult>> UploadMediaAsync(List<IBrowserFile> files);

        /// <summary>
        /// Clear temp uploads folder
        /// </summary>
        Task<ApiResponse<string>> ClearTempUploadsAsync();
    }

    // DTOs
    public class PreviewImportResult
    {
        public string Summary { get; set; } = string.Empty;
        public int TotalFound { get; set; }
        public int ValidCount { get; set; }
        public int InvalidCount { get; set; }
        public bool HasErrors { get; set; }
        public bool CanImport { get; set; }
        public List<string> GeneralErrors { get; set; } = new();
        public List<QuestionValidation> Questions { get; set; } = new();
    }

    public class QuestionValidation
    {
        public int QuestionNumber { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public bool IsGroup { get; set; }
        public string? CLO { get; set; }
        public string Preview { get; set; } = string.Empty;
        public int AnswersCount { get; set; }
        public int CorrectAnswersCount { get; set; }
        public int SubQuestionsCount { get; set; }
        public FeatureFlags Features { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public List<SubQuestionValidation> SubQuestions { get; set; } = new();
    }

    public class SubQuestionValidation
    {
        public string Identifier { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public int AnswersCount { get; set; }
        public int CorrectAnswersCount { get; set; }
        public FeatureFlags Features { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }

    public class FeatureFlags
    {
        public bool HasImages { get; set; }
        public bool HasAudio { get; set; }
        public bool HasLatex { get; set; }
    }

    public class ImportResult
    {
        public int SuccessCount { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public class UploadMediaResult
    {
        public List<string> Files { get; set; } = new();
        public int Count { get; set; }
    }
}
