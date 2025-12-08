using System.Text.RegularExpressions;
using BeQuestionBank.Shared.DTOs.CauHoi;
using BeQuestionBank.Shared.DTOs.Common;
using FEQuestionBank.Client.Services.Interface;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Threading.Tasks;
using BeQuestionBank.Shared.DTOs.Khoa;
using BeQuestionBank.Shared.DTOs.MonHoc;
using BeQuestionBank.Shared.DTOs.Phan;
using FEQuestionBank.Client.Component;
using FEQuestionBank.Client.Components;
using FEQuestionBank.Client.Services;

namespace FEQuestionBank.Client.Pages
{
    public partial class QuestionPageBase : ComponentBase
    {
        [Inject] protected ICauHoiApiClient CauHoiClient { get; set; }
        [Inject] protected IDialogService DialogService { get; set; }
        [Inject] protected IKhoaApiClient KhoaClient { get; set; }
        [Inject] protected IMonHocApiClient MonHocClient { get; set; }
        [Inject] protected IPhanApiClient PhanClient { get; set; }
        [Inject] protected ISnackbar Snackbar { get; set; }
        [Inject] protected NavigationManager Navigation { get; set; } = default!;

        protected string? _searchTerm;
        protected MudTable<CauHoiDto> essayTable = new();
        protected MudTable<CauHoiDto> singleTable = new();
        protected MudTable<CauHoiDto> groupTable = new();
        protected MudTable<CauHoiDto> fillblankTable = new();
        protected MudTable<CauHoiDto> pairingTable = new();
        protected MudTable<CauHoiDto> multipleTable = new();

        protected Guid? SelectedKhoaId { get; set; }
        protected Guid? SelectedMonHocId { get; set; }
        protected Guid? SelectedPhanId { get; set; }

        protected List<KhoaDto> Khoas { get; set; } = new();
        protected List<MonHocDto> MonHocs { get; set; } = new();
        protected List<PhanDto> Phans { get; set; } = new();

        protected bool LoadingKhoa = false, LoadingMon = false, LoadingPhan = false;

        protected int TotalQuestions = 0;

        protected int EssayCount = 0,
            SingleCount = 0,
            FillCount = 0,
            GroupCount = 0,
            PairingCount = 0,
            MultipleChoiceCount = 0;

        protected bool LoadingEssay = false,
            LoadingSingle = false,
            LoadingGroup = false,
            LoadingFillBlink = false,
            LoadingPairing = false,
            LoadingMultiple = false;

        protected List<BreadcrumbItem> _breadcrumbs = new()
        {
            new BreadcrumbItem("Trang chủ", href: "/"),
            new BreadcrumbItem("Ngân hàng câu hỏi", href: "/cauhoi")
        };

        protected override async Task OnInitializedAsync()
        {
            await Task.WhenAll(LoadCountsAsync(), LoadKhoasAsync());
        }

        protected async Task LoadCountsAsync()
        {
            try
            {
                var t1 = await CauHoiClient.GetEssaysPagedAsync(
                    page: 1, pageSize: 1,
                    search: _searchTerm,
                    khoaId: SelectedKhoaId,
                    monHocId: SelectedMonHocId,
                    phanId: SelectedPhanId
                );

                var t2 = await CauHoiClient.GetSinglesPagedAsync(
                    page: 1, pageSize: 1,
                    search: _searchTerm,
                    khoaId: SelectedKhoaId,
                    monHocId: SelectedMonHocId,
                    phanId: SelectedPhanId
                );
                var t3 = await CauHoiClient.GetGroupsPagedAsync(
                    page: 1, pageSize: 1,
                    search: _searchTerm,
                    khoaId: SelectedKhoaId,
                    monHocId: SelectedMonHocId,
                    phanId: SelectedPhanId
                );
                var t4 = await CauHoiClient.GetFillingsPagedAsync(
                    page: 1, pageSize: 1,
                    search: _searchTerm,
                    khoaId: SelectedKhoaId,
                    monHocId: SelectedMonHocId,
                    phanId: SelectedPhanId
                );
                var t5 = await CauHoiClient.GetPairingsPagedAsync(
                    page: 1, pageSize: 1,
                    search: _searchTerm,
                    khoaId: SelectedKhoaId,
                    monHocId: SelectedMonHocId,
                    phanId: SelectedPhanId
                );
                var t6 = await CauHoiClient.GetMultipeChoicesPagedAsync(
                    page: 1, pageSize: 1,
                    search: _searchTerm,
                    khoaId: SelectedKhoaId,
                    monHocId: SelectedMonHocId,
                    phanId: SelectedPhanId
                );

                EssayCount = t1.Data?.TotalCount ?? 0;
                SingleCount = t2.Data?.TotalCount ?? 0;
                FillCount = t4.Data?.TotalCount ?? 0;
                GroupCount = t3.Data?.TotalCount ?? 0;
                PairingCount = t5.Data?.TotalCount ?? 0;
                MultipleChoiceCount = t6.Data?.TotalCount ?? 0;
                TotalQuestions = EssayCount + SingleCount;
            }
            catch
            {
                // ignore
            }
        }


