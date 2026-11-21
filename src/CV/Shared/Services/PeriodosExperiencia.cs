namespace CV.Shared.Services;

/// <summary>
/// Periodos de la seccion de experiencia. Fuente unica para la duracion de cada puesto
/// y para el total que se muestra en la portada.
/// </summary>
public static class PeriodosExperiencia
{
    /// <summary>Inicio del puesto actual (sin fecha de fin).</summary>
    public static readonly DateTime InicioActual = new(2022, 5, 1);

    /// <summary>Puestos ya finalizados: inicio y fin, ambos meses inclusive.</summary>
    public static readonly (DateTime Inicio, DateTime Fin)[] Finalizados =
    [
        (new DateTime(2021, 12, 1), new DateTime(2022, 3, 1)),  // Hiberus, practicas (4 meses)
        (new DateTime(2021, 9, 1), new DateTime(2021, 12, 1)),  // Gnoss, practicas (4 meses)
        (new DateTime(2021, 7, 1), new DateTime(2021, 9, 1)),   // Hiberus, practicas (3 meses)
    ];

    /// <summary>Meses del puesto actual hasta <paramref name="hoy"/>.</summary>
    public static int MesesActual(DateTime hoy)
        => (hoy.Year - InicioActual.Year) * 12 + hoy.Month - InicioActual.Month;

    /// <summary>Suma de los meses de todos los puestos de la experiencia.</summary>
    public static int TotalMeses(DateTime hoy)
        => MesesActual(hoy) + Finalizados.Sum(p => (p.Fin.Year - p.Inicio.Year) * 12 + p.Fin.Month - p.Inicio.Month + 1);
}
