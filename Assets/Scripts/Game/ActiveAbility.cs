using System.Collections.Generic;
using UnityEngine;

public abstract class ActiveAbility
{
    public Card portador;

    // Si es true, el jugador debe hacer click en una carta del tablero
    public virtual bool RequiereObjetivo => false; 

    // Cantidad de objetivos a elegir si RequiereObjetivo es true
    public virtual int NumObjetivos => 1;

    public virtual void Inicializar(Card portador)
    {
        this.portador = portador;
    }

    // Comprueba si la carta clickada es un objetivo válido
    public virtual bool EsObjetivoValido(Card objetivo) => true;

    // Ejecuta la lógica. 'objetivos' contendrá las cartas elegidas si RequiereObjetivo es true.
    public abstract void Ejecutar(List<Card> objetivos = null);

    // Método factoría para instanciar la habilidad correcta
    public static ActiveAbility Crear(Card carta)
    {
        if (carta == null || carta.cardData == null)
            return null;

        string rawName = carta.cardData.nombre;
        if (string.IsNullOrWhiteSpace(rawName))
            return null;

        string nombre = rawName.Replace("(Clone)", "").Trim().Normalize(System.Text.NormalizationForm.FormC);

        // Monstruos
        if (nombre == "Guerrero oscuro") return new ActiveGuerreroOscuro();
        if (nombre == "Guerrero acorazado") return new ActiveGuerreroAcorazado();
        if (nombre == "Mago") return new ActiveMago();
        if (nombre == "Esqueleto gigante") return new ActiveEsqueletoGigante();
        if (nombre == "Cura oscuro") return new ActiveCuraOscuro();
        if (nombre == "Dragón de agua") return new ActiveDragonAgua();
        if (nombre == "Dragón de fuego") return new ActiveDragonFuego();
        if (nombre == "Ninja ") return new ActiveNinja();
        if (nombre == "Cura protector") return new ActiveCuraProtector();
        if (nombre == "Arquero largo") return new ActiveArqueroLargo();
        if (nombre == "Bebé dragón") return new ActiveBebeDragon();
        if (nombre == "Tanque") return new ActiveTanque();
        if (nombre == "Caballero") return new ActiveCaballero();
        if (nombre == "Arquero reforzado") return new ActiveArqueroReforzado();
        if (nombre == "Cura") return new ActiveCura();
        if (nombre == "Arquero") return new ActiveArquero();
        if (nombre == "Guerrero") return new ActiveGuerrero();
        if (nombre == "Esqueleto quemado") return new ActiveEsqueletoQuemado();
        if (nombre == "Espía") return new ActiveEspia();
        if (nombre == "Esqueleto") return new ActiveEsqueleto();

        // Estructuras
        if (nombre == "Castillo") return new ActiveCastillo();
        if (nombre == "Torre infernal") return new ActiveTorreInfernal();
        if (nombre == "Torre protectora") return new ActiveTorreProtectora();
        if (nombre == "Torreta destructora") return new ActiveTorretaDestructora();
        if (nombre == "Muro reforzado") return new ActiveMuroReforzado();
        if (nombre == "Torre mágica") return new ActiveTorreMagica();
        if (nombre == "Casa de constructor") return new ActiveCasaConstructor();
        if (nombre == "Herrería") return new ActiveHerreria();
        if (nombre == "Castillo falso") return new ActiveCastilloFalso();
        if (nombre == "Torreta") return new ActiveTorreta();
        if (nombre == "Muro") return new ActiveMuro();

        // Trampas
        if (nombre == "Pinchos") return new ActivePinchos();
        if (nombre == "Bombas") return new ActiveBombas();
        if (nombre == "Trampa ígnea") return new ActiveTrampaIgnea();
        if (nombre == "Bomba") return new ActiveBomba();
        if (nombre == "Trampa eléctrica") return new ActiveTrampaElectrica();
        if (nombre == "Clavos") return new ActiveClavos();

        return null; // Si no tiene habilidad activa programada
    }

