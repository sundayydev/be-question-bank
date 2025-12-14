using Microsoft.AspNetCore.Components;
using System.Text.Json;
using BeQuestionBank.Shared.DTOs.Common;
using BeQuestionBank.Shared.DTOs.Tool;
using FEQuestionBank.Client.Services.Interface;
using Microsoft.JSInterop;
using MudBlazor;

namespace FEQuestionBank.Client.Pages.Tool;

public partial class ToolLatexConverter : ComponentBase
{
    [Inject] private IToolApiClient ToolApi { get; set; } = default!;
    [Inject] protected ISnackbar Snackbar { get; set; } = default!;
    [Inject] protected IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] protected IDialogService DialogService { get; set; }

    private string LatexInput { get; set; } = string.Empty;
    private string HtmlOutput { get; set; } = string.Empty;
    private bool IsConverted = false;
    private bool MergeSpans { get; set; } = true;

    protected List<BreadcrumbItem> _breadcrumbs = new()
    {
        new("Trang chủ", href: "/"),
        new("Công cụ", href: "#", disabled: true),
        new("Chuyển đổi LaTeX", href: "/tools/tool-latex-converter")
    };

    private async Task ConvertLatexAsync()
    {
        if (string.IsNullOrWhiteSpace(LatexInput))
        {
            Snackbar.Add("Vui lòng nhập nội dung LaTeX", Severity.Warning);
            return;
        }

        Snackbar.Add("Đang chuyển đổi...", Severity.Info);

        var request = new ConvertLatexRequest
        {
            Content = LatexInput.Trim(),
            MergeSpans = MergeSpans
        };

        try
        {
            var response = await ToolApi.ConvertLatexAsync(request);

            if (response.Success)
            {
                var data = response.Data;

                HtmlOutput = data.TryGetProperty("html", out var htmlProp)
                    ? htmlProp.GetString() ?? ""
                    : "";

                IsConverted = true;
                Snackbar.Add("Chuyển đổi thành công!", Severity.Success);
                StateHasChanged();
            }
            else
            {
                Snackbar.Add(response.Message ?? "Chuyển đổi thất bại!", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add("Lỗi kết nối đến server", Severity.Error);
        }
    }

    private async Task CopyToClipboard(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            Snackbar.Add("Không có nội dung để sao chép", Severity.Warning);
            return;
        }

        try
        {
            await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", content);
            Snackbar.Add("Đã sao chép vào clipboard!", Severity.Success);
        }
        catch
        {
            Snackbar.Add("Không thể sao chép (trình duyệt chặn)", Severity.Error);
        }
    }

    private void OpenWarningDialog()
    {
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true
        };

        DialogService.Show<LatexWarningDialog>(
            "Lưu ý khi nhập LaTeX",
            options
        );
    }
}