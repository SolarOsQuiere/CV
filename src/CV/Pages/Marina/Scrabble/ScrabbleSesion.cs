using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.JSInterop;

namespace CV.Pages.Marina.Scrabble;

/// <summary>
/// Una sesion de juego entre dos navegadores, sin servidor propio. El canal lo pone <c>scrabble-red.js</c> (WebRTC con PeerJS).
/// <para>
/// El que crea la sala es el <b>anfitrion</b>: la partida (<see cref="SalaPartida"/>) vive en su navegador y es la autoridad.
/// El que entra con el codigo es el <b>invitado</b>: solo envia sus acciones y recibe el estado y el chat.
/// Para el panel los dos son iguales: mismos metodos y mismos eventos.
/// </para>
/// Los eventos pueden dispararse en medio de un clic: quien los escuche debe pasar por <c>InvokeAsync</c>.
/// </summary>
public sealed class ScrabbleSesion(IJSRuntime js) : IAsyncDisposable
{
    private const string Anfitrion = "anfitrion";
    private const string Invitado = "invitado";
    private const int MaxNombre = 20;
    private const int MaxMensaje = 300;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private IJSObjectReference? _modulo;
    private DotNetObjectReference<ScrabbleSesion>? _referencia;
    private SalaPartida? _sala;

    /// <summary>Llega la foto de la partida (la propia, con su atril).</summary>
    public event Action<EstadoPartida>? EstadoRecibido;

    public event Action<MensajeChat>? ChatRecibido;

    /// <summary>Una accion no se ha podido hacer (no es su turno, jugada invalida...).</summary>
    public event Action<string>? ErrorRecibido;

    /// <summary>La sesion se ha acabado (se ha ido el otro, la sala estaba llena...). Trae el motivo.</summary>
    public event Action<string>? Cerrada;

    public bool EsAnfitrion => _sala is not null;

    // ---------- Entrar ----------

    /// <summary>Abre la sala con ese codigo y se queda esperando al otro jugador. Devuelve el error, o null.</summary>
    public async Task<string?> CrearAsync(string codigo, string nombre)
    {
        await PrepararAsync();

        _sala = new SalaPartida(codigo);
        _sala.Unir(Anfitrion, nombre);

        var error = await _modulo!.InvokeAsync<string?>("crear", _referencia, codigo);
        if (error is not null)
        {
            _sala = null;
            return error;
        }

        await EmitirChatAsync(_sala.AnadirChat("", $"Sala {codigo} abierta. Pasa el código a la otra persona.", esSistema: true));
        await DifundirAsync();
        return null;
    }

    /// <summary>Se conecta a la sala de ese codigo. Si el anfitrion no lo acepta (nombre repetido...), llega por <see cref="Cerrada"/>.</summary>
    public async Task<string?> UnirseAsync(string codigo, string nombre)
    {
        await PrepararAsync();
        _sala = null;

        var error = await _modulo!.InvokeAsync<string?>("unirse", _referencia, codigo);
        if (error is not null)
        {
            return error;
        }

        await EnviarAsync(new Paquete(Tipos.Hola, Nombre: nombre));
        return null;
    }

    private async Task PrepararAsync()
    {
        _referencia ??= DotNetObjectReference.Create(this);
        _modulo ??= await js.InvokeAsync<IJSObjectReference>("import", "./assets/marina/scrabble-red.js");
    }

    // ---------- Acciones (iguales para anfitrion e invitado) ----------

    public Task JugarAsync(List<Colocacion> fichas) => AccionAsync(new Paquete(Tipos.Jugar, Fichas: fichas));

    public Task PasarAsync() => AccionAsync(new Paquete(Tipos.Pasar));

    public Task CambiarAsync(string letras) => AccionAsync(new Paquete(Tipos.Cambiar, Texto: letras));

    public Task NuevaPartidaAsync() => AccionAsync(new Paquete(Tipos.Nueva));

    public Task EnviarChatAsync(string texto) => AccionAsync(new Paquete(Tipos.Chat, Texto: texto));

    private Task AccionAsync(Paquete paquete) =>
        _sala is null ? EnviarAsync(paquete) : ProcesarAsync(Anfitrion, paquete);

    // ---------- Anfitrion: aplica las acciones de los dos y reparte el resultado ----------

    private async Task ProcesarAsync(string conexion, Paquete paquete)
    {
        var sala = _sala!;
        string? error;

        switch (paquete.Tipo)
        {
            case Tipos.Jugar: error = sala.Jugar(conexion, paquete.Fichas ?? []); break;
            case Tipos.Pasar: error = sala.Pasar(conexion); break;
            case Tipos.Cambiar: error = sala.Cambiar(conexion, paquete.Texto ?? ""); break;
            case Tipos.Nueva: error = sala.NuevaPartida(conexion); break;
            case Tipos.Chat:
                await ChatDeAsync(conexion, paquete.Texto);
                return;
            default:
                return;
        }

        if (error is null)
        {
            await DifundirAsync();
        }
        else if (conexion == Anfitrion)
        {
            ErrorRecibido?.Invoke(error);
        }
        else
        {
            await EnviarAsync(new Paquete(Tipos.Error, Texto: error));
        }
    }

