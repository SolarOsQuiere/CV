using CV.Shared.Constants;
using CV.Shared.Resources;
using Microsoft.Extensions.Localization;

namespace CV.Pages;

public partial class Education(IStringLocalizer<EducationRes> L, IStringLocalizer<CommonRes> Common)
{
    private record Estudio(string Fecha, string Titulo, string Logo, string Descripcion, bool Destacado = false);

    private List<Estudio> _estudios = [];

    protected override void OnInitialized()
    {
        _estudios =
        [
            new("sept. 2017 - jul. 2022", "Universidad de La Rioja", AssetPaths.Logos.Universidad,
                L["descripcionUniversidad"], Destacado: true),
            new("sept. 2015 - jul. 2017", "IES Valle del Oja", AssetPaths.Logos.Instituto,
                L["descripcionBachiller"]),
            new("ago. 2012 - may. 2016", "Conservatorio de Haro", AssetPaths.Logos.Conservatorio,
                L["descripcionConservatorio"]),
            new("sept. 2011 - jul. 2015", "IES Valle del Oja", AssetPaths.Logos.Instituto,
                L["descripcionEso"])
        ];
    }
}
