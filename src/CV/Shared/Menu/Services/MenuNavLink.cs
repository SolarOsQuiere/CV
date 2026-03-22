namespace CV.Shared.Menu.Services;
/// <summary>
/// Representar items de menu
/// </summary>
/// <param name="text">Texto mostrado en el menu</param>
/// <param name="href">Enlace</param>
/// <param name="isActive">Nodo activo en el menu</param>
public class MenuNavLink(string text, string href,bool isActive=false)
{
    public string Text { get; set; } = text;
    public string Href { get; set; } = href;
    public bool IsActive { get; set; } = isActive;

    public override bool Equals(object? obj)
    {
        if (obj is MenuNavLink other)
        {
            return Text == other.Text && Href == other.Href;
        }
        return false;
    }

}