    // Helper: Aplica daño a una celda
    protected void AplicarDanyoHelper(Cell cell, int danyo)
    {
        if (cell == null || !cell.ocupada || cell.cartaActual == null) return;
        if (PassiveAbility.EsInvulnerableATodo(cell.cartaActual)) return;
        if (cell.cartaActual.cardData is DamageableCardData dData)
        {
            int danyoFinal = cell.cartaActual.pasiva?.OnRecibirDanyo(danyo) ?? danyo;
            int nuevaVida  = dData.vida - danyoFinal;
            if (nuevaVida <= 0)
                cell.LiberarCasilla(false);
            else
                cell.cartaActual.UpdateVida(nuevaVida);
        }
    }
}

// ---------------- MONSTRUOS ----------------

public class ActiveGuerreroOscuro : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null)
    {
        for (int dr = -1; dr <= 1; dr++)
        {
            for (int dc = -1; dc <= 1; dc++)
            {
                if (dr == 0 && dc == 0) continue;
                int r = portador.casilla.row + dr, c = portador.casilla.col + dc;
                if (r >= 0 && r < Board.Instance.rows && c >= 0 && c < Board.Instance.columns)
                {
                    Cell cell = Board.Instance.cells[r, c];
                    if (cell.ocupada && cell.cartaActual != null && cell.cartaActual.cardData is MonsterCardData)
                    {
                        AplicarDanyoHelper(cell, 5);
                    }
                }
            }
        }
    }
}

public class ActiveGuerreroAcorazado : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null) => portador.multDanyoProximoAtaque = 2;
}

public class ActiveMago : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null) => portador.multDanyoProximoAtaque = 3;
}

public class ActiveEsqueletoGigante : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null)
    {
        if (portador.cardData is DamageableCardData dData)
            portador.UpdateVida(dData.vidaMaxima);
    }
}

public class ActiveCuraOscuro : ActiveAbility
{
    public override bool RequiereObjetivo => true;
    public override bool EsObjetivoValido(Card objetivo) => objetivo.cardData is MonsterCardData;
    
    public override void Ejecutar(List<Card> objetivos = null)
    {
        if (objetivos != null && objetivos.Count > 0)
            objetivos[0].multDanyoIndefinido = 2;
    }
}

public class ActiveDragonAgua : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null)
    {
        // Cura toda su vida
        if (portador.cardData is DamageableCardData dData)
            portador.UpdateVida(dData.vidaMaxima);

        // Enemigos a su alcance pierden 1 punto
        int alcance = (portador.cardData as DamageableCardData)?.alcance ?? 0;
        bool esP1 = portador.clickableObject.propietarioP1;

        foreach (Cell cell in Board.Instance.cells)
        {
            if (cell.ocupada && cell.cartaActual != null && cell.cartaActual.cardData is MonsterCardData &&
                cell.cartaActual.clickableObject.propietarioP1 != esP1)
            {
                int dist = Mathf.Max(Mathf.Abs(cell.row - portador.casilla.row), Mathf.Abs(cell.col - portador.casilla.col));
                if (dist <= alcance)
                {
                    AplicarDanyoHelper(cell, 1);
                }
            }
        }
    }
}

public class ActiveDragonFuego : ActiveAbility
{
    public override bool RequiereObjetivo => true;
    public override int NumObjetivos => 3;
    public override bool EsObjetivoValido(Card objetivo)
    {
        int alcance = (portador.cardData as DamageableCardData)?.alcance ?? 0;
        int dist = Mathf.Max(Mathf.Abs(objetivo.casilla.row - portador.casilla.row), Mathf.Abs(objetivo.casilla.col - portador.casilla.col));
        return objetivo.cardData is MonsterCardData && dist <= alcance;
    }

    public override void Ejecutar(List<Card> objetivos = null)
    {
        if (objetivos != null)
        {
            foreach (Card obj in objetivos)
            {
                if (obj.casilla != null)
                    AplicarDanyoHelper(obj.casilla, 5);
            }
        }
    }
}

public class ActiveNinja : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null) => portador.invulnerableHastaProximoTurno = true;
}

