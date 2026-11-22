using CV.Shared.Constants;
using CV.Shared.Services.Menu;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CV.Pages;

public partial class Home(NavigationManager Nav, IJSRuntime JS, MenuService MenuService) : IAsyncDisposable
{
    private DotNetObjectReference<Home>? _self;

    private int _indice => Math.Max(0, MenuService.MenuItems.FindIndex(i => i.IsActive));

    /// <summary>Segmento de la ruta (<c>/{section}</c>); nulo en la portada.</summary>
    [Parameter] public string? Section { get; set; }

    private bool _listo;

    protected override void OnInitialized() => MenuService.OnChange += Refrescar;

    private void Refrescar() => InvokeAsync(StateHasChanged);

    private async Task Ir(MenuNavLink item)
    {
        MenuService.CambiarPagina(item);
        await JS.InvokeVoidAsync("cvNav.scrollTo", item.Href);
    }

    private Task Mover(int paso)
    {
        var destino = _indice + paso;
        return destino < 0 || destino >= MenuService.MenuItems.Count
            ? Task.CompletedTask
            : Ir(MenuService.MenuItems[destino]);
    }

    protected override async Task OnParametersSetAsync()
    {
        if (Section is not null && !AppRoutes.TryGetSection(Section, out _))
        {
            Nav.NotFound();
            return;
        }

        // Navegacion dentro de la propia pagina (p. ej. atras/adelante del navegador).
        if (_listo)
        {
            await IrASeccionDeLaRutaAsync();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        _self = DotNetObjectReference.Create(this);
        await JS.InvokeVoidAsync("cvNav.observe", SectionIds.All, _self);

        _listo = true;
        await IrASeccionDeLaRutaAsync();
    }

    private async Task IrASeccionDeLaRutaAsync()
    {
        if (AppRoutes.TryGetSection(Section, out var id))
        {
            await JS.InvokeVoidAsync("cvNav.scrollTo", id);
        }
    }

    [JSInvokable]
    public void SetActive(string id) => MenuService.CambiarPaginaPorRuta(id);

    /// <summary>Navegacion con las flechas del teclado.</summary>
    [JSInvokable]
    public Task Step(int paso) => Mover(paso);

    public async ValueTask DisposeAsync()
    {
        MenuService.OnChange -= Refrescar;
        try { await JS.InvokeVoidAsync("cvNav.unobserve"); } catch (JSDisconnectedException) { }
        _self?.Dispose();
    }
}
