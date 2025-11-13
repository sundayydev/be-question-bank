using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FEQuestionBank.Client.Pages.CauHoi;

public partial class UploadQuestion : ComponentBase
{
    protected List<BreadcrumbItem> _breadcrumbs = new()
    {
        new BreadcrumbItem("Trang chủ", href: "/"),
        new BreadcrumbItem("Quản lý câu hỏi", href: "#", disabled: true),
        new BreadcrumbItem("Thêm câu hỏi", href: "/question/create-question"),
        new BreadcrumbItem("Tải lên câu hỏi", href: "/create-question/single")
    };
}