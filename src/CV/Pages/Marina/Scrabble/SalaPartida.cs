namespace CV.Pages.Marina.Scrabble;

/// <summary>
/// Una sala: hasta dos plazas (con su conexion), el historial del chat y el <see cref="MotorScrabble"/> que lleva las reglas.
/// Traduce "quien es esta conexion" a "que jugador es" y arma el estado que ve cada uno.
/// </summary>
public sealed class SalaPartida(string id)
{
    private const int MaxChat = 60;

    private sealed class Plaza(string nombre, string conexion)
    {
        public string Nombre { get; } = nombre;
        public string? Conexion { get; set; } = conexion;
    }

    private readonly object _cerrojo = new();
    private readonly List<Plaza> _plazas = [];
    private readonly MotorScrabble _motor = new();
    private readonly List<MensajeChat> _chat = [];

    public string Id { get; } = id;

    /// <summary>Jugadas hechas en la partida actual (para descartar movimientos desactualizados).</summary>
    public int NumeroTurno { get { lock (_cerrojo) { return _motor.NumeroTurno; } } }

    // ---------- Plazas ----------

    /// <summary>Mete a un jugador. Devuelve el texto del error, o null si todo bien. Un nombre desconectado se reengancha.</summary>
    public string? Unir(string conexion, string nombre)
    {
        lock (_cerrojo)
        {
            var existente = _plazas.FirstOrDefault(p => p.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase));
            if (existente is not null)
            {
                if (existente.Conexion is not null)
                {
                    return "Ya hay alguien con ese nombre en la sala.";
                }

                existente.Conexion = conexion;
                return null;
            }

            if (_motor.Llena)
            {
                return "La sala está llena (máximo dos jugadores).";
            }

            _plazas.Add(new Plaza(nombre, conexion));
            _motor.AnadirJugador(nombre);
            return null;
        }
    }

    /// <summary>Marca desconectado al jugador de esa conexion y devuelve su nombre (o null si no estaba).</summary>
    public string? Desconectar(string conexion)
    {
        lock (_cerrojo)
        {
            var plaza = _plazas.FirstOrDefault(p => p.Conexion == conexion);
            if (plaza is null)
            {
                return null;
            }

            plaza.Conexion = null;
            return plaza.Nombre;
        }
    }

    public string? NombreDe(string conexion)
    {
        lock (_cerrojo)
        {
            return _plazas.FirstOrDefault(p => p.Conexion == conexion)?.Nombre;
        }
    }

    // ---------- Chat ----------

    public MensajeChat AnadirChat(string autor, string texto, bool esSistema)
    {
        var mensaje = new MensajeChat(autor, texto, DateTimeOffset.UtcNow, esSistema);
        lock (_cerrojo)
        {
            _chat.Add(mensaje);
            if (_chat.Count > MaxChat)
            {
                _chat.RemoveAt(0);
            }
        }

        return mensaje;
    }

    public IReadOnlyList<MensajeChat> HistorialChat()
    {
        lock (_cerrojo)
        {
            return [.. _chat];
        }
    }

    // ---------- Estado ----------

    /// <summary>Una foto personalizada (su atril) para cada jugador conectado.</summary>
    public IReadOnlyList<(string Conexion, EstadoPartida Estado)> Estados()
    {
        lock (_cerrojo)
        {
            var tablero = _motor.Tablero();
            var vistas = _plazas.Select((p, i) => new JugadorVista(p.Nombre, _motor.Puntos(i), p.Conexion is not null)).ToList();

            return _plazas
                .Select((plaza, indice) => (plaza, indice))
                .Where(x => x.plaza.Conexion is not null)
                .Select(x => (x.plaza.Conexion!, new EstadoPartida(
                    Id, _motor.Fase, tablero, _motor.Atril(x.indice), vistas, x.indice, _motor.Turno,
                    _motor.FichasEnBolsa, _motor.Aviso, _motor.NumeroTurno)))
                .ToList();
        }
    }

    // ---------- Acciones de juego (por conexion) ----------

    public string? Jugar(string conexion, IReadOnlyList<Colocacion> fichas) => Accion(conexion, j => _motor.Jugar(j, fichas));

    public string? Pasar(string conexion) => Accion(conexion, _motor.Pasar);

    public string? Cambiar(string conexion, string letras) => Accion(conexion, j => _motor.Cambiar(j, letras));

    public string? NuevaPartida(string conexion) => Accion(conexion, _ => _motor.NuevaPartida());

    private string? Accion(string conexion, Func<int, string?> accion)
    {
        lock (_cerrojo)
        {
            var indice = _plazas.FindIndex(p => p.Conexion == conexion);
            return indice < 0 ? "No estás en esta sala." : accion(indice);
        }
    }
}
