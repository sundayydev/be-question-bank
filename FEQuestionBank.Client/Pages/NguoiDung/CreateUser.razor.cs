using Microsoft.AspNetCore.Components;

namespace FEQuestionBank.Client.Pages;

public class CreateUserBase : ComponentBase
{
    [Inject] protected NavigationManager Navigation { get; set; } = default!;

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