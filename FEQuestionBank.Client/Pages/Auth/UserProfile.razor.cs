// UserProfile.razor.cs

using BeQuestionBank.Shared.DTOs.Khoa;
using BEQuestionBank.Shared.DTOs.user;
using BeQuestionBank.Shared.Enums;
using FEQuestionBank.Client.Services;
using FEQuestionBank.Client.Services.Interface;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FEQuestionBank.Client.Pages.Auth;

public partial class UserProfile : ComponentBase
{
    [Inject] private IAuthApiClient AuthApi { get; set; } = default!;
    [Inject] protected IKhoaApiClient KhoaApiClient { get; set; } = default!;
    protected List<KhoaDto> Khoas { get; set; } = new();

    private NguoiDungDto? CurrentUser { get; set; }
    private bool IsLoading { get; set; } = true;

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
}