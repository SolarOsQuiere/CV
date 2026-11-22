namespace CV.Shared.Constants;

/// <summary>Rutas (relativas al <c>base href</c>) de las imagenes de <c>wwwroot/assets</c>.</summary>
public static class AssetPaths
{
    private const string Img = "assets/img/";
    private const string Icons = "assets/icons/";
    private const string CourseIcons = Icons + "cursos/";

    public const string Profile = Img + "profile.jpg";
    public const string Marina = Img + "marina.webp";

    /// <summary>Logos de empresas y centros de estudio.</summary>
    public static class Logos
    {
        public const string Hiberus = Img + "logoHiberus.png";
        public const string Gnoss = Img + "logoGnoss.png";
        public const string Universidad = Img + "logoUniversidad.png";
        public const string Instituto = Img + "logoInstituto.png";
        public const string Conservatorio = Img + "logoConservatorio.png";
    }

    /// <summary>Logos de las entidades que emiten los cursos.</summary>
    public static class Courses
    {
        public const string Docker = Icons + "docker.png";
        public const string CampusMvp = CourseIcons + "campusmvp.png";
        public const string Pue = CourseIcons + "pue.png";
        public const string LaRioja = CourseIcons + "gobrioja.png";
        public const string AzureFundamentals = CourseIcons + "azurefundamentals.png";
        public const string GoogleActivate = CourseIcons + "googleactivate.png";
    }

    /// <summary>Fotos de la galeria de "Sobre mi", en el orden en que se muestran.</summary>
    public static readonly IReadOnlyList<string> Gallery =
    [
        Img + "personales/d.jpeg",
        Img + "personales/c.png",
        Img + "personales/a.jpeg",
        Img + "personales/b.png",
        Img + "personales/e.jpeg",
        Img + "personales/f.jpeg",
        Img + "personales/g.jpeg",
        Img + "personales/h.jpeg"
    ];
}
