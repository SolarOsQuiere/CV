using System.Security.Cryptography;

namespace CV.Pages.Marina.Scrabble;

/// <summary>
/// Una sesion de juego entre dos navegadores. Habla con el otro solo a traves de <see cref="IGameTransport"/>.
/// <para>
/// El que crea la sala es el <b>anfitrion</b>: la partida (<see cref="SalaPartida"/>) vive en su navegador y es la autoridad.
/// Valida cada movimiento (turno, token, duplicados, turno desactualizado) y reparte el estado. El <b>invitado</b> solo
/// envia sus acciones y recibe el estado y el chat. Para el panel los dos son iguales: mismos metodos y mismos eventos.
/// </para>
/// Los eventos pueden dispararse en medio de un clic: quien los escuche debe pasar por <c>InvokeAsync</c>.
/// </summary>
public sealed class ScrabbleSesion : IAsyncDisposable
{
    private const string Anfitrion = "anfitrion";
    private const string Invitado = "invitado";
    private const int MaxNombre = 20;
    private const int MaxMensaje = 300;
    private const int MaxIdsRecordados = 200;

    private readonly IGameTransport _transporte;

    private string _sala = "";
    private SalaPartida? _partida;

    // Anfitrion
    private string? _tokenInvitado;
    private readonly HashSet<string> _idsVistos = [];
    private readonly Queue<string> _ordenIds = new();

    // Invitado
    private string? _token;
    private int _turnoVisto;

    public ScrabbleSesion(IGameTransport transporte)
    {
        _transporte = transporte;
        _transporte.MessageReceived += AlMensajeAsync;
        _transporte.Disconnected += AlCierreAsync;
    }

    /// <summary>Llega la foto de la partida (la propia, con su atril).</summary>
    public event Action<EstadoPartida>? EstadoRecibido;

    public event Action<MensajeChat>? ChatRecibido;

    /// <summary>Una accion no se ha podido hacer (no es su turno, jugada invalida...).</summary>
    public event Action<string>? ErrorRecibido;

    /// <summary>La sesion se ha acabado (se ha ido el otro, la sala estaba llena...). Trae el motivo.</summary>
    public event Action<string>? Cerrada;

    public bool EsAnfitrion => _partida is not null;

    // ---------- Entrar ----------

    /// <summary>Abre la sala con ese codigo y se queda esperando al otro jugador. Devuelve el error, o null.</summary>
    public async Task<string?> CrearAsync(string codigo, string nombre)
    {
        _sala = codigo;
        _partida = new SalaPartida(codigo);
        _partida.Unir(Anfitrion, nombre);

        var error = await _transporte.OpenRoomAsync(codigo);
        if (error is not null)
        {
            _partida = null;
            return error;
        }

        await EmitirChatAsync(_partida.AnadirChat("", $"Sala {codigo} abierta. Comparte el enlace de invitación.", esSistema: true));
        await DifundirAsync(MessageTypes.GameInitialState);
        return null;
    }

    /// <summary>Se conecta a la sala de ese codigo. Si el anfitrion no lo acepta (nombre repetido, sala llena...), llega por <see cref="Cerrada"/>.</summary>
    public async Task<string?> UnirseAsync(string codigo, string nombre)
    {
        _sala = codigo;
        _partida = null;
        _token = null;

        var error = await _transporte.JoinRoomAsync(codigo);
        if (error is not null)
        {
            return error;
        }

        await _transporte.SendAsync(GameMessage.Crear(MessageTypes.JoinRequest, _sala, new JoinRequestPayload(nombre)));
        return null;
    }

    // ---------- Acciones (iguales para anfitrion e invitado) ----------

    public Task JugarAsync(List<Colocacion> fichas) => MoverAsync(new Movimiento(Movimiento.Jugar, Fichas: fichas));

    public Task PasarAsync() => MoverAsync(new Movimiento(Movimiento.Pasar));

    public Task CambiarAsync(string letras) => MoverAsync(new Movimiento(Movimiento.Cambiar, Letras: letras));

    public Task NuevaPartidaAsync() => MoverAsync(new Movimiento(Movimiento.NuevaPartida));

