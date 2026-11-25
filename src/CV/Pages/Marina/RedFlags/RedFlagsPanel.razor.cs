using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace CV.Pages.Marina.RedFlags;

public partial class RedFlagsPanel(IJSRuntime JS) : IDisposable
{
    private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-ES");

    /// <summary>Clave de localStorage con la que se recuerda que el buscaminas ya esta resuelto.</summary>
    private const string ClaveResuelto = "marina-redflags-resuelto";

    private const int Columnas = 8;

    /// <summary>Casillas por cada mina: asi el campo crece a medida que se anaden red flags a la coleccion.</summary>
    private const int CasillasPorMina = 7;

    /// <summary>Objetos que caben en cada balda del expediente.</summary>
    private const int PorBalda = 4;

    /// <summary>Lo que se tarda en pulsar una casilla para que cuente como "mantener pulsado" (poner bandera) en pantallas tactiles.</summary>
    private const int PulsacionLargaMs = 420;

    /// <summary>Pausa entre ganar y abrir el expediente, para ver la lluvia de banderas.</summary>
    private const int CelebracionMs = 2600;

    /// <summary>Se lanza al volver (boton, fondo o tecla Escape).</summary>
    [Parameter] public EventCallback OnCerrar { get; set; }

    private readonly CancellationTokenSource _cancelar = new();
    private CancellationTokenSource? _pulsacion;

    private ElementReference _panel;

    /// <summary>Una mina por cada red flag de la coleccion (el campo tiene siempre sitio de sobra).</summary>
    private static int NumMinas => Math.Max(1, Banderas.Count);

    private static int Filas => Math.Max(6, (int)Math.Ceiling(NumMinas * CasillasPorMina / (double)Columnas));

    private Tablero Tablero { get; set; } = new(Filas, Columnas, NumMinas);

    // Expediente (lista de red flags): objeto elegido y hoja de detalle (en movil sube desde abajo)
    private int _seleccionada;
    private bool _fichaAbierta;

    private RedFlag Seleccionada => Banderas[Math.Clamp(_seleccionada, 0, Banderas.Count - 1)];

    private IEnumerable<(RedFlag Flag, int Indice)[]> Baldas =>
        Banderas.Select((flag, indice) => (Flag: flag, Indice: indice)).Chunk(PorBalda);

    private void Elegir(int indice)
    {
        _seleccionada = indice;
        _fichaAbierta = true;
    }

    private void CerrarFicha() => _fichaAbierta = false;

    private void Mover(int paso) =>
        _seleccionada = ((_seleccionada + paso) % Banderas.Count + Banderas.Count) % Banderas.Count;


    private bool _cargando = true;
    private bool _resuelto;
    private bool _celebrando;
    private bool _modoBandera;
    private bool _largaHecha;

    private static IReadOnlyList<RedFlag> Banderas => MarinaRedFlags.Todas;

    private string Contador => Math.Clamp(Tablero.MinasRestantes, -99, 999).ToString("000", CultureInfo.InvariantCulture);

    private string Cara => Tablero.Estado switch
    {
        EstadoJuego.Perdido => "😵",
        EstadoJuego.Ganado => "😎",
        _ => "🙂"
    };

    private string Mensaje => Tablero.Estado switch
    {
        EstadoJuego.Perdido => "¡Boom! Has pisado una red flag. Pulsa la carita y vuelve a intentarlo.",
        EstadoJuego.Ganado => Tablero.Minas == 1 ? "¡Marcada! Abriendo el expediente…" : "¡Todas marcadas! Abriendo el expediente…",
        EstadoJuego.Esperando => "Toca una casilla para empezar: la primera nunca es una mina.",
        _ => "Los números dicen cuántas red flags hay alrededor de esa casilla."
    };

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        await _panel.FocusAsync();

        try
        {
            _resuelto = await JS.InvokeAsync<string?>("localStorage.getItem", ClaveResuelto) == "1";
        }
        catch (Exception)
        {
            // Sin localStorage (modo privado, etc.): se juega cada vez.
        }

