using BeQuestionBank.Shared.DTOs.CauHoi;
using BeQuestionBank.Shared.DTOs.CauTraLoi;
using BeQuestionBank.Shared.DTOs.Khoa;
using BeQuestionBank.Shared.DTOs.MonHoc;
using BeQuestionBank.Shared.DTOs.Phan;
using BeQuestionBank.Shared.Enums;
using FEQuestionBank.Client.Component;
using FEQuestionBank.Client.Component.Preview;
using FEQuestionBank.Client.Services;
using FEQuestionBank.Client.Services.Interface;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FEQuestionBank.Client.Pages.CauHoi;

public partial class CreatePairingQuestionBase : ComponentBase
{
    [Inject] protected ICauHoiApiClient CauHoiApiClient { get; set; } = default!;
    [Inject] protected IKhoaApiClient KhoaApiClient { get; set; } = default!;
    [Inject] protected IMonHocApiClient MonHocApiClient { get; set; } = default!;
    [Inject] protected IPhanApiClient PhanApiClient { get; set; } = default!;
    [Inject] protected NavigationManager Nav { get; set; } = default!;
    [Inject] protected ISnackbar Snackbar { get; set; } = default!;
    [Inject] protected IDialogService Dialog { get; set; } = default!;

    protected List<KhoaDto> Khoas = new();
    protected List<MonHocDto> MonHocs = new();
    protected List<PhanDto> Phans = new();
    protected List<EnumCLO> CLOs = Enum.GetValues(typeof(EnumCLO)).Cast<EnumCLO>().ToList();

    protected Guid? SelectedKhoaId { get; set; }
    protected Guid? SelectedMonHocId { get; set; }
    protected Guid? SelectedPhanId { get; set; }
    protected EnumCLO SelectedCLO { get; set; }
    protected string HuongDanChung { get; set; } = "Ghép các khái niệm ở cột trái với đáp án phù hợp ở cột phải";
    protected short CapDo { get; set; }
    protected bool HoanVi { get; set; } = true;

    protected List<PairItem> Pairs = new()
    {
        new PairItem(),
        new PairItem()
    };

    protected List<BreadcrumbItem> _breadcrumbs = new()
    {
        new("Trang chủ", "/"),
        new("Tạo câu hỏi", "/questions"),
        new("Ghép nối", "/create-question/create-pairing", disabled: true)
    };


    protected override async Task OnInitializedAsync()
    {
        await LoadKhoas();
    }

    private async Task LoadKhoas()
    {
        var res = await KhoaApiClient.GetAllKhoasAsync();
        if (res.Success) Khoas = res.Data ?? new();
    }

    protected async Task OnKhoaChanged(Guid? khoaId)
    {
        SelectedKhoaId = khoaId;
        SelectedMonHocId = null;
        SelectedPhanId = null;
        MonHocs.Clear();
        Phans.Clear();

        if (khoaId.HasValue)
        {
            var res = await MonHocApiClient.GetMonHocsByMaKhoaAsync(khoaId.Value);
            if (res.Success && res.Data != null) MonHocs = res.Data;
        }
    }

    protected async Task OnMonHocChanged(Guid? monHocId)
    {
        SelectedMonHocId = monHocId;
        SelectedPhanId = null;
        Phans.Clear();

        if (monHocId.HasValue)
        {
            var res = await PhanApiClient.GetPhanByMonHocAsync(monHocId.Value);
            if (res.Success && res.Data != null) Phans = res.Data;
        }
    }


    protected void AddPair() => Pairs.Add(new PairItem());
    protected void RemovePair(int index) => Pairs.RemoveAt(index);

    protected async Task PreviewQuestion()
    {
        if (Pairs.Count < 2 ||
            Pairs.Any(p => string.IsNullOrWhiteSpace(p.Question) || string.IsNullOrWhiteSpace(p.Answer)))
        {
            Snackbar.Add("Vui lòng nhập ít nhất 2 cặp đầy đủ", Severity.Warning);
            return;
        }

        var tenPhan = Phans.FirstOrDefault(p => p.MaPhan == SelectedPhanId)?.TenPhan ?? "Chưa chọn";

        var parameters = new DialogParameters
        {
            ["Pairs"] = Pairs,
            ["TenPhan"] = tenPhan,
            ["HuongDanChung"] = HuongDanChung,
            ["CapDo"] = CapDo,
            ["HoanVi"] = HoanVi,
            ["CloName"] = SelectedCLO.ToString(),
            [nameof(QuestionFillBlankPreviewDialog.CloName)] = SelectedCLO.ToString()
        };

        var options = new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true };
        var dialog = Dialog.Show<PairingPreviewDialog>("Xem trước câu hỏi ghép nối", parameters, options);
        var result = await dialog.Result;

        if (!result.Canceled && result.Data is true)
            await SaveQuestion();
    }

    protected async Task SaveQuestion()
    {
        if (!SelectedPhanId.HasValue)
        {
            Snackbar.Add("Vui lòng chọn chương/phần", Severity.Error);
            return;
        }

        if (Pairs.Count < 2 ||
            Pairs.Any(p => string.IsNullOrWhiteSpace(p.Question) || string.IsNullOrWhiteSpace(p.Answer)))
        {
            Snackbar.Add("Vui lòng nhập đầy đủ ít nhất 2 cặp", Severity.Error);
            return;
        }

        var dto = new CreateCauHoiGhepNoiDto
        {
            MaPhan = SelectedPhanId.Value,
            NoiDung = HuongDanChung,
            CapDo = CapDo,
            HoanVi = HoanVi,
            CLO = SelectedCLO,
            CauHoiCons = Pairs.Select(p => new CreateCauHoiWithCauTraLoiDto
            {
                NoiDung = p.Question,
                CapDo = CapDo,
                HoanVi = HoanVi,
                CauTraLois = new List<CreateCauTraLoiDto>
                {
                    new() { NoiDung = p.Answer, LaDapAn = true }
                }
            }).ToList()
        };

        var result = await CauHoiApiClient.CreatePairingQuestionAsync(dto);

        if (result.Success)
        {
            Snackbar.Add("Tạo câu hỏi ghép nối thành công!", Severity.Success);
            Nav.NavigateTo("/questions");
        }
        else
        {
            Snackbar.Add(result.Message ?? "Có lỗi xảy ra", Severity.Error);
        }
    }

    protected void GoBack() => Nav.NavigateTo("/questions");

    public class PairItem
    {
        public string Question { get; set; } = "";
        public string Answer { get; set; } = "";
    }
}