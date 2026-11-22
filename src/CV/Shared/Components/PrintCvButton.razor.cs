using System.Globalization;
using Microsoft.AspNetCore.Components;
using SolarBlazorDocs.Browser.Services;

namespace CV.Shared.Components;

public partial class PrintCvButton(IBrowserDocumentPrinter Printer, NavigationManager Nav)
{
    /// <summary>Clase CSS del boton (por defecto el boton flotante).</summary>
    [Parameter] public string CssClass { get; set; } = "cv-print-fab";
    [Parameter] public string Label { get; set; } = "📄 PDF";
    [Parameter] public string BusyLabel { get; set; } = "Generando…";

    private bool _busy;

    private async Task ImprimirCv()
    {
        _busy = true;
        try
        {
            var parametros = new Dictionary<string, object?>
            {
                ["BaseUri"] = Nav.BaseUri,
                ["Language"] = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName
            };

            await Printer.PrintComponentAsync<CvDocument>(parametros);
        }
        finally
        {
            _busy = false;
        }
    }
}