        _cargando = false;
        StateHasChanged();
    }

    private static string ClaseCelda(Celda c)
    {
        var clases = new List<string> { "celda" };

        if (c.Abierta)
        {
            clases.Add("is-abierta");
            if (c.Vecinas > 0)
            {
                clases.Add("n-" + c.Vecinas);
            }
        }

        if (c.Bandera)
        {
            clases.Add("is-bandera");
        }

        if (c.Explotada)
        {
            clases.Add("is-explotada");
        }

        return string.Join(' ', clases);
    }

    private string EtiquetaCelda(Celda c) =>
        c.Bandera ? "Casilla con bandera" : c.Abierta ? (c.Vecinas > 0 ? $"{c.Vecinas} red flags alrededor" : "Casilla vacía") : "Casilla sin descubrir";

    /// <summary>Un toque o clic: descubre, o pone/quita bandera si esta activo el modo bandera.</summary>
    private void Pulsar(int fila, int columna)
    {
        _pulsacion?.Cancel();

        if (_largaHecha)
        {
            // La pulsacion larga ya puso la bandera: este clic es su "soltar".
            _largaHecha = false;
            return;
        }

        if (_modoBandera)
        {
            Tablero.AlternarBandera(fila, columna);
        }
        else
        {
            Tablero.Descubrir(fila, columna);
        }

        TrasJugar();
    }

    /// <summary>Boton derecho del raton (o pulsacion larga en Android): bandera.</summary>
    private void ClicDerecho(MouseEventArgs e, int fila, int columna)
    {
        if (e.Button != 2)
        {
            return;
        }

        Tablero.AlternarBandera(fila, columna);
        TrasJugar();
    }

    /// <summary>Al tocar una casilla arranca el temporizador de la pulsacion larga (pone bandera tambien en iOS).</summary>
    private void Abajo(int fila, int columna)
    {
        _largaHecha = false;
        _pulsacion?.Cancel();
        _pulsacion?.Dispose();
        _pulsacion = CancellationTokenSource.CreateLinkedTokenSource(_cancelar.Token);
        _ = PulsacionLargaAsync(fila, columna, _pulsacion.Token);
    }

    private async Task PulsacionLargaAsync(int fila, int columna, CancellationToken token)
    {
        try
        {
            await Task.Delay(PulsacionLargaMs, token);

            _largaHecha = true;
            Tablero.AlternarBandera(fila, columna);
            TrasJugar();
            await InvokeAsync(StateHasChanged);
        }
        catch (OperationCanceledException)
        {
            // Se solto antes: es un toque normal.
        }
    }

    private void Cancelar() => _pulsacion?.Cancel();

    private void TrasJugar()
    {
        if (Tablero.Estado == EstadoJuego.Ganado && !_celebrando)
        {
            _ = CelebrarAsync(_cancelar.Token);
        }
    }

    private async Task CelebrarAsync(CancellationToken token)
    {
        try
        {
            _celebrando = true;
            await InvokeAsync(StateHasChanged);

            await Task.Delay(CelebracionMs, token);

            _resuelto = true;
            _celebrando = false;
            await InvokeAsync(StateHasChanged);

            try
            {
                await JS.InvokeVoidAsync("localStorage.setItem", ClaveResuelto, "1");
            }
            catch (Exception)
            {
                // Sin localStorage: no se recuerda, no pasa nada.
            }
        }
        catch (OperationCanceledException)
        {
            // Se ha vuelto antes de terminar.
        }
    }

    private void Reiniciar()
    {
        Tablero.Reiniciar();
        _celebrando = false;
    }

    private void Rejugar()
    {
        Reiniciar();
        _resuelto = false;
        _fichaAbierta = false;
    }

    private Task Teclado(KeyboardEventArgs e) => e.Key == "Escape" ? OnCerrar.InvokeAsync() : Task.CompletedTask;

    private static string Fecha(DateTimeOffset fecha) => fecha.ToLocalTime().ToString("d 'de' MMMM 'de' yyyy", Es);

    /// <summary>Posicion y tiempos de cada bandera de la lluvia (reparto fijo, sin azar).</summary>
    private static string EstiloLluvia(int i) =>
        $"--x: {N(2 + i * 4.1 % 94)}%; --d: {N(2.3 + i * 0.37 % 1.8)}s; --t: {N(i * 0.11 % 1.1)}s; --s: {N(1.6 + i * 0.53 % 2.2)}rem; --r: {N((i % 2 == 0 ? 1 : -1) * (180 + i * 37 % 300))}deg";

    private static string N(double valor) => valor.ToString("0.##", CultureInfo.InvariantCulture);

    public void Dispose()
    {
        _cancelar.Cancel();
        _cancelar.Dispose();
        _pulsacion?.Dispose();
    }
}
