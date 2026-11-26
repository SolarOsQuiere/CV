namespace CV.Pages.Marina.Scrabble;

/// <summary>
/// Reglas y datos fijos del Scrabble (version en castellano, sin digrafos CH/LL/RR ni comodines).
/// Este archivo lo comparten el cliente (Blazor) y el servidor (<c>Servidor/</c>).
/// </summary>
public static class Reglas
{
    public const int Tamano = 15;
    public const int TamanoAtril = 7;
    public const int PremioBingo = 50;

    /// <summary>Marca de casilla vacia en el tablero que viaja por la red.</summary>
    public const char Vacia = '.';

    private static readonly (char Letra, int Cantidad, int Puntos)[] Distribucion =
    [
        ('A', 12, 1), ('B', 2, 3), ('C', 4, 3), ('D', 5, 2), ('E', 12, 1), ('F', 1, 4), ('G', 2, 2), ('H', 2, 4),
        ('I', 6, 1), ('J', 1, 8), ('L', 4, 1), ('M', 2, 3), ('N', 5, 1), ('Ñ', 1, 8), ('O', 9, 1), ('P', 2, 3),
        ('Q', 1, 5), ('R', 5, 1), ('S', 6, 1), ('T', 4, 1), ('U', 5, 1), ('V', 1, 4), ('X', 1, 8), ('Y', 1, 4),
        ('Z', 1, 10)
    ];

    private static readonly Dictionary<char, int> PuntosPorLetra = Distribucion.ToDictionary(d => d.Letra, d => d.Puntos);

    /// <summary>
    /// Premios del tablero: T = palabra x3, D = palabra x2, t = letra x3, d = letra x2, punto = sin premio.
    /// Solo se escriben las 8 primeras filas; el resto es el reflejo.
    /// </summary>
    private static readonly string[] Premios = Reflejar(
    [
        "T..d...T...d..T",
        ".D...t...t...D.",
        "..D...d.d...D..",
        "d..D...d...D..d",
        "....D.....D....",
        ".t...t...t...t.",
        "..d...d.d...d..",
        "T..d...D...d..T"
    ]);

    public static bool EsLetra(char letra) => PuntosPorLetra.ContainsKey(letra);

    public static int Puntos(char letra) => PuntosPorLetra.GetValueOrDefault(letra);

    public static char Premio(int fila, int columna) => Premios[fila][columna];

    /// <summary>Todas las fichas de la bolsa (95), sin mezclar.</summary>
    public static IEnumerable<char> BolsaCompleta() =>
        Distribucion.SelectMany(d => Enumerable.Repeat(d.Letra, d.Cantidad));

    private static string[] Reflejar(string[] mitad) => [.. mitad, .. mitad.Take(7).Reverse()];
}
