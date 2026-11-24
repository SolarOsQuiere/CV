namespace CV.Pages.Marina.Logros;

/// <summary>Animacion a pantalla completa que se lanza al pulsar un logro desbloqueado.</summary>
public enum EfectoLogro
{
    /// <summary>Confeti de colores cayendo (el efecto por defecto).</summary>
    Confeti,
    /// <summary>Verduras saltando y rebotando por la pantalla.</summary>
    Verduras,
    /// <summary>Palabras en ingles apareciendo con un golpe por toda la pantalla.</summary>
    Palabras,
    /// <summary>Lluvia de numeros dorados y relojes de arena.</summary>
    Tiempo,
    /// <summary>Corazones subiendo.</summary>
    Corazones,
    /// <summary>Estrellas y destellos que aparecen y se apagan.</summary>
    Estrellas,
    /// <summary>Notas musicales subiendo.</summary>
    Notas,
    /// <summary>Flashes de camara de fotos.</summary>
    Flashes
}

/// <summary>Como se mueve una particula (cada movimiento es una animacion CSS distinta).</summary>
public enum Movimiento { Salto, Pop, Caer, Subir }

/// <summary>
/// Una particula del efecto.
/// <paramref name="X"/> e <paramref name="Y"/> son posiciones en % de la pantalla; <paramref name="Dx"/> el desplazamiento
/// horizontal (vw); <paramref name="Altura"/> la altura del salto (vh); <paramref name="Giro"/> los grados que gira.
/// </summary>
public record Particula(
    string Glifo,
    Movimiento Movimiento,
    double X,
    double Y,
    double Dx,
    double Altura,
    double Giro,
    double Duracion,
    double Retraso,
    double Tamano,
    string? Color = null);

/// <summary>Genera las particulas de cada efecto. Con la misma semilla sale siempre lo mismo; cada pulsacion usa una distinta.</summary>
public static class EfectosLogro
{
    private static readonly string[] ColoresConfeti = ["#ffb347", "#ff7e5f", "#ffd27a", "#7fd1ae", "#6fb7ff", "#ff9ecb", "#ffffff"];

    public static IReadOnlyList<Particula> Generar(EfectoLogro efecto, int semilla)
    {
        var azar = new Random(semilla);

        double Entre(double min, double max) => min + azar.NextDouble() * (max - min);
        T Uno<T>(IReadOnlyList<T> lista) => lista[azar.Next(lista.Count)];
        double Signo() => azar.Next(2) == 0 ? -1 : 1;

        return efecto switch
        {
            // Salen de abajo, suben y rebotan: "cocina con verduras saltando por la pantalla".
            EfectoLogro.Verduras => Crear(22, _ => new Particula(
                Uno(["🥕", "🍅", "🧅", "🥬", "🍄", "🥦", "🌽", "🥔", "🧄", "🌶️"]), Movimiento.Salto,
                X: Entre(4, 94), Y: 0, Dx: Entre(14, 42) * Signo(), Altura: Entre(42, 86), Giro: Entre(360, 900) * Signo(),
                Duracion: Entre(2.4, 3.4), Retraso: Entre(0, 1.5), Tamano: Entre(2.6, 4.8))),

            EfectoLogro.Palabras => Crear(14, _ => new Particula(
                Uno(["Hello!", "Homework", "Teacher", "Good job!", "Class rep", "Yes!", "Let's go", "Well done", "A+", "English", "Please", "Thank you"]),
                Movimiento.Pop,
                X: Entre(4, 72), Y: Entre(8, 82), Dx: 0, Altura: 0, Giro: Entre(-12, 12),
                Duracion: Entre(2.1, 2.8), Retraso: Entre(0, 2), Tamano: Entre(1.8, 3.6), Color: Uno(["#ffffff", "#ffe27a", "#bfe6ff"]))),

            EfectoLogro.Tiempo => Crear(46, i => new Particula(
                i % 11 == 0 ? "⏳" : azar.Next(10).ToString(), Movimiento.Caer,
                X: Entre(0, 98), Y: 0, Dx: Entre(-4, 4), Altura: 0, Giro: i % 11 == 0 ? Entre(-60, 60) : 0,
                Duracion: Entre(1.9, 3.3), Retraso: Entre(0, 1.7), Tamano: Entre(1.5, 3.2), Color: "#ffc978")),

            EfectoLogro.Corazones => Crear(28, _ => new Particula(
                Uno(["💛", "🧡", "❤️", "💖", "💗"]), Movimiento.Subir,
                X: Entre(4, 94), Y: 0, Dx: Entre(-9, 9), Altura: 0, Giro: 0,
                Duracion: Entre(2.8, 4.2), Retraso: Entre(0, 1.7), Tamano: Entre(1.7, 3.8))),

            EfectoLogro.Estrellas => Crear(30, _ => new Particula(
                Uno(["⭐", "✨", "🌟", "💫"]), Movimiento.Pop,
                X: Entre(2, 94), Y: Entre(4, 88), Dx: 0, Altura: 0, Giro: Entre(-25, 25),
                Duracion: Entre(1.6, 2.6), Retraso: Entre(0, 2), Tamano: Entre(1.6, 4))),

            EfectoLogro.Notas => Crear(24, _ => new Particula(
                Uno(["🎵", "🎶", "🎼", "🎸"]), Movimiento.Subir,
                X: Entre(4, 94), Y: 0, Dx: Entre(-12, 12), Altura: 0, Giro: 0,
                Duracion: Entre(2.8, 4.2), Retraso: Entre(0, 1.8), Tamano: Entre(1.8, 3.8))),

            // Flashes de camara: circulos de luz blanca que estallan y se apagan.
            EfectoLogro.Flashes => Crear(14, _ => new Particula(
                string.Empty, Movimiento.Pop,
                X: Entre(0, 90), Y: Entre(0, 85), Dx: 0, Altura: 0, Giro: 0,
                Duracion: Entre(0.45, 0.8), Retraso: Entre(0, 2.4), Tamano: Entre(14, 44) )),

            _ => Crear(54, _ => new Particula(
                string.Empty, Movimiento.Caer,
                X: Entre(0, 99), Y: 0, Dx: Entre(-8, 8), Altura: 0, Giro: Entre(360, 1000) * Signo(),
                Duracion: Entre(2.2, 3.6), Retraso: Entre(0, 1.4), Tamano: Entre(0.7, 1.3), Color: Uno(ColoresConfeti)))
        };
    }

    private static List<Particula> Crear(int cantidad, Func<int, Particula> fabrica) =>
        Enumerable.Range(0, cantidad).Select(fabrica).ToList();
}
