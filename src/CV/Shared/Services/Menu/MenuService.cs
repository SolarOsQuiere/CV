using CV.Shared.Resources;
using Microsoft.Extensions.Localization;

namespace CV.Shared.Services.Menu;
/// <summary>
/// Servicio para manejar el estado global del menu de la app
/// </summary>
public class MenuService(IStringLocalizer<CommonRes> L)
{

    public event Action? OnChange;

    public List<MenuNavLink> MenuItems { get => _menuItems; }
    public readonly List<MenuNavLink> _menuItems =
    [
             new(text: L["nav_inicio"], href: "inicio", isActive: true),
             new(text: L["nav_sobremi"], href: "sobremi"),
             new(text: L["nav_experiencia"], href: "experiencia"),
             new(text: L["nav_educacion"], href: "educacion"),
             new(text: L["nav_cursos"], href: "cursos"),
             new(text: L["nav_tecnologias"], href: "tecnologias"),
             new(text: L["nav_proyectos"], href: "proyectos")
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

