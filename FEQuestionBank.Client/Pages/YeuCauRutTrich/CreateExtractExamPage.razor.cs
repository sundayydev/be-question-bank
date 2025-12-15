using Microsoft.AspNetCore.Components;
using MudBlazor;

public class CreateExtractExamPageBase : ComponentBase
{
    [Inject] NavigationManager Navigation { get; set; } = default!;

    protected List<BreadcrumbItem> _breadcrumbs = new()
    {
        new BreadcrumbItem("Trang chủ", href: "/"),
        new BreadcrumbItem("Công cụ", href: "#", disabled: true),
        new BreadcrumbItem("Rút trích đề thi", href: "/tools/exam-extract")
    };

    protected void SelectQuestionType(string type)
    {
        // Điều hướng đến trang tương ứng hoặc xử lý logic khác
        switch (type)
        {
            case "single":
                Navigation.NavigateTo("/tools/exam-extract/single");
                break;
            case "essay":
                Navigation.NavigateTo("/tools/exam-extract/essay");
                break;
        }
    }
}