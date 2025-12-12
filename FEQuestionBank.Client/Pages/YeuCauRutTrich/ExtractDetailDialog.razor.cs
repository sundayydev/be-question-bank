using BeQuestionBank.Shared.DTOs.YeuCauRutTrich;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Text.Json;
using Microsoft.JSInterop;

namespace FEQuestionBank.Client.Pages
{
    public partial class YeuCauDetailDialogBase :ComponentBase
    {
        [CascadingParameter] public IMudDialogInstance MudDialog { get; set; } = default!;
        [Parameter] public YeuCauRutTrichDto YeuCau { get; set; } = new();
        [Inject] ISnackbar Snackbar { get; set; } = default!;
        [Inject] protected IJSRuntime JSRuntime { get; set; } = default!;


        // Đóng dialog
        public void Cancel() => MudDialog.Cancel();

        // Property format JSON MaTran đẹp
        public string FormattedMaTran => FormatJson(YeuCau.MaTran);

        private string FormatJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return "-";

            try
            {
                using var doc = JsonDocument.Parse(json);
                return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
            }
            catch
            {
                // Nếu JSON không hợp lệ, trả nguyên chuỗi
                return json;
            }
        }
        protected async Task CopyMaTran()
        {
            if (!string.IsNullOrWhiteSpace(YeuCau.MaTran))
            {
                await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", YeuCau.MaTran);
                // Hiển thị thông báo
                Snackbar.Add("Ma trận đã được sao chép!", Severity.Success);
            }
        }

        
    }
}