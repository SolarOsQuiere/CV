namespace CV.Pages.Marina;

/// <summary>Configuracion de la pagina oculta de Marina: ruta, foto y fechas clave.</summary>
internal static class MarinaConfig
{
    /// <summary>Ruta de la pagina (relativa al base href). No hay ningun enlace a ella: solo se llega escribiendola.</summary>
    public const string Ruta = "/Marina";

    /// <summary>Foto de la pagina, relativa al base href.</summary>
    public const string Foto = "assets/marina/marina.webp";

    /// <summary>
    /// Momento de la cita: 6 de diciembre de 2026 a las 23:00 en la peninsula (UTC+1 en invierno).
    /// Va con su desfase para que la cuenta atras sea la misma sea cual sea la zona horaria del dispositivo.
    /// </summary>
    public static readonly DateTimeOffset Objetivo = new(2026, 12, 6, 23, 0, 0, TimeSpan.FromHours(1));

    /// <summary>Desde cuando se cuenta: el reloj de arena empieza lleno aqui y se vacia hasta <see cref="Objetivo"/>.</summary>
    public static readonly DateTimeOffset Inicio = new(2026, 11, 23, 0, 0, 0, TimeSpan.FromHours(1));
}
