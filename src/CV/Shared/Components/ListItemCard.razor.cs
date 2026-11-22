using Microsoft.AspNetCore.Components;

namespace CV.Shared.Components;

public partial class ListItemCard
{
    /// <summary>Ruta de la imagen (ver <see cref="CV.Shared.Constants.AssetPaths"/>).</summary>
    [Parameter, EditorRequired] public string Logo { get; set; } = "";
    [Parameter] public string LogoAlt { get; set; } = "";
    [Parameter] public string Fecha { get; set; } = "";
    [Parameter, EditorRequired] public string Titulo { get; set; } = "";

    /// <summary>Ocupa todo el ancho del listado y se resalta.</summary>
    [Parameter] public bool Featured { get; set; }

    /// <summary>Version mas densa (logo y espaciados menores), para listados largos.</summary>
    [Parameter] public bool Compact { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }
}
