using CV.Pages.Marina.Logros;

namespace CV.Pages.Marina;

/// <summary>
/// Coleccion de logros de la pagina de Marina. Aqui se va rellenando:
/// <list type="number">
///   <item>Para anadir uno, agrega un <see cref="Logro"/> a la lista (o sustituye uno de los secretos).</item>
///   <item>Para darlo por conseguido, pon <c>Conseguido: true</c>; si quieres que se vea la fecha, anade tambien <c>Desbloqueo</c>.</item>
///   <item>Si le pones solo <c>Desbloqueo</c> con una fecha futura, se desbloquea solo cuando llegue (y sale un aviso).</item>
///   <item>Los iconos son clases de Font Awesome 5 gratuito (<c>fa-heart</c>, <c>fa-star</c>, <c>fa-music</c>...).</item>
/// </list>
/// </summary>
internal static class MarinaLogros
{
    public static readonly IReadOnlyList<Logro> Todos =
    [
        new("Cocineros en apuros", "Primera partida de Overcooked.", "fa-utensils",
            Conseguido: true),

        new("Subdelegada de inglés", "Subdelegada de la clase de inglés.", "fa-chalkboard-teacher",
            Conseguido: true),

        new("Empieza la cuenta atrás", "Alguien se ha puesto a contar los días.", "fa-hourglass-start",
            Desbloqueo: MarinaConfig.Inicio),

        new("Por fin nos vemos", "Llegó el 6 de diciembre.", "fa-calendar-check",
            Desbloqueo: MarinaConfig.Objetivo,
            Pista: "Se desbloquea cuando termine la cuenta atrás."),

        new("Logro secreto", "Todavía por descubrir.", "fa-star", Secreto: true),
        new("Logro secreto", "Todavía por descubrir.", "fa-heart", Secreto: true),
        new("Logro secreto", "Todavía por descubrir.", "fa-camera", Secreto: true),
        new("Logro secreto", "Todavía por descubrir.", "fa-music", Secreto: true)
    ];
}
