// UserProfile.razor.cs

using BeQuestionBank.Shared.DTOs.Khoa;
using BeQuestionBank.Shared.Enums;
using BEQuestionBank.Shared.DTOs.user;
using BeQuestionBank.Shared.DTOs.YeuCauRutTrich;
using FEQuestionBank.Client.Implementation;
using FEQuestionBank.Client.Services;
using FEQuestionBank.Client.Services.Interface;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using NPOI.SS.Formula.Functions;

namespace FEQuestionBank.Client.Pages.Auth;

public partial class UserProfile : ComponentBase
{
    [Inject] private IAuthApiClient AuthApi { get; set; } = default!;
    [Inject] protected IKhoaApiClient KhoaApiClient { get; set; } = default!;
    [Inject] private IYeuCauRutTrichApiClient YeuCauRutTrichApi { get; set; } = default!;

    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private CustomAuthStateProvider AuthState { get; set; } = default!;
    protected List<KhoaDto> Khoas { get; set; } = new();

    private NguoiDungDto? CurrentUser { get; set; }
    private bool IsLoading { get; set; } = true;
    
    // Withdrawal History
    private List<YeuCauRutTrichDto> WithdrawalHistory { get; set; } = new();
    private bool IsLoadingHistory { get; set; } = true;

    // Các property để binding trực tiếp trong Razor (giữ nguyên UI)
    private string HoTen => CurrentUser?.HoTen ?? "Chưa có tên";
    private string TenDangNhap => CurrentUser?.TenDangNhap ?? "N/A";
    private string Email => CurrentUser?.Email ?? "Chưa có email";
    private string? AvatarUrl =>"images/default-avatar.png"; 
    private bool BiKhoa => CurrentUser?.BiKhoa ?? false;
    private DateTime NgayTao => CurrentUser?.NgayTao ?? DateTime.MinValue;

    // Xử lý vai trò: 1 = Admin, 0 = User
    private string VaiTro => (int)(CurrentUser?.VaiTro ?? 0) == 1 ? "Admin" : "User";
    
    private string TenKhoa => string.IsNullOrEmpty(CurrentUser?.TenKhoa) || CurrentUser?.TenKhoa == "Không thuộc khoa"
        ? ""
        : CurrentUser.TenKhoa;

    protected override async Task OnInitializedAsync()
    {
        await LoadCurrentUserAsync();
       // await LoadKhoasAsync();
    }

    private async Task LoadCurrentUserAsync()
    {
        try
        {
            var response = await AuthApi.GetCurrentUserAsync();

            if (response.Success && response.Data != null)
            {
                CurrentUser = response.Data;
                // Load withdrawal history after user is loaded
                await LoadWithdrawalHistoryAsync();
            }
            else
            {
                Snackbar.Add(response.Message ?? "Không thể tải thông tin người dùng", Severity.Warning);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Lỗi kết nối: {ex.Message}", Severity.Error);
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    // Helper để hiển thị (nếu cần dùng ở nơi khác)
    private string GetVaiTroText() => CurrentUser?.VaiTro switch
    {
        EnumRole.Admin => "Quản trị viên",
        EnumRole.User => "Người dùng thường",
        null => "Không xác định", 
        _ => "Không xác định"
    };


    private string GetTrangThaiText() => BiKhoa ? "Bị khóa" : "Hoạt động";
    private Color GetTrangThaiColor() => BiKhoa ? Color.Error : Color.Success;

    // private async Task LoadKhoasAsync()
    // {
    //     try
    //     {
    //         var response = await KhoaApiClient.GetAllKhoasAsync(); 
    //         if (response.Success && response.Data != null)
    //         {
    //             Khoas = response.Data.ToList();
    //         }
    //     }
    //     catch (Exception ex)
    //     {
    //         Snackbar.Add("Không tải được danh sách khoa: " + ex.Message, Severity.Warning);
    //     }
    // }
    // protected string GetKhoaName(Guid maKhoa)
    // {
    //     var khoa = Khoas.Find(k => k.MaKhoa == maKhoa);
    //     return khoa?.TenKhoa ?? "Không xác định";
    // }

    private async Task OpenChangePasswordDialog()
    {
        var model = new ChangePasswordDialog.ChangePasswordModel();
        var parameters = new DialogParameters<ChangePasswordDialog>
    {
        { x => x.Model, model }
    };

        var options = new DialogOptions
        {
            CloseButton = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
            CloseOnEscapeKey = true
        };

        var dialog = await DialogService.ShowAsync<ChangePasswordDialog>("Đổi mật khẩu", parameters, options);
        var result = await dialog.Result;

        if (result.Canceled == false)
        {
            // Có thể refresh lại user nếu cần
            await LoadCurrentUserAsync();
        }
    }

    private async Task Logout()
    {
        await AuthState.MarkUserAsLoggedOut();
        Nav.NavigateTo("/login", true);
    }

    private async Task LoadWithdrawalHistoryAsync()
    {
        if (CurrentUser == null)
        {
            IsLoadingHistory = false;
            return;
        }

        try
        {
            var response = await YeuCauRutTrichApi.GetByMaNguoiDungAsync(CurrentUser.MaNguoiDung);
            
            if (response.Success && response.Data != null)
            {
                WithdrawalHistory = response.Data;
            }
            else
            {
                WithdrawalHistory = new List<YeuCauRutTrichDto>();
                Snackbar.Add(response.Message ?? "Không thể tải lịch sử rút trích", Severity.Warning);
            }
        }
        catch (Exception ex)
        {
            WithdrawalHistory = new List<YeuCauRutTrichDto>();
            Snackbar.Add($"Lỗi khi tải lịch sử: {ex.Message}", Severity.Error);
        }
        finally
        {
            IsLoadingHistory = false;
            StateHasChanged();
        }
    }

}