namespace CV.Pages.Marina.RedFlags;

/// <summary>Una red flag encontrada: aparece en el historial una vez resuelto el buscaminas.</summary>
/// <param name="Titulo">Nombre corto de la red flag.</param>
/// <param name="Descripcion">Explicacion o anecdota.</param>
/// <param name="Gravedad">De 1 (casi nada) a 5 (para salir corriendo): numero de banderas que se pintan.</param>
/// <param name="Fecha">Cuando se encontro (opcional).</param>
public record RedFlag(
    string Titulo,
    string Descripcion,
    int Gravedad = 1,
    DateTimeOffset? Fecha = null);
