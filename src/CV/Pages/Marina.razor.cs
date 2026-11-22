namespace CV.Pages;

public partial class Marina : IDisposable
{
    private const string Nombre = "Marina";
    private const string Mensaje = "La mejor gallega";

    /// <summary>Frase que se va escribiendo tras unos segundos, como si alguien la tecleara.</summary>
    private const string Frase = "Tengo ganas de verte";

    /// <summary>Espera antes de empezar a teclear: deja terminar la animacion del titulo (~5 s).</summary>
    private static readonly TimeSpan RetrasoInicial = TimeSpan.FromSeconds(6);

    /// <summary>Ritmo de tecleo (ms por letra, al azar entre ambos valores) y pausa tras cada palabra.</summary>
    private const int TecleoMinMs = 70;
    private const int TecleoMaxMs = 150;
    private const int PausaPalabraMs = 260;

    private readonly CancellationTokenSource _cancelar = new();

    private string _escrito = "";
    private bool _tecleando;

    private record Letra(char Caracter, int Indice);

    /// <summary>Hojas de otono que caen de fondo: posicion horizontal (%), tamano (px), duracion y retraso (s).</summary>
    private record Hoja(int X, int Tamano, double Duracion, double Retraso);

    private static readonly Hoja[] Hojas =
    [
        new(6, 14, 16, 0), new(14, 10, 20, 4), new(23, 16, 18, 9), new(31, 12, 22, 2),
        new(40, 18, 17, 6), new(48, 11, 21, 11), new(57, 15, 19, 1), new(65, 12, 23, 7),
        new(73, 17, 16, 13), new(81, 10, 20, 3), new(89, 14, 18, 10), new(96, 12, 22, 5)
    ];

    /// <summary>El mensaje partido en palabras y letras, para animarlo letra a letra sin romper palabras.</summary>
    private static readonly IReadOnlyList<Letra[]> Palabras = PartirEnPalabras(Mensaje);

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            _ = TeclearAsync(_cancelar.Token);
        }
    }

    private async Task TeclearAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(RetrasoInicial, token);
            _tecleando = true;
            await InvokeAsync(StateHasChanged);

            foreach (var caracter in Frase)
            {
                await Task.Delay(Random.Shared.Next(TecleoMinMs, TecleoMaxMs), token);
                _escrito += caracter;
                await InvokeAsync(StateHasChanged);

                if (caracter == ' ')
                {
                    await Task.Delay(PausaPalabraMs, token);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Se ha salido de la pagina mientras se tecleaba.
        }
    }

    private static List<Letra[]> PartirEnPalabras(string texto)
    {
        var palabras = new List<Letra[]>();
        var indice = 0;
        foreach (var palabra in texto.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            palabras.Add(palabra.Select(c => new Letra(c, indice++)).ToArray());
        }
        return palabras;
    }

    public void Dispose()
    {
        _cancelar.Cancel();
        _cancelar.Dispose();
    }
}
