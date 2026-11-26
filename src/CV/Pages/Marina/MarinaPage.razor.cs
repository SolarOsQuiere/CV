using Microsoft.AspNetCore.Components;
using System.Globalization;
using CV.Pages.Marina.Logros;

namespace CV.Pages.Marina;

public partial class MarinaPage : IDisposable
{
    private const string Nombre = "Marina";
    private const string Antetitulo = "Tiempo para vernos";
    private const string TextoFecha = "6 de diciembre";
    private const string TextoHora = "a las 23:00 h";
    private const string Frase = "Cada vez falta menos para vernos";
    private const string MensajeFinal = "¡Hoy nos vemos!";

    private static DateTimeOffset Objetivo => MarinaConfig.Objetivo;
    private static DateTimeOffset Inicio => MarinaConfig.Inicio;

    private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-ES");

    private enum Unidad { Dias, Horas, Minutos, Segundos }

    /// <summary>Mota de polvo dorado: posicion horizontal (%), tamano (px), duracion y retraso (s) y deriva lateral (vw).</summary>
    private record Mota(int X, int Tamano, double Duracion, double Retraso, int Deriva);

    /// <summary>
    /// Polvo de arena que sube y titila. Se reparte con una formula fija (sin azar) para que sea siempre igual,
    /// y con retrasos negativos para que ya este en marcha desde el primer instante.
    /// </summary>
    private static readonly Mota[] Polvo = Enumerable.Range(0, 30)
        .Select(i =>
        {
            var duracion = 13 + (i * 7) % 11;
            return new Mota(
                X: (i * 37 + 11) % 100,
                Tamano: 3 + (i * 5) % 6,
                Duracion: duracion,
                Retraso: -((i * 1.7) % duracion),
                Deriva: ((i * 13) % 24) - 8);
        })
        .ToArray();

    // Geometria del reloj de arena (viewBox 0 0 120 180): cuello, base de la ampolla inferior y alto de cada ampolla.
    private const double YCuello = 90;
    private const double YBase = 158;
    private const double AltoAmpolla = 68;
    private const double AltoMaxMonticulo = 9;

    private readonly CancellationTokenSource _cancelar = new();

    private TimeSpan _restante;
    private Unidad? _seleccionada;
    private bool _girando;

    // Coleccion de logros
    private bool _redFlagsAbiertas;
    private bool _scrabbleAbierto;
    private bool _logrosAbiertos;
    private string? _aviso;
    private DateTimeOffset _ultimaComprobacion = DateTimeOffset.UtcNow;

    private bool Terminado => _restante <= TimeSpan.Zero;

    private static IReadOnlyList<Logro> Logros => MarinaLogros.Todos;
    private int NumDesbloqueados => Logros.Count(l => l.EstaDesbloqueado(DateTimeOffset.UtcNow));

    /// <summary>Las cuatro casillas con su valor actual (los "huecos" del reloj: dias, horas, minutos y segundos).</summary>
    private IEnumerable<(Unidad Unidad, long Valor)> Casillas =>
    [
        (Unidad.Dias, (long)_restante.TotalDays),
        (Unidad.Horas, _restante.Hours),
        (Unidad.Minutos, _restante.Minutes),
        (Unidad.Segundos, _restante.Seconds)
    ];

    /// <summary>Tiempo restante en total, expresado en la unidad elegida (con decimales solo en dias).</summary>
    private string TotalTexto => _seleccionada switch
    {
        Unidad.Dias => _restante.TotalDays.ToString("N2", Es),
        Unidad.Horas => ((long)_restante.TotalHours).ToString("N0", Es),
        Unidad.Minutos => ((long)_restante.TotalMinutes).ToString("N0", Es),
        _ => ((long)_restante.TotalSeconds).ToString("N0", Es)
    };

    // ---- Reloj de arena: la arena de arriba es el tiempo que falta, la de abajo el que ya ha pasado ----

    private double FraccionRestante =>
        Math.Clamp(_restante.TotalSeconds / (Objetivo - Inicio).TotalSeconds, 0, 1);

    private double AlturaArenaArriba => FraccionRestante * AltoAmpolla;
    private double AlturaArenaAbajo => (1 - FraccionRestante) * AltoAmpolla;

    private double YArenaArriba => YCuello - AlturaArenaArriba;
    private double YArenaAbajo => YBase - AlturaArenaAbajo;

    /// <summary>La arena que cae forma un monticulo en el centro; crece con la arena acumulada.</summary>
    private double AlturaMonticulo => Math.Min(AltoMaxMonticulo, AlturaArenaAbajo);

    /// <summary>Contorno de la arena acumulada abajo: base plana y superficie con un monticulo en el centro.</summary>
    private string TrazadoArenaAbajo =>
        $"M20 {N(YBase + 1)} V{N(YArenaAbajo)} Q60 {N(YArenaAbajo - AlturaMonticulo * 2)} 100 {N(YArenaAbajo)} V{N(YBase + 1)} Z";

