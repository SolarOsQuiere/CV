namespace CV.Shared.Constants;

/// <summary>
/// Identificadores (atributo <c>id</c>) de cada seccion del CV. Los usan las propias secciones,
/// el menu, el scroll del carrusel y los enlaces profundos.
/// </summary>
public static class SectionIds
{
    public const string Inicio = "inicio";
    public const string SobreMi = "sobremi";
    public const string Experiencia = "experiencia";
    public const string Educacion = "educacion";
    public const string Cursos = "cursos";
    public const string Tecnologias = "tecnologias";
    public const string Proyectos = "proyectos";

    /// <summary>Secciones en el orden en que aparecen en el carrusel y en el menu.</summary>
    public static readonly IReadOnlyList<string> All =
    [
        Inicio, SobreMi, Experiencia, Educacion, Cursos, Tecnologias, Proyectos
    ];
}
