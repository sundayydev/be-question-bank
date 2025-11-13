using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using BeQuestionBank.Shared.DTOs.DeThi;
using BEQuestionBank.Shared.DTOs.DeThi;
using FEQuestionBank.Client.Services;

namespace FEQuestionBank.Client.Pages.DeThi
{
    public class DeThiDetailBase : ComponentBase
    {
        [Parameter] public Guid MaDeThi { get; set; }
        protected DeThiWithChiTietAndCauTraLoiDto? DeThi;

        [Inject] protected IDeThiApiClient DeThiApiClient { get; set; } = default!;
        [Inject] protected NavigationManager Nav { get; set; } = default!;
        [Inject] protected ISnackbar Snackbar { get; set; } = default!;
        protected List<BreadcrumbItem> _breadcrumbs = new();

        protected override async Task OnInitializedAsync()
        {
            var res = await DeThiApiClient.GetByIdWithChiTietAndCauTraLoiAsync(MaDeThi);
            if (res.Success && res.Data != null)
            {
                DeThi = res.Data;
                UpdateBreadcrumbs();
            }
            else
            {
                Snackbar.Add("Không tải được đề thi!", Severity.Error);
                Nav.NavigateTo("/exams");
            }
        }

        protected void GoBack() => Nav.NavigateTo("/exams");
        private void UpdateBreadcrumbs()
        {
            _breadcrumbs.Clear();
            _breadcrumbs.Add(new BreadcrumbItem("Trang chủ", href: "/"));
            _breadcrumbs.Add(new BreadcrumbItem("Danh sách đề thi", href: "/exams"));
            _breadcrumbs.Add(new BreadcrumbItem(DeThi?.TenDeThi ?? "Chi tiết đề thi", null,disabled: true));
        }
    }
}