        protected async Task LoadKhoasAsync()
        {
            LoadingKhoa = true;
            var response = await KhoaClient.GetAllKhoasAsync();
            if (response.Success)
                Khoas = response.Data ?? new();
            LoadingKhoa = false;
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
                LoadingMon = true;
                StateHasChanged();
                var response = await MonHocClient.GetMonHocsByMaKhoaAsync(khoaId.Value);
                if (response.Success)
                    MonHocs = response.Data ?? new();
                LoadingMon = false;
            }

            await ReloadBothTables();
            await LoadCountsAsync();
        }

        // Khi chọn Môn học → load Phần
        protected async Task OnMonHocChanged(Guid? monHocId)
        {
            SelectedMonHocId = monHocId;
            SelectedPhanId = null;
            Phans.Clear();

            if (monHocId.HasValue)
            {
                LoadingPhan = true;
                StateHasChanged();
                var response = await PhanClient.GetPhanByMonHocAsync(monHocId.Value);
                if (response.Success)
                    Phans = response.Data ?? new();
                LoadingPhan = false;
            }

            await ReloadBothTables();
            await LoadCountsAsync();
        }

        // Hàm xử lý khi từ khóa tìm kiếm thay đổi
        protected async Task OnSearchChanged(string value)
        {
            _searchTerm = value;
            await ReloadBothTables();
            await LoadCountsAsync();
        }

        protected async Task OnPhanChanged(Guid? phanId)
        {
            SelectedPhanId = phanId;
            await ReloadBothTables();
            await LoadCountsAsync();
        }

        // Hàm này được gọi khi nhấn Enter → tìm ngay lập tức
        protected async Task ForceSearchNow()
        {
            await ReloadBothTables();
            await LoadCountsAsync();
        }

        // Hàm chung reload cả 2 bảng
        protected async Task ReloadBothTables()
        {
            await Task.WhenAll(
                essayTable?.ReloadServerData() ?? Task.CompletedTask,
                singleTable?.ReloadServerData() ?? Task.CompletedTask,
                groupTable?.ReloadServerData() ?? Task.CompletedTask,
                fillblankTable?.ReloadServerData() ?? Task.CompletedTask,
                pairingTable?.ReloadServerData() ?? Task.CompletedTask,
                multipleTable?.ReloadServerData() ?? Task.CompletedTask
            );
        }

        // Khi chuyển tab, nếu đang có từ khóa thì reload bảng mới
        protected async Task OnTabChanged(int index)
        {
            if (!string.IsNullOrWhiteSpace(_searchTerm))
            {
                await ReloadBothTables();
            }
        }

        // Load Tự luận
        protected async Task<TableData<CauHoiDto>> LoadEssayData(TableState state, CancellationToken token)
        {
            LoadingEssay = true;
            StateHasChanged();

            var response = await CauHoiClient.GetEssaysPagedAsync(
                page: state.Page + 1,
                pageSize: state.PageSize,
                sort: state.SortLabel + (state.SortDirection == SortDirection.Descending ? ",desc" : ""),
                search: _searchTerm,
                khoaId: SelectedKhoaId,
                monHocId: SelectedMonHocId,
                phanId: SelectedPhanId);

            // Console.WriteLine($"TotalCount: {response.Data?.TotalCount}");
            // Console.WriteLine($"Items: {System.Text.Json.JsonSerializer.Serialize(response.Data?.Items)}");
            // Console.WriteLine($"Full Response: {System.Text.Json.JsonSerializer.Serialize(response)}");

            LoadingEssay = false;

            if (response.Success && response.Data != null && response.Data.TotalCount > 0)
            {
                return new TableData<CauHoiDto>
                {
                    Items = response.Data.Items,
                    TotalItems = response.Data.TotalCount
                };
            }
            
            return new TableData<CauHoiDto>
            {
                Items = null, 
                TotalItems = 0
            };
        }

