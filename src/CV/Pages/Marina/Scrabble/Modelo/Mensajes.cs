namespace CV.Pages.Marina.Scrabble;

/// <summary>Ficha puesta en el tablero durante una jugada.</summary>
public record Colocacion(int Fila, int Columna, char Letra);

public enum FasePartida { Esperando, Jugando, Terminada }

public record JugadorVista(string Nombre, int Puntos, bool Conectado);

/// <summary>
/// Foto de la partida que el anfitrion envia a cada jugador. El atril es solo el suyo: el del rival no viaja nunca.
/// </summary>
/// <param name="Tablero">15 filas de 15 caracteres; <see cref="Reglas.Vacia"/> marca la casilla vacia.</param>
/// <param name="Turno">Indice (en <paramref name="Jugadores"/>) de quien juega.</param>
/// <param name="NumeroTurno">Jugadas hechas en la partida; el cliente lo devuelve en cada movimiento para detectar los desactualizados.</param>
public record EstadoPartida(
    string Sala,
    FasePartida Fase,
    string[] Tablero,
    string Atril,
    IReadOnlyList<JugadorVista> Jugadores,
    int Yo,
    int Turno,
    int FichasEnBolsa,
    string? Aviso,
    int NumeroTurno);

public record MensajeChat(string Autor, string Texto, DateTimeOffset Hora, bool EsSistema);

/// <summary>Contenido de <c>PLAYER_MOVE</c>: una accion de juego del Scrabble.</summary>
public record Movimiento(string Accion, List<Colocacion>? Fichas = null, string? Letras = null)
{
    public const string Jugar = "jugar";
    public const string Pasar = "pasar";
    public const string Cambiar = "cambiar";
    public const string NuevaPartida = "nueva";
}