    public Task EnviarChatAsync(string texto) => _partida is not null
        ? ChatDeAsync(Anfitrion, texto)
        : _transporte.SendAsync(GameMessage.Crear(MessageTypes.ChatSend, _sala, new TextoPayload(texto), token: _token));

    private Task MoverAsync(Movimiento movimiento) => _partida is not null
        ? AplicarAsync(Anfitrion, movimiento)
        : _transporte.SendAsync(GameMessage.Crear(MessageTypes.PlayerMove, _sala, movimiento, _turnoVisto, _token));

    // ---------- Anfitrion: valida lo que llega y reparte el resultado ----------

    private async Task RecibirComoAnfitrionAsync(GameMessage mensaje)
    {
        var partida = _partida!;

        // Duplicados: un mismo MessageId solo se procesa una vez.
        if (!_idsVistos.Add(mensaje.MessageId))
        {
            return;
        }

        _ordenIds.Enqueue(mensaje.MessageId);
        if (_ordenIds.Count > MaxIdsRecordados)
        {
            _idsVistos.Remove(_ordenIds.Dequeue());
        }

        if (mensaje.Type == MessageTypes.JoinRequest)
        {
            await AceptarInvitadoAsync(mensaje, partida);
            return;
        }

        // Todo lo demas exige el token que se entrego al aceptar al invitado.
        if (_tokenInvitado is null || mensaje.Token != _tokenInvitado)
        {
            return;
        }

        switch (mensaje.Type)
        {
            case MessageTypes.PlayerMove:
                var movimiento = mensaje.Leer<Movimiento>();
                if (movimiento is null)
                {
                    await RechazarAsync("Movimiento no válido.");
                }
                else if (mensaje.Turn != partida.NumeroTurno)
                {
                    await RechazarAsync("Tu pantalla estaba desactualizada. Mira el tablero y vuelve a intentarlo.");
                }
                else
                {
                    await AplicarAsync(Invitado, movimiento);
                }

                break;

            case MessageTypes.ChatSend:
                if (mensaje.Leer<TextoPayload>() is { } texto)
                {
                    await ChatDeAsync(Invitado, texto.Text);
                }

                break;
        }
    }

    private async Task AceptarInvitadoAsync(GameMessage mensaje, SalaPartida partida)
    {
        var nombre = (mensaje.Leer<JoinRequestPayload>()?.Name ?? "").Trim();
        var error = nombre.Length is 0 or > MaxNombre
            ? "Nombre no válido."
            : partida.Unir(Invitado, nombre);

        if (error is not null)
        {
            await _transporte.SendAsync(GameMessage.Crear(MessageTypes.JoinRejected, _sala, new TextoPayload(error)));
            return;
        }

        _tokenInvitado = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        await _transporte.SendAsync(GameMessage.Crear(MessageTypes.JoinAccepted, _sala, new JoinAcceptedPayload(_tokenInvitado)));

        foreach (var anterior in partida.HistorialChat())
        {
            await _transporte.SendAsync(GameMessage.Crear(MessageTypes.ChatMessage, _sala, anterior));
        }

        await EmitirChatAsync(partida.AnadirChat("", $"{nombre} ha entrado en la sala.", esSistema: true));
        await DifundirAsync(MessageTypes.GameInitialState);
    }

    private async Task AplicarAsync(string conexion, Movimiento movimiento)
    {
        var partida = _partida!;
        var error = movimiento.Accion switch
        {
            Movimiento.Jugar => partida.Jugar(conexion, movimiento.Fichas ?? []),
            Movimiento.Pasar => partida.Pasar(conexion),
            Movimiento.Cambiar => partida.Cambiar(conexion, movimiento.Letras ?? ""),
            Movimiento.NuevaPartida => partida.NuevaPartida(conexion),
            _ => "Acción desconocida."
        };

        if (error is null)
        {
            await DifundirAsync(MessageTypes.GameStateUpdated);
        }
        else if (conexion == Anfitrion)
        {
            ErrorRecibido?.Invoke(error);
        }
        else
        {
            await RechazarAsync(error);
        }
    }

