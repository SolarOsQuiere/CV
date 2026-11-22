using Microsoft.AspNetCore.Components;

namespace CV.Shared.Extensions;

/// <summary>Conversion de cadenas con HTML a contenido que Blazor renderiza sin escapar.</summary>
public static class MarkupExtensions
{
    /// <summary>
    /// Convierte una cadena con HTML en un <see cref="RenderFragment"/>, p. ej. para pasarlo como parametro de un componente.
    /// El contenido no se escapa: usar solo con texto propio (recursos), nunca con datos de usuario.
    /// </summary>
    public static RenderFragment ToRenderFragment(this string content)
        => builder => builder.AddMarkupContent(0, content);

    /// <summary>Convierte una cadena con HTML en un <see cref="MarkupString"/> para pintarla directamente en una vista.</summary>
    public static MarkupString ToMarkupString(this string content) => new(content);
}