public class ActiveCuraProtector : ActiveAbility
{
    public override bool RequiereObjetivo => true;
    public override bool EsObjetivoValido(Card objetivo) => objetivo.cardData is MonsterCardData;

    public override void Ejecutar(List<Card> objetivos = null)
    {
        if (objetivos != null && objetivos.Count > 0)
            objetivos[0].inmuneHechizosIndefinido = true;
    }
}

public class ActiveArqueroLargo : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null) => portador.bonusDanyoProximoAtaque = 3;
}

public class ActiveBebeDragon : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null)
    {
        int alcance = (portador.cardData as DamageableCardData)?.alcance ?? 0;
        bool esP1 = portador.clickableObject.propietarioP1;

        foreach (Cell cell in Board.Instance.cells)
        {
            if (cell.ocupada && cell.cartaActual != null && cell.cartaActual.cardData is MonsterCardData &&
                cell.cartaActual.clickableObject.propietarioP1 != esP1)
            {
                int dist = Mathf.Max(Mathf.Abs(cell.row - portador.casilla.row), Mathf.Abs(cell.col - portador.casilla.col));
                if (dist <= alcance)
                {
                    AplicarDanyoHelper(cell, 1);
                }
            }
        }
    }
}

public class ActiveTanque : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null) => portador.areaProximoAtaque = true;
}

public class ActiveCaballero : ActiveAbility
{
    public override bool RequiereObjetivo => true;
    public override bool EsObjetivoValido(Card objetivo) => objetivo.cardData is MonsterCardData;
    
    public override void Ejecutar(List<Card> objetivos = null)
    {
        if (objetivos != null && objetivos.Count > 0)
            objetivos[0].multDanyoIndefinido = 2;
    }
}

public class ActiveArqueroReforzado : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null) => portador.invulnerableHastaProximoTurno = true;
}

public class ActiveCura : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null)
    {
        bool esP1 = portador.clickableObject.propietarioP1;
        foreach (Cell cell in Board.Instance.cells)
        {
            if (cell.ocupada && cell.cartaActual != null && cell.cartaActual.cardData is MonsterCardData &&
                cell.cartaActual.clickableObject.propietarioP1 == esP1)
            {
                if (cell.cartaActual.cardData is DamageableCardData dData)
                    cell.cartaActual.UpdateVida(Mathf.Min(dData.vidaMaxima, dData.vida + 5));
            }
        }
    }
}

public class ActiveArquero : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null) => portador.multDanyoProximoAtaque = 3;
}

public class ActiveGuerrero : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null)
    {
        if (portador.cardData is DamageableCardData dData)
            portador.UpdateVida(dData.vidaMaxima);
    }
}

public class ActiveEsqueletoQuemado : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null)
    {
        if (portador.cardData is DamageableCardData dData)
        {
            int alcanceExp = dData.vida;
            
            // Recoger objetivos primero para evitar errores de modificación durante iteración
            List<Cell> afectados = new List<Cell>();
            foreach (Cell cell in Board.Instance.cells)
            {
                if (cell.ocupada && cell.cartaActual != null && cell.cartaActual.cardData is MonsterCardData)
                {
                    int dist = Mathf.Max(Mathf.Abs(cell.row - portador.casilla.row), Mathf.Abs(cell.col - portador.casilla.col));
                    if (dist <= alcanceExp && dist > 0) // No se hace daño a sí mismo doblemente
                    {
                        afectados.Add(cell);
                    }
                }
            }

            foreach (Cell c in afectados)
                AplicarDanyoHelper(c, 5);

            portador.casilla.LiberarCasilla(false); // Muere
        }
    }
}

public class ActiveEspia : ActiveAbility
{
    // Espía hace triple daño si ataca al castillo. Lo gestionaremos en CalcularDanyoAtacante
    // usando un flag especial o directamente comprobando el nombre.
    // Como requiere que SE ACTIVE, pondremos un flag `espiaActivado`.
    public override void Ejecutar(List<Card> objetivos = null) => portador.espiaActivoProximoAtaque = true;
}

