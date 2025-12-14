using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Text.Json;
using BeQuestionBank.Shared.DTOs.Common;
using FEQuestionBank.Client.Services.Interface;
using Microsoft.JSInterop;
using MudBlazor;

namespace FEQuestionBank.Client.Pages.Tool;

public partial class ToolImagesBase : ComponentBase
{
    [Inject] private IToolApiClient ToolApi { get; set; } = default!;
    [Inject] protected ISnackbar Snackbar { get; set; } = default!;
    [Inject] protected IJSRuntime JSRuntime { get; set; } = default!;
    protected IList<IBrowserFile> files = new List<IBrowserFile>();

    private string HtmlContent { get; set; } = string.Empty;
    private string Base64String { get; set; } = string.Empty;

    private bool _isDragging = false;

    protected List<BreadcrumbItem> _breadcrumbs = new()
    {
        new("Trang chủ", href: "/"),
        new("Công cụ", href: "#", disabled: true),
        new("Chuyển ảnh mã hóa", href: "/tools/tool-images-base")
    };

    protected async Task UploadFile(IBrowserFile file)
    {
        ClearResults();

        if (file == null)
        {
            Snackbar.Add("Vui lòng chọn hình ảnh", Severity.Warning);
            return;
        }

        const long maxFileSize = 10 * 1024 * 1024; // 10MB
        if (file.Size > maxFileSize)
        {
            Snackbar.Add("File quá lớn! Tối đa 10MB", Severity.Error);
            return;
        }

        Snackbar.Add("Đang xử lý hình ảnh...", Severity.Info);

        var result = await ToolApi.UploadImageAsync(file);

        if (result.Success && result.Data.ValueKind == JsonValueKind.Object)
        {
            var data = result.Data;

            Base64String = data.TryGetProperty("base64", out var b64)
                ? b64.GetString() ?? ""
                : "";

            HtmlContent = data.TryGetProperty("html", out var html)
                ? html.GetString() ?? ""
                : "";

            Snackbar.Add("Chuyển đổi thành công!", Severity.Success);
        }
        else
        {
            Snackbar.Add(result.Message ?? "Upload thất bại!", Severity.Error);
        }
    }


    private void ClearResults()
    {
        HtmlContent = string.Empty;
        Base64String = string.Empty;
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
            Snackbar.Add("Không thể sao chép (trình duyệt chặn clipboard)", Severity.Error);
        }
    }
}