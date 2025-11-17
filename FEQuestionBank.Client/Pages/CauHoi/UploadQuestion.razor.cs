using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using BeQuestionBank.Shared.DTOs.Khoa;
using BeQuestionBank.Shared.DTOs.MonHoc;
using BeQuestionBank.Shared.DTOs.Phan;
using FEQuestionBank.Client.Services;

namespace FEQuestionBank.Client.Pages.CauHoi;

public partial class UploadQuestion : ComponentBase
{
    [Inject] private IKhoaApiClient KhoaClient { get; set; } = default!;
    [Inject] private IMonHocApiClient MonHocClient { get; set; } = default!;
    [Inject] private IPhanApiClient PhanClient { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    // Breadcrumb
    protected List<BreadcrumbItem> _breadcrumbs = new()
    {
        new BreadcrumbItem("Trang chủ", href: "/"),
        new BreadcrumbItem("Quản lý câu hỏi", href: "#", disabled: true),
        new BreadcrumbItem("Thêm câu hỏi", href: "/question/create-question"),
        new BreadcrumbItem("Tải lên câu hỏi", href: "/question/upload")
    };

    // Dữ liệu
    private List<KhoaDto> Khoas { get; set; } = new();
    private List<MonHocDto> MonHocs { get; set; } = new();
    private List<PhanDto> Phans { get; set; } = new();

    // Giá trị chọn
    private Guid? SelectedKhoa { get; set; }
    private Guid? SelectedMonHoc { get; set; }
    private Guid? SelectedPhanId { get; set; }

    // File
    private IBrowserFile? WordFile;
    private IBrowserFile? ZipFile;

    protected override async Task OnInitializedAsync()
    {
        await LoadKhoasAsync();
    }

    private async Task LoadKhoasAsync()
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

    private async Task OnKhoaChanged(Guid? maKhoa)
    {
        SelectedKhoa = maKhoa;
        SelectedMonHoc = null;
        SelectedPhanId = null;
        MonHocs.Clear();
        Phans.Clear();

        if (maKhoa.HasValue)
        {
            await LoadMonHocsByKhoaAsync(maKhoa.Value);
        }
    }

    private async Task LoadMonHocsByKhoaAsync(Guid maKhoa)
    {
        var response = await MonHocClient.GetMonHocsByMaKhoaAsync(maKhoa); // Nếu API nhận string
        // Hoặc: await MonHocClient.GetMonHocsByKhoaIdAsync(maKhoa); // nếu có API riêng
        if (response.Success && response.Data != null)
        {
            MonHocs = response.Data;
        }
        else
        {
            Snackbar.Add("Không tải được môn học.", Severity.Warning);
        }
    }

    private async Task OnMonHocChanged(Guid? maMonHoc)
    {
        SelectedMonHoc = maMonHoc;
        SelectedPhanId = null;
        Phans.Clear();

        if (maMonHoc.HasValue)
        {
            await LoadPhansByMonHocAsync(maMonHoc.Value);
        }
    }

    private async Task LoadPhansByMonHocAsync(Guid monHocId)
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

    private async Task OnWordFileSelected(InputFileChangeEventArgs e)
    {
        WordFile = e.File;
        if (WordFile != null)
        {
            Snackbar.Add($"Đã chọn: {WordFile.Name}", Severity.Success);
        }
    }

    private async Task OnZipFileSelected(InputFileChangeEventArgs e)
    {
        ZipFile = e.File;
        if (ZipFile != null)
        {
            Snackbar.Add($"Đã chọn: {ZipFile.Name}", Severity.Info);
        }
    }
}