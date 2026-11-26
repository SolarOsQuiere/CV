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
public record EstadoPartida(
    string Sala,
    FasePartida Fase,
    string[] Tablero,
    string Atril,
    IReadOnlyList<JugadorVista> Jugadores,
    int Yo,
    int Turno,
    int FichasEnBolsa,
    string? Aviso);

public record MensajeChat(string Autor, string Texto, DateTimeOffset Hora, bool EsSistema);

/// <summary>Lo que viaja por el canal entre los dos navegadores (como JSON). Cada <see cref="Tipos"/> usa solo los campos que le tocan.</summary>
public record Paquete(
    string Tipo,
    string? Texto = null,
    string? Nombre = null,
    List<Colocacion>? Fichas = null,
    EstadoPartida? Estado = null,
    MensajeChat? Chat = null);

/// <summary>Valores de <see cref="Paquete.Tipo"/>. Invitado -> anfitrion: hola, jugar, pasar, cambiar, nueva, chat. Anfitrion -> invitado: estado, chat, error, rechazo, lleno.</summary>
public static class Tipos
{
    public const string Hola = "hola";
    public const string Jugar = "jugar";
    public const string Pasar = "pasar";
    public const string Cambiar = "cambiar";
    public const string Nueva = "nueva";
    public const string Chat = "chat";
    public const string Estado = "estado";
    public const string Error = "error";
    public const string Rechazo = "rechazo";
    public const string Lleno = "lleno";
}
