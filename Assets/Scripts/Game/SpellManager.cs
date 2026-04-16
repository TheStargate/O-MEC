using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// CLASE BASE
// Todos los hechizos heredan de aquí e implementan Lanzar().
// cell: la casilla sobre la que se arrastra la carta al soltarla.
// Usar SpellManager.Crear(nombre) para obtener el hechizo correspondiente.
// ─────────────────────────────────────────────────────────────────────────────
public abstract class SpellManager
{
    // Ejecuta el efecto del hechizo.
    // Devuelve true si el lanzamiento fue válido (para que CardUI descarte la carta).
    public abstract bool Lanzar(Cell casilla);

    // Devuelve el SpellManager correspondiente al nombre de la carta.
    public static SpellManager Crear(string nombreHechizo)
    {
        return nombreHechizo switch
        {
            "Lentitud eterna"    => new SpellLentitudEterna(),
            "Virus"              => new SpellVirus(),
            "Muerte instantánea" => new SpellMuerteInstantanea(),
            "Flecha ardiente"    => new SpellFlechaArdiente(),
            "Caos"               => new SpellCaos(),
            "Explosivo"          => new SpellExplosivo(),
            "Bola de fuego"      => new SpellBolaFuego(),
            "Bomba nuclear"      => new SpellBombaNuclear(),
            "Impacto Solar"      => new SpellImpactoSolar(),
            _                    => null
        };
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// IMPLEMENTACIONES
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Lentitud eterna (coste 4): reduce la velocidad del monstruo objetivo a 1
/// durante el resto de la partida.
/// Objetivo: casilla ocupada por un monstruo enemigo.
/// </summary>
public class SpellLentitudEterna : SpellManager
{
    public override bool Lanzar(Cell casilla)
    {
        if (casilla == null || !casilla.ocupada) return false;

        Card carta = casilla.cartaActual;
        if (carta == null) return false;

        bool esEnemigo = carta.clickableObject.propietarioP1 == TurnManager.turnoP1;
        if (esEnemigo) return false; // solo afecta a cartas enemigas

        if (carta.cardData is MonsterCardData datos)
        {
            datos.velocidad = 1;
            Debug.Log($"[Lentitud eterna] {carta.name} reducido a velocidad 1.");
            return true;
        }
        return false;
    }
}

/// <summary>
/// Virus (coste 4): reduce el ataque del objetivo a la mitad (redondeado abajo).
/// Objetivo: casilla ocupada por cualquier carta enemiga con ataque.
/// </summary>
public class SpellVirus : SpellManager
{
    public override bool Lanzar(Cell casilla)
    {
        if (casilla == null || !casilla.ocupada) return false;

        Card carta = casilla.cartaActual;
        if (carta == null) return false;

        bool esEnemigo = carta.clickableObject.propietarioP1 == TurnManager.turnoP1;
        if (esEnemigo) return false;

        if (carta.cardData is DamageableCardData datos)
        {
            datos.ataque = Mathf.FloorToInt(datos.ataque / 2f);
            Debug.Log($"[Virus] {carta.name} ataque reducido a {datos.ataque}.");
            return true;
        }
        return false;
    }
}

/// <summary>
/// Muerte instantánea (coste 7): destruye al instante la carta enemiga objetivo.
/// Objetivo: casilla ocupada por cualquier carta enemiga (excepto castillo).
/// </summary>
public class SpellMuerteInstantanea : SpellManager
{
    public override bool Lanzar(Cell casilla)
    {
        if (casilla == null || !casilla.ocupada) return false;

        Card carta = casilla.cartaActual;
        if (carta == null) return false;

        bool esEnemigo = carta.clickableObject.propietarioP1 == TurnManager.turnoP1;
        if (esEnemigo) return false;

        if (carta.cardData.nombre.Equals("Castillo"))
        {
            Debug.Log("[Muerte instantánea] No puede destruir el castillo.");
            return false;
        }

        casilla.LiberarCasilla(false);
        Debug.Log($"[Muerte instantánea] {carta.name} destruida.");
        return true;
    }
}

/// <summary>
/// Flecha ardiente (coste 6): inflige daño fijo de 8 a la carta objetivo.
/// Objetivo: casilla ocupada por cualquier carta enemiga con vida.
/// </summary>
public class SpellFlechaArdiente : SpellManager
{
    private const int DANYO = 8;

    public override bool Lanzar(Cell casilla)
    {
        if (casilla == null || !casilla.ocupada) return false;

        Card carta = casilla.cartaActual;
        if (carta == null) return false;

        bool esEnemigo = carta.clickableObject.propietarioP1 == TurnManager.turnoP1;
        if (esEnemigo) return false;

        if (carta.cardData is DamageableCardData datos)
        {
            int nuevaVida = datos.vida - DANYO;
            if (nuevaVida <= 0)
            {
                casilla.LiberarCasilla(false);
                Debug.Log($"[Flecha ardiente] {carta.name} destruida.");
            }
            else
            {
                carta.UpdateVida(nuevaVida);
                Debug.Log($"[Flecha ardiente] {carta.name} recibe {DANYO} de daño. Vida: {nuevaVida}");
            }
            return true;
        }
        return false;
    }
}

/// <summary>
/// Caos (coste 7): intercambia los valores de ataque y vida de la carta objetivo.
/// Objetivo: casilla ocupada por cualquier carta enemiga con vida y ataque.
/// </summary>
public class SpellCaos : SpellManager
{
    public override bool Lanzar(Cell casilla)
    {
        if (casilla == null || !casilla.ocupada) return false;

        Card carta = casilla.cartaActual;
        if (carta == null) return false;

        bool esEnemigo = carta.clickableObject.propietarioP1 == TurnManager.turnoP1;
        if (esEnemigo) return false;

        if (carta.cardData is DamageableCardData datos)
        {
            (datos.vida, datos.ataque) = (datos.ataque, datos.vida);
            datos.vidaMaxima = datos.vida;
            carta.UpdateVida(datos.vida);
            Debug.Log($"[Caos] {carta.name}: ataque={datos.ataque}, vida={datos.vida}");
            return true;
        }
        return false;
    }
}

/// <summary>
/// Explosivo (coste 9): inflige 6 de daño a todas las cartas enemigas del tablero
/// que tengan vida.
/// </summary>
public class SpellExplosivo : SpellManager
{
    private const int DANYO = 6;

    public override bool Lanzar(Cell casilla)
    {
        // El hechizo afecta a todo el tablero; la casilla solo sirve para validar
        // que se ha soltado sobre el tablero (no null).
        if (casilla == null) return false;

        bool afecto = false;
        foreach (Cell cell in Board.Instance.cells)
        {
            if (!cell.ocupada || cell.cartaActual == null) continue;

            Card carta = cell.cartaActual;
            bool esEnemigo = carta.clickableObject.propietarioP1 == TurnManager.turnoP1;
            if (esEnemigo) continue;

            if (carta.cardData is DamageableCardData datos)
            {
                int nuevaVida = datos.vida - DANYO;
                if (nuevaVida <= 0)
                {
                    cell.LiberarCasilla(false);
                    Debug.Log($"[Explosivo] {carta.name} destruida.");
                }
                else
                {
                    carta.UpdateVida(nuevaVida);
                    Debug.Log($"[Explosivo] {carta.name} recibe {DANYO} de daño. Vida: {nuevaVida}");
                }
                afecto = true;
            }
        }
        return afecto;
    }
}

/// <summary>
/// Bola de fuego (coste 11): inflige 11 de daño a la carta objetivo y a cada 
/// carta adyacente (incluidas las propias).
/// Objetivo: cualquier casilla del tablero
/// </summary>
public class SpellBolaFuego : SpellManager
{
    private const int DANYO = 11;

    public override bool Lanzar(Cell casilla)
    {
        if (casilla == null) return false;

        // Aplicar daño a la casilla central
        AplicarDanyo(casilla, DANYO);

        // Daño en área (8 celdas adyacentes)
        int[] dFilas  = { -1, -1, -1, 0, 0, 1, 1, 1 };
        int[] dColums = { -1,  0,  1,-1, 1,-1, 0, 1 };

        for (int i = 0; i < dFilas.Length; i++)
        {
            int nf = casilla.row + dFilas[i];
            int nc = casilla.col + dColums[i];

            if (nf >= 0 && nf < Board.Instance.rows &&
                nc >= 0 && nc < Board.Instance.columns)
            {
                Cell adyacente = Board.Instance.cells[nf, nc];
                AplicarDanyo(adyacente, DANYO);
            }
        }
        return true;
    }

    private void AplicarDanyo(Cell cell, int danyo)
    {
        if (!cell.ocupada || cell.cartaActual == null) return;

        Card carta = cell.cartaActual;
        if (carta.cardData is DamageableCardData datos)
        {
            int nuevaVida = datos.vida - danyo;
            if (nuevaVida <= 0)
            {
                cell.LiberarCasilla(false);
                Debug.Log($"[Bola de fuego] {carta.name} destruida.");
            }
            else
            {
                carta.UpdateVida(nuevaVida);
                Debug.Log($"[Bola de fuego] {carta.name} recibe {danyo} de daño. Vida: {nuevaVida}");
            }
        }
    }
}

/// <summary>
/// Bomba nuclear (coste 17): destruye TODAS las cartas del tablero (de ambos
/// jugadores) excepto los castillos.
/// </summary>
public class SpellBombaNuclear : SpellManager
{
    public override bool Lanzar(Cell casilla)
    {
        if (casilla == null) return false;

        // Recogemos primero las celdas a limpiar para no modificar la colección mientras iteramos
        System.Collections.Generic.List<Cell> aDestruir = new();

        foreach (Cell cell in Board.Instance.cells)
        {
            if (!cell.ocupada || cell.cartaActual == null) continue;
            if (cell.cartaActual.cardData.nombre.Equals("Castillo")) continue;

            aDestruir.Add(cell);
        }

        foreach (Cell cell in aDestruir)
            cell.LiberarCasilla(false);

        Debug.Log($"[Bomba nuclear] {aDestruir.Count} cartas destruidas.");
        return true;
    }
}

/// <summary>
/// Impacto Solar (coste 8): Inflige 18 de daño en la casilla central y 7 en las adyacentes.
/// NO IMPLEMENTADO AÚN
/// </summary>
public class SpellImpactoSolar : SpellManager
{
    private const int DANYO_CENTRAL = 18;
    private const int DANYO_ADYACENTE = 7;

    public override bool Lanzar(Cell casilla)
    {
        if (casilla == null) return false;

        // Daño central
        AplicarDanyo(casilla, DANYO_CENTRAL);

        // Daño adyacente
        int[] dFilas = { -1, -1, -1, 0, 0, 1, 1, 1 };
        int[] dColums = { -1, 0, 1, -1, 1, -1, 0, 1 };

        for (int i = 0; i < dFilas.Length; i++)
        {
            int nf = casilla.row + dFilas[i];
            int nc = casilla.col + dColums[i];

            if (nf >= 0 && nf < Board.Instance.rows &&
                nc >= 0 && nc < Board.Instance.columns)
            {
                Cell adyacente = Board.Instance.cells[nf, nc];
                AplicarDanyo(adyacente, DANYO_ADYACENTE);
            }
        }
        return true;
    }

    private void AplicarDanyo(Cell cell, int danyo)
    {
        if (!cell.ocupada || cell.cartaActual == null) return;

        Card carta = cell.cartaActual;
        if (carta.cardData is DamageableCardData datos)
        {
            int nuevaVida = datos.vida - danyo;
            if (nuevaVida <= 0)
            {
                cell.LiberarCasilla(false);
            }
            else
            {
                carta.UpdateVida(nuevaVida);
            }
        }
    }
}

