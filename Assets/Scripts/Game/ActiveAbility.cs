using System.Collections.Generic;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// CLASE BASE
// Las habilidades activas se ejecutan manualmente por el jugador consumiendo energía.
// Se instancian usando ActiveAbility.Crear(nombre, carta) desde Cell.OcuparCasilla.
// Propiedades y métodos disponibles:
//   RequiereObjetivo           : true si la habilidad requiere seleccionar objetivos.
//   MinObjetivos               : mínimo de objetivos que deben elegirse para validar la habilidad.
//   NumObjetivos               : máximo de objetivos que se pueden elegir para la habilidad.
//   EsObjetivoValido(objetivo) : devuelve true si la carta clickada puede ser objetivo.
//   RespetaAlcance             : true si la habilidad debe filtrar objetivos por alcance.
//   Ejecutar(objetivos)        : ejecuta la lógica principal de la habilidad activa.
// ─────────────────────────────────────────────────────────────────────────────
public abstract class ActiveAbility
{
    public Card portador; // La carta a la que pertenece esta habilidad activa.

    // Si es true, el jugador debe hacer click en una carta del tablero
    public virtual bool RequiereObjetivo => false; 

    // Mínima cantidad de objetivos que debe seleccionar el jugador para validar la habilidad
    public virtual int MinObjetivos => 1;

    // Cantidad máxima de objetivos que se pueden elegir si RequiereObjetivo es true
    public virtual int NumObjetivos => 1;

    // ─── CREACIÓN DE HABILIDADES ACTIVAS ───────────────────────────────────────
    public static ActiveAbility Crear(string nombre, Card portador)
    {
        ActiveAbility habilidadActiva = nombre switch
        {
            // ── Monstruos ──────────────────────────────────────────────────────
            "Guerrero oscuro"     => new ActiveGuerreroOscuro(),
            "Guerrero acorazado"  => new ActiveGuerreroAcorazado(),
            "Mago"                => new ActiveMago(),
            "Esqueleto gigante"   => new ActiveEsqueletoGigante(),
            "Cura oscuro"         => new ActiveCuraOscuro(),
            "Dragón de agua"      => new ActiveDragonAgua(),
            "Dragón de fuego"     => new ActiveDragonFuego(),
            "Ninja"               => new ActiveNinja(),
            "Cura protector"      => new ActiveCuraProtector(),
            "Arquero largo"       => new ActiveArqueroLargo(),
            "Bebé dragón"         => new ActiveBebeDragon(),
            "Tanque"              => new ActiveTanque(),
            "Caballero"           => new ActiveCaballero(),
            "Arquero reforzado"   => new ActiveArqueroReforzado(),
            "Cura"                => new ActiveCura(),
            "Arquero"             => new ActiveArquero(),
            "Guerrero"            => new ActiveGuerrero(),
            "Esqueleto quemado"   => new ActiveEsqueletoQuemado(),
            "Espía"               => new ActiveEspia(),
            "Esqueleto"           => new ActiveEsqueleto(),
            // ── Estructuras ────────────────────────────────────────────────────
            "Castillo"            => new ActiveCastillo(),
            "Torre infernal"      => new ActiveTorreInfernal(),
            "Torre protectora"    => new ActiveTorreProtectora(),
            "Torreta destructora" => new ActiveTorretaDestructora(),
            "Muro reforzado"      => new ActiveMuroReforzado(),
            "Torre mágica"        => new ActiveTorreMagica(),
            "Casa de constructor" => new ActiveCasaConstructor(),
            "Herrería"            => new ActiveHerreria(),
            "Castillo falso"      => new ActiveCastilloFalso(),
            "Torreta"             => new ActiveTorreta(),
            "Muro"                => new ActiveMuro(),
            // ── Trampas ────────────────────────────────────────────────────────
            "Pinchos"             => new ActivePinchos(),
            "Bombas"              => new ActiveBombas(),
            "Trampa ígnea"        => new ActiveTrampaIgnea(),
            "Bomba"               => new ActiveBomba(),
            "Trampa eléctrica"    => new ActiveTrampaElectrica(),
            "Clavos"              => new ActiveClavos(),
            _                     => null
        };
        if (habilidadActiva != null) habilidadActiva.portador = portador;
        return habilidadActiva;
    }

    // Comprueba si la carta clickada es un objetivo válido
    public virtual bool EsObjetivoValido(Card objetivo) => true;

    // Si es true, el sistema de selección filtrara automaticamente objetivos fuera del alcance de la carta
    public virtual bool RespetaAlcance => false;

