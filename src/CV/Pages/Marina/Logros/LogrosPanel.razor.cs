using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace CV.Pages.Marina.Logros;

public partial class LogrosPanel : IDisposable
{
    private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-ES");

    /// <summary>Objetos que caben en cada balda de la vitrina.</summary>
    private const int PorBalda = 4;

    /// <summary>Duracion del viaje hasta la sala de la vitrina, y del fundido de la llegada (ms).</summary>
    private const int ViajeMs = 2800;
    private const int LlegadaMs = 900;

    /// <summary>Cuanto dura en pantalla el efecto de un logro (ms).</summary>
    private const int EfectoMs = 4800;

    /// <summary>Rayo del salto en el tiempo: angulo (grados), largo (px) y retraso negativo (s) para que ya este en marcha.</summary>
    private record Rayo(double Angulo, int Largo, double Retraso);

    /// <summary>Rayos que salen del centro durante el viaje. Reparto fijo (sin azar) para que siempre sea igual.</summary>
    private static readonly Rayo[] Rayos = Enumerable.Range(0, 56)
        .Select(i => new Rayo(
            Angulo: i * 360.0 / 56 + (i * 17) % 7,
            Largo: 90 + (i * 53) % 130,
            Retraso: -((i * 0.37) % 1.15)))
        .ToArray();

    /// <summary>Coleccion completa, en el orden en que se muestra.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<Logro> Logros { get; set; } = [];

