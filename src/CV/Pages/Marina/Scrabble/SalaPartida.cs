namespace CV.Pages.Marina.Scrabble;

/// <summary>
/// Una sala: hasta dos jugadores, una partida y el historial del chat. Toda la logica del juego vive aqui
/// (el servidor es la autoridad: el cliente solo propone jugadas). Es segura entre hilos.
/// Sin diccionario: las palabras no se validan, se confia en los jugadores.
/// </summary>
public sealed class SalaPartida(string id)
{
    private const int MaxJugadores = 2;
    private const int PasesParaTerminar = 4;
    private const int MaxChat = 60;

    private sealed class Jugador(string nombre, string conexion)
    {
        public string Nombre { get; } = nombre;
        public string? Conexion { get; set; } = conexion;
        public List<char> Atril { get; } = [];
        public int Puntos { get; set; }
    }

    private readonly object _cerrojo = new();
    private readonly List<Jugador> _jugadores = [];
    private readonly List<char> _bolsa = [];
    private readonly char[,] _tablero = new char[Reglas.Tamano, Reglas.Tamano];
    private readonly List<MensajeChat> _chat = [];

    private FasePartida _fase = FasePartida.Esperando;
    private int _turno;
    private int _pasesSeguidos;
    private string? _aviso;

    public string Id { get; } = id;

    public bool Vacia { get { lock (_cerrojo) { return _jugadores.All(j => j.Conexion is null); } } }

    // ---------- Jugadores ----------

