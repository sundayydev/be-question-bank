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

        protected override async Task OnInitializedAsync()
        {
            var res = await DeThiApiClient.GetByIdWithChiTietAndCauTraLoiAsync(MaDeThi);
            if (res.Success && res.Data != null)
            {
                DeThi = res.Data;
            }
            else
            {
                Snackbar.Add("Không tải được đề thi!", Severity.Error);
                Nav.NavigateTo("/dethi");
            }
        }

        protected void GoBack() => Nav.NavigateTo("/exams");
    }
}