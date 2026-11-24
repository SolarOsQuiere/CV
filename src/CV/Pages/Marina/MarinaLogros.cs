using CV.Pages.Marina.Logros;

namespace CV.Pages.Marina;

/// <summary>
/// Coleccion de logros de la pagina de Marina. Aqui se va rellenando:
/// <list type="number">
///   <item>Para anadir uno, agrega un <see cref="Logro"/> a la lista (o sustituye uno de los secretos).</item>
///   <item>Para darlo por conseguido, pon <c>Conseguido: true</c>; si quieres que se vea la fecha, anade tambien <c>Desbloqueo</c>.</item>
///   <item>Si le pones solo <c>Desbloqueo</c> con una fecha futura, se desbloquea solo cuando llegue (y sale un aviso).</item>
///   <item>Los iconos son clases de Font Awesome 5 gratuito (<c>fa-heart</c>, <c>fa-star</c>, <c>fa-music</c>...).</item>
///   <item>Con <c>Nuevo: true</c> sale una insignia NEW hasta que se pulsa el logro; con <c>Rareza:</c> lo cambias de categoria; con <c>Efecto:</c> eliges la animacion que sale al pulsar el logro: Verduras, Palabras, Sueno, Tiempo, Corazones, Estrellas, Notas, Flashes o Confeti (por defecto).</item>
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

        new("Dormirse en una conversación", "Quedarse dormida mientras hablábamos.", "fa-bed",
            Conseguido: true, Desbloqueo: new(2026, 11, 25, 12, 0, 0, TimeSpan.FromHours(1)), Efecto: EfectoLogro.Sueno, Nuevo: true),

        new("Empieza la cuenta atrás", "Alguien se ha puesto a contar los días.", "fa-hourglass-start",
            Desbloqueo: MarinaConfig.Inicio, Efecto: EfectoLogro.Tiempo),

        new("Por fin nos vemos", "Llegó el 6 de diciembre.", "fa-calendar-check",
            Desbloqueo: MarinaConfig.Objetivo, Efecto: EfectoLogro.Corazones, Rareza: Rareza.Legendario,
            Pista: "Se desbloquea cuando termine la cuenta atrás."),

        new("Logro secreto", "Todavía por descubrir.", "fa-star", Secreto: true, Efecto: EfectoLogro.Estrellas),
        new("Logro secreto", "Todavía por descubrir.", "fa-heart", Secreto: true, Efecto: EfectoLogro.Corazones),
        new("Logro secreto", "Todavía por descubrir.", "fa-camera", Secreto: true, Efecto: EfectoLogro.Flashes),
        new("Logro secreto", "Todavía por descubrir.", "fa-music", Secreto: true, Efecto: EfectoLogro.Notas)
    ];
}
