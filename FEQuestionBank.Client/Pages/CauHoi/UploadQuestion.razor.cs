using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using BeQuestionBank.Shared.DTOs.Khoa;
using BeQuestionBank.Shared.DTOs.MonHoc;
using BeQuestionBank.Shared.DTOs.Phan;
using FEQuestionBank.Client.Services;
using FEQuestionBank.Client.Services.Interface;

namespace FEQuestionBank.Client.Pages.CauHoi;

public partial class UploadQuestion : ComponentBase
{
    [Inject] private IKhoaApiClient KhoaClient { get; set; } = default!;
    [Inject] private IMonHocApiClient MonHocClient { get; set; } = default!;
    [Inject] private IPhanApiClient PhanClient { get; set; } = default!;
    [Inject] private IImportApiClient ImportClient { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    // Data
    private List<KhoaDto> Khoas { get; set; } = new();
    private List<MonHocDto> MonHocs { get; set; } = new();
    private List<PhanDto> Phans { get; set; } = new();

    // Selected Values
    private Guid? SelectedKhoa { get; set; }
    private Guid? SelectedMonHoc { get; set; }
    private Guid? SelectedPhanId { get; set; }

    // Files
    private IBrowserFile? WordFile;
    private IBrowserFile? ZipFile;

    // Preview Results
    private PreviewImportResult? PreviewResult;

    // UI State
    private int CurrentStep = 1;
    private bool IsProcessing = false;
    private string ProcessingMessage = "Đang xử lý...";
    
    // Expand/Collapse State
    private HashSet<int> ExpandedQuestions = new();
    
    // Answers Display (will be populated from Preview)
    private Dictionary<int, List<AnswerDisplay>> QuestionAnswers = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadKhoasAsync();
    }

    #region Data Loading