    /// <summary>Instante con el que se decide que logros estan desbloqueados.</summary>
    [Parameter] public DateTimeOffset Ahora { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Se lanza al volver del viaje (boton, fondo o tecla Escape).</summary>
    [Parameter] public EventCallback OnCerrar { get; set; }

    private readonly CancellationTokenSource _cancelar = new();

    private ElementReference _panel;

    private bool _viajando = true;
    private bool _llegando;
    private bool _abierta;
    private int? _seleccionado;

    // En movil la ficha es una hoja que sube desde abajo al elegir un logro (asi se lee sin hacer scroll)
    private bool _fichaAbierta;

    // Efecto a pantalla completa del ultimo logro pulsado, y objeto que se sacude si esta bloqueado
    private IReadOnlyList<Particula>? _particulas;
    private int _efectoId;
    private int? _sacudido;

    // Logros "nuevos" que ya se han pulsado (dejan de mostrar la insignia NEW)
    private readonly HashSet<int> _vistos = [];

    private bool EsNuevo(int indice) => Logros[indice].Nuevo && !_vistos.Contains(indice);

    private int Total => Logros.Count;
    private int Desbloqueados => Logros.Count(l => l.EstaDesbloqueado(Ahora));
    private int Porcentaje => Total == 0 ? 0 : Desbloqueados * 100 / Total;

    /// <summary>% de jugadores que tienen cada logro (mismo orden que <see cref="Logros"/>); fijo mientras la vitrina esta abierta.</summary>
    private double[] _porcentajes = [];

    protected override void OnInitialized() =>
        _porcentajes = Logros.Select(l =>
        {
            if (l.Porcentaje is { } fijo)
            {
                return fijo;
            }

            var (min, max) = l.Rareza.Rango();
            return Math.Round(min + Random.Shared.NextDouble() * (max - min), 1);
        }).ToArray();

    /// <summary>Los logros agrupados por categoria (de comun a legendario) y repartidos por baldas, con su posicion en la coleccion.</summary>
    private IEnumerable<(Rareza Rareza, int Cantidad, IEnumerable<(Logro Logro, int Indice)[]> Baldas)> Categorias =>
        Logros.Select((logro, indice) => (Logro: logro, Indice: indice))
            .GroupBy(x => x.Logro.Rareza)
            .OrderBy(g => g.Key)
            .Select(g => (g.Key, g.Count(), g.Chunk(PorBalda)));

    private string PorcentajeJugadores(int indice) =>
        indice < _porcentajes.Length ? _porcentajes[indice].ToString("0.#", Es) : "?";

    private Logro? Seleccionado => _seleccionado is { } i && i < Logros.Count ? Logros[i] : null;

    /// <summary>Al montarse toma el foco (para el teclado) y arranca el viaje.</summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await _panel.FocusAsync();
            _ = ViajarAsync(_cancelar.Token);
        }
    }

    /// <summary>El viaje dura <see cref="ViajeMs"/>; despues se llega a la sala con un fundido.</summary>
    private async Task ViajarAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(ViajeMs, token);
            await Llegar(token);
        }
        catch (OperationCanceledException)
        {
            // Se ha vuelto antes de llegar.
        }
    }

    /// <summary>Termina el viaje (por tiempo o porque se salta) y retira el fundido tras la llegada.</summary>
    private async Task Llegar(CancellationToken token)
    {
        if (!_viajando)
        {
            return;
        }

        _viajando = false;
        _llegando = true;
        await InvokeAsync(StateHasChanged);

        await Task.Delay(LlegadaMs, token);
        _llegando = false;
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>Saltar el viaje: pulsar en cualquier sitio o una tecla durante el trayecto.</summary>
    private void SaltarViaje() => _ = Llegar(_cancelar.Token);

    /// <summary>Un clic durante el viaje lo salta; en la sala, un clic fuera de la vitrina vuelve.</summary>
    private Task ClicFondo() => _viajando ? Saltar() : OnCerrar.InvokeAsync();

    private Task Saltar()
    {
        SaltarViaje();
        return Task.CompletedTask;
    }

    /// <summary>Abre las puertas de la vitrina y deja elegido el primer objeto para ver su detalle.</summary>
    private void AbrirVitrina()
    {
        _abierta = true;
        _seleccionado ??= 0;
    }

    /// <summary>
    /// Pulsar un objeto lo elige y lanza su efecto en pantalla; si esta bloqueado, el objeto se sacude.
    /// </summary>
    private void Elegir(int indice)
    {
        if (!_abierta)
        {
            return;
        }

        _seleccionado = indice;
        _fichaAbierta = true;
        _vistos.Add(indice);

        var logro = Logros[indice];
        if (logro.EstaDesbloqueado(Ahora))
        {
            LanzarEfecto(logro.Efecto);
        }
        else
        {
            _ = SacudirAsync(indice, _cancelar.Token);
        }
    }

    /// <summary>Muestra el efecto durante unos segundos; cada pulsacion usa una semilla distinta, asi que no sale igual.</summary>
    private void LanzarEfecto(EfectoLogro efecto)
    {
        var id = ++_efectoId;
        _particulas = EfectosLogro.Generar(efecto, id * 7919 + (int)efecto);
        _ = RetirarEfectoAsync(id, _cancelar.Token);
    }

    private async Task RetirarEfectoAsync(int id, CancellationToken token)
    {
        try
        {
            await Task.Delay(EfectoMs, token);
            if (_efectoId == id)
            {
                _particulas = null;
                await InvokeAsync(StateHasChanged);
            }
        }
        catch (OperationCanceledException)
        {
            // Se ha vuelto antes de que terminara.
        }
    }

    private async Task SacudirAsync(int indice, CancellationToken token)
    {
        try
        {
            _sacudido = null;
            await InvokeAsync(StateHasChanged);
            _sacudido = indice;
            await InvokeAsync(StateHasChanged);

            await Task.Delay(700, token);
            if (_sacudido == indice)
            {
                _sacudido = null;
                await InvokeAsync(StateHasChanged);
            }
        }
        catch (OperationCanceledException)
        {
            // Se ha vuelto antes de que terminara.
        }
    }

    /// <summary>Variables CSS de una particula (siempre con punto decimal, sea cual sea la cultura).</summary>
    private static string EstiloParticula(Particula p) =>
        $"--x: {N(p.X)}%; --y: {N(p.Y)}%; --dx: {N(p.Dx)}vw; --h: {N(p.Altura)}vh; --r: {N(p.Giro)}deg; " +
        $"--d: {N(p.Duracion)}s; --t: {N(p.Retraso)}s; --s: {N(p.Tamano)}{(p.Glifo.Length == 0 ? "vmin" : "rem")}" +
        (p.Color is null ? "" : $"; --c: {p.Color}");

    private void CerrarFicha() => _fichaAbierta = false;

    private void Siguiente() => Mover(+1);

    private void Anterior() => Mover(-1);

    private void Mover(int paso)
    {
        if (!_abierta || Total == 0)
        {
            return;
        }

        _seleccionado = (((_seleccionado ?? 0) + paso) % Total + Total) % Total;
    }

    private Task Teclado(KeyboardEventArgs e)
    {
        if (e.Key == "Escape")
        {
            return OnCerrar.InvokeAsync();
        }

        if (_viajando)
        {
            // Cualquier otra tecla salta el viaje.
            return Saltar();
        }

        switch (e.Key)
        {
            case "ArrowRight":
                Siguiente();
                break;
            case "ArrowLeft":
                Anterior();
                break;
            case "Enter" or " " when !_abierta:
                AbrirVitrina();
                break;
        }

        return Task.CompletedTask;
    }

    private static string TituloVisible(Logro logro, bool conseguido) =>
        conseguido || !logro.Secreto ? logro.Titulo : "???";

    private static string TextoVisible(Logro logro, bool conseguido) =>
        conseguido ? logro.Descripcion : logro.Pista ?? "Aún por conseguir";

    private static string Fecha(DateTimeOffset fecha) =>
        fecha.ToLocalTime().ToString("d 'de' MMMM 'de' yyyy", Es);

    /// <summary>Numero para estilos en linea: siempre con punto decimal, sea cual sea la cultura del navegador.</summary>
    private static string N(double valor) => valor.ToString("0.##", CultureInfo.InvariantCulture);

    public void Dispose()
    {
        _cancelar.Cancel();
        _cancelar.Dispose();
    }
}
