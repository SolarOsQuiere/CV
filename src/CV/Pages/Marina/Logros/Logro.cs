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
/// <param name="Efecto">Animacion a pantalla completa que se lanza al pulsar el logro ya desbloqueado.</param>
public record Logro(
    string Titulo,
    string Descripcion,
    string Icono,
    bool Conseguido = false,
    DateTimeOffset? Desbloqueo = null,
    bool Secreto = false,
    string? Pista = null,
    EfectoLogro Efecto = EfectoLogro.Confeti)
{
    public bool EstaDesbloqueado(DateTimeOffset ahora) =>
        Conseguido || (Desbloqueo is not null && Desbloqueo <= ahora);
}
