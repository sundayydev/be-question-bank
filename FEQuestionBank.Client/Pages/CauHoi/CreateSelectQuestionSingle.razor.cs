using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FEQuestionBank.Client.Pages.CauHoi;

public partial class CreateSelectQuestionSingleBase : ComponentBase
{
    [Inject] NavigationManager Navigation { get; set; } = default!;

    protected List<BreadcrumbItem> _breadcrumbs = new()
    {
        new BreadcrumbItem("Trang chủ", href: "/"),
        new BreadcrumbItem("Quản lý câu hỏi", href: "#", disabled: true),
        new BreadcrumbItem("Thêm câu hỏi", href: "/question/create-question")
    };

    protected void SelectQuestionType(string type)
    {
        // Điều hướng đến trang tương ứng hoặc xử lý logic khác
        switch (type)
        {
            case "single":
                Navigation.NavigateTo("/create-question/single");
                break;
            case "multipechoice":
                Navigation.NavigateTo("/create-question/create-multiple-choice");
                break;
        }
    }
}