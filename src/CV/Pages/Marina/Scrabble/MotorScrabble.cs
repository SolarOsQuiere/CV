namespace CV.Pages.Marina.Scrabble;

/// <summary>
/// Motor de reglas del Scrabble: tablero, bolsa, atriles, puntuacion y turnos. Solo conoce jugadores por su posicion (0 o 1):
/// no sabe nada de red, conexiones ni chat (eso es <see cref="SalaPartida"/>).
/// Sin diccionario: las palabras no se validan, se confia en los jugadores.
/// </summary>
public sealed class MotorScrabble
{
    private const int MaxJugadores = 2;
    private const int PasesParaTerminar = 4;

    private sealed class Jugador(string nombre)
    {
        public string Nombre { get; } = nombre;
        public List<char> Atril { get; } = [];
        public int Puntos { get; set; }
    }

    private readonly List<Jugador> _jugadores = [];
    private readonly List<char> _bolsa = [];
    private readonly char[,] _tablero = new char[Reglas.Tamano, Reglas.Tamano];
    private int _pasesSeguidos;

    public FasePartida Fase { get; private set; } = FasePartida.Esperando;

    /// <summary>Posicion del jugador al que le toca.</summary>
    public int Turno { get; private set; }

    /// <summary>Jugadas hechas desde el reparto. Sirve para descartar movimientos desactualizados.</summary>
    public int NumeroTurno { get; private set; }

    public string? Aviso { get; private set; }

    public int FichasEnBolsa => _bolsa.Count;

    public int NumJugadores => _jugadores.Count;

    public bool Llena => _jugadores.Count >= MaxJugadores;

    public string Nombre(int jugador) => _jugadores[jugador].Nombre;

    public int Puntos(int jugador) => _jugadores[jugador].Puntos;

    public string Atril(int jugador) => new([.. _jugadores[jugador].Atril]);

    /// <summary>El tablero como 15 filas de 15 caracteres (<see cref="Reglas.Vacia"/> = casilla libre).</summary>
    public string[] Tablero() =>
        [.. Enumerable.Range(0, Reglas.Tamano)
            .Select(f => new string([.. Enumerable.Range(0, Reglas.Tamano).Select(c => Celda(f, c))]))];

    private char Celda(int fila, int columna) => _tablero[fila, columna] == '\0' ? Reglas.Vacia : _tablero[fila, columna];

    private bool EstaOcupada(int fila, int columna) => _tablero[fila, columna] != '\0';

    // ---------- Partida ----------

    /// <summary>Anade un jugador; al llegar al segundo se reparte y empieza la partida. Devuelve su posicion.</summary>
    public int AnadirJugador(string nombre)
    {
        _jugadores.Add(new Jugador(nombre));
        if (_jugadores.Count == MaxJugadores && Fase == FasePartida.Esperando)
        {
            Empezar();
        }

        return _jugadores.Count - 1;
    }

    /// <summary>Reparte una partida nueva. Solo cuando no hay una en juego y estan los dos.</summary>
    public string? NuevaPartida()
    {
        if (Fase == FasePartida.Jugando)
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

        Fase = FasePartida.Jugando;
        Turno = Random.Shared.Next(_jugadores.Count);
        NumeroTurno = 0;
        _pasesSeguidos = 0;
        Aviso = $"Empieza {_jugadores[Turno].Nombre}.";
    }

    private void Robar(Jugador jugador)
    {
        while (jugador.Atril.Count < Reglas.TamanoAtril && _bolsa.Count > 0)
        {
            jugador.Atril.Add(_bolsa[^1]);
            _bolsa.RemoveAt(_bolsa.Count - 1);
        }
    }

    public string? Pasar(int quien)
    {
        if (!EsSuTurno(quien, out var jugador, out var error))
        {
            return error;
        }

        _pasesSeguidos++;
        Aviso = $"{jugador.Nombre} pasa.";
        CerrarTurno();
        if (_pasesSeguidos >= PasesParaTerminar)
        {
            Terminar(null);
        }

        return null;
    }

    public string? Cambiar(int quien, string letras)
    {
        if (!EsSuTurno(quien, out var jugador, out var error))
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
        Aviso = $"{jugador.Nombre} cambia {cambio.Count} ficha{(cambio.Count == 1 ? "" : "s")}.";
        CerrarTurno();
        return null;
    }

