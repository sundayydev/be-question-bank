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
using FEQuestionBank.Client.Component.DiaLog;
using FEQuestionBank.Client.Components;
using FEQuestionBank.Client.Helpers;
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
        private string? _currentSearchTerm;
        private Guid? _currentKhoaId;
        private Guid? _currentMonHocId;
        private Guid? _currentPhanId;
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
            new BreadcrumbItem("Ngân hàng câu hỏi", href: "/question/list")
        };

        protected override async Task OnInitializedAsync()
        {
            var query = UrlStateHelper.GetQueryParams(Navigation);

            _searchTerm = query.GetValueOrDefault("search");
            Guid.TryParse(query.GetValueOrDefault("khoa"), out var k);
            SelectedKhoaId = k != Guid.Empty ? k : null;
            Guid.TryParse(query.GetValueOrDefault("mon"), out var m);
            SelectedMonHocId = m != Guid.Empty ? m : null;
            Guid.TryParse(query.GetValueOrDefault("phan"), out var p);
            SelectedPhanId = p != Guid.Empty ? p : null;

            await Task.WhenAll(LoadCountsAsync(), LoadKhoasAsync());

            if (SelectedKhoaId.HasValue) await OnKhoaChanged(SelectedKhoaId);
            if (SelectedMonHocId.HasValue) await OnMonHocChanged(SelectedMonHocId);

            StateHasChanged();
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

        protected async Task OnKhoaChanged(Guid? id)
        {
            SelectedKhoaId = id;
            SelectedMonHocId = null;
            SelectedPhanId = null;
            MonHocs.Clear();
            Phans.Clear();

            if (id.HasValue)
            {
                LoadingMon = true;
                var res = await MonHocClient.GetMonHocsByMaKhoaAsync(id.Value);
                if (res.Success) MonHocs = res.Data ?? new();
                LoadingMon = false;
            }

            SyncUrlAndReload();
            await LoadCountsAsync();
        }

        protected async Task OnMonHocChanged(Guid? id)
        {
            SelectedMonHocId = id;
            SelectedPhanId = null;
            Phans.Clear();

            if (id.HasValue)
            {
                LoadingPhan = true;
                var res = await PhanClient.GetPhanByMonHocAsync(id.Value);
                if (res.Success) Phans = res.Data ?? new();
                LoadingPhan = false;
            }

            SyncUrlAndReload();
            await LoadCountsAsync();
        }

        protected async Task OnPhanChanged(Guid? id)
        {
            SelectedPhanId = id;
            SyncUrlAndReload();
            await LoadCountsAsync();
        }

        // Hàm xử lý khi từ khóa tìm kiếm thay đổi
        protected async Task OnSearchChanged(string value)
        {
            _searchTerm = value;
            await ReloadBothTables();
            await LoadCountsAsync();
        }
        
        protected async Task ForceSearchNow()
        {
            SyncUrlAndReload();
            //await ReloadBothTables();
            await LoadCountsAsync();
        }

        // Hàm chung reload cả  bảng
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

        // Khi chuyển tab
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

        private void SyncUrlAndReload()
        {
            UrlStateHelper.UpdateUrl(Navigation, new()
            {
                ["search"] = _searchTerm,
                ["khoa"] = SelectedKhoaId?.ToString(),
                ["mon"] = SelectedMonHocId?.ToString(),
                ["phan"] = SelectedPhanId?.ToString()
            });

            _ = ReloadAllTables();
        }

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
                "Essay" or "Self" => "Essay", // Tự luận (có hoặc không có câu con)
                "Single" => "Single", // Trắc nghiệm chọn 1
                "MultipleChoice" => "Multi", // Trắc nghiệm chọn nhiều
                "FillBlank" or "Filling" => "Fill", // Điền từ / điền chỗ trống
                "Group" => "Group", // Câu nhóm (có câu con)
                "Pairing" or "Matching" => "Pairing", // Ghép nối
                _ => "Single" // Mặc định
            };

            var parameters = new DialogParameters<QuestionDetailDialog>
            {
                { x => x.CauHoi, cauHoi },
                //  {x => x.}
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
            _searchTerm = null;
            _currentSearchTerm = null;
            SelectedKhoaId = _currentKhoaId = null;
            SelectedMonHocId = _currentMonHocId = null;
            SelectedPhanId = _currentPhanId = null;
            MonHocs.Clear();
            Phans.Clear();

            Navigation.NavigateTo("/question/list", forceLoad: false);
            await ReloadAllTables();
            await LoadCountsAsync();
            Snackbar.Add("Đã làm mới bộ lọc!", Severity.Info);
        }

        protected Color GetLevelColor(short level) => level switch
        {
            <= 2 => Color.Success, // 1–2: xanh
            3 => Color.Warning, // 3: vàng
            >= 4 => Color.Error // 4 trở lên: đỏ
        };
        
        private async Task ReloadAllTables()
        {
            await Task.WhenAll(
                essayTable.ReloadServerData(),
                singleTable.ReloadServerData(),
                fillblankTable.ReloadServerData(),
                groupTable.ReloadServerData(),
                pairingTable.ReloadServerData(),
                multipleTable.ReloadServerData()
            );
        }
    }

    public static class StringExtensions
    {
        public static string Truncate(this string? value, int maxLength)
            => string.IsNullOrEmpty(value) ? "" : value.Length <= maxLength ? value : value[..maxLength] + "...";
    }
}