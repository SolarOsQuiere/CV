namespace CV.Shared.Constants;

/// <summary>
/// Rutas de la aplicacion. Son relativas al <c>base href</c> (en produccion <c>/CV/</c>, en local <c>/</c>),
/// igual que las plantillas de <c>@page</c>/<c>[Route]</c>, asi que no llevan prefijo.
/// </summary>
public static class AppRoutes
{
    public const string Home = "/";

    /// <summary>
    /// Ruta de una seccion (<c>/Cursos</c>, <c>/Courses</c>...). El nombre del parametro debe coincidir
    /// con la propiedad <c>Section</c> del componente que la recibe.
    /// </summary>
    public const string Section = "/{section}";

    public const string NotFound = "/NotFound";


    /// <summary>
    /// Segmentos aceptados por <see cref="Section"/> y la seccion a la que llevan. El primero de cada
    /// seccion es el canonico (el que usa la propia web); el resto son alias, en ingles y heredados.
    /// </summary>
    private static readonly (string SectionId, string[] Segments)[] Aliases =
    [
        (SectionIds.Inicio,      ["Inicio", "Index"]),
        (SectionIds.SobreMi,     ["SobreMi", "AboutMe", "Proyectos", "Projects"]),
        (SectionIds.Experiencia, ["Experiencia", "Experience"]),
        (SectionIds.Educacion,   ["Educacion", "Education"]),
        (SectionIds.Cursos,      ["Cursos", "Courses"]),
        (SectionIds.Tecnologias, ["Tecnologias", "Technologies"]),
    ];

    private static readonly Dictionary<string, string> SectionBySegment = Aliases
        .SelectMany(a => a.Segments.Select(s => (Segment: s, a.SectionId)))
        .ToDictionary(x => x.Segment, x => x.SectionId, StringComparer.OrdinalIgnoreCase);

    /// <summary>Id de la seccion a la que lleva un segmento de ruta, o <c>false</c> si no existe.</summary>
    public static bool TryGetSection(string? segment, out string sectionId)
    {
        sectionId = string.Empty;
        return segment is not null && SectionBySegment.TryGetValue(segment, out sectionId!);
    }

    /// <summary>
    /// Ruta relativa al <c>base href</c> (sin barra inicial) que abre la pagina principal en una seccion.
    /// Cadena vacia para la portada.
    /// </summary>
    public static string RelativeRouteFor(string sectionId)
    {
        if (sectionId == SectionIds.Inicio) return string.Empty;

        var match = Aliases.FirstOrDefault(a => a.SectionId == sectionId);
        return match.Segments?[0] ?? string.Empty;
    }
}
