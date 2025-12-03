using BeQuestionBank.Shared.DTOs.Common;
using BeQuestionBank.Shared.DTOs.Phan;
using FEQuestionBank.Client.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using FEQuestionBank.Client.Pages.OtherPage;
using FEQuestionBank.Client.Pages.Phan;

namespace FEQuestionBank.Client.Pages
{
   public partial class PhanBase : ComponentBase
{

    [Parameter] public Guid MaMonHoc { get; set; } = Guid.Empty;
    [Inject] protected IPhanApiClient PhanApiClient { get; set; } = default!;
    [Inject] protected ISnackbar Snackbar { get; set; } = default!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
    [Inject] protected IDialogService DialogService { get; set; } = default!;

    protected List<PhanDto> phanList = new();
    protected string? _searchTerm;
    protected bool loading = true;
        

    protected override async Task OnInitializedAsync()
    {
        await LoadPhanList();
    }

        //private async Task LoadPhanList()
        //{
        //    loading = true;
        //    StateHasChanged();

        //    try
        //    {
        //        var response = await PhanApiClient.GetTreeAsync(); // Backend trả cây
        //        if (response.Success && response.Data != null)
        //        {
        //            phanList = FlattenTree(response.Data); // Làm phẳng
        //            phanList = ApplySearch(phanList);
        //        }
        //        else
        //        {
        //            Snackbar.Add("Không có dữ liệu.", Severity.Info);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Snackbar.Add($"Lỗi: {ex.Message}", Severity.Error);
        //    }
        //    finally
        //    {
        //        loading = false;
        //        StateHasChanged();
        //    }
        //}


        private async Task LoadPhanList()
        {
            loading = true;
            try
            {
                var response = await PhanApiClient.GetPhanByMonHocAsync(MaMonHoc);
                var filteredData = response.Data.Where(x => x.XoaTam == false).ToList();


                if (response.Success && response.Data != null)
                {
                    // ⭐ Chỉ lấy phần cha (root)
                    phanList = filteredData
                        .Select(x => new PhanDto
                        {
                            MaPhan = x.MaPhan,
                            MaMonHoc = x.MaMonHoc,
                            TenPhan = x.TenPhan,
                            NoiDung = x.NoiDung,
                            ThuTu = x.ThuTu,
                            SoLuongCauHoi = x.SoLuongCauHoi,
                            XoaTam = x.XoaTam,
                            LaCauHoiNhom = x.LaCauHoiNhom,
                            NgayTao = x.NgayTao,
                            NgayCapNhat = x.NgayCapNhat,
                            // ⭐ KHÔNG lấy phần con
                            PhanCons = null,
                            MaPhanCha = x.MaPhanCha
                        })
                        .ToList();
                    phanList = ApplySearch(phanList);
                }
                else
                    StateHasChanged();
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Lỗi: {ex.Message}", Severity.Error);
            }
            finally
            {
                loading = false;
                StateHasChanged();
            }
        }
        


    // Làm phẳng cây → danh sách
    private List<PhanDto> FlattenTree(List<PhanDto> tree)
    {
        var result = new List<PhanDto>();
        foreach (var node in tree)
        {
            result.Add(node);
            if (node.PhanCons?.Any() == true)
            {
                result.AddRange(FlattenTree(node.PhanCons));
            }
        }
        return result;
    }
        protected void ViewTree(PhanDto phan)
        {
            NavigationManager.NavigateTo($"/phancon/{phan.MaPhan}");
        }
        // Tìm kiếm
        private List<PhanDto> ApplySearch(List<PhanDto> list)
    {
        if (string.IsNullOrWhiteSpace(_searchTerm))
            return list;
        return list
            .Where(x => x.TenPhan.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    protected async Task OnSearch()
    {
        await LoadPhanList();
    }

    protected async Task OnCreateNew()
    {
        var parameters = new DialogParameters
        {
            ["Phan"] = new PhanDto(),
            ["DialogTitle"] = "Tạo mới Phần"
        };
        var dialog = DialogService.Show<EditPhanDialog>("Tạo mới", parameters);
        var result = await dialog.Result;
        if (!result.Canceled)
        {
            await SavePhanAsync((PhanDto)result.Data);
            await LoadPhanList();
        }
    }

    protected async Task OnEdit(PhanDto phan)
    {
        var parameters = new DialogParameters
        {
            ["Phan"] = phan,
            ["DialogTitle"] = "Chỉnh sửa Phần"
        };
        var dialog = DialogService.Show<EditPhanDialog>("Chỉnh sửa", parameters);
        var result = await dialog.Result;
        if (!result.Canceled)
        {
            await SavePhanAsync((PhanDto)result.Data);
            await LoadPhanList();
        }
    }

    protected async Task OnConfirmDelete(PhanDto phan)
    {
        var confirm = await DialogService.ShowMessageBox(
            "Xác nhận xóa",
            $"Bạn có chắc chắn muốn xóa phần \"{phan.TenPhan}\"?",
            yesText: "Xóa", cancelText: "Hủy");

        if (confirm == true)
        {
            await DeletePhanAsync(phan.MaPhan);
            await LoadPhanList();
        }
    }

    protected void OnViewDetail(PhanDto phan)
    {
        DialogService.Show<PhanDetailDialog>("Chi tiết", new DialogParameters { ["Phan"] = phan });
    }

    protected void OnViewSubjects(PhanDto phan)
    {
        if (phan.MaMonHoc != Guid.Empty)
            NavigationManager.NavigateTo($"/monhoc/{phan.MaMonHoc}/cauhoi?phan={phan.MaPhan}");
    }

    private async Task SavePhanAsync(PhanDto phan)
    {
        try
        {
            if (phan.MaPhan == Guid.Empty)
            {
                var create = new CreatePhanDto { TenPhan = phan.TenPhan,
            NoiDung = phan.NoiDung,
            MaSoPhan = phan.MaSoPhan,
            ThuTu = phan.ThuTu,
            SoLuongCauHoi = phan.SoLuongCauHoi,
            LaCauHoiNhom = phan.LaCauHoiNhom,
            MaPhanCha = phan.MaPhanCha,
            MaMonHoc = phan.MaMonHoc };
                var res = await PhanApiClient.CreatePhanAsync(create);
                Snackbar.Add(res.Success ? "Tạo thành công!" : res.Message, res.Success ? Severity.Success : Severity.Error);
            }
            else
            {
                var update = new UpdatePhanDto { TenPhan = phan.TenPhan,
                    NoiDung = phan.NoiDung,
                    MaSoPhan = phan.MaSoPhan,
                    ThuTu = phan.ThuTu,
                    SoLuongCauHoi = phan.SoLuongCauHoi,
                    LaCauHoiNhom = phan.LaCauHoiNhom,
                    MaPhanCha = phan.MaPhanCha,
                    MaMonHoc = phan.MaMonHoc };
                var res = await PhanApiClient.UpdatePhanAsync(phan.MaPhan, update);
                Snackbar.Add(res.Success ? "Cập nhật thành công!" : res.Message, res.Success ? Severity.Success : Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Lỗi: {ex.Message}", Severity.Error);
        }
    }

    private async Task DeletePhanAsync(Guid id)
    {
        var res = await PhanApiClient.DeletePhanAsync(id);
        Snackbar.Add(res.Success ? "Xóa thành công!" : res.Message, res.Success ? Severity.Success : Severity.Error);
    }
}
}