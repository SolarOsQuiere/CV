using Microsoft.AspNetCore.Components;

namespace CV;

public partial class CvDocument
{
    /// <summary>URL base absoluta de la app (para resolver imagenes y CSS relativos).</summary>
    [Parameter] public string BaseUri { get; set; } = "/";

    /// <summary>Idioma del documento (atributo lang).</summary>
    [Parameter] public string Language { get; set; } = "es";
}
