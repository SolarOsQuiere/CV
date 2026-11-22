using Microsoft.AspNetCore.Components;

namespace CV.Shared.Components;

public partial class TimelineItem
{
    [Parameter] public string SrcImagen { get; set; } = "";
    [Parameter] public string AltImagen { get; set; } = "";
    [Parameter] public string Tiempo { get; set; } = "";
    [Parameter] public string Titulo { get; set; } = "";
    [Parameter] public string Subtitulo { get; set; } = "";
    [Parameter] public RenderFragment? Description { get; set; }

    /// <summary>Puesto actual: el punto de la linea se resalta.</summary>
    [Parameter] public bool Current { get; set; }
}
