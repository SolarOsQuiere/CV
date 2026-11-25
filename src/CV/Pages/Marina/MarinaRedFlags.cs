using CV.Pages.Marina.RedFlags;

namespace CV.Pages.Marina;

/// <summary>
/// Historial de red flags de la pagina de Marina. Aqui se va rellenando; <b>el buscaminas tiene tantas minas como red flags
/// haya en esta lista</b> y, al resolverlo, todas se listan en un panel como el de los logros:
/// <list type="number">
///   <item>Agrega un <see cref="RedFlag"/> a la lista (o sustituye uno de los de ejemplo).</item>
///   <item><c>Gravedad</c> va de 1 a 5 y es el numero de banderas rojas que se pintan en la tarjeta.</item>
///   <item><c>Fecha</c> es opcional y se muestra en la tarjeta.</item>
/// </list>
/// </summary>
internal static class MarinaRedFlags
{
    public static readonly IReadOnlyList<RedFlag> Todas =
    [
        new("Jugar a los juegos de LinkedIn", "Tener 60 años y jugar a los juegos de LinkedIn.", Gravedad: 2),
        new("Ser de Galicia", "Ser de Galicia.", Gravedad: 1)
    ];
}