    /// <summary>Valida y aplica una jugada. Devuelve el error (no cambia nada) o null.</summary>
    public string? Jugar(int quien, IReadOnlyList<Colocacion> fichas)
    {
        if (!EsSuTurno(quien, out var jugador, out var error))
        {
            return error;
        }

        var colocadas = fichas.Select(f => f with { Letra = char.ToUpperInvariant(f.Letra) }).ToList();
        error = Comprobar(jugador, colocadas, out var palabras);
        if (error is not null)
        {
            return error;
        }

        foreach (var ficha in colocadas)
        {
            _tablero[ficha.Fila, ficha.Columna] = ficha.Letra;
            jugador.Atril.Remove(ficha.Letra);
        }

        var puntos = palabras.Sum(p => Puntuar(p, colocadas));
        var bingo = colocadas.Count == Reglas.TamanoAtril;
        if (bingo)
        {
            puntos += Reglas.PremioBingo;
        }

        jugador.Puntos += puntos;
        Robar(jugador);
        _pasesSeguidos = 0;
        Aviso = $"{jugador.Nombre}: {string.Join(", ", palabras.Select(Texto))} (+{puntos}{(bingo ? ", ¡scrabble!" : "")}).";

        if (jugador.Atril.Count == 0)
        {
            NumeroTurno++;
            Terminar(jugador);
        }
        else
        {
            CerrarTurno();
        }

        return null;
    }

    // ---------- Reglas de la jugada ----------

    /// <summary>Una palabra del tablero: donde empieza, hacia donde va y su longitud.</summary>
    private readonly record struct Palabra(int Fila, int Columna, bool Horizontal, int Largo);

    private string Texto(Palabra p) =>
        new([.. Casillas(p).Select(c => _tablero[c.Fila, c.Columna])]);

    private static IEnumerable<(int Fila, int Columna)> Casillas(Palabra p) =>
        Enumerable.Range(0, p.Largo).Select(i => (p.Fila + (p.Horizontal ? 0 : i), p.Columna + (p.Horizontal ? i : 0)));

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

        // Se colocan de forma provisional para leer las palabras; siempre se quitan al acabar.
        foreach (var ficha in colocadas)
        {
            _tablero[ficha.Fila, ficha.Columna] = ficha.Letra;
        }

        try
        {
            return ComprobarSobreTablero(colocadas, mismaFila, out palabras);
        }
        finally
        {
            foreach (var ficha in colocadas)
            {
                _tablero[ficha.Fila, ficha.Columna] = '\0';
            }
        }
    }

    private string? ComprobarSobreTablero(List<Colocacion> colocadas, bool mismaFila, out List<Palabra> palabras)
    {
        palabras = [];
        var nuevas = colocadas.Select(f => (f.Fila, f.Columna)).ToHashSet();

        // Sin huecos entre la primera y la ultima ficha de la linea.
        var hayHueco = mismaFila
            ? Enumerable.Range(colocadas.Min(f => f.Columna), colocadas.Max(f => f.Columna) - colocadas.Min(f => f.Columna) + 1)
                .Any(c => !EstaOcupada(colocadas[0].Fila, c))
            : Enumerable.Range(colocadas.Min(f => f.Fila), colocadas.Max(f => f.Fila) - colocadas.Min(f => f.Fila) + 1)
                .Any(f => !EstaOcupada(f, colocadas[0].Columna));

        if (hayHueco)
        {
            return "No puede haber huecos entre las fichas.";
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

    private bool EsSuTurno(int quien, out Jugador jugador, out string? error)
    {
        jugador = null!;
        error = null;

        if (Fase != FasePartida.Jugando)
        {
            error = "La partida no está en juego.";
            return false;
        }

        if (quien < 0 || quien >= _jugadores.Count)
        {
            error = "No estás en esta partida.";
            return false;
        }

        if (quien != Turno)
        {
            error = "No es tu turno.";
            return false;
        }

        jugador = _jugadores[quien];
        return true;
    }

    private void CerrarTurno()
    {
        NumeroTurno++;
        Turno = (Turno + 1) % _jugadores.Count;
    }

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

        Fase = FasePartida.Terminada;

        var ordenados = _jugadores.OrderByDescending(j => j.Puntos).ToList();
        var resultado = ordenados[0].Puntos == ordenados[^1].Puntos ? "Empate." : $"Gana {ordenados[0].Nombre}.";
        Aviso = $"{Aviso} Fin de la partida. {resultado}";
    }
}