    private async Task LoadKhoasAsync()
    {
        try
        {
            var response = await KhoaClient.GetAllKhoasAsync();
            if (response.Success && response.Data != null)
            {
                Khoas = response.Data;
            }
            else
            {
                Snackbar.Add("Không thể tải danh sách khoa.", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Lỗi: {ex.Message}", Severity.Error);
        }
    }

    private async Task OnKhoaChanged(Guid? maKhoa)
    {
        SelectedKhoa = maKhoa;
        SelectedMonHoc = null;
        SelectedPhanId = null;
        MonHocs.Clear();
        Phans.Clear();
        PreviewResult = null;
        ExpandedQuestions.Clear();
        QuestionAnswers.Clear();
        CurrentStep = 1;

        if (maKhoa.HasValue)
        {
            await LoadMonHocsByKhoaAsync(maKhoa.Value);
        }
    }

    private async Task LoadMonHocsByKhoaAsync(Guid maKhoa)
    {
        try
        {
            var response = await MonHocClient.GetMonHocsByMaKhoaAsync(maKhoa);
            if (response.Success && response.Data != null)
            {
                MonHocs = response.Data;
            }
            else
            {
                Snackbar.Add("Không tải được môn học.", Severity.Warning);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Lỗi: {ex.Message}", Severity.Error);
        }
    }

    private async Task OnMonHocChanged(Guid? maMonHoc)
    {
        SelectedMonHoc = maMonHoc;
        SelectedPhanId = null;
        Phans.Clear();
        PreviewResult = null;
        ExpandedQuestions.Clear();
        QuestionAnswers.Clear();
        CurrentStep = 1;

        if (maMonHoc.HasValue)
        {
            await LoadPhansByMonHocAsync(maMonHoc.Value);
        }
    }

    private async Task LoadPhansByMonHocAsync(Guid monHocId)
    {
        try
        {
            var response = await PhanClient.GetTreeByMonHocAsync(monHocId);
            if (response.Success && response.Data != null)
            {
                Phans = response.Data;
            }
            else
            {
                Snackbar.Add("Không tải được chương/phần.", Severity.Warning);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Lỗi: {ex.Message}", Severity.Error);
        }
    }

    private Task OnPhanChanged(Guid? maPhan)
    {
        SelectedPhanId = maPhan;
        PreviewResult = null;
        ExpandedQuestions.Clear();
        QuestionAnswers.Clear();
        CurrentStep = maPhan.HasValue ? 2 : 1;
        return Task.CompletedTask;
    }

    #endregion

    #region File Selection

    private Task OnWordFileSelected(InputFileChangeEventArgs e)
    {
        WordFile = e.File;
        ZipFile = null; // Clear ZIP if Word is selected
        PreviewResult = null;
        ExpandedQuestions.Clear();
        QuestionAnswers.Clear();
        
        if (WordFile != null)
        {
            // Validate file size
            const long maxSize = 100 * 1024 * 1024; // 100MB
            if (WordFile.Size > maxSize)
            {
                Snackbar.Add($"File quá lớn. Kích thước tối đa: 100MB", Severity.Error);
                WordFile = null;
                return Task.CompletedTask;
            }

            // Validate extension
            if (!WordFile.Name.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            {
                Snackbar.Add("Chỉ hỗ trợ file .docx", Severity.Error);
                WordFile = null;
                return Task.CompletedTask;
            }

            Snackbar.Add($"✓ Đã chọn: {WordFile.Name}", Severity.Success);
            CurrentStep = 2;
        }

        return Task.CompletedTask;
    }

    private Task OnZipFileSelected(InputFileChangeEventArgs e)
    {
        ZipFile = e.File;
        WordFile = null; // Clear Word if ZIP is selected
        PreviewResult = null;
        ExpandedQuestions.Clear();
        QuestionAnswers.Clear();
        
        if (ZipFile != null)
        {
            // Validate file size
            const long maxSize = 200 * 1024 * 1024; // 200MB
            if (ZipFile.Size > maxSize)
            {
                Snackbar.Add($"File ZIP quá lớn. Kích thước tối đa: 200MB", Severity.Error);
                ZipFile = null;
                return Task.CompletedTask;
            }

            // Validate extension
            if (!ZipFile.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                Snackbar.Add("Chỉ hỗ trợ file .zip", Severity.Error);
                ZipFile = null;
                return Task.CompletedTask;
            }

            Snackbar.Add($"✓ Đã chọn: {ZipFile.Name}", Severity.Info);
            CurrentStep = 2;
        }

        return Task.CompletedTask;
    }

    #endregion

    #region Preview

    private async Task PreviewWordFile()
    {
        if (WordFile == null)
        {
            Snackbar.Add("Vui lòng chọn file Word", Severity.Warning);
            return;
        }

        IsProcessing = true;
        ProcessingMessage = "Đang phân tích file Word...";
        PreviewResult = null;
        ExpandedQuestions.Clear();
        QuestionAnswers.Clear();

        try
        {
            var response = await ImportClient.PreviewWordAsync(WordFile);
            
            if (response.Success && response.Data != null)
            {
                PreviewResult = response.Data;
                CurrentStep = 3;
                
                // Generate answers for display
                GenerateAnswersFromPreview();
                
                Snackbar.Add(response.Message ?? "✓ Preview hoàn tất", 
                    PreviewResult.HasErrors ? Severity.Warning : Severity.Success,
                    config => { config.VisibleStateDuration = 5000; });
            }
            else
            {
                Snackbar.Add(response.Message ?? "Lỗi preview file", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Lỗi: {ex.Message}", Severity.Error);
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private async Task PreviewZipFile()
    {
        if (ZipFile == null)
        {
            Snackbar.Add("Vui lòng chọn file ZIP", Severity.Warning);
            return;
        }

        IsProcessing = true;
        ProcessingMessage = "Đang giải nén và phân tích file ZIP...";
        PreviewResult = null;
        ExpandedQuestions.Clear();
        QuestionAnswers.Clear();

        try
        {
            var response = await ImportClient.PreviewZipAsync(ZipFile);
            
            if (response.Success && response.Data != null)
            {
                PreviewResult = response.Data;
                CurrentStep = 3;
                
                // Generate answers for display
                GenerateAnswersFromPreview();
                
                Snackbar.Add(response.Message ?? "✓ Preview hoàn tất", 
                    PreviewResult.HasErrors ? Severity.Warning : Severity.Success,
                    config => { config.VisibleStateDuration = 5000; });
            }
            else
            {
                Snackbar.Add(response.Message ?? "Lỗi preview file", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Lỗi: {ex.Message}", Severity.Error);
        }
        finally
        {
            IsProcessing = false;
        }
    }

    #endregion

    #region Import

    private async Task PerformImport()
    {
        if (PreviewResult == null || !SelectedPhanId.HasValue)
        {
            Snackbar.Add("Vui lòng preview file trước", Severity.Warning);
            return;
        }

        if (PreviewResult.HasErrors)
        {
            Snackbar.Add("Không thể import khi còn lỗi. Vui lòng sửa file Word.", Severity.Error);
            return;
        }

        // Confirm dialog
        var confirm = await DialogService.ShowMessageBox(
            "Xác nhận Import",
            $"Bạn có chắc chắn muốn import {PreviewResult.TotalFound} câu hỏi vào hệ thống?",
            yesText: "Import", 
            cancelText: "Hủy");

        if (confirm != true) return;

        IsProcessing = true;
        ProcessingMessage = $"Đang import {PreviewResult.TotalFound} câu hỏi vào hệ thống...";

        try
        {
            BeQuestionBank.Shared.DTOs.Common.ApiResponse<ImportResult>? response = null;

            if (WordFile != null)
            {
                response = await ImportClient.ImportWordAsync(WordFile, SelectedPhanId.Value);
            }
            else if (ZipFile != null)
            {
                response = await ImportClient.ImportZipAsync(ZipFile, SelectedPhanId.Value);
            }

            if (response != null && response.Success && response.Data != null)
            {
                CurrentStep = 4;
                
                // Success dialog
                await DialogService.ShowMessageBox(
                    "✅ Import Thành Công",
                    $"Đã import thành công {response.Data.SuccessCount} câu hỏi vào hệ thống!",
                    yesText: "Đóng");

                Snackbar.Add($"✓ Import thành công {response.Data.SuccessCount} câu hỏi", Severity.Success,
                    config => { config.VisibleStateDuration = 8000; });

                // Reset after 2 seconds
                await Task.Delay(2000);
                ResetUpload();
                
                // Navigate to question list
                NavigationManager.NavigateTo("/question");
            }
            else
            {
                Snackbar.Add(response?.Message ?? "Lỗi import", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Lỗi: {ex.Message}", Severity.Error);
        }
        finally
        {
            IsProcessing = false;
        }
    }

    #endregion

    #region Utility

    private void ResetUpload()
    {
        WordFile = null;
        ZipFile = null;
        PreviewResult = null;
        ExpandedQuestions.Clear();
        QuestionAnswers.Clear();
        CurrentStep = SelectedPhanId.HasValue ? 2 : 1;
        StateHasChanged();
    }

    private string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    private void ToggleExpand(int questionNumber)
    {
        if (ExpandedQuestions.Contains(questionNumber))
        {
            ExpandedQuestions.Remove(questionNumber);
        }
        else
        {
            ExpandedQuestions.Add(questionNumber);
            
            // Generate answers for this question if not already exists
            if (!QuestionAnswers.ContainsKey(questionNumber))
            {
                GenerateAnswersForQuestion(questionNumber);
            }
        }
        StateHasChanged();
    }

    private void ExpandAll()
    {
        if (PreviewResult == null) return;

        if (ExpandedQuestions.Count == PreviewResult.Questions.Count)
        {
            // Collapse all
            ExpandedQuestions.Clear();
        }
        else
        {
            // Expand all
            ExpandedQuestions.Clear();
            foreach (var question in PreviewResult.Questions)
            {
                ExpandedQuestions.Add(question.QuestionNumber);
                if (!QuestionAnswers.ContainsKey(question.QuestionNumber))
                {
                    GenerateAnswersForQuestion(question.QuestionNumber);
                }
            }
        }
        StateHasChanged();
    }

    private void GenerateAnswersFromPreview()
    {
        if (PreviewResult == null) return;

        // Pre-generate answers for all non-group questions
        foreach (var question in PreviewResult.Questions.Where(q => !q.IsGroup))
        {
            if (question.AnswersCount > 0)
            {
                GenerateAnswersForQuestion(question.QuestionNumber);
            }
        }
    }

    private void GenerateAnswersForQuestion(int questionNumber)
    {
        if (PreviewResult == null) return;

        var question = PreviewResult.Questions.FirstOrDefault(q => q.QuestionNumber == questionNumber);
        if (question == null || question.IsGroup) return;

        // Generate sample answers based on answer count
        var answers = new List<AnswerDisplay>();
        char[] labels = { 'A', 'B', 'C', 'D', 'E', 'F' };
        
        int correctIndex = 0; // Usually first correct answer
        
        for (int i = 0; i < question.AnswersCount && i < labels.Length; i++)
        {
            bool isCorrect = (correctIndex < question.CorrectAnswersCount && i == correctIndex);
            
            // Rich content với formatting examples
            string content = isCorrect 
                ? $"<strong>Đáp án {labels[i]}</strong> - Nội dung đáp án đúng từ file Word"
                : $"Đáp án {labels[i]} - Nội dung đáp án từ file Word";
            
            if (question.Features.HasLatex)
            {
                content += " <span style='color: #f5576c;'>(có công thức LaTeX)</span>";
            }
            
            answers.Add(new AnswerDisplay
            {
                Label = labels[i].ToString(),
                Content = content,
                IsCorrect = isCorrect
            });
            
            if (isCorrect) correctIndex++;
        }
        
        QuestionAnswers[questionNumber] = answers;
    }

    #endregion
}

// Helper class for answer display
public class AnswerDisplay
{
    public string Label { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}
