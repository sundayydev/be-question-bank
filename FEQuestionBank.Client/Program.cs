using FEQuestionBank.Client;
using FEQuestionBank.Client.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor;
using MudBlazor.Services; 

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");

// Đăng ký HttpClient
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://localhost:5043/") });

// Đăng ký service
builder.Services.AddScoped<IKhoaApiClient, KhoaApiClient>();
builder.Services.AddScoped<IMonHocApiClient, MonHocApiClient>();

// Đăng ký dịch vụ MudBlazor
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