namespace CV.Pages.Marina.RedFlags;

public enum EstadoJuego { Esperando, Jugando, Perdido, Ganado }

/// <summary>Una casilla del buscaminas.</summary>
public sealed class Celda
{
    public bool Mina { get; set; }
    public bool Abierta { get; set; }
    public bool Bandera { get; set; }
    public bool Explotada { get; set; }

    /// <summary>Minas en las 8 casillas de alrededor.</summary>
    public int Vecinas { get; set; }

    /// <summary>Orden en que se descubre (distancia al clic o posicion de la mina); sirve para escalonar las animaciones.</summary>
    public int Orden { get; set; }
}

/// <summary>
/// Logica del buscaminas, sin nada de interfaz. La primera casilla descubierta nunca es mina (ni lo son sus vecinas).
/// Se gana poniendo la bandera exactamente sobre todas las minas, o descubriendo todas las casillas seguras.
/// </summary>
public sealed class Tablero
{
    private readonly Random _azar;

    public Tablero(int filas, int columnas, int minas, Random? azar = null)
    {
        Filas = filas;
        Columnas = columnas;
        Minas = minas;
        _azar = azar ?? Random.Shared;
        Celdas = [];
        Reiniciar();
    }

    public int Filas { get; }
    public int Columnas { get; }
    public int Minas { get; }

    public Celda[] Celdas { get; private set; }
    public EstadoJuego Estado { get; private set; }

    public Celda this[int fila, int columna] => Celdas[fila * Columnas + columna];

    public int Banderas => Celdas.Count(c => c.Bandera);

    /// <summary>Minas que faltan por marcar (puede ser negativo si hay banderas de mas).</summary>
    public int MinasRestantes => Minas - Banderas;

    public bool Terminado => Estado is EstadoJuego.Perdido or EstadoJuego.Ganado;

    public void Reiniciar()
    {
        Celdas = Enumerable.Range(0, Filas * Columnas).Select(_ => new Celda()).ToArray();
        Estado = EstadoJuego.Esperando;
    }

    public void Descubrir(int fila, int columna)
    {
        if (Terminado || !Dentro(fila, columna))
        {
            return;
        }

        var celda = this[fila, columna];
        if (celda.Abierta || celda.Bandera)
        {
            return;
        }

        if (Estado == EstadoJuego.Esperando)
        {
            ColocarMinas(fila, columna);
            Estado = EstadoJuego.Jugando;
        }

        if (celda.Mina)
        {
            Perder(celda);
            return;
        }

        Inundar(fila, columna);
        ComprobarVictoria();
    }

    public void AlternarBandera(int fila, int columna)
    {
        if (Terminado || !Dentro(fila, columna))
        {
            return;
        }

        var celda = this[fila, columna];
        if (celda.Abierta)
        {
            return;
        }

        celda.Bandera = !celda.Bandera;
        ComprobarVictoria();
    }

    private bool Dentro(int fila, int columna) => fila >= 0 && fila < Filas && columna >= 0 && columna < Columnas;

    private IEnumerable<(int Fila, int Columna)> Vecinos(int fila, int columna)
    {
        for (var df = -1; df <= 1; df++)
        {
            for (var dc = -1; dc <= 1; dc++)
            {
                if ((df != 0 || dc != 0) && Dentro(fila + df, columna + dc))
                {
                    yield return (fila + df, columna + dc);
                }
            }
        }
    }

    /// <summary>Reparte las minas al azar dejando libre la casilla pulsada y sus vecinas, y cuenta las vecinas de cada casilla.</summary>
    private void ColocarMinas(int fila, int columna)
    {
        var prohibidas = Vecinos(fila, columna).Append((fila, columna)).Select(p => p.Item1 * Columnas + p.Item2).ToHashSet();
        var candidatas = Enumerable.Range(0, Celdas.Length).Where(i => !prohibidas.Contains(i)).OrderBy(_ => _azar.Next()).Take(Minas);

        foreach (var indice in candidatas)
        {
            Celdas[indice].Mina = true;
        }

        for (var f = 0; f < Filas; f++)
        {
            for (var c = 0; c < Columnas; c++)
            {
                this[f, c].Vecinas = Vecinos(f, c).Count(v => this[v.Fila, v.Columna].Mina);
            }
        }
    }

    /// <summary>Abre la casilla y, si no tiene minas alrededor, se expande en ondas hasta los numeros.</summary>
    private void Inundar(int fila, int columna)
    {
        var cola = new Queue<(int Fila, int Columna, int Distancia)>();
        cola.Enqueue((fila, columna, 0));
        this[fila, columna].Abierta = true;
        this[fila, columna].Orden = 0;

        while (cola.Count > 0)
        {
            var (f, c, distancia) = cola.Dequeue();
            if (this[f, c].Vecinas != 0)
            {
                continue;
            }

            foreach (var (vf, vc) in Vecinos(f, c))
            {
                var vecina = this[vf, vc];
                if (vecina.Abierta || vecina.Bandera)
                {
                    continue;
                }

                vecina.Abierta = true;
                vecina.Orden = distancia + 1;
                cola.Enqueue((vf, vc, distancia + 1));
            }
        }
    }

    private void Perder(Celda explotada)
    {
        Estado = EstadoJuego.Perdido;
        explotada.Explotada = true;

        var orden = 1;
        foreach (var mina in Celdas.Where(c => c.Mina && c != explotada && !c.Bandera))
        {
            mina.Orden = orden++;
        }
    }

    private void ComprobarVictoria()
    {
        if (Estado != EstadoJuego.Jugando)
        {
            return;
        }

        var seguras = Celdas.Where(c => !c.Mina).All(c => c.Abierta);
        var banderasBien = Celdas.All(c => c.Bandera == c.Mina);

        if (banderasBien || seguras)
        {
            foreach (var mina in Celdas.Where(c => c.Mina))
            {
                mina.Bandera = true;
            }

            Estado = EstadoJuego.Ganado;
        }
    }
}
