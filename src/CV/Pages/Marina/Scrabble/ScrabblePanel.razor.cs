using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace CV.Pages.Marina.Scrabble;

public partial class ScrabblePanel(IJSRuntime JS) : IAsyncDisposable
{
    /// <summary>Letras y numeros sin las que se confunden (0/O, 1/I): los codigos de sala se dictan o se escriben a mano.</summary>
    private const string AlfabetoCodigo = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private const int LongitudCodigo = 5;

    /// <summary>Se lanza al volver (boton, fondo o tecla Escape).</summary>
    [Parameter] public EventCallback OnCerrar { get; set; }

    /// <summary>Codigo de sala que viene en el enlace de invitacion (<c>?sala=ABC12</c>): se deja escrito en "Entrar con código".</summary>
    [Parameter] public string? CodigoInicial { get; set; }

    [Inject] private NavigationManager Navegacion { get; set; } = default!;

    /// <summary>Lo que se espera a que el anfitrion conteste tras conectar antes de dar la sala por perdida.</summary>
    private static readonly TimeSpan EsperaAnfitrion = TimeSpan.FromSeconds(10);

    private readonly ScrabbleSesion _sesion = new(new JsGameTransport(JS, "./assets/marina/scrabble-mqtt.js"));
    private ElementReference _panel;
    private ElementReference _campoNombre;
    private bool _enlaceCopiado;
    private bool _eliminado;

    /// <summary>Se ha llegado por un enlace de invitacion.</summary>
    private bool Invitado => !string.IsNullOrWhiteSpace(CodigoInicial);

    private string _nombre = "";
    private string _codigo = "";
    private bool _conectando;
    private string? _error;

    private EstadoPartida? _estado;
    private readonly List<MensajeChat> _chat = [];
    private string _textoChat = "";

    // Jugada a medias: fichas del atril puestas en el tablero (casilla -> posicion en el atril) y ficha elegida
    private readonly Dictionary<(int Fila, int Columna), int> _pendientes = [];
    private int? _seleccion;
    private bool _modoCambio;
    private readonly HashSet<int> _cambio = [];

    private bool EsMiTurno => _estado is { Fase: FasePartida.Jugando } e && e.Turno == e.Yo;

    private string MiNombre => _estado is { } e && e.Yo < e.Jugadores.Count ? e.Jugadores[e.Yo].Nombre : "";

    private string Mensaje => _estado switch
    {
        null => "",
        { Fase: FasePartida.Esperando } => $"Esperando al otro jugador. Pásale el código {_estado.Sala}.",
        { Fase: FasePartida.Terminada } => _estado.Aviso ?? "Partida terminada.",
        _ when EsMiTurno => "Tu turno. " + (_estado.Aviso ?? ""),
        _ => _estado.Aviso ?? ""
    };

    /// <summary>Enlace para pasarle a la otra persona: abre la pagina con el codigo ya puesto.</summary>
    private string EnlaceInvitacion => Navegacion.ToAbsoluteUri($"{MarinaConfig.Ruta.TrimStart('/')}?sala={Uri.EscapeDataString(_estado?.Sala ?? "")}").ToString();

    private async Task CopiarEnlaceAsync()
    {
        try
        {
            await JS.InvokeVoidAsync("navigator.clipboard.writeText", EnlaceInvitacion);
            _enlaceCopiado = true;
            StateHasChanged();
            await Task.Delay(2000);
            _enlaceCopiado = false;
        }
        catch (Exception)
        {
            // Sin permiso para el portapapeles: el enlace sigue visible para copiarlo a mano.
        }
    }

