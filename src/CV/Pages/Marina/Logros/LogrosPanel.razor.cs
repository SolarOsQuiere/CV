using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace CV.Pages.Marina.Logros;

public partial class LogrosPanel
{
    private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-ES");

    /// <summary>Coleccion completa, en el orden en que se muestra.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<Logro> Logros { get; set; } = [];

    /// <summary>Instante con el que se decide que logros estan desbloqueados.</summary>
    [Parameter] public DateTimeOffset Ahora { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Se lanza al cerrar el panel (boton, fondo o tecla Escape).</summary>
    [Parameter] public EventCallback OnCerrar { get; set; }

    private ElementReference _panel;

    private int Total => Logros.Count;
    private int Desbloqueados => Logros.Count(l => l.EstaDesbloqueado(Ahora));
    private int Porcentaje => Total == 0 ? 0 : Desbloqueados * 100 / Total;

    /// <summary>Al abrirse toma el foco, para que la tecla Escape cierre el panel.</summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await _panel.FocusAsync();
        }
    }

    private Task Teclado(KeyboardEventArgs e) =>
        e.Key == "Escape" ? OnCerrar.InvokeAsync() : Task.CompletedTask;

    private static string TituloVisible(Logro logro, bool conseguido) =>
        conseguido || !logro.Secreto ? logro.Titulo : "???";

    private static string TextoVisible(Logro logro, bool conseguido) =>
        conseguido ? logro.Descripcion : logro.Pista ?? "Aún por conseguir";

    private static string Fecha(DateTimeOffset fecha) =>
        fecha.ToLocalTime().ToString("d 'de' MMMM 'de' yyyy", Es);
}
