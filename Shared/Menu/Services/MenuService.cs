using CV.Shared.Menu.Services;
using CV.Shared.Resources;
using Microsoft.Extensions.Localization;

namespace CV.Shared.Menu;
/// <summary>
/// Servicio para manejar el estado global del menu de la app
/// </summary>
public class MenuService(IStringLocalizer<AllResourcesRes> AllResourcesResolver)
{

    public event Action? OnChange;

    public List<MenuNavLink> MenuItems { get => _menuItems; }
    public readonly List<MenuNavLink> _menuItems =
    [
             new(text: AllResourcesResolver["navbar_inicio"], href: AllResourcesResolver["navbar_inicio"]),
             new(text: AllResourcesResolver["navbar_experiencia"], href: AllResourcesResolver["navbar_experiencia"]),
             new(text: AllResourcesResolver["navbar_educacion"], href: AllResourcesResolver["navbar_educacion"]),
             new(text: AllResourcesResolver["navbar_intereses"], href: AllResourcesResolver["navbar_intereses"]),
             new(text: AllResourcesResolver["navbar_cursos"], href: AllResourcesResolver["navbar_cursos"]),
             new(text: AllResourcesResolver["navbar_tecnologias"], href: AllResourcesResolver["navbar_tecnologias"])
    ];

    public MenuNavLink GetPaginaActual()
        => _menuItems.FirstOrDefault(i => i.IsActive) ?? _menuItems[0];

    public void CambiarPagina(MenuNavLink pagina)
    {
        _menuItems.ForEach(item => item.IsActive = false);
        pagina.IsActive = true;

        NotifyStateChanged();
    }

    public void CambiarPaginaPorRuta(string ruta)
    {
        var item = _menuItems.FirstOrDefault(i => i.Href == ruta);
        if (item is null) return;

        CambiarPagina(item);
    }

    private void NotifyStateChanged()
        => OnChange?.Invoke();
}

