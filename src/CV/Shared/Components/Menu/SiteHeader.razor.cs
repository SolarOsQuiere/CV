using CV.Shared.Constants;
using CV.Shared.Services.Menu;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CV.Shared.Components.Menu;

public partial class SiteHeader(MenuService MenuService, IJSRuntime JS, NavigationManager Nav) : IDisposable
{
    private bool _abierto;

    protected override void OnInitialized() => MenuService.OnChange += StateHasChanged;

    private async Task Ir(MenuNavLink item)
    {
        _abierto = false;
        MenuService.CambiarPagina(item);

        // Si la pagina actual no tiene carrusel (p. ej. la 404), se navega a la pagina principal en esa seccion.
        var enCarrusel = await JS.InvokeAsync<bool>("cvNav.scrollTo", item.Href);
        if (!enCarrusel)
        {
            Nav.NavigateTo(AppRoutes.RelativeRouteFor(item.Href));
        }
    }

    public void Dispose() => MenuService.OnChange -= StateHasChanged;
}