    /// <summary>Hay hilo de arena mientras quede arena arriba y la cita no haya llegado.</summary>
    private bool HayHilo => !Terminado && AlturaArenaArriba > 0.3;

    /// <summary>Codigo de sala del enlace de invitacion del Scrabble (<c>/Marina?sala=ABC12</c>): abre el Scrabble con el codigo puesto.</summary>
    [SupplyParameterFromQuery(Name = "sala")]
    [Parameter]
    public string? Sala { get; set; }

    protected override void OnInitialized()
    {
        Actualizar();
        _scrabbleAbierto = !string.IsNullOrWhiteSpace(Sala);
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            _ = ContarAsync(_cancelar.Token);
        }
    }

    /// <summary>Recalcula cada 100 ms y solo repinta cuando cambia el segundo, sin acumular deriva.</summary>
    private async Task ContarAsync(CancellationToken token)
    {
        using var reloj = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
        try
        {
            while (await reloj.WaitForNextTickAsync(token))
            {
                var segundoAnterior = (long)_restante.TotalSeconds;
                var terminadoAntes = Terminado;
                Actualizar();

                // Tambien se repinta al llegar a cero: de 0,9 s a 0 s el segundo entero no cambia,
                // y sin esto no apareceria el mensaje final.
                var hayAviso = ComprobarLogros();

                if ((long)_restante.TotalSeconds != segundoAnterior || Terminado != terminadoAntes || hayAviso)
                {
                    await InvokeAsync(StateHasChanged);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Se ha salido de la pagina.
        }
    }

    /// <summary>Detecta logros que se acaban de desbloquear por fecha y prepara el aviso (se retira solo a los 7 s).</summary>
    private bool ComprobarLogros()
    {
        var ahora = DateTimeOffset.UtcNow;
        var nuevos = Logros
            .Where(l => l.Desbloqueo is { } fecha && fecha > _ultimaComprobacion && fecha <= ahora)
            .Select(l => l.Titulo)
            .ToList();
        _ultimaComprobacion = ahora;

        if (nuevos.Count == 0)
        {
            return false;
        }

        _aviso = string.Join(" · ", nuevos);
        _ = RetirarAvisoAsync(_aviso, _cancelar.Token);
        return true;
    }

    private async Task RetirarAvisoAsync(string aviso, CancellationToken token)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(7), token);
            if (_aviso == aviso)
            {
                _aviso = null;
                await InvokeAsync(StateHasChanged);
            }
        }
        catch (OperationCanceledException)
        {
            // Se ha salido de la pagina.
        }
    }

    private void AbrirLogros() => _logrosAbiertos = true;

    private void AbrirRedFlags() => _redFlagsAbiertas = true;

    private void CerrarRedFlags() => _redFlagsAbiertas = false;

    private void AbrirScrabble() => _scrabbleAbierto = true;

    private void CerrarScrabble() => _scrabbleAbierto = false;

    private void CerrarLogros() => _logrosAbiertos = false;

    private void Actualizar()
    {
        var restante = Objetivo - DateTimeOffset.UtcNow;
        _restante = restante > TimeSpan.Zero ? restante : TimeSpan.Zero;
    }

    /// <summary>Pulsar una casilla muestra el total en esa unidad; volver a pulsarla lo oculta.</summary>
    private void Seleccionar(Unidad unidad) => _seleccionada = _seleccionada == unidad ? null : unidad;

    /// <summary>Da la vuelta al reloj de arena (animacion de media vuelta y regreso, solo decorativa).</summary>
    private async Task Girar()
    {
        if (_girando)
        {
            return;
        }

        _girando = true;
        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(1300), _cancelar.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        finally
        {
            _girando = false;
        }
    }

    /// <summary>Numero para atributos SVG: siempre con punto decimal, sea cual sea la cultura del navegador.</summary>
    private static string N(double valor) => valor.ToString("0.##", CultureInfo.InvariantCulture);

    private static string Etiqueta(Unidad unidad, long valor) => (unidad, valor == 1) switch
    {
        (Unidad.Dias, true) => "Día",
        (Unidad.Dias, false) => "Días",
        (Unidad.Horas, true) => "Hora",
        (Unidad.Horas, false) => "Horas",
        (Unidad.Minutos, true) => "Minuto",
        (Unidad.Minutos, false) => "Minutos",
        (_, true) => "Segundo",
        _ => "Segundos"
    };

    /// <summary>Nombre de la unidad elegida, en minuscula, para la frase del total.</summary>
    private string NombreUnidadTotal => _seleccionada switch
    {
        Unidad.Dias => "días",
        Unidad.Horas => "horas",
        Unidad.Minutos => "minutos",
        _ => "segundos"
    };

    public void Dispose()
    {
        _cancelar.Cancel();
        _cancelar.Dispose();
    }
}