    // Ejecuta la lógica. 'objetivos' contendrá las cartas elegidas si RequiereObjetivo es true.
    public abstract void Ejecutar(List<Card> objetivos = null);


}

// ─────────────────────────────────────────────────────────────────────────────
// HABILIDADES ACTIVAS DE MONSTRUOS
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Guerrero oscuro: inflige 5 puntos de daño a todos los monstruos enemigos en un área de 3x3 a su alrededor.</summary>
public class ActiveGuerreroOscuro : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null)
    {
        PassiveAbility.AplicarDanyoAreaGeneral(portador, portador.casilla, 5, 1, soloMonstruos: true, afectarAliados: true);
    }
}

/// <summary>Guerrero acorazado: su próximo ataque en este turno inflige el doble de daño.</summary>
public class ActiveGuerreroAcorazado : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null)
    {
        portador.multDanyoProximoAtaque *= 2;
        portador.RefrescarAtaqueUI();
    }
}

/// <summary>Mago: su próximo ataque en este turno inflige el triple de daño.</summary>
public class ActiveMago : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null)
    {
        portador.multDanyoProximoAtaque *= 3;
        portador.RefrescarAtaqueUI();
    }
}

/// <summary>Esqueleto gigante: se cura toda su vida (recupera sus PV máximos).</summary>
public class ActiveEsqueletoGigante : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null)
    {
        if (portador.cardData is DamageableCardData dData)
            portador.UpdateVida(dData.vidaMaxima);
    }
}

/// <summary>Cura oscuro: el monstruo objetivo (aliado o enemigo) multiplica su daño por 2 de forma indefinida.</summary>
public class ActiveCuraOscuro : ActiveAbility
{
    public override bool RequiereObjetivo => true;
    public override bool RespetaAlcance => true;
    public override bool EsObjetivoValido(Card objetivo) => objetivo.cardData is MonsterCardData;
    
    public override void Ejecutar(List<Card> objetivos = null)
    {
        if (objetivos != null && objetivos.Count > 0)
        {
            objetivos[0].multDanyoIndefinido *= 2;
            objetivos[0].RefrescarAtaqueUI();
        }
    }
}

/// <summary>Dragón de agua: se cura toda su vida y los monstruos enemigos a su alcance pierden 1 PV.</summary>
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
                int dist = Mathf.Abs(cell.row - portador.casilla.row) + Mathf.Abs(cell.col - portador.casilla.col);
                if (dist <= alcance)
                {
                    PassiveAbility.AplicarDanyo(cell, 1);
                }
            }
        }
    }
}

/// <summary>Dragón de fuego: selecciona hasta 3 monstruos a su alcance e inflige 5 puntos de daño a cada uno.</summary>
public class ActiveDragonFuego : ActiveAbility
{
    public override bool RequiereObjetivo => true;
    public override bool RespetaAlcance => true;
    public override int MinObjetivos => 1;
    public override int NumObjetivos => 3;
    public override bool EsObjetivoValido(Card objetivo)
    {
        if (objetivo == null || objetivo.casilla == null || objetivo.clickableObject == null || portador == null || portador.clickableObject == null || objetivo == portador)
            return false;

        int alcance = (portador.cardData as DamageableCardData)?.alcance ?? 0;
        int dist = Mathf.Abs(objetivo.casilla.row - portador.casilla.row) + Mathf.Abs(objetivo.casilla.col - portador.casilla.col);
        return objetivo.cardData is MonsterCardData && dist <= alcance;
    }

    public override void Ejecutar(List<Card> objetivos = null)
    {
        if (objetivos == null) return;

        int aplicados = 0;
        foreach (Card obj in objetivos)
        {
            if (aplicados >= 3) break;
            if (obj == null || obj.casilla == null || !EsObjetivoValido(obj)) continue;

            PassiveAbility.AplicarDanyo(obj.casilla, 5);
            aplicados++;
        }
    }
}

/// <summary>Ninja: se vuelve invulnerable a todo daño hasta el próximo turno.</summary>
public class ActiveNinja : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null) => portador.invulnerableHastaProximoTurno = true;
}

/// <summary>Cura protector: el monstruo objetivo se vuelve inmune a los hechizos de forma indefinida.</summary>
public class ActiveCuraProtector : ActiveAbility
{
    public override bool RequiereObjetivo => true;
    public override bool EsObjetivoValido(Card objetivo) => objetivo.cardData is MonsterCardData;

    public override void Ejecutar(List<Card> objetivos = null)
    {
        if (objetivos != null && objetivos.Count > 0 && objetivos[0] != null)
            PassiveAbility.AplicarInmunidadHechizosIndefinida(objetivos[0]);
    }
}

