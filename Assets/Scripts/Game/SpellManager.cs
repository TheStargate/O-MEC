using UnityEngine;
using System.Collections.Generic;

// ─────────────────────────────────────────────────────────────────────────────
// CLASE BASE
// Todos los hechizos heredan de aquí e implementan Lanzar().
// cell: la casilla sobre la que se arrastra la carta al soltarla.
// Usar SpellManager.Crear(nombre) para obtener el hechizo correspondiente.
// ─────────────────────────────────────────────────────────────────────────────
public abstract class SpellManager
{
    public SpellCardData data;

    // Ejecuta el efecto del hechizo.
    // Devuelve true si el lanzamiento fue válido (para que CardUI descarte la carta).
    public abstract bool Lanzar(Cell casilla);

    // Devuelve el SpellManager correspondiente al nombre de la carta.
    public static SpellManager Crear(SpellCardData data)
    {
        SpellManager sm = data.nombre switch
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
            "Debilitar"          => new SpellDebilitar(),
            "Intercambio"        => new SpellIntercambio(),
            _                    => null
        };
        if (sm != null) sm.data = data;
        return sm;
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
/// Virus (coste 4): la carta objetivo pierde 1 de vida en cada turno.
/// Objetivo: casilla ocupada por un monstruo enemigo con vida.
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

        if (carta.cardData is MonsterCardData datos)
        {
            datos.efectosDanyo.Add(new DanyoEfecto(1, -1));
            Debug.Log($"[Virus] {carta.name} infectado. Perderá 1 PV por turno.");
            return true;
        }
        return false;
    }
}

/// <summary>
/// Debilitar (coste 4): reduce el ataque del objetivo a la mitad (redondeado abajo).
/// Objetivo: casilla ocupada por cualquier carta enemiga con ataque.
/// NO IMPLEMENTADO AÚN
/// </summary>
public class SpellDebilitar : SpellManager
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
            Debug.Log($"[Debilitar] {carta.name} ataque reducido a {datos.ataque}.");
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

        if (!(carta.cardData is MonsterCardData))
        {
            Debug.Log("[Muerte instantánea] Solo puede destruir monstruos.");
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
    private const int DANYO_INICIAL = 9;
    private const int DANYO_QUEMADURA = 3;
    private const int TURNOS_QUEMADURA = 5;

    public override bool Lanzar(Cell casilla)
    {
        if (casilla == null || !casilla.ocupada) return false;

        Card carta = casilla.cartaActual;
        if (carta == null) return false;

        bool esEnemigo = carta.clickableObject.propietarioP1 == TurnManager.turnoP1;
        if (esEnemigo) return false;

        if (carta.cardData is MonsterCardData datos)
        {
            datos.efectosDanyo.Add(new DanyoEfecto(DANYO_QUEMADURA, TURNOS_QUEMADURA));

            int nuevaVida = datos.vida - DANYO_INICIAL;
            Debug.Log($"[Flecha ardiente] {carta.name} recibe {DANYO_INICIAL} de daño e inicia quemadura ({DANYO_QUEMADURA} PV x {TURNOS_QUEMADURA} turnos).");
            
            if (nuevaVida <= 0)
            {
                casilla.LiberarCasilla(false);
                Debug.Log($"[Flecha ardiente] {carta.name} destruida.");
            }
            else
            {
                carta.UpdateVida(nuevaVida);
            }
            return true;
        }
        return false;
    }
}

