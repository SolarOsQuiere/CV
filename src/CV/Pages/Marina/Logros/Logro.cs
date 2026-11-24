namespace CV.Pages.Marina.Logros;

/// <summary>Un logro de la coleccion: se muestra apagado hasta que se consigue o llega su fecha de desbloqueo.</summary>
/// <param name="Titulo">Nombre del logro.</param>
/// <param name="Descripcion">Texto que se ve cuando esta desbloqueado.</param>
/// <param name="Icono">Clase de Font Awesome, p. ej. <c>fa-heart</c> (se pinta con <c>fas</c>).</param>
/// <param name="Conseguido">Logro ya conseguido (con o sin fecha). Si no se pone, depende de <paramref name="Desbloqueo"/>.</param>
/// <param name="Desbloqueo">
/// Fecha en que se consigue; se muestra en la tarjeta. Si es una fecha futura, el logro se desbloquea solo cuando llegue.
/// </param>
/// <param name="Secreto">Si es <c>true</c>, mientras este bloqueado el titulo se oculta ("???").</param>
/// <param name="Pista">Pista que se ve mientras esta bloqueado.</param>
/// <param name="Rareza">Categoria del logro: Comun, Raro, Epico o Legendario (por defecto Comun). Agrupa los logros en la vitrina.</param>
/// <param name="Porcentaje">
/// % de jugadores que tienen el logro. Si no se pone, se genera uno aleatorio acorde a la rareza cada vez que se abre la vitrina.
/// </param>
/// <param name="Nuevo">Muestra una insignia "NEW" sobre el logro hasta que se pulsa (para destacar el que se acaba de conseguir).</param>
/// <param name="Comentario">Texto que sustituye a la linea del % de jugadores (p. ej. "Solo tu hermana y tu tenéis este logro").</param>
/// <param name="Efecto">Animacion a pantalla completa que se lanza al pulsar el logro ya desbloqueado.</param>
public record Logro(
    string Titulo,
    string Descripcion,
    string Icono,
    bool Conseguido = false,
    DateTimeOffset? Desbloqueo = null,
    bool Secreto = false,
    string? Pista = null,
    EfectoLogro Efecto = EfectoLogro.Confeti,
    Rareza Rareza = Rareza.Comun,
    double? Porcentaje = null,
    bool Nuevo = false,
    string? Comentario = null)
{
    public bool EstaDesbloqueado(DateTimeOffset ahora) =>
        Conseguido || (Desbloqueo is not null && Desbloqueo <= ahora);
}

/// <summary>Categoria de un logro, de mas a menos frecuente.</summary>
public enum Rareza { Comun, Raro, Epico, Legendario }

public static class RarezaExtensions
{
    public static string Nombre(this Rareza r) => r switch
    {
        Rareza.Comun => "Común",
        Rareza.Raro => "Raro",
        Rareza.Epico => "Épico",
        _ => "Legendario"
    };

    public static string Plural(this Rareza r) => r switch
    {
        Rareza.Comun => "Comunes",
        Rareza.Raro => "Raros",
        Rareza.Epico => "Épicos",
        _ => "Legendarios"
    };

    /// <summary>Clase CSS de la categoria (para el color).</summary>
    public static string Clase(this Rareza r) => "r-" + r.ToString().ToLowerInvariant();

    /// <summary>Rango (en %) del que sale el porcentaje aleatorio de jugadores que tienen un logro de esta rareza.</summary>
    public static (double Min, double Max) Rango(this Rareza r) => r switch
    {
        Rareza.Comun => (35, 82),
        Rareza.Raro => (10, 28),
        Rareza.Epico => (3, 9),
        _ => (0.4, 2.4)
    };
}