/// <summary>Arquero largo: su próximo ataque en este turno inflige 3 puntos de daño extra.</summary>
public class ActiveArqueroLargo : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null)
    {
        portador.bonusDanyoProximoAtaque += 3;
        portador.RefrescarAtaqueUI();
    }
}

/// <summary>Bebé dragón: todos los monstruos enemigos a su alcance reciben 1 punto de daño.</summary>
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
                int dist = Mathf.Abs(cell.row - portador.casilla.row) + Mathf.Abs(cell.col - portador.casilla.col);
                if (dist <= alcance)
                {
                    PassiveAbility.AplicarDanyo(cell, 1);
                }
            }
        }
    }
}

/// <summary>Tanque: su próximo ataque en este turno afecta a todos los enemigos en un área de 3x3 alrededor del objetivo principal.</summary>
public class ActiveTanque : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null) => portador.areaProximoAtaque = true;
}

/// <summary>Caballero: el monstruo objetivo multiplica su daño por 2 de forma indefinida.</summary>
public class ActiveCaballero : ActiveAbility
{
    public override bool RequiereObjetivo => true;
    public override bool RespetaAlcance => true;
    public override bool EsObjetivoValido(Card objetivo) => objetivo.cardData is MonsterCardData;
    
    public override void Ejecutar(List<Card> objetivos = null)
    {
        if (objetivos != null && objetivos.Count > 0)
        {
            objetivos[0].multDanyoIndefinido *= 2;
            objetivos[0].RefrescarAtaqueUI();
        }
    }
}

/// <summary>Arquero reforzado: se vuelve invulnerable a todo daño hasta el próximo turno.</summary>
public class ActiveArqueroReforzado : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null) => portador.invulnerableHastaProximoTurno = true;
}

/// <summary>Cura: cura 5 PV a todos los monstruos aliados en el tablero.</summary>
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

/// <summary>Arquero: su próximo ataque en este turno inflige el triple de daño.</summary>
public class ActiveArquero : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null)
    {
        portador.multDanyoProximoAtaque *= 3;
        portador.RefrescarAtaqueUI();
    }
}

/// <summary>Guerrero: se cura toda su vida (recupera sus PV máximos).</summary>
public class ActiveGuerrero : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null)
    {
        if (portador.cardData is DamageableCardData dData)
            portador.UpdateVida(dData.vidaMaxima);
    }
}

/// <summary>Esqueleto quemado: se autodestruye e inflige 5 puntos de daño a los monstruos en un alcance igual a su vida actual.</summary>
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
                    int dist = Mathf.Abs(cell.row - portador.casilla.row) + Mathf.Abs(cell.col - portador.casilla.col);
                    if (dist <= alcanceExp && dist > 0) // No se hace daño a sí mismo doblemente
                    {
                        afectados.Add(cell);
                    }
                }
            }

            foreach (Cell c in afectados)
                PassiveAbility.AplicarDanyo(c, 5);

            portador.casilla.LiberarCasilla(false); // Muere
            CameraController.Instance.VolverAPosicionOriginal();
        }
    }
}

/// <summary>Espía: su próximo ataque en este turno inflige el triple de daño si el objetivo es el castillo.</summary>
public class ActiveEspia : ActiveAbility
{
    // Espía hace triple daño si ataca al castillo. Lo gestionaremos en CalcularDanyoAtacante
    // usando un flag especial o directamente comprobando el nombre.
    // Como requiere que SE ACTIVE, pondremos un flag `espiaActivado`.
    public override void Ejecutar(List<Card> objetivos = null) => portador.espiaActivoProximoAtaque = true;
}

/// <summary>Esqueleto: se cura toda su vida y su próximo ataque inflige el triple de daño.</summary>
public class ActiveEsqueleto : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null)
    {
        if (portador.cardData is DamageableCardData dData)
            portador.UpdateVida(dData.vidaMaxima);
        portador.multDanyoProximoAtaque *= 3;
        portador.RefrescarAtaqueUI();
    }
}


// ─────────────────────────────────────────────────────────────────────────────
// HABILIDADES ACTIVAS DE ESTRUCTURAS
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Castillo: se vuelve invulnerable a todo daño hasta el próximo turno.</summary>
public class ActiveCastillo : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null) => portador.invulnerableHastaProximoTurno = true;
}

/// <summary>Torre infernal: gana 3 ataques adicionales durante este turno.</summary>
public class ActiveTorreInfernal : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null) 
    {
        if (portador.pasiva is PassiveTorreInfernal p)
        {
            p.ataquesMaximos += 3; // Puede atacar 3 veces más
        }
        portador.clickableObject.usado = false;
        portador.clickableObject.ultimoAtaque = 0; // Para que se vuelva a habilitar si ya había atacado
        portador.clickableObject.actualizarResaltado();
    }
}

