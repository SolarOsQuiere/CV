using Blazorise;
using Blazorise.Bootstrap;
using Blazorise.Icons.FontAwesome;
using CV;
using CV.Shared.Services.Menu;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SolarBlazorDocs.Extensions;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
//builder.RootComponents.Add<Inicio>("#preRenderComponent");
//builder.RootComponents.Add<App>("#app");
//builder.RootComponents.Add<HeadOutlet>("head::after");

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services
    .AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddSingleton<MenuService>();


builder.Services
    .AddBlazorise(options =>
    {
        options.Immediate = true;
    })
    .AddBootstrapProviders()
    .AddFontAwesomeIcons();


builder.Services.AddLocalization();

// SolarBlazorDocs (soporte Blazor WebAssembly): renderizar componentes a HTML e imprimir/PDF en cliente.
builder.Services.AddSolarBlazorDocsBrowser();



var app = builder.Build();
await app.SetDefaultCulture();


await app.RunAsync();