        protected async Task<TableData<CauHoiDto>> LoadGroupData(TableState state, CancellationToken token)
        {
            var response = await CauHoiClient.GetGroupsPagedAsync(
                page: state.Page + 1,
                pageSize: state.PageSize,
                sort: state.SortLabel + (state.SortDirection == SortDirection.Descending ? ",desc" : ""),
                search: _searchTerm,
                khoaId: SelectedKhoaId,
                monHocId: SelectedMonHocId,
                phanId: SelectedPhanId);

            return response.Success && response.Data != null
                ? new TableData<CauHoiDto> { Items = response.Data.Items, TotalItems = response.Data.TotalCount }
                : new TableData<CauHoiDto> { Items = new List<CauHoiDto>(), TotalItems = 0 };
        }

        protected async Task<TableData<CauHoiDto>> LoadMultipeChoiceData(TableState state, CancellationToken token)
        {
            var response = await CauHoiClient.GetMultipeChoicesPagedAsync(
                page: state.Page + 1,
                pageSize: state.PageSize,
                sort: state.SortLabel + (state.SortDirection == SortDirection.Descending ? ",desc" : ""),
                search: _searchTerm,
                khoaId: SelectedKhoaId,
                monHocId: SelectedMonHocId,
                phanId: SelectedPhanId);

            return response.Success && response.Data != null
                ? new TableData<CauHoiDto> { Items = response.Data.Items, TotalItems = response.Data.TotalCount }
                : new TableData<CauHoiDto> { Items = new List<CauHoiDto>(), TotalItems = 0 };
        }

        protected async Task<TableData<CauHoiDto>> LoadPairingData(TableState state, CancellationToken token)
        {
            var response = await CauHoiClient.GetPairingsPagedAsync(
                page: state.Page + 1,
                pageSize: state.PageSize,
                sort: state.SortLabel + (state.SortDirection == SortDirection.Descending ? ",desc" : ""),
                search: _searchTerm,
                khoaId: SelectedKhoaId,
                monHocId: SelectedMonHocId,
                phanId: SelectedPhanId);

            return response.Success && response.Data != null
                ? new TableData<CauHoiDto> { Items = response.Data.Items, TotalItems = response.Data.TotalCount }
                : new TableData<CauHoiDto> { Items = new List<CauHoiDto>(), TotalItems = 0 };
        }

        protected async Task<TableData<CauHoiDto>> LoadFillingData(TableState state, CancellationToken token)
        {
            var response = await CauHoiClient.GetFillingsPagedAsync(
                page: state.Page + 1,
                pageSize: state.PageSize,
                sort: state.SortLabel + (state.SortDirection == SortDirection.Descending ? ",desc" : ""),
                search: _searchTerm,
                khoaId: SelectedKhoaId,
                monHocId: SelectedMonHocId,
                phanId: SelectedPhanId);

            return response.Success && response.Data != null
                ? new TableData<CauHoiDto> { Items = response.Data.Items, TotalItems = response.Data.TotalCount }
                : new TableData<CauHoiDto> { Items = new List<CauHoiDto>(), TotalItems = 0 };
        }

        protected async Task<TableData<CauHoiDto>> LoadSingleData(TableState state, CancellationToken token)
        {
            var response = await CauHoiClient.GetSinglesPagedAsync(
                page: state.Page + 1,
                pageSize: state.PageSize,
                sort: state.SortLabel + (state.SortDirection == SortDirection.Descending ? ",desc" : ""),
                search: _searchTerm,
                khoaId: SelectedKhoaId,
                monHocId: SelectedMonHocId,
                phanId: SelectedPhanId);

            return response.Success && response.Data != null
                ? new TableData<CauHoiDto> { Items = response.Data.Items, TotalItems = response.Data.TotalCount }
                : new TableData<CauHoiDto> { Items = new List<CauHoiDto>(), TotalItems = 0 };
        }

        protected void OpenCreateDialog()
        {
            Navigation.NavigateTo("/question/create-question");
        }