/// <summary>Torre protectora: todos los monstruos aliados a su alcance se curan completamente (recuperan sus PV máximos).</summary>
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
                int dist = Mathf.Abs(cell.row - portador.casilla.row) + Mathf.Abs(cell.col - portador.casilla.col);
                if (dist <= alcance)
                {
                    if (cell.cartaActual.cardData is DamageableCardData dData)
                        cell.cartaActual.UpdateVida(dData.vidaMaxima);
                }
            }
        }
    }
}

/// <summary>Torreta destructora: su próximo ataque en este turno afecta a todos los enemigos en un área de 3x3 alrededor del objetivo principal.</summary>
public class ActiveTorretaDestructora : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null) => portador.areaProximoAtaque = true;
}

/// <summary>Muro reforzado: recupera 20 PV (sin superar su vida máxima).</summary>
public class ActiveMuroReforzado : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null)
    {
        if (portador.cardData is DamageableCardData dData)
            portador.UpdateVida(Mathf.Min(dData.vidaMaxima, dData.vida + 20));
    }
}

/// <summary>Torre mágica: inflige 3 puntos de daño a todas las estructuras enemigas del tablero (excepto al castillo).</summary>
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
                PassiveAbility.AplicarDanyo(cell, 3);
            }
        }
    }
}

/// <summary>Casa de constructor: la estructura objetivo (excepto el castillo) recupera todos sus PV máximos.</summary>
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

/// <summary>Herrería: otorga de forma temporal +5 de daño extra a todos los monstruos aliados.</summary>
public class ActiveHerreria : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null)
    {
        // Se aplicará a nivel global en TurnManager o Board
        TurnManager.bonusHerreriaActiva += 5;
        
        foreach (Cell cell in Board.Instance.cells)
        { // Actualiza visualmente todos los monstruos aliados para que se muestre el bonus de ataque
            if (cell.ocupada && cell.cartaActual != null && cell.cartaActual.cardData is MonsterCardData &&
                cell.cartaActual.clickableObject.propietarioP1 == portador.clickableObject.propietarioP1)
            {
                cell.cartaActual.RefrescarAtaqueUI();
            }
        }
    }
}

/// <summary>Castillo falso: recupera 15 PV (sin superar su vida máxima).</summary>
public class ActiveCastilloFalso : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null)
    {
        if (portador.cardData is DamageableCardData dData)
            portador.UpdateVida(Mathf.Min(dData.vidaMaxima, dData.vida + 15));
    }
}

/// <summary>Torreta: su próximo ataque en este turno inflige el triple de daño.</summary>
public class ActiveTorreta : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null)
    {
        portador.multDanyoProximoAtaque *= 3;
        portador.RefrescarAtaqueUI();
    }
}

/// <summary>Muro: recupera 10 PV (sin superar su vida máxima).</summary>
public class ActiveMuro : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null)
    {
        if (portador.cardData is DamageableCardData dData)
            portador.UpdateVida(Mathf.Min(dData.vidaMaxima, dData.vida + 10));
    }
}


// ─────────────────────────────────────────────────────────────────────────────
// HABILIDADES ACTIVAS DE TRAMPAS
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Pinchos: aplica el estado de aturdimiento cuando se activa la trampa.</summary>
public class ActivePinchos : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null) => portador.trampaAplicaAturdimiento = true;
}

/// <summary>Bombas: otorga un bonus temporal de 6 puntos de daño a la trampa.</summary>
public class ActiveBombas : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null) => portador.bonusDanyoTrampa = 6;
}

/// <summary>Trampa ígnea: aplica el estado de quemadura (1 de daño continuo) cuando se activa la trampa.</summary>
public class ActiveTrampaIgnea : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null) => portador.trampaAplicaFuego = 1;
}

/// <summary>Bomba: otorga un bonus temporal de 8 puntos de daño a la trampa.</summary>
public class ActiveBomba : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null) => portador.bonusDanyoTrampa = 8;
}

/// <summary>Trampa eléctrica: aplica el estado de ralentización cuando se activa la trampa.</summary>
public class ActiveTrampaElectrica : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null) => portador.trampaAplicaRalentizacion = true;
}

/// <summary>Clavos: multiplica por 2 el daño infligido por la trampa.</summary>
public class ActiveClavos : ActiveAbility
{
    public override void Ejecutar(List<Card> objetivos = null) => portador.multDanyoTrampa *= 2;
}