    /// <summary>Mete a un jugador. Devuelve el texto del error, o null si todo bien. Un nombre desconectado se reengancha (recargar la pagina).</summary>
    public string? Unir(string conexion, string nombre)
    {
        lock (_cerrojo)
        {
            var existente = _jugadores.FirstOrDefault(j => j.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase));
            if (existente is not null)
            {
                if (existente.Conexion is not null)
                {
                    return "Ya hay alguien con ese nombre en la sala.";
                }

                existente.Conexion = conexion;
                return null;
            }

            if (_jugadores.Count >= MaxJugadores)
            {
                return "La sala está llena (máximo dos jugadores).";
            }

            _jugadores.Add(new Jugador(nombre, conexion));
            if (_jugadores.Count == MaxJugadores && _fase == FasePartida.Esperando)
            {
                Empezar();
            }

            return null;
        }
    }

    /// <summary>Marca desconectado al jugador de esa conexion y devuelve su nombre (o null si no estaba).</summary>
    public string? Desconectar(string conexion)
    {
        lock (_cerrojo)
        {
            var jugador = _jugadores.FirstOrDefault(j => j.Conexion == conexion);
            if (jugador is null)
            {
                return null;
            }

            jugador.Conexion = null;
            return jugador.Nombre;
        }
    }

    public string? NombreDe(string conexion)
    {
        lock (_cerrojo)
        {
            return _jugadores.FirstOrDefault(j => j.Conexion == conexion)?.Nombre;
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
            var tablero = Enumerable.Range(0, Reglas.Tamano)
                .Select(f => new string(Enumerable.Range(0, Reglas.Tamano).Select(c => Celda(f, c)).ToArray()))
                .ToArray();
            var vistas = _jugadores.Select(j => new JugadorVista(j.Nombre, j.Puntos, j.Conexion is not null)).ToList();

            return _jugadores
                .Select((j, i) => (j, i))
                .Where(x => x.j.Conexion is not null)
                .Select(x => (x.j.Conexion!, new EstadoPartida(
                    Id, _fase, tablero, new string([.. x.j.Atril]), vistas, x.i, _turno, _bolsa.Count, _aviso)))
                .ToList();
        }
    }

    private char Celda(int fila, int columna) => _tablero[fila, columna] == '\0' ? Reglas.Vacia : _tablero[fila, columna];

    private bool EstaOcupada(int fila, int columna) => _tablero[fila, columna] != '\0';

    // ---------- Partida ----------

    /// <summary>Reparte una partida nueva. Solo se puede pedir cuando no hay una en juego.</summary>
    public string? NuevaPartida(string conexion)
    {
        lock (_cerrojo)
        {
            if (_fase == FasePartida.Jugando)
            {
                return "La partida sigue en juego.";
            }

            if (_jugadores.Count < MaxJugadores)
            {
                return "Falta el otro jugador.";
            }

            Empezar();
            return null;
        }
    }

    private void Empezar()
    {
        Array.Clear(_tablero);
        _bolsa.Clear();
        _bolsa.AddRange(Reglas.BolsaCompleta().OrderBy(_ => Random.Shared.Next()));

        foreach (var jugador in _jugadores)
        {
            jugador.Puntos = 0;
            jugador.Atril.Clear();
            Robar(jugador);
        }

        _fase = FasePartida.Jugando;
        _turno = Random.Shared.Next(_jugadores.Count);
        _pasesSeguidos = 0;
        _aviso = $"Empieza {_jugadores[_turno].Nombre}.";
    }

    private void Robar(Jugador jugador)
    {
        while (jugador.Atril.Count < Reglas.TamanoAtril && _bolsa.Count > 0)
        {
            jugador.Atril.Add(_bolsa[^1]);
            _bolsa.RemoveAt(_bolsa.Count - 1);
        }
    }

    public string? Pasar(string conexion)
    {
        lock (_cerrojo)
        {
            if (!EsSuTurno(conexion, out var jugador, out var error))
            {
                return error;
            }

            _pasesSeguidos++;
            _aviso = $"{jugador.Nombre} pasa.";
            AvanzarTurno();
            if (_pasesSeguidos >= PasesParaTerminar)
            {
                Terminar(null);
            }

            return null;
        }
    }

    public string? Cambiar(string conexion, string letras)
    {
        lock (_cerrojo)
        {
            if (!EsSuTurno(conexion, out var jugador, out var error))
            {
                return error;
            }

            var cambio = letras.ToUpperInvariant().ToList();
            if (cambio.Count == 0 || cambio.Count > Reglas.TamanoAtril)
            {
                return "Elige las fichas que quieres cambiar.";
            }

            if (_bolsa.Count < Reglas.TamanoAtril)
            {
                return "Quedan pocas fichas en la bolsa para cambiar.";
            }

            var atril = jugador.Atril.ToList();
            foreach (var letra in cambio)
            {
                if (!atril.Remove(letra))
                {
                    return "Esas fichas no están en tu atril.";
                }
            }

            _bolsa.AddRange(cambio);
            var mezcla = _bolsa.OrderBy(_ => Random.Shared.Next()).ToList();
            _bolsa.Clear();
            _bolsa.AddRange(mezcla);
            jugador.Atril.Clear();
            jugador.Atril.AddRange(atril);
            Robar(jugador);

            _pasesSeguidos = 0;
            _aviso = $"{jugador.Nombre} cambia {cambio.Count} ficha{(cambio.Count == 1 ? "" : "s")}.";
            AvanzarTurno();
            return null;
        }
    }

    /// <summary>Valida y aplica una jugada. Devuelve el error (no cambia nada) o null.</summary>
    public string? Jugar(string conexion, IReadOnlyList<Colocacion> fichas)
    {
        lock (_cerrojo)
        {
            if (!EsSuTurno(conexion, out var jugador, out var error))
            {
                return error;
            }

            var colocadas = fichas.Select(f => f with { Letra = char.ToUpperInvariant(f.Letra) }).ToList();
            error = Comprobar(jugador, colocadas, out var palabras);
            if (error is not null)
            {
                return error;
            }

            var puntos = 0;
            foreach (var ficha in colocadas)
            {
                _tablero[ficha.Fila, ficha.Columna] = ficha.Letra;
                jugador.Atril.Remove(ficha.Letra);
            }

            foreach (var palabra in palabras)
            {
                puntos += Puntuar(palabra, colocadas);
            }

            var bingo = colocadas.Count == Reglas.TamanoAtril;
            if (bingo)
            {
                puntos += Reglas.PremioBingo;
            }

            jugador.Puntos += puntos;
            Robar(jugador);
            _pasesSeguidos = 0;
            _aviso = $"{jugador.Nombre}: {string.Join(", ", palabras.Select(Texto))} (+{puntos}{(bingo ? ", ¡scrabble!" : "")}).";

            if (jugador.Atril.Count == 0)
            {
                Terminar(jugador);
            }
            else
            {
                AvanzarTurno();
            }

            return null;
        }
    }

    // ---------- Reglas de la jugada ----------

    /// <summary>Una palabra del tablero: donde empieza, hacia donde va y su longitud.</summary>
    private readonly record struct Palabra(int Fila, int Columna, bool Horizontal, int Largo);

    private string Texto(Palabra p) =>
        new([.. Enumerable.Range(0, p.Largo).Select(i => LetraEn(p.Fila + (p.Horizontal ? 0 : i), p.Columna + (p.Horizontal ? i : 0)))]);

    private char LetraEn(int fila, int columna) => _tablero[fila, columna];

    private string? Comprobar(Jugador jugador, List<Colocacion> colocadas, out List<Palabra> palabras)
    {
        palabras = [];

        if (colocadas.Count == 0)
        {
            return "Coloca alguna ficha en el tablero.";
        }

        if (colocadas.Count > Reglas.TamanoAtril)
        {
            return "Demasiadas fichas.";
        }

        var atril = jugador.Atril.ToList();
        foreach (var ficha in colocadas)
        {
            if (ficha.Fila is < 0 or >= Reglas.Tamano || ficha.Columna is < 0 or >= Reglas.Tamano)
            {
                return "Una ficha está fuera del tablero.";
            }

            if (EstaOcupada(ficha.Fila, ficha.Columna))
            {
                return "Hay una ficha en una casilla ocupada.";
            }

            if (!atril.Remove(ficha.Letra))
            {
                return "Usas una ficha que no tienes.";
            }
        }

        if (colocadas.Select(f => (f.Fila, f.Columna)).Distinct().Count() != colocadas.Count)
        {
            return "Dos fichas en la misma casilla.";
        }

        var mismaFila = colocadas.All(f => f.Fila == colocadas[0].Fila);
        var mismaColumna = colocadas.All(f => f.Columna == colocadas[0].Columna);
        if (!mismaFila && !mismaColumna)
        {
            return "Las fichas deben estar en una misma fila o columna.";
        }

        // Se colocan de forma provisional para leer las palabras; si algo falla se quitan.
        foreach (var ficha in colocadas)
        {
            _tablero[ficha.Fila, ficha.Columna] = ficha.Letra;
        }

        try
        {
            return ComprobarSobreTablero(colocadas, mismaFila, mismaColumna, out palabras);
        }
        finally
        {
            foreach (var ficha in colocadas)
            {
                _tablero[ficha.Fila, ficha.Columna] = '\0';
            }
        }
    }

    private string? ComprobarSobreTablero(List<Colocacion> colocadas, bool mismaFila, bool mismaColumna, out List<Palabra> palabras)
    {
        palabras = [];
        var nuevas = colocadas.Select(f => (f.Fila, f.Columna)).ToHashSet();

        // Sin huecos entre la primera y la ultima ficha de la linea.
        if (mismaFila)
        {
            var fila = colocadas[0].Fila;
            for (var c = colocadas.Min(f => f.Columna); c <= colocadas.Max(f => f.Columna); c++)
            {
                if (!EstaOcupada(fila, c))
                {
                    return "No puede haber huecos entre las fichas.";
                }
            }
        }
        else
        {
            var columna = colocadas[0].Columna;
            for (var f = colocadas.Min(x => x.Fila); f <= colocadas.Max(x => x.Fila); f++)
            {
                if (!EstaOcupada(f, columna))
                {
                    return "No puede haber huecos entre las fichas.";
                }
            }
        }

        // Todas las palabras (de 2 o mas letras) que toca alguna ficha nueva, sin repetir.
        var vistas = new HashSet<Palabra>();
        foreach (var (fila, columna) in nuevas)
        {
            foreach (var horizontal in new[] { true, false })
            {
                var palabra = PalabraQuePasaPor(fila, columna, horizontal);
                if (palabra.Largo >= 2 && vistas.Add(palabra))
                {
                    palabras.Add(palabra);
                }
            }
        }

        if (palabras.Count == 0)
        {
            return "Tienes que formar una palabra de al menos dos letras.";
        }

        var primeraJugada = !Enumerable.Range(0, Reglas.Tamano)
            .SelectMany(f => Enumerable.Range(0, Reglas.Tamano).Select(c => (f, c)))
            .Any(p => EstaOcupada(p.f, p.c) && !nuevas.Contains(p));

        if (primeraJugada)
        {
            var centro = Reglas.Tamano / 2;
            return nuevas.Contains((centro, centro)) ? null : "La primera palabra debe pasar por la casilla central.";
        }

        // Debe engancharse con lo que ya habia: alguna palabra incluye una ficha antigua.
        var enganchada = palabras.Any(p => Casillas(p).Any(c => !nuevas.Contains(c)));
        return enganchada ? null : "La palabra debe tocar alguna ficha que ya esté en el tablero.";
    }

    private IEnumerable<(int Fila, int Columna)> Casillas(Palabra p) =>
        Enumerable.Range(0, p.Largo).Select(i => (p.Fila + (p.Horizontal ? 0 : i), p.Columna + (p.Horizontal ? i : 0)));

    private Palabra PalabraQuePasaPor(int fila, int columna, bool horizontal)
    {
        var (df, dc) = horizontal ? (0, 1) : (1, 0);
        var f = fila;
        var c = columna;
        while (f - df >= 0 && c - dc >= 0 && EstaOcupada(f - df, c - dc))
        {
            f -= df;
            c -= dc;
        }

        var largo = 0;
        while (f + df * largo < Reglas.Tamano && c + dc * largo < Reglas.Tamano && EstaOcupada(f + df * largo, c + dc * largo))
        {
            largo++;
        }

        return new Palabra(f, c, horizontal, largo);
    }

    /// <summary>Puntos de una palabra: los premios solo cuentan en las casillas donde se acaba de poner ficha.</summary>
    private int Puntuar(Palabra palabra, List<Colocacion> colocadas)
    {
        var nuevas = colocadas.Select(f => (f.Fila, f.Columna)).ToHashSet();
        var suma = 0;
        var multiplicador = 1;

        foreach (var (fila, columna) in Casillas(palabra))
        {
            var valor = Reglas.Puntos(_tablero[fila, columna]);
            if (nuevas.Contains((fila, columna)))
            {
                switch (Reglas.Premio(fila, columna))
                {
                    case 'd': valor *= 2; break;
                    case 't': valor *= 3; break;
                    case 'D': multiplicador *= 2; break;
                    case 'T': multiplicador *= 3; break;
                }
            }

            suma += valor;
        }

        return suma * multiplicador;
    }

    // ---------- Turnos y final ----------

    private bool EsSuTurno(string conexion, out Jugador jugador, out string? error)
    {
        jugador = null!;
        error = null;

        if (_fase != FasePartida.Jugando)
        {
            error = "La partida no está en juego.";
            return false;
        }

        var indice = _jugadores.FindIndex(j => j.Conexion == conexion);
        if (indice < 0)
        {
            error = "No estás en esta sala.";
            return false;
        }

        if (indice != _turno)
        {
            error = "No es tu turno.";
            return false;
        }

        jugador = _jugadores[indice];
        return true;
    }

    private void AvanzarTurno() => _turno = (_turno + 1) % _jugadores.Count;

    /// <summary>Fin de partida: quien se queda sin fichas suma lo que le sobra al rival, y cada uno resta lo suyo.</summary>
    private void Terminar(Jugador? sinFichas)
    {
        foreach (var jugador in _jugadores)
        {
            var sobrante = jugador.Atril.Sum(Reglas.Puntos);
            jugador.Puntos -= sobrante;
            if (sinFichas is not null && jugador != sinFichas)
            {
                sinFichas.Puntos += sobrante;
            }
        }

        _fase = FasePartida.Terminada;

        var ordenados = _jugadores.OrderByDescending(j => j.Puntos).ToList();
        var resultado = ordenados[0].Puntos == ordenados[^1].Puntos
            ? "Empate."
            : $"Gana {ordenados[0].Nombre}.";
        _aviso = $"{_aviso} Fin de la partida. {resultado}";
    }
}
