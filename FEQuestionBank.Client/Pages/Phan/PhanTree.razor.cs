// File: PhanTreeBase.cs
using BeQuestionBank.Shared.DTOs.Phan;
using FEQuestionBank.Client.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FEQuestionBank.Client.Pages.OtherPage;

namespace FEQuestionBank.Client.Pages.Phan
{
    public partial class PhanTreeBase : ComponentBase
    {
        [Parameter] public Guid MaMonHoc { get; set; }

        [Inject] protected IPhanApiClient PhanApiClient { get; set; } = default!;
        [Inject] protected ISnackbar Snackbar { get; set; } = default!;
        [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
        [Inject] protected IDialogService DialogService { get; set; } = default!;

        protected List<PhanDto> phanList = new();
        protected List<TreeItemData<PhanDto>> TreeItems = new();
        protected string? _searchTerm;
        protected bool loading = true;

        protected override async Task OnInitializedAsync()
        {
            await LoadData();
        }

        protected override async Task OnParametersSetAsync()
        {
            await LoadData();
        }

        private async Task LoadData()
        {
            loading = true;
            StateHasChanged();

            try
            {
                var response = await PhanApiClient.GetTreeByMonHocAsync(MaMonHoc);
                if (response.Success && response.Data != null)
                {
                    phanList = FlattenTree(response.Data);
                    RebuildTree();
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Lỗi tải dữ liệu: {ex.Message}", Severity.Error);
            }
            finally
            {
                loading = false;
                StateHasChanged();
            }
        }

        private List<PhanDto> FlattenTree(List<PhanDto> tree)
        {
            var list = new List<PhanDto>();
            void Traverse(List<PhanDto> nodes)
            {
                foreach (var n in nodes)
                {
                    list.Add(n);
                    if (n.PhanCons?.Any() == true) Traverse(n.PhanCons);
                }
            }
            Traverse(tree);
            return list;
        }

        private void RebuildTree()
        {
            var filtered = string.IsNullOrWhiteSpace(_searchTerm)
                ? phanList
                : phanList.Where(x => x.TenPhan.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase)).ToList();

            var map = filtered.ToDictionary(x => x.MaPhan, x => new TreeItemData<PhanDto>
            {
                Value = x,
                Text = x.TenPhan,
                Icon = GetIconForPhan(x),
                Expandable = false // will be set to true if has children
            });

            var roots = new HashSet<TreeItemData<PhanDto>>();
            foreach (var item in map.Values)
            {
                var value = item.Value;
                if (value == null) continue;
                
                if (value.MaPhanCha == null || value.MaPhanCha == Guid.Empty)
                {
                    roots.Add(item);
                }
                else if (map.TryGetValue(value.MaPhanCha.Value, out var parent))
                {
                    parent.Children ??= new List<TreeItemData<PhanDto>>();
                    parent.Children.Add(item);
                    parent.Expandable = true;
                }
            }

            // Sắp xếp theo ThuTu
            void Sort(TreeItemData<PhanDto> n)
            {
                if (n.Children != null)
                {
                    n.Children = n.Children.OrderBy(c => c.Value!.ThuTu).ToList();
                    foreach (var c in n.Children) Sort(c);
                }
            }
            foreach (var r in roots) Sort(r);

            TreeItems = roots.ToList()  ;
        }

        // Navigation
        protected void NavigateToCardView()
        {
            NavigationManager.NavigateTo($"/monhoc/phan/{MaMonHoc}");
        }

        // Khi click vào node → chuyển tới trang câu hỏi
        protected void OnNodeSelected(PhanDto? phan)
        {
            if (phan != null)
            {
                NavigationManager.NavigateTo($"/monhoc/{MaMonHoc}/cauhoi?phan={phan.MaPhan}");
            }
        }

        protected string GetIconForPhan(PhanDto phan)
        {
            if (phan.PhanCons?.Any() == true)
                return Icons.Material.Filled.Folder;
            return phan.LaCauHoiNhom
                ? Icons.Material.Filled.FolderSpecial
                : Icons.Material.Filled.Description;
        }

        // Các hàm reuse từ trang cũ
        protected async Task OnEdit(PhanDto phan)
        {
            var parameters = new DialogParameters
            {
                ["Phan"] = phan,
                ["DialogTitle"] = "Chỉnh sửa Phần"
            };
            var dialog = DialogService.Show<EditPhanDialog>("Chỉnh sửa", parameters);
            var result = await dialog.Result;
            if (result != null && !result.Canceled && result.Data != null)
            {
                await SavePhanAsync((PhanDto)result.Data);
                await LoadData();
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
                await LoadData();
            }
        }

        protected void OnViewDetail(PhanDto phan)
        {
            DialogService.Show<PhanDetailDialog>("Chi tiết", new DialogParameters { ["Phan"] = phan });
        }

        private async Task SavePhanAsync(PhanDto phan)
        {
            try
            {
                if (phan.MaPhan == Guid.Empty)
                {
                    var create = new CreatePhanDto
                    {
                        TenPhan = phan.TenPhan,
                        NoiDung = phan.NoiDung,
                        MaSoPhan = phan.MaSoPhan,
                        ThuTu = phan.ThuTu,
                        SoLuongCauHoi = phan.SoLuongCauHoi,
                        LaCauHoiNhom = phan.LaCauHoiNhom,
                        MaPhanCha = phan.MaPhanCha,
                        MaMonHoc = phan.MaMonHoc
                    };
                    var res = await PhanApiClient.CreatePhanAsync(create);
                    Snackbar.Add(res.Success ? "Tạo thành công!" : res.Message, res.Success ? Severity.Success : Severity.Error);
                }
                else
                {
                    var update = new UpdatePhanDto
                    {
                        TenPhan = phan.TenPhan,
                        NoiDung = phan.NoiDung,
                        MaSoPhan = phan.MaSoPhan,
                        ThuTu = phan.ThuTu,
                        SoLuongCauHoi = phan.SoLuongCauHoi,
                        LaCauHoiNhom = phan.LaCauHoiNhom,
                        MaPhanCha = phan.MaPhanCha,
                        MaMonHoc = phan.MaMonHoc
                    };
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