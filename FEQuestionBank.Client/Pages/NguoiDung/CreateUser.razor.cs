using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FEQuestionBank.Client.Pages;

public class CreateUserBase : ComponentBase
{
    [Inject] protected NavigationManager Navigation { get; set; } = default!;
    protected List<BreadcrumbItem> _breadcrumbs = new()
    {
        new BreadcrumbItem("Trang chủ", href: "/"),
        new BreadcrumbItem("Quản lý", href: "#", disabled: true),
        new BreadcrumbItem("Tạo mới người dùng", href: "/user/create-user")
    };

    protected void SelectCreateMethod(string method)
    {
        if (method == "manual")
        {
            Navigation.NavigateTo("/user/create-manual");
        }
        else if (method == "excel")
        {
            Navigation.NavigateTo("/user/upload-excel");
        }
    }
}