    private Task RechazarAsync(string motivo) =>
        _transporte.SendAsync(GameMessage.Crear(MessageTypes.MoveRejected, _sala, new TextoPayload(motivo)));

    private async Task ChatDeAsync(string conexion, string? texto)
    {
        var autor = _partida!.NombreDe(conexion);
        texto = (texto ?? "").Trim();

        if (autor is null || texto.Length == 0)
        {
            return;
        }

        if (texto.Length > MaxMensaje)
        {
            texto = texto[..MaxMensaje];
        }

        await EmitirChatAsync(_partida.AnadirChat(autor, texto, esSistema: false));
    }

    /// <summary>El mensaje aparece aqui y se manda al otro (si esta conectado).</summary>
    private async Task EmitirChatAsync(MensajeChat mensaje)
    {
        ChatRecibido?.Invoke(mensaje);
        await _transporte.SendAsync(GameMessage.Crear(MessageTypes.ChatMessage, _sala, mensaje));
    }

    /// <summary>Cada jugador recibe su propia foto de la partida (con su atril, sin el del rival).</summary>
    private async Task DifundirAsync(string tipo)
    {
        foreach (var (conexion, estado) in _partida!.Estados())
        {
            if (conexion == Anfitrion)
            {
                EstadoRecibido?.Invoke(estado);
            }
            else
            {
                await _transporte.SendAsync(GameMessage.Crear(tipo, _sala, estado));
            }
        }
    }

    // ---------- Invitado: aplica lo que manda el anfitrion ----------

    private async Task RecibirComoInvitadoAsync(GameMessage mensaje)
    {
        switch (mensaje.Type)
        {
            case MessageTypes.JoinAccepted when mensaje.Leer<JoinAcceptedPayload>() is { } aceptado:
                _token = aceptado.Token;
                break;

            case MessageTypes.GameInitialState or MessageTypes.GameStateUpdated when mensaje.Leer<EstadoPartida>() is { } estado:
                _turnoVisto = estado.NumeroTurno;
                EstadoRecibido?.Invoke(estado);
                break;

            case MessageTypes.ChatMessage when mensaje.Leer<MensajeChat>() is { } chat:
                ChatRecibido?.Invoke(chat);
                break;

            case MessageTypes.MoveRejected when mensaje.Leer<TextoPayload>() is { } rechazo:
                ErrorRecibido?.Invoke(rechazo.Text);
                break;

            case MessageTypes.JoinRejected:
                Cerrada?.Invoke(mensaje.Leer<TextoPayload>()?.Text ?? "No se ha podido entrar en la sala.");
                await _transporte.CloseAsync();
                break;
        }
    }

    // ---------- Eventos del transporte ----------

    private async Task AlMensajeAsync(GameMessage mensaje)
    {
        if (mensaje.RoomId != _sala)
        {
            return;
        }

        if (_partida is not null)
        {
            await RecibirComoAnfitrionAsync(mensaje);
        }
        else
        {
            await RecibirComoInvitadoAsync(mensaje);
        }
    }

    private async Task AlCierreAsync()
    {
        if (_partida is null)
        {
            Cerrada?.Invoke("Se ha perdido la conexión con quien creó la sala.");
            return;
        }

        _tokenInvitado = null;
        var nombre = _partida.Desconectar(Invitado);
        if (nombre is not null)
        {
            await EmitirChatAsync(_partida.AnadirChat("", $"{nombre} se ha desconectado. Puede volver a entrar con el mismo nombre.", esSistema: true));
            await DifundirAsync(MessageTypes.GameStateUpdated);
        }
    }

    /// <summary>Cierra el canal (sin avisar por <see cref="Cerrada"/>): se vuelve a poder entrar o crear sala.</summary>
    public Task CerrarAsync() => _transporte.CloseAsync();

    public async ValueTask DisposeAsync()
    {
        _transporte.MessageReceived -= AlMensajeAsync;
        _transporte.Disconnected -= AlCierreAsync;
        await _transporte.DisposeAsync();
    }
}
