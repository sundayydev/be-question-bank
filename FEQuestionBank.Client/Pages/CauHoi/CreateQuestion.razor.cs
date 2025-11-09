using Microsoft.AspNetCore.Components;

public class CreateQuestionBase : ComponentBase
{
    [Inject] NavigationManager Navigation { get; set; } = default!;

    protected void SelectQuestionType(string type)
    {
        // Điều hướng đến trang tương ứng hoặc xử lý logic khác
        switch (type)
        {
            case "single":
                Navigation.NavigateTo("/create-question/single");
                break;
            case "group":
                Navigation.NavigateTo("/create-question/group");
                break;
            case "upload":
                Navigation.NavigateTo("/question/upload");
                break;
        }
    }
}