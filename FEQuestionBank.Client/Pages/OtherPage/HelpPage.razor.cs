using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FEQuestionBank.Client.Pages.OtherPage
{
    public partial class HelpPageBase : ComponentBase
    {
        // Inject NavigationManager
        [Inject]
        protected NavigationManager Navigation { get; set; } = default!;

        protected int _activeTabIndex = 0;

        protected readonly List<BreadcrumbItem> _breadcrumbs = new()
        {
            new BreadcrumbItem("Trang chủ", href: "/"),
            new BreadcrumbItem("Trợ giúp", href: "/help", disabled: true)
        };

        protected MudTheme _theme = new MudTheme()
        {
            PaletteLight = new PaletteLight()
            {
                Primary = Colors.Blue.Default,
                Secondary = Colors.Gray.Darken1,
                AppbarBackground = Colors.Blue.Darken1,
            },
            PaletteDark = new PaletteDark()
            {
                Primary = Colors.Blue.Lighten1,
            }
        };

        protected void OpenSupportDialog()
        {
            // Dùng instance đã inject
            Navigation.NavigateTo("/support/ticket/new");
        }
    }
}