    private async Task RecibirInvitadoAsync(Paquete paquete)
    {
        var sala = _sala!;

        if (paquete.Tipo != Tipos.Hola)
        {
            await ProcesarAsync(Invitado, paquete);
            return;
        }

        var nombre = (paquete.Nombre ?? "").Trim();
        var error = nombre.Length is 0 or > MaxNombre
            ? "Nombre no válido."
            : sala.Unir(Invitado, nombre);

        if (error is not null)
        {
            await EnviarAsync(new Paquete(Tipos.Rechazo, Texto: error));
            return;
        }

        foreach (var mensaje in sala.HistorialChat())
        {
            await EnviarAsync(new Paquete(Tipos.Chat, Chat: mensaje));
        }

        await EmitirChatAsync(sala.AnadirChat("", $"{nombre} ha entrado en la sala.", esSistema: true));
        await DifundirAsync();
    }

    private async Task ChatDeAsync(string conexion, string? texto)
    {
        var autor = _sala!.NombreDe(conexion);
        texto = (texto ?? "").Trim();

        if (autor is null || texto.Length == 0)
        {
            return;
        }

        if (texto.Length > MaxMensaje)
        {
            texto = texto[..MaxMensaje];
        }

        await EmitirChatAsync(_sala.AnadirChat(autor, texto, esSistema: false));
    }

    /// <summary>El mensaje aparece aqui y se manda al otro (si esta conectado).</summary>
    private async Task EmitirChatAsync(MensajeChat mensaje)
    {
        ChatRecibido?.Invoke(mensaje);
        await EnviarAsync(new Paquete(Tipos.Chat, Chat: mensaje));
    }

    /// <summary>Cada jugador recibe su propia foto de la partida (con su atril, sin el del rival).</summary>
    private async Task DifundirAsync()
    {
        foreach (var (conexion, estado) in _sala!.Estados())
        {
            if (conexion == Anfitrion)
            {
                EstadoRecibido?.Invoke(estado);
            }
            else
            {
                await EnviarAsync(new Paquete(Tipos.Estado, Estado: estado));
            }
        }
    }

    // ---------- Llamadas desde JavaScript ----------

    /// <summary>Llega un paquete del otro navegador.</summary>
    [JSInvokable]
    public async Task AlMensaje(string json)
    {
        Paquete? paquete;
        try
        {
            paquete = JsonSerializer.Deserialize<Paquete>(json, Json);
        }
        catch (JsonException)
        {
            return;
        }

        if (paquete is null)
        {
            return;
        }

        if (_sala is not null)
        {
            await RecibirInvitadoAsync(paquete);
            return;
        }

        switch (paquete.Tipo)
        {
            case Tipos.Estado when paquete.Estado is not null:
                EstadoRecibido?.Invoke(paquete.Estado);
                break;
            case Tipos.Chat when paquete.Chat is not null:
                ChatRecibido?.Invoke(paquete.Chat);
                break;
            case Tipos.Error when paquete.Texto is not null:
                ErrorRecibido?.Invoke(paquete.Texto);
                break;
            case Tipos.Rechazo or Tipos.Lleno:
                Cerrada?.Invoke(paquete.Texto ?? "No se ha podido entrar en la sala.");
                await CerrarCanalAsync();
                break;
        }
    }

    /// <summary>Se ha cerrado el canal con el otro navegador.</summary>
    [JSInvokable]
    public async Task AlCierre()
    {
        if (_sala is null)
        {
            Cerrada?.Invoke("Se ha perdido la conexión con quien creó la sala.");
            return;
        }

        var nombre = _sala.Desconectar(Invitado);
        if (nombre is not null)
        {
            await EmitirChatAsync(_sala.AnadirChat("", $"{nombre} se ha desconectado. Puede volver a entrar con el mismo nombre.", esSistema: true));
            await DifundirAsync();
        }
    }

    // ---------- Auxiliares ----------

    private async Task EnviarAsync(Paquete paquete)
    {
        if (_modulo is null)
        {
            return;
        }

        try
        {
            await _modulo.InvokeVoidAsync("enviar", JsonSerializer.Serialize(paquete, Json));
        }
        catch (JSException)
        {
            // Canal cerrado justo ahora: AlCierre se encarga.
        }
    }

    private async Task CerrarCanalAsync()
    {
        if (_modulo is null)
        {
            return;
        }

        try
        {
            await _modulo.InvokeVoidAsync("cerrar");
        }
        catch (Exception)
        {
            // Ya estaba cerrado.
        }
    }

    public async ValueTask DisposeAsync()
    {
        await CerrarCanalAsync();

        try
        {
            if (_modulo is not null)
            {
                await _modulo.DisposeAsync();
            }
        }
        catch (Exception)
        {
            // La pagina ya se esta cerrando.
        }

        _modulo = null;
        _referencia?.Dispose();
        _referencia = null;
    }
}