public class ActiveEsqueleto : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null)
    {
        if (portador.cardData is DamageableCardData dData)
            portador.UpdateVida(dData.vidaMaxima);
        portador.multDanyoProximoAtaque = 3;
    }
}


// ---------------- ESTRUCTURAS ----------------

public class ActiveCastillo : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null) => portador.invulnerableHastaProximoTurno = true;
}

public class ActiveTorreInfernal : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null) 
    {
        // Al restar 3 al contador del turno, le permite atacar 3 veces más.
        portador.clickableObject.ultimoAtaque -= 3;
    }
}

public class ActiveTorreProtectora : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null)
    {
        int alcance = (portador.cardData as DamageableCardData)?.alcance ?? 0;
        bool esP1 = portador.clickableObject.propietarioP1;

        foreach (Cell cell in Board.Instance.cells)
        {
            if (cell.ocupada && cell.cartaActual != null && cell.cartaActual.cardData is MonsterCardData &&
                cell.cartaActual.clickableObject.propietarioP1 == esP1)
            {
                int dist = Mathf.Max(Mathf.Abs(cell.row - portador.casilla.row), Mathf.Abs(cell.col - portador.casilla.col));
                if (dist <= alcance)
                {
                    if (cell.cartaActual.cardData is DamageableCardData dData)
                        cell.cartaActual.UpdateVida(dData.vidaMaxima);
                }
            }
        }
    }
}

public class ActiveTorretaDestructora : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null) => portador.areaProximoAtaque = true;
}

public class ActiveMuroReforzado : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null)
    {
        if (portador.cardData is DamageableCardData dData)
            portador.UpdateVida(Mathf.Min(dData.vidaMaxima, dData.vida + 20));
    }
}

public class ActiveTorreMagica : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null)
    {
        bool esP1 = portador.clickableObject.propietarioP1;
        foreach (Cell cell in Board.Instance.cells)
        {
            if (cell.ocupada && cell.cartaActual != null && cell.cartaActual.cardData is StructureCardData &&
                cell.cartaActual.clickableObject.propietarioP1 != esP1 &&
                cell.cartaActual.cardData.nombre != "Castillo")
            {
                AplicarDanyoHelper(cell, 3);
            }
        }
    }
}

public class ActiveCasaConstructor : ActiveAbility
{
    public override bool RequiereObjetivo => true;
    public override bool EsObjetivoValido(Card objetivo) => 
        objetivo.cardData is StructureCardData && objetivo.cardData.nombre != "Castillo";

    public override void Ejecutar(List<Card> objetivos = null)
    {
        if (objetivos != null && objetivos.Count > 0)
        {
            if (objetivos[0].cardData is DamageableCardData dData)
                objetivos[0].UpdateVida(dData.vidaMaxima);
        }
    }
}

public class ActiveHerreria : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null)
    {
        // Se aplicará a nivel global en TurnManager o Board
        TurnManager.bonusHerreriaActiva += 5;
    }
}

public class ActiveCastilloFalso : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null)
    {
        if (portador.cardData is DamageableCardData dData)
            portador.UpdateVida(Mathf.Min(dData.vidaMaxima, dData.vida + 15));
    }
}

public class ActiveTorreta : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null) => portador.multDanyoProximoAtaque = 3;
}

public class ActiveMuro : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null)
    {
        if (portador.cardData is DamageableCardData dData)
            portador.UpdateVida(Mathf.Min(dData.vidaMaxima, dData.vida + 10));
    }
}


// ---------------- TRAMPAS ----------------

public class ActivePinchos : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null) => portador.trampaAplicaAturdimiento = true;
}

public class ActiveBombas : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null) => portador.bonusDanyoTrampa = 6;
}

public class ActiveTrampaIgnea : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null) => portador.trampaAplicaFuego = 1;
}

public class ActiveBomba : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null) => portador.bonusDanyoTrampa = 8;
}

public class ActiveTrampaElectrica : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null) => portador.trampaAplicaRalentizacion = true;
}

public class ActiveClavos : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null) => portador.multDanyoTrampa = 2;
}