        // protected async Task OnDeleteQuestionAsync(Guid id, string noiDung)
        // {
        //     var parameters = new DialogParameters
        //     {
        //         ["Message"] = "Bạn có chắc chắn muốn xóa câu hỏi này?",
        //         ["NoiDung"] = noiDung 
        //     };
        //
        //     var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small, FullWidth = true };
        //     var dialog = DialogService.Show<ConfirmDeleteDialog>("Xác nhận xóa", parameters, options);
        //     var result = await dialog.Result;
        //     
        //     if (!result.Canceled)
        //     {
        //         var response = await CauHoiClient.DeleteQuestionAsync(id);
        //
        //         if (response.Success)
        //         {
        //             Snackbar.Add("Xóa câu hỏi thành công!", Severity.Success);
        //             await ReloadBothTables();
        //             await LoadCountsAsync();
        //         }
        //         else
        //         {
        //             Snackbar.Add($"Xóa thất bại: {response.Message}", Severity.Error);
        //         }
        //     }
        // }
        protected async Task OnDeleteQuestionAsync(Guid id, string noiDung, int? soLanDung)
        {
            // Chặn xóa nếu câu hỏi đã dùng nhiều hơn 1 lần
            if (soLanDung > 1)
            {
                Snackbar.Add(" Câu hỏi đã được sử dụng nhiều lần nên không thể xóa!", Severity.Warning);
                return;
            }

            var parameters = new DialogParameters
            {
                ["Message"] = "Bạn có chắc chắn muốn xóa câu hỏi này?",
                ["NoiDung"] = noiDung
            };

            var options = new DialogOptions
            {
                CloseOnEscapeKey = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = DialogService.Show<ConfirmDeleteDialog>("Xác nhận xóa", parameters, options);
            var result = await dialog.Result;

            if (!result.Canceled)
            {
                var response = await CauHoiClient.DeleteQuestionAsync(id);

                if (response.Success)
                {
                    Snackbar.Add("Xóa câu hỏi thành công!", Severity.Success);
                    await ReloadBothTables();
                    await LoadCountsAsync();
                }
                else
                {
                    Snackbar.Add($"Xóa thất bại: {response.Message}", Severity.Error);
                }
            }
        }


        /// <summary>
        /// Mở dialog xem chi tiết câu hỏi – tự động xác định kiểu hiển thị dựa trên tab hiện tại
        /// </summary>
        protected void ViewDetail(CauHoiDto cauHoi, string? forceViewType = null)
        {
            var viewType = forceViewType ?? cauHoi.LoaiCauHoi switch
            {
                "Essay" or "Self" => "GroupEssay", // Tự luận (có hoặc không có câu con)
                "Single" => "SingleMulti", // Trắc nghiệm chọn 1
                "MultipleChoice" => "SingleMulti", // Trắc nghiệm chọn nhiều
                "FillBlank" or "Filling" => "Fill", // Điền từ / điền chỗ trống
                "Group" => "GroupEssay", // Câu nhóm (có câu con)
                "Pairing" or "Matching" => "Pairing", // Ghép nối
                _ => "SingleMulti" // Mặc định
            };

            var parameters = new DialogParameters<QuestionDetailDialog>
            {
                { x => x.CauHoi, cauHoi },
                { x => x.ViewType, viewType }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true,
                CloseOnEscapeKey = true,
                Position = DialogPosition.Center
            };

            DialogService.Show<QuestionDetailDialog>("Chi tiết câu hỏi", parameters, options);
        }

        protected async Task ResetFiltersAsync()
        {
            _searchTerm = string.Empty;
            SelectedKhoaId = null;
            SelectedMonHocId = null;
            SelectedPhanId = null;
            MonHocs.Clear();
            Phans.Clear();
            StateHasChanged();
            await ReloadBothTables();
            await LoadCountsAsync();
            Snackbar.Add("Đã làm mới bộ lọc!", Severity.Info);
        }


        protected Color GetLevelColor(short level) => level switch
        {
            <= 2 => Color.Success,
            <= 4 => Color.Warning,
            _ => Color.Error
        };
    }

    public static class StringExtensions
    {
        public static string Truncate(this string? value, int maxLength)
            => string.IsNullOrEmpty(value) ? "" : value.Length <= maxLength ? value : value[..maxLength] + "...";
    }
}