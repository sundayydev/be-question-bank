using Blazored.LocalStorage;
using FEQuestionBank.Client;
using FEQuestionBank.Client.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// 1. CHỈ 1 HttpClient DUY NHẤT
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("http://localhost:5043/")
});

// 2. Tất cả ApiClient dùng chung HttpClient
builder.Services.AddScoped<IKhoaApiClient, KhoaApiClient>();
builder.Services.AddScoped<IMonHocApiClient, MonHocApiClient>();
builder.Services.AddScoped<IPhanApiClient, PhanApiClient>();
builder.Services.AddScoped<INguoiDungApiClient, NguoiDungApiClient>();
builder.Services.AddScoped<IDeThiApiClient,DeThiApiClient>();


// 3. AuthService: Tự thêm Bearer
builder.Services.AddBlazoredLocalStorage();

// 4. MudBlazor
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.TopRight;
    config.SnackbarConfiguration.PreventDuplicates = true;
    config.SnackbarConfiguration.NewestOnTop = true;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 10000;
    config.SnackbarConfiguration.HideTransitionDuration = 500;
    config.SnackbarConfiguration.ShowTransitionDuration = 500;
    config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
});

await builder.Build().RunAsync();