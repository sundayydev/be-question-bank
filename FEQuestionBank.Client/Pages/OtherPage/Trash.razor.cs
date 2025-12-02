using BeQuestionBank.Shared.DTOs.Common;
using BeQuestionBank.Shared.DTOs.Khoa;
using BeQuestionBank.Shared.DTOs.MonHoc;
using BeQuestionBank.Shared.DTOs.Phan;
using FEQuestionBank.Client.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FEQuestionBank.Client.Pages;

public partial class TrashBase : ComponentBase
{
    [Inject] private IKhoaApiClient KhoaClient { get; set; }
    [Inject] private IMonHocApiClient MonHocClient { get; set; } = default!;
    [Inject] private IPhanApiClient PhanClient { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;

    protected bool LoadingKhoa, LoadingMonHoc, LoadingPhan;
    protected int KhoaCount { get; set; }
    protected int MonHocCount { get; set; }
    protected int PhanCount { get; set; }
    protected int TotalDeleted => KhoaCount + MonHocCount + PhanCount;

    protected override async Task OnInitializedAsync()
    {
        await LoadCountsAsync();
    }
    private List<KhoaDto> allKhoas = new List<KhoaDto>();
    private List<MonHocDto> allMonHocs = new List<MonHocDto>();
    private List<PhanDto> allPhans = new List<PhanDto>();
    protected List<BreadcrumbItem> _breadcrumbs = new()
    {
        new BreadcrumbItem("Trang chủ", href: "/"),
        new BreadcrumbItem("Quản lý câu hỏi", href: "#", disabled: true),
        new BreadcrumbItem("Rác", href: "/trash")
    };
    private async Task LoadCountsAsync()
    {
        try
        {
            var t1 =await KhoaClient.GetAllKhoasAsync();
            var t2 = await MonHocClient.GetAllMonHocsAsync();
            var t3 = await PhanClient.GetAllPhansAsync();
            allKhoas = t1.Data;
            allMonHocs = t2.Data;
            allPhans = t3.Data;
            KhoaCount   = allKhoas.Count(k => k.XoaTam==true);;
            MonHocCount = allMonHocs.Count(x => x.XoaTam==true) ;
            PhanCount   = allPhans.Count(x => x.XoaTam==true);
        }
        catch (Exception ex)
        {
            Snackbar.Add("Lỗi khi tải số liệu thùng rác: " + ex.Message, Severity.Error);
        }

        finally
        {
            StateHasChanged(); 
        }
    }

    // KHOA - Dùng API /trashed
    protected async Task<TableData<KhoaDto>> LoadKhoaTrash(TableState state, CancellationToken cancellationToken)
    {
        LoadingKhoa = true;
        StateHasChanged();

        var resp = await KhoaClient.GetTrashedKhoasAsync(state.Page + 1, state.PageSize);

        LoadingKhoa = false;

        if (resp?.Success == true && resp.Data != null)
        {
            return new TableData<KhoaDto>
            {
                Items = resp.Data.Items,
                TotalItems = resp.Data.TotalCount
            };
        }

        return new TableData<KhoaDto> { Items = new List<KhoaDto>(), TotalItems = 0 };
    }

    // MÔN HỌC
    protected async Task<TableData<MonHocDto>> LoadMonHocTrash(TableState state, CancellationToken cancellationToken)
    {
        LoadingMonHoc = true;
        StateHasChanged();

        var resp = await MonHocClient.GetTrashedMonHocsAsync(state.Page + 1, state.PageSize);

        LoadingMonHoc = false;

        if (resp?.Success == true && resp.Data != null)
        {
            return new TableData<MonHocDto>
            {
                Items = resp.Data.Items,        
                TotalItems = resp.Data.TotalCount
            };
        }

        return new TableData<MonHocDto> { Items = new List<MonHocDto>(), TotalItems = 0 };
    }


    // PHẦN
    protected async Task<TableData<PhanDto>> LoadPhanTrash(TableState state, CancellationToken cancellationToken)
    {
        LoadingPhan = true;
        StateHasChanged();

        var resp = await PhanClient.GetAllPhansAsync();

        LoadingPhan = false;

        if (resp?.Success == true && resp.Data != null)
        {
            var deleted = resp.Data.Where(x => x.XoaTam==false)
                                    .Skip(state.Page * state.PageSize)
                                    .Take(state.PageSize)
                                    .ToList();

            return new TableData<PhanDto>
            {
                Items = deleted,
                TotalItems = resp.Data.Count(x => x.XoaTam==false)
            };
        }
        return new TableData<PhanDto> { Items = new List<PhanDto>(), TotalItems = 0 };
    }

    // KHÔI PHỤC
    protected async Task RestoreKhoa(Guid id)
    {
        if (await Confirm("Khôi phục khoa này?"))
        {
            var res = await KhoaClient.RestoreKhoaAsync(id);
            ShowMessage(res, "khoa");
            await LoadCountsAsync(); // Cập nhật lại số lượng
        }
    }

    protected async Task RestoreMonHoc(Guid id)
    {
        if (await Confirm("Khôi phục môn học này?"))
        {
            var res = await MonHocClient.RestoreMonHocAsync(id);
            ShowMessage(res, "môn học");
            await LoadCountsAsync();
        }
    }

    protected async Task RestorePhan(Guid id)
    {
        if (await Confirm("Khôi phục phần này?"))
        {
            var res = await PhanClient.RestorePhanAsync(id);
            ShowMessage(res, "phần");
            await LoadCountsAsync();
        }
    }
    protected string GetKhoaName(Guid maKhoa)
    {
        var khoa = allKhoas.Find(k => k.MaKhoa == maKhoa);
        return khoa?.TenKhoa ?? "Không xác định";
    }

    private async Task<bool> Confirm(string msg) =>
        await DialogService.ShowMessageBox("Xác nhận", msg, yesText: "Khôi phục", cancelText: "Hủy") == true;

    private void ShowMessage(ApiResponse<string> res, string entity)
    {
        Snackbar.Add(
            res.Success ? $"Đã khôi phục {entity} thành công!" : res.Message,
            res.Success ? Severity.Success : Severity.Error);
    }
}