using BeQuestionBank.Shared.DTOs.Common;
using BeQuestionBank.Shared.DTOs.Khoa;
using BeQuestionBank.Shared.DTOs.MonHoc;
using BeQuestionBank.Shared.DTOs.Phan;
using FEQuestionBank.Client.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FEQuestionBank.Client.Pages;

public partial class HomeBase : ComponentBase
{
    [Inject] NavigationManager Navigation { get; set; } = default!;
    
    protected void SelectType(string type)
    {
        // Điều hướng đến trang tương ứng hoặc xử lý logic khác
        switch (type)
        {
            case "question":
                Navigation.NavigateTo("/question/create-question");
                break;
            case "exam":
                Navigation.NavigateTo("/exams");
                break;
            case "upload":
                Navigation.NavigateTo("/question/upload");
                break;
        }
    }
}