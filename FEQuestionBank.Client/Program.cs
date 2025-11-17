using Blazored.LocalStorage;
using FEQuestionBank.Client;
using FEQuestionBank.Client.Implementation;
using FEQuestionBank.Client.Services;
using FEQuestionBank.Client.Services.Implementation;
using FEQuestionBank.Client.Services.Interface;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");

// Đăng ký HttpClient
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://localhost:5043/") });

// THÊM: Blazored.LocalStorage
builder.Services.AddBlazoredLocalStorage();

// Đăng ký các ApiClient
builder.Services.AddScoped<IKhoaApiClient, KhoaApiClient>();
builder.Services.AddScoped<IMonHocApiClient, MonHocApiClient>();
builder.Services.AddScoped<IPhanApiClient, PhanApiClient>();
builder.Services.AddScoped<INguoiDungApiClient, NguoiDungApiClient>();
builder.Services.AddScoped<IDeThiApiClient,DeThiApiClient>();
builder.Services.AddScoped<IYeuCauRutTrichApiClient,YeuCauRutTrichApiClient>();

// AuthApiClient (dùng HttpClient đơn giản, không TokenHandler)
builder.Services.AddScoped<IAuthApiClient, AuthApiClient>();

// Auth State: Sử dụng CustomAuthStateProvider (như cũ)
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddAuthorizationCore();

// MudBlazor
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