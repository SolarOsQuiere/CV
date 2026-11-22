using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CV.Shared.Components;

public partial class CultureSelector(NavigationManager Nav, IJSRuntime JS)
{
    private static readonly (string Codigo, string Etiqueta)[] Idiomas = [("es", "ES"), ("en", "EN")];

    private static string IdiomaActual =>
        CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "es" ? "es" : "en";

    private void Cambiar(string codigo)
    {
        if (codigo == IdiomaActual) return;

        var js = (IJSInProcessRuntime)JS;
        js.InvokeVoid("blazorCulture.set", codigo);

        Nav.NavigateTo(Nav.Uri, forceLoad: true);
    }
}
