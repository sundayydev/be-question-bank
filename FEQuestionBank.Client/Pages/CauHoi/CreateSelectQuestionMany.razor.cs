using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FEQuestionBank.Client.Pages.CauHoi;

public partial class CreateSelectQuestionManyBase : ComponentBase
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
            case "group":
                Navigation.NavigateTo("/create-question/group");
                break;
            case "essay":
                Navigation.NavigateTo("/create-question/create-essay");
                break;
            case "filling":
                Navigation.NavigateTo("/create-question/create-fill-blank");
                break;
            case "pairing":
                Navigation.NavigateTo("/create-question/create-pairing");
                break;
        }
    }
}