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
    /// <summary>Zzz, lunas y estrellas flotando hacia arriba, como un sueno.</summary>
    Sueno,
    /// <summary>Lluvia de numeros dorados y relojes de arena.</summary>
    Tiempo,
    /// <summary>Corazones subiendo.</summary>
    Corazones,
    /// <summary>Estrellas y destellos que aparecen y se apagan.</summary>
    Estrellas,
    /// <summary>Notas musicales subiendo.</summary>
    Notas,
    /// <summary>Flashes de camara de fotos.</summary>
    Flashes,
    /// <summary>Attack on Titan: pisadas gigantes, espadas, vapor, muros que caen y el grito de guerra.</summary>
    Titanes,
    /// <summary>Dados, fichas, cartas y piezas de ajedrez saltando, con frases de juego de mesa.</summary>
    JuegosDeMesa,
    /// <summary>Consola de PowerShell: comandos azules cayendo, prompts y calaveras subiendo.</summary>
    Consola,
    /// <summary>Un cumplido: caritas y brillos subiendo, y piropos que aparecen por la pantalla.</summary>
    Elogio,
    /// <summary>El millon de segundos: ceros dorados cayendo, "1.000.000" y cronometros.</summary>
    Millon
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
                Uno(["🥕", "🍅", "🧅", "🥬", "🍄", "🥦", "🌽", "🥔", "🧄", "🌶️", "🔥", "🍳", "🧯"]), Movimiento.Salto,
                X: Entre(4, 94), Y: 0, Dx: Entre(14, 42) * Signo(), Altura: Entre(42, 86), Giro: Entre(360, 900) * Signo(),
                Duracion: Entre(2.4, 3.4), Retraso: Entre(0, 1.5), Tamano: Entre(2.6, 4.8))),

            EfectoLogro.Palabras => Crear(14, _ => new Particula(
                Uno(["Hello!", "Homework", "Teacher", "Good job!", "Class rep", "Yes!", "Let's go", "Well done", "A+", "English", "Please", "Thank you"]),
                Movimiento.Pop,
                X: Entre(4, 72), Y: Entre(8, 82), Dx: 0, Altura: 0, Giro: Entre(-12, 12),
                Duracion: Entre(2.1, 2.8), Retraso: Entre(0, 2), Tamano: Entre(1.8, 3.6), Color: Uno(["#ffffff", "#ffe27a", "#bfe6ff"]))),

            EfectoLogro.Sueno => Crear(26, _ => new Particula(
                Uno(["💤", "Z", "z", "🌙", "✨", "⭐", "💬", "🥱", "😴"]), Movimiento.Subir,
                X: Entre(4, 94), Y: 0, Dx: Entre(-10, 10), Altura: 0, Giro: 0,
                Duracion: Entre(3.4, 4.6), Retraso: Entre(0, 1.9), Tamano: Entre(2, 4.6), Color: "#cfd8ff")),

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

            // Attack on Titan: el suelo tiembla con pisadas gigantes, vuelan espadas, sube vapor y caen trozos de muro.
            EfectoLogro.Titanes => Mezclar(
                Crear(5, i => new Particula("👣", Movimiento.Pop,
                    X: Entre(8, 68), Y: Entre(12, 58), Dx: 0, Altura: 0, Giro: Entre(-14, 14),
                    Duracion: Entre(1.5, 2), Retraso: i * 0.55, Tamano: Entre(8, 13))),
                Crear(3, i => new Particula(Uno(["¡Tatakae!", "¡Shinzō wo Sasageyo!", "¡Adelante!"]), Movimiento.Pop,
                    X: Entre(6, 48), Y: Entre(60, 82), Dx: 0, Altura: 0, Giro: Entre(-6, 6),
                    Duracion: Entre(1.9, 2.4), Retraso: 0.4 + i * 0.9, Tamano: Entre(3.4, 5), Color: "#ff4d4d")),
                Crear(10, _ => new Particula(Uno(["⚔️", "🗡️"]), Movimiento.Salto,
                    X: Entre(4, 94), Y: 0, Dx: Entre(14, 36) * Signo(), Altura: Entre(40, 78), Giro: Entre(360, 800) * Signo(),
                    Duracion: Entre(2.2, 3), Retraso: Entre(0, 1.6), Tamano: Entre(2.4, 4))),
                Crear(12, _ => new Particula("💨", Movimiento.Subir,
                    X: Entre(2, 96), Y: 0, Dx: Entre(-8, 8), Altura: 0, Giro: 0,
                    Duracion: Entre(2.6, 3.8), Retraso: Entre(0, 1.9), Tamano: Entre(3, 6))),
                Crear(9, _ => new Particula("🧱", Movimiento.Caer,
                    X: Entre(2, 96), Y: 0, Dx: Entre(-6, 6), Altura: 0, Giro: Entre(-200, 200),
                    Duracion: Entre(1.6, 2.6), Retraso: Entre(0.2, 2), Tamano: Entre(2, 3.8)))),

            // Juegos de mesa: dados, fichas y cartas que saltan por la mesa, y frases de partida.
            EfectoLogro.JuegosDeMesa => Mezclar(
                Crear(24, _ => new Particula(Uno(["🎲", "🎲", "♟️", "🃏", "🧩", "🎯", "🀄"]), Movimiento.Salto,
                    X: Entre(4, 94), Y: 0, Dx: Entre(14, 40) * Signo(), Altura: Entre(38, 80), Giro: Entre(360, 1000) * Signo(),
                    Duracion: Entre(2.2, 3.2), Retraso: Entre(0, 1.6), Tamano: Entre(2.4, 4.6))),
                Crear(4, i => new Particula(Uno(["¡Tu turno!", "¡Doble seis!", "¡Jaque!", "¡Otra partida!"]), Movimiento.Pop,
                    X: Entre(6, 60), Y: Entre(10, 80), Dx: 0, Altura: 0, Giro: Entre(-10, 10),
                    Duracion: Entre(1.8, 2.4), Retraso: 0.5 + i * 0.6, Tamano: Entre(2.6, 4.2), Color: Uno(["#ffffff", "#ffe27a", "#9be7c4"])))),

            // Tecnico de sistemas: comandos de PowerShell en azul cayendo como lluvia de consola, prompts y calaveras.
            EfectoLogro.Consola => Mezclar(
                Crear(28, _ => new Particula(
                    Uno(["Get-Process", "Write-Host", "Get-Help", "Set-Location", "Get-ChildItem", "| Select", "$_", "-Force", "Out-Null", "0", "1", "{ }", "ls"]),
                    Movimiento.Caer,
                    X: Entre(0, 92), Y: 0, Dx: Entre(-2, 2), Altura: 0, Giro: 0,
                    Duracion: Entre(2, 3.4), Retraso: Entre(0, 1.8), Tamano: Entre(1.2, 2.4), Color: Uno(["#3b9cff", "#7cc0ff", "#ffffff"]))),
                Crear(4, i => new Particula(i == 0 ? "Write-Host \"¡Funciona!\"" : "PS C:\\>", Movimiento.Pop,
                    X: Entre(8, 52), Y: Entre(14, 76), Dx: 0, Altura: 0, Giro: Entre(-4, 4),
                    Duracion: Entre(1.9, 2.5), Retraso: 0.3 + i * 0.7, Tamano: Entre(2.6, 4), Color: i == 0 ? "#7dff9a" : "#cfe8ff")),
                Crear(14, _ => new Particula(Uno(["💀", "☠️"]), Movimiento.Subir,
                    X: Entre(2, 96), Y: 0, Dx: Entre(-9, 9), Altura: 0, Giro: 0,
                    Duracion: Entre(2.8, 4), Retraso: Entre(0, 1.9), Tamano: Entre(2, 4.4)))),

            // Primera cosa positiva: un cumplido. Caritas y brillos suben, y aparecen piropos.
            EfectoLogro.Elogio => Mezclar(
                Crear(22, _ => new Particula(Uno(["😊", "🥰", "😄", "✨", "💖", "🌟", "👏", "💐"]), Movimiento.Subir,
                    X: Entre(4, 94), Y: 0, Dx: Entre(-10, 10), Altura: 0, Giro: 0,
                    Duracion: Entre(2.8, 4.2), Retraso: Entre(0, 1.8), Tamano: Entre(2, 4.4))),
                Crear(7, i => new Particula(Uno(["¡Qué guapa!", "¡Increíble!", "¡Genial!", "Eres top", "¡Bravo!", "Qué bien lo haces"]), Movimiento.Pop,
                    X: Entre(4, 62), Y: Entre(10, 82), Dx: 0, Altura: 0, Giro: Entre(-10, 10),
                    Duracion: Entre(1.9, 2.5), Retraso: 0.2 + i * 0.4, Tamano: Entre(2.4, 4), Color: Uno(["#ffffff", "#ffd1e3", "#ffe27a"])))),

            // 1.000.000 de segundos: lluvia de ceros dorados, el numero en grande y cronometros.
            EfectoLogro.Millon => Mezclar(
                Crear(40, _ => new Particula("0", Movimiento.Caer,
                    X: Entre(0, 98), Y: 0, Dx: Entre(-3, 3), Altura: 0, Giro: 0,
                    Duracion: Entre(1.9, 3.2), Retraso: Entre(0, 1.9), Tamano: Entre(2, 5), Color: "#ffc978")),
                Crear(3, i => new Particula("1.000.000", Movimiento.Pop,
                    X: Entre(8, 34), Y: Entre(26, 56), Dx: 0, Altura: 0, Giro: Entre(-6, 6),
                    Duracion: Entre(1.9, 2.4), Retraso: 0.2 + i * 0.9, Tamano: Entre(5, 7), Color: "#ffe3a3")),
                Crear(2, i => new Particula("¡Como si nada!", Movimiento.Pop,
                    X: Entre(10, 50), Y: Entre(66, 84), Dx: 0, Altura: 0, Giro: Entre(-5, 5),
                    Duracion: Entre(1.9, 2.4), Retraso: 1 + i * 1, Tamano: Entre(2.6, 3.6), Color: "#ffffff")),
                Crear(8, _ => new Particula(Uno(["⏱️", "🕐", "⏲️"]), Movimiento.Subir,
                    X: Entre(4, 94), Y: 0, Dx: Entre(-8, 8), Altura: 0, Giro: 0,
                    Duracion: Entre(2.8, 3.8), Retraso: Entre(0, 1.8), Tamano: Entre(2, 3.8)))),

            _ => Crear(54, _ => new Particula(
                string.Empty, Movimiento.Caer,
                X: Entre(0, 99), Y: 0, Dx: Entre(-8, 8), Altura: 0, Giro: Entre(360, 1000) * Signo(),
                Duracion: Entre(2.2, 3.6), Retraso: Entre(0, 1.4), Tamano: Entre(0.7, 1.3), Color: Uno(ColoresConfeti)))
        };
    }

    private static List<Particula> Crear(int cantidad, Func<int, Particula> fabrica) =>
        Enumerable.Range(0, cantidad).Select(fabrica).ToList();

    /// <summary>Junta varios grupos de particulas (para los efectos que mezclan movimientos distintos).</summary>
    private static List<Particula> Mezclar(params List<Particula>[] grupos) =>
        [.. grupos.SelectMany(grupo => grupo)];
}
