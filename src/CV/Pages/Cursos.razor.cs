using CV.Shared.Constants;
using CV.Shared.Resources;
using Microsoft.Extensions.Localization;

namespace CV.Pages;

public partial class Cursos(IStringLocalizer<CoursesRes> L)
{
    private record Curso(int Anio, string Fecha, string Titulo, string Logo, string Alt,
        string? Subtitulo = null, string? Credencial = null, string? Descripcion = null, string? Url = null);

    private List<Curso> _cursos = [];

    protected override void OnInitialized()
    {
        string T(string clave) => L[clave];

        _cursos =
        [
            new(2026, "feb. 2026", T("AccesibilidadTitulo"), AssetPaths.Logos.Hiberus, "Hiberus",
                Subtitulo: T("AccesibilidadSubtitulo"), Descripcion: T("AccesibilidadDescripcion")),

            new(2025, "dic. 2025", T("Docker2025Titulo"), AssetPaths.Courses.Docker, "Docker",
                Subtitulo: T("Docker2025Subtitulo"), Descripcion: T("Docker2025Descripcion")),
            new(2025, "sept. 2025", T("EntityFrameworkMvpTitulo"), AssetPaths.Courses.CampusMvp, "Campus MVP",
                Subtitulo: T("EntityFrameworkMvpSubtitulo")),

            new(2023, "dic. 2023", T("CssHtmlMVPTitulo"), AssetPaths.Courses.CampusMvp, "Campus MVP",
                Subtitulo: T("CssHtmlMVPSubtitulo"), Descripcion: T("CssHtmlMVPDescripcion")),
            new(2023, "sept. 2023", T("Az204Titulo"), AssetPaths.Courses.Pue, "PUE",
                Subtitulo: T("Az204Subtitulo"), Descripcion: T("Az204Descripcion")),
            new(2023, "jun. 2023", T("tituloReact"), AssetPaths.Courses.LaRioja, "Gobierno de La Rioja",
                Credencial: "A5B5KNFRNNOALQ8", Url: ExternalLinks.Certificates.LaRioja),

            new(2022, "dic. 2022", T("tituloaz900"), AssetPaths.Courses.AzureFundamentals, "Azure Fundamentals",
                Credencial: "994243461", Url: ExternalLinks.Certificates.AzureFundamentals),
            new(2022, "oct. 2022", T("tituloCursoMaui"), AssetPaths.Courses.LaRioja, "Gobierno de La Rioja",
                Credencial: "E5M024D62Y90BZT", Descripcion: T("descripcionCursoMaui"), Url: ExternalLinks.Certificates.LaRioja),
            new(2022, "sept. 2022", T("tituloCursoBlazor"), AssetPaths.Courses.LaRioja, "Gobierno de La Rioja",
                Credencial: "Y83Q73BHZDJY9BF", Descripcion: T("descripcionCursoBlazor"), Url: ExternalLinks.Certificates.LaRioja),
            new(2022, "jul. 2022", T("tituloProgramacionNET"), AssetPaths.Courses.LaRioja, "Gobierno de La Rioja",
                Credencial: "IYYS1J3Y8FGH5J5ID", Descripcion: T("descripcionProgramacionNET"), Url: ExternalLinks.Certificates.LaRioja),

            new(2021, "oct. 2021", "Cloud Computing", AssetPaths.Courses.GoogleActivate, "Google Activate",
                Credencial: "NZQ DVY NBU", Descripcion: T("descripcionCloudComputing"), Url: ExternalLinks.Certificates.GoogleActivate),
            new(2021, "sept. 2021", T("tituloContenedoresDocker"), AssetPaths.Courses.LaRioja, "Gobierno de La Rioja",
                Credencial: "UXGJYIVF8J4JEZX", Descripcion: T("descripcionContenedoresDocker"), Url: ExternalLinks.Certificates.LaRioja),
            new(2021, "sept. 2021", T("tituloIntroduccionDesarrolloWeb"), AssetPaths.Courses.GoogleActivate, "Google Activate",
                Credencial: "AAU 9KS DU9 & R3D SRM CTN", Descripcion: T("descripcionIntroduccionDesarrolloWeb"), Url: ExternalLinks.Certificates.GoogleActivate)
        ];
    }
}