/// <summary>
/// Caos (coste 7): destruye todos los monstruos del tablero (propios y enemigos)
/// y resta 10 PV a todos los castillos del tablero.
/// </summary>
public class SpellCaos : SpellManager
{
    public override bool Lanzar(Cell casilla)
    {
        if (casilla == null) return false;

        bool tieneEfecto = false;
        List<Cell> aDestruir = new();

        foreach (Cell cell in Board.Instance.cells)
        {
            if (!cell.ocupada || cell.cartaActual == null) continue;

            Card carta = cell.cartaActual;

            if (carta.cardData.nombre.Equals("Castillo"))
            {
                if (carta.cardData is DamageableCardData datos)
                {
                    int nuevaVida = datos.vida - 10;
                    if (nuevaVida <= 0)
                    {
                        cell.LiberarCasilla(false);
                    }
                    else
                    {
                        carta.UpdateVida(nuevaVida);
                    }
                    tieneEfecto = true;
                }
            }
            else if (carta.cardData is MonsterCardData)
            {
                // Destruye todos los monstruos (tanto aliados como enemigos)
                aDestruir.Add(cell);
                tieneEfecto = true;
            }
        }

        foreach (Cell cell in aDestruir)
            cell.LiberarCasilla(false);

        Debug.Log($"[Caos] {aDestruir.Count} monstruos destruidos y daño a castillos aplicado.");
        return tieneEfecto;
    }
}

/// <summary>
/// Intercambio (coste 7): intercambia los valores de ataque y vida de la carta objetivo.
/// Objetivo: casilla ocupada por cualquier carta enemiga con vida y ataque.
/// NO IMPLEMENTADO AÚN
/// </summary>
public class SpellIntercambio : SpellManager
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
            Debug.Log($"[Intercambio] {carta.name}: ataque={datos.ataque}, vida={datos.vida}");
            return true;
        }
        return false;
    }
}

/// <summary>
/// Explosivo (coste 9): inflige 10 de daño en un área de 5x5.
/// El castillo solo recibe 3 de daño.
/// </summary>
public class SpellExplosivo : SpellManager
{
    private const int DANYO_NORMAL = 10;
    private const int DANYO_CASTILLO = 3;

    public override bool Lanzar(Cell casilla)
    {
        if (casilla == null) return false;

        bool tieneEfecto = false;
        int radio = data.radioArea;
        // El área centrada en la casilla según el radio definido en la carta
        for (int df = -radio; df <= radio; df++)
        {
            for (int dc = -radio; dc <= radio; dc++)
            {
                int nf = casilla.row + df;
                int nc = casilla.col + dc;

                if (nf >= 0 && nf < Board.Instance.rows &&
                    nc >= 0 && nc < Board.Instance.columns)
                {
                    Cell objetivo = Board.Instance.cells[nf, nc];
                    if (AplicarDanyoExplosivo(objetivo)) tieneEfecto = true;
                }
            }
        }
        return tieneEfecto;
    }

    private bool AplicarDanyoExplosivo(Cell cell)
    { // Aplica el daño de la bomba explosiva a la carta objetivo
        if (!cell.ocupada || cell.cartaActual == null) return false;

        Card carta = cell.cartaActual;
        if (carta.cardData is DamageableCardData datos)
        {
            int danyo = carta.cardData.nombre.Equals("Castillo") ? DANYO_CASTILLO : DANYO_NORMAL;

            int nuevaVida = datos.vida - danyo;
            Debug.Log($"[Explosivo] {carta.name} recibe {danyo} de daño.");
            
            if (nuevaVida <= 0)
            {
                cell.LiberarCasilla(false);
            }
            else
            {
                carta.UpdateVida(nuevaVida);
            }
            return true;
        }
        return false;
    }
}

/// <summary>
/// Bola de fuego (coste 11): inflige 16 de daño a la carta objetivo y adyacentes.
/// Los edificios reciben 7 de daño extra el siguiente turno (el castillo solo 4 inicial y 2 extra).
/// </summary>
public class SpellBolaFuego : SpellManager
{
    private const int DANYO_GENERAL = 16;
    private const int DANYO_CASTILLO_INICIAL = 4;
    private const int DANYO_EXTRA_EDIFICIOS = 7;
    private const int DANYO_EXTRA_CASTILLO = 2;

