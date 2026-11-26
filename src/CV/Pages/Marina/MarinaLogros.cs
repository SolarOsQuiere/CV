using CV.Pages.Marina.Logros;

namespace CV.Pages.Marina;

/// <summary>
/// Coleccion de logros de la pagina de Marina. Aqui se va rellenando:
/// <list type="number">
///   <item>Para anadir uno, agrega un <see cref="Logro"/> a la lista.</item>
///   <item>Para darlo por conseguido, pon <c>Conseguido: true</c>; si quieres que se vea la fecha, anade tambien <c>Desbloqueo</c>.</item>
///   <item>Si le pones solo <c>Desbloqueo</c> con una fecha futura, se desbloquea solo cuando llegue (y sale un aviso).</item>
///   <item>Los iconos son clases de Font Awesome 5 gratuito (<c>fa-heart</c>, <c>fa-star</c>, <c>fa-music</c>...).</item>
///   <item>Con <c>Nuevo: true</c> sale una insignia NEW hasta que se pulsa el logro; con <c>Rareza:</c> lo cambias de categoria (Comun, Raro, Epico, Legendario o Frikada, esta ultima aparte, para las frikadas); con <c>Efecto:</c> eliges la animacion que sale al pulsar el logro: Verduras, Palabras, Sueno, Tiempo, Corazones, Estrellas, Notas, Flashes o Confeti (por defecto).</item>
/// </list>
/// </summary>
internal static class MarinaLogros
{
    public static readonly IReadOnlyList<Logro> Todos =
    [
        new("Cocineros en apuros", "Primera partida de Overcooked.", "fa-utensils",
            Conseguido: true, Desbloqueo: new(2026, 11, 24, 12, 0, 0, TimeSpan.FromHours(1)), Efecto: EfectoLogro.Verduras),

        new("Subdelegada de inglés", "Subdelegada de la clase de inglés.", "fa-chalkboard-teacher",
            Conseguido: true, Efecto: EfectoLogro.Palabras, Rareza: Rareza.Raro,
            Comentario: "Solo tu hermana y tú tenéis este logro."),

        new("Héctor en Galicia", "¿Cómo llegué aquí?", "fa-map-marked-alt",
            Efecto: EfectoLogro.Estrellas, Rareza: Rareza.Raro, Pista: "¿Cómo llegué aquí?"),

        new("Hyrox", "Ver a Marina en Hyrox.", "fa-dumbbell",
            Efecto: EfectoLogro.Flashes, Rareza: Rareza.Raro, Pista: "Ver a Marina en Hyrox."),

        new("Dormirse en una conversación", "Quedarse dormida mientras hablábamos.", "fa-bed",
            Conseguido: true, Desbloqueo: new(2026, 11, 25, 12, 0, 0, TimeSpan.FromHours(1)), Efecto: EfectoLogro.Sueno),

        new("Sobrevivir a Oporto", "Sobrevivir a Oporto.", "fa-wine-bottle", Pista: "Sobrevivir a Oporto."),

        new("AOT", "Ver Attack on Titan.", "fa-tv",
            Conseguido: true, Desbloqueo: new(2026, 11, 23, 12, 0, 0, TimeSpan.FromHours(1)), Efecto: EfectoLogro.Flashes,
            Rareza: Rareza.Frikada, Nuevo: true),

        new("1ª temporada de AOT", "Completaste la primera temporada de Attack on Titan.", "fa-play-circle",
            Efecto: EfectoLogro.Flashes, Rareza: Rareza.Frikada, Pista: "Completar la primera temporada de Attack on Titan."),

        new("Jugar a juegos de mesa", "Echar una partida a un juego de mesa.", "fa-dice",
            Conseguido: true, Efecto: EfectoLogro.Estrellas, Rareza: Rareza.Frikada, Nuevo: true),

        new("Terminar AOT", "Terminar Attack on Titan.", "fa-flag-checkered",
            Efecto: EfectoLogro.Flashes, Rareza: Rareza.Frikada, Pista: "Terminar Attack on Titan."),

        new("Empieza la cuenta atrás", "Alguien se ha puesto a contar los días.", "fa-hourglass-start",
            Desbloqueo: MarinaConfig.Inicio, Efecto: EfectoLogro.Tiempo),

        new("Por fin nos vemos", "Llegó el 6 de diciembre.", "fa-calendar-check",
            Desbloqueo: MarinaConfig.Objetivo, Efecto: EfectoLogro.Corazones, Rareza: Rareza.Legendario,
            Pista: "Se desbloquea cuando termine la cuenta atrás.")
    ];
}