    protected override void OnInitialized()
    {
        _codigo = (CodigoInicial ?? "").Trim().ToUpperInvariant();
        _sesion.EstadoRecibido += e => _ = InvokeAsync(() => AlEstado(e));
        _sesion.ChatRecibido += m => _ = InvokeAsync(() => AlChat(m));
        _sesion.ErrorRecibido += texto => _ = InvokeAsync(() => { _error = texto; StateHasChanged(); });
        _sesion.Cerrada += motivo => _ = InvokeAsync(() => AlCerrar(motivo));
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Con enlace de invitacion se va directo al campo del nombre.
            await (Invitado && _campoNombre.Context is not null ? _campoNombre.FocusAsync() : _panel.FocusAsync());
        }
    }

    // ---------- Entrar ----------

    private Task CrearSala() => EntrarAsync(crear: true);

    private Task EntrarSala() => EntrarAsync(crear: false);

    private Task TeclaCodigo(KeyboardEventArgs e) => e.Key == "Enter" ? EntrarSala() : Task.CompletedTask;

    private async Task EntrarAsync(bool crear)
    {
        if (_conectando)
        {
            return;
        }

        var nombre = _nombre.Trim();
        var codigo = crear ? GenerarCodigo() : _codigo.Trim().ToUpperInvariant();

        if (nombre.Length == 0)
        {
            _error = "Escribe tu nombre.";
            return;
        }

        if (!crear && (codigo.Length < 3 || !codigo.All(char.IsLetterOrDigit)))
        {
            _error = "Escribe el código de la sala (3 a 12 letras o números).";
            return;
        }

        _error = null;
        _conectando = true;
        _codigo = codigo;

        _error = crear
            ? await _sesion.CrearAsync(codigo, nombre)
            : await _sesion.UnirseAsync(codigo, nombre);

        _conectando = false;

        if (!crear && _error is null)
        {
            _ = EsperarAnfitrionAsync();
        }
    }

    /// <summary>
    /// Conectar con el broker no basta: el anfitrion tiene que aceptar y mandar el estado. Si en unos segundos no llega nada
    /// se avisa en vez de dejar la pantalla de entrada como si no hubiera pasado nada.
    /// </summary>
    private async Task EsperarAnfitrionAsync()
    {
        await Task.Delay(EsperaAnfitrion);

        if (_eliminado || _estado is not null)
        {
            return;
        }

        await _sesion.CerrarAsync();
        _error = "Te has conectado, pero el anfitrión no ha respondido. Comprueba que sigue en la sala (con la página abierta) y vuelve a intentarlo.";
        await InvokeAsync(StateHasChanged);
    }

    private Task TeclaNombre(KeyboardEventArgs e) =>
        e.Key == "Enter" && _codigo.Length > 0 ? EntrarSala() : Task.CompletedTask;

    private static string GenerarCodigo() =>
        new(Enumerable.Range(0, LongitudCodigo).Select(_ => AlfabetoCodigo[Random.Shared.Next(AlfabetoCodigo.Length)]).ToArray());

    // ---------- Lo que llega de la red ----------

    private void AlEstado(EstadoPartida nuevo)
    {
        var cambio = _estado is null
            || nuevo.Atril != _estado.Atril
            || nuevo.Turno != _estado.Turno
            || nuevo.Fase != _estado.Fase
            || !nuevo.Tablero.SequenceEqual(_estado.Tablero);

        _estado = nuevo;
        _error = null;

        if (cambio)
        {
            LimpiarJugada();
        }

        StateHasChanged();
    }

    private void AlChat(MensajeChat mensaje)
    {
        _chat.Add(mensaje);
        StateHasChanged();
    }

    /// <summary>Se acabo la sesion: se vuelve a la entrada con el motivo.</summary>
    private void AlCerrar(string motivo)
    {
        _estado = null;
        _chat.Clear();
        LimpiarJugada();
        _conectando = false;
        _error = motivo;
        StateHasChanged();
    }

    // ---------- Tablero y atril ----------

    private void LimpiarJugada()
    {
        _pendientes.Clear();
        _seleccion = null;
        _modoCambio = false;
        _cambio.Clear();
    }

    private void PulsarFicha(int posicion)
    {
        if (_modoCambio)
        {
            if (!_cambio.Remove(posicion))
            {
                _cambio.Add(posicion);
            }

            return;
        }

        if (EsMiTurno)
        {
            _seleccion = _seleccion == posicion ? null : posicion;
        }
    }

    private void PulsarCasilla(int fila, int columna)
    {
        if (!EsMiTurno || _modoCambio || _estado!.Tablero[fila][columna] != Reglas.Vacia)
        {
            return;
        }

        if (_pendientes.Remove((fila, columna)))
        {
            // Volver a coger una ficha ya colocada.
            return;
        }

        if (_seleccion is { } posicion)
        {
            _pendientes[(fila, columna)] = posicion;
            _seleccion = null;
            _error = null;
        }
    }

    private void Recoger()
    {
        _pendientes.Clear();
        _seleccion = null;
    }

    private void EmpezarCambio()
    {
        Recoger();
        _modoCambio = true;
    }

    private void CancelarCambio()
    {
        _modoCambio = false;
        _cambio.Clear();
    }

    // ---------- Acciones ----------

    private Task JugarAsync()
    {
        var fichas = _pendientes
            .Select(p => new Colocacion(p.Key.Fila, p.Key.Columna, _estado!.Atril[p.Value]))
            .ToList();
        return _sesion.JugarAsync(fichas);
    }

    private Task PasarAsync() => _sesion.PasarAsync();

    private Task ConfirmarCambio() =>
        _sesion.CambiarAsync(string.Concat(_cambio.Order().Select(i => _estado!.Atril[i])));

    private Task NuevaPartidaAsync() => _sesion.NuevaPartidaAsync();

    private async Task EnviarChatAsync()
    {
        var texto = _textoChat.Trim();
        _textoChat = "";

        if (texto.Length > 0)
        {
            await _sesion.EnviarChatAsync(texto);
        }
    }

    private Task Teclado(KeyboardEventArgs e) => e.Key == "Escape" ? OnCerrar.InvokeAsync() : Task.CompletedTask;

    // ---------- Pintado de casillas ----------

    private static string ClaseCasilla(int fila, int columna, char fija, char? pendiente)
    {
        var clases = "casilla";

        if (fija != Reglas.Vacia)
        {
            return clases + " is-fija";
        }

        if (pendiente is not null)
        {
            return clases + " is-pendiente";
        }

        return Reglas.Premio(fila, columna) switch
        {
            'T' => clases + " prem-T",
            'D' => clases + " prem-D",
            't' => clases + " prem-t",
            'd' => clases + " prem-d",
            _ => clases
        };
    }

    private static string TextoPremio(int fila, int columna) =>
        fila == Reglas.Tamano / 2 && columna == Reglas.Tamano / 2
            ? "★"
            : Reglas.Premio(fila, columna) switch
            {
                'T' => "3P",
                'D' => "2P",
                't' => "3L",
                'd' => "2L",
                _ => ""
            };

    private static string EtiquetaCasilla(int fila, int columna, char fija, char? pendiente)
    {
        var posicion = $"fila {fila + 1}, columna {columna + 1}";
        if (fija != Reglas.Vacia)
        {
            return $"{fija}, {posicion}";
        }

        return pendiente is { } p ? $"{p} sin confirmar, {posicion}" : $"Casilla vacía, {posicion}";
    }

    public async ValueTask DisposeAsync()
    {
        _eliminado = true;
        await _sesion.DisposeAsync();
    }
}