    public override bool Lanzar(Cell casilla)
    {
        if (casilla == null) return false;

        bool tieneEfecto = false;
        int radio = data.radioArea;

        for (int df = -radio; df <= radio; df++)
        {
            for (int dc = -radio; dc <= radio; dc++)
            {
                int nf = casilla.row + df;
                int nc = casilla.col + dc;

                if (nf >= 0 && nf < Board.Instance.rows &&
                    nc >= 0 && nc < Board.Instance.columns)
                {
                    Cell objetivo = Board.Instance.cells[nf, nc];
                    if (AplicarDanyoBola(objetivo)) tieneEfecto = true;
                }
            }
        }
        return tieneEfecto;
    }

    private bool AplicarDanyoBola(Cell cell)
    { // Aplica el daño de la bola de fuego a la carta objetivo
        if (!cell.ocupada || cell.cartaActual == null) return false;

        Card carta = cell.cartaActual;
        if (carta.cardData is DamageableCardData datos)
        {
            int danyoInicial = DANYO_GENERAL;
            bool esCastillo = carta.cardData.nombre.Equals("Castillo");
            bool esEdificio = carta.cardData is StructureCardData || esCastillo;

            if (esCastillo)
            {
                danyoInicial = DANYO_CASTILLO_INICIAL;
                datos.efectosDanyo.Add(new DanyoEfecto(DANYO_EXTRA_CASTILLO, 1));
            }
            else if (esEdificio)
            {
                datos.efectosDanyo.Add(new DanyoEfecto(DANYO_EXTRA_EDIFICIOS, 1));
            }

            int nuevaVida = datos.vida - danyoInicial;
            Debug.Log($"[Bola de fuego] {carta.name} recibe {danyoInicial} de daño inicial.");
            
            if (nuevaVida <= 0)
            {
                cell.LiberarCasilla(false);
            }
            else
            {
                carta.UpdateVida(nuevaVida);
            }
            return true;
        }
        return false;
    }
}

/// <summary>
/// Bomba nuclear (coste 17): todas las estructuras enemigas (excepto castillo) pierden 35 PV. 
/// El castillo enemigo pierde 10 PV.
/// </summary>
public class SpellBombaNuclear : SpellManager
{
    public override bool Lanzar(Cell casilla)
    {
        if (casilla == null) return false;

        bool tieneEfecto = false; // Indica si el hechizo ha afectado a alguna carta
        foreach (Cell cell in Board.Instance.cells)
        {
            if (!cell.ocupada || cell.cartaActual == null) continue;

             Card carta = cell.cartaActual;
             bool esEnemigo = carta.clickableObject.propietarioP1 != TurnManager.turnoP1;
             if (!esEnemigo) continue;

             if (carta.cardData.nombre.Equals("Castillo"))
             {
                 if (carta.cardData is DamageableCardData datos)
                 {
                     int nuevaVida = datos.vida - 10;
                     if (nuevaVida <= 0) cell.LiberarCasilla(false);
                     else carta.UpdateVida(nuevaVida);
                     tieneEfecto = true;
                 }
             }
             else if (carta.cardData is StructureCardData)
             {
                 if (carta.cardData is DamageableCardData datos)
                 {
                     int nuevaVida = datos.vida - 35;
                     if (nuevaVida <= 0) cell.LiberarCasilla(false);
                     else carta.UpdateVida(nuevaVida);
                     tieneEfecto = true;
                 }
             }
        }
        return tieneEfecto;
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

        bool tieneEfecto = false;
        int radio = data.radioArea;

        for (int df = -radio; df <= radio; df++)
        {
            for (int dc = -radio; dc <= radio; dc++)
            {
                int nf = casilla.row + df;
                int nc = casilla.col + dc;

                if (nf >= 0 && nf < Board.Instance.rows &&
                    nc >= 0 && nc < Board.Instance.columns)
                {
                    Cell adyacente = Board.Instance.cells[nf, nc];
                    if (AplicarDanyo(adyacente, (df == 0 && dc == 0) ? DANYO_CENTRAL : DANYO_ADYACENTE)) 
                        tieneEfecto = true;
                }
            }
        }
        return tieneEfecto;
    }

    private bool AplicarDanyo(Cell cell, int danyo)
    { // 
        if (!cell.ocupada || cell.cartaActual == null) return false;

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
            return true;
        }
        return false;
    }
}

