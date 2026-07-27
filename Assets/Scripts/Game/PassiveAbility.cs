using UnityEngine;
using System.Collections.Generic;

// ─────────────────────────────────────────────────────────────────────────────
// CLASE BASE
// Cada carta de tipo Monstruo, MonstruoLeg o Estructura tiene una PassiveAbility.
// Se instancia con PassiveAbility.Crear(nombre, carta) desde Cell.OcuparCasilla.
// Eventos disponibles:
//   OnColocar()                           : al colocar la carta en el tablero
//   OnTurnoInicio()                       : al inicio del turno del propietario
//   OnMorir()                             : justo antes de destruir la carta
//   ModificarDanyoAtacante(objetivo)      : daño extra que añade esta carta al atacar
//   OnRecibirDanyo(danyo)                 : modifica el daño entrante (puede reducirlo)
//   PuedeSerAtacadoPor(atacante)          : false = no puede ser seleccionado como objetivo
//   PuedeAtacar(objetivo)                 : false = no puede atacar a ese tipo de carta
//   PuedeAtacarAliados                    : true = puede seleccionar aliados (curar)
//   PuedeAtacarEnemigos                   : false = solo puede curar (no atacar)
//   OnDespuesDeAtacar(objetivo)           : tras aplicar el daño normal (efectos secundarios)
// ─────────────────────────────────────────────────────────────────────────────
public abstract class PassiveAbility
{
    public Card portador; // La carta a la que pertenece esta habilidad pasiva.

    public virtual void OnColocar()                               { }
    public virtual void OnTurnoInicio()                          { }
    public virtual void OnMorir()                                { }
    public virtual int  ModificarDanyoAtacante(Card objetivo)    => 0;
    public virtual int  OnRecibirDanyo(int danyo)                => danyo;
    public virtual bool PuedeSerAtacadoPor(Card atacante)        => true;
    public virtual bool PuedeAtacar(Card objetivo)               => true;
    public virtual bool PuedeAtacarAliados                       => false;
    public virtual bool PuedeAtacarEnemigos                      => true;
    public virtual void OnDespuesDeAtacar(Card objetivo)         { }

    // ─────────────────────────────────────────────────────────────────────────────
    // MÉTODOS ESTÁTICOS
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>Devuelve true si el castillo aliado de castilloP1 está protegido por al menos
    /// un Castillo falso en pie en el tablero.</summary>
    public static bool EsCastilloInvulnerable(bool castilloP1)
    {
        foreach (Cell cell in Board.Instance.cells)
        {
            if (!cell.ocupada || cell.cartaActual == null) continue;
            if (cell.cartaActual.pasiva is PassiveCastilloFalso &&
                cell.cartaActual.clickableObject.propietarioP1 == castilloP1)
                return true;
        }
        return false;
    }

    /// <summary>Devuelve true si la carta es un monstruo adyacente (8 direcciones) a una
    /// Torre protectora aliada y por tanto es invulnerable a ataques y hechizos.</summary>
    public static bool EsInvulnerablePorTorreProtectora(Card carta)
    {
        if (carta.casilla == null) return false;
        if (!(carta.cardData is MonsterCardData)) return false; // Solo protege monstruos, no estructuras
        return HayPasivaAdyacente<PassiveTorreProtectora>(
            carta.casilla.row, carta.casilla.col, carta.clickableObject.propietarioP1);
    }

    /// <summary>Devuelve true si la carta está adyacente a un Rey cura aliado
    /// y por tanto es inmune a hechizos.</summary>
    public static bool EsInmuneHechizo(Card carta)
    {
        if (carta.casilla == null) return false;
        return HayPasivaAdyacente<PassiveReyCura>(
            carta.casilla.row, carta.casilla.col, carta.clickableObject.propietarioP1);
    }

    /// <summary>Devuelve true si la carta es completamente invulnerable a cualquier daño
    /// (físico y hechizos). Combina Torre protectora y Castillo falso.</summary>
    public static bool EsInvulnerableATodo(Card carta)
    {
        if (carta == null) return false;
        
        // Invulnerable hasta el próximo turno
        if (carta.invulnerableHastaProximoTurno) return true;

        // Protegida por Torre protectora adyacente
        if (EsInvulnerablePorTorreProtectora(carta)) return true;
        // Castillo protegido por uno o más Castillos falsos
        if (carta.cardData.nombre == "Castillo" && EsCastilloInvulnerable(carta.clickableObject.propietarioP1)) return true;
        return false;
    }

    /// <summary>Aplica la inmunidad a hechizos indefinida otorgada por habilidades como Cura protector.</summary>
    public static void AplicarInmunidadHechizosIndefinida(Card carta)
    {
        if (carta == null) return;
        carta.inmuneHechizosIndefinido = true;
    }

    /// <summary>Devuelve true si la carta es inmune a hechizos por cualquier motivo
    /// (Torre protectora, Castillo falso, Rey cura o efectos indefinidos como Cura protector).</summary>
    public static bool EsInmuneTotalHechizos(Card carta)
    {
        if (carta == null) return false;
        
        if (carta.inmuneHechizosIndefinido) return true;

        return EsInvulnerableATodo(carta) || EsInmuneHechizo(carta);
    }

    /// <summary>Devuelve el multiplicador de daño que aplica el Rey cura a monstruos
    /// aliados adyacentes (3x si está cerca de uno, 1x si no).</summary>
    public static int MultiplicadorDanyoReyCura(Card atacante)
    {
        if (atacante.casilla == null) return 1;
        return HayPasivaAdyacente<PassiveReyCura>(
            atacante.casilla.row, atacante.casilla.col, atacante.clickableObject.propietarioP1) ? 3 : 1;
    }

    /// <summary>Devuelve cuántos puntos de bonus de daño tienen las estructuras aliadas
    /// gracias a las Casas de constructor activas.</summary>
    public static int BonusDanyoEstructuras(bool propietarioP1)
    {
        int bonus = 0;
        if (Board.Instance == null || Board.Instance.cells == null) return 0;
        foreach (Cell cell in Board.Instance.cells)
        {
            if (!cell.ocupada || cell.cartaActual == null) continue;
            if (cell.cartaActual.clickableObject == null) continue;
            if (cell.cartaActual.clickableObject.propietarioP1 != propietarioP1) continue;
            if (cell.cartaActual.pasiva is PassiveCasaConstructor)
                bonus++;
        }
        return bonus;
    }

    /// <summary>Devuelve cuántos puntos de bonus de daño tienen los monstruos aliados
    /// gracias a las Herrerías activas.</summary>
    public static int BonusDanyoMonstruos(bool propietarioP1)
    {
        int bonus = 0;
        if (Board.Instance == null || Board.Instance.cells == null) return 0;
        foreach (Cell cell in Board.Instance.cells)
        {
            if (!cell.ocupada || cell.cartaActual == null) continue;
            if (cell.cartaActual.clickableObject == null) continue;
            if (cell.cartaActual.clickableObject.propietarioP1 != propietarioP1) continue;
            if (cell.cartaActual.pasiva is PassiveHerreria)
                bonus++;
        }

        // Aumento de daño por habilidad activa de la Herrería (solo aplica si es el turno de ese jugador)
        if (propietarioP1 == TurnManager.turnoP1)
            bonus += TurnManager.bonusHerreriaActiva;

        return bonus;
    }

    // Comprueba si hay una celda adyacente con una pasiva del tipo T y el propietario indicado
    private static bool HayPasivaAdyacente<T>(int row, int col, bool esP1) where T : PassiveAbility
    {
        for (int dr = -1; dr <= 1; dr++)
        {
            for (int dc = -1; dc <= 1; dc++)
            {
                if (dr == 0 && dc == 0) continue;
                int r = row + dr, c = col + dc;
                if (r < 0 || r >= Board.Instance.rows || c < 0 || c >= Board.Instance.columns) continue;
                Cell cell = Board.Instance.cells[r, c];
                if (cell.ocupada && cell.cartaActual?.pasiva is T &&
                    cell.cartaActual.clickableObject.propietarioP1 == esP1)
                    return true;
            }
        }
        return false;
    }

    /// <summary>Calcula el daño total que un atacante infligiría a un objetivo, 
    /// teniendo en cuenta todos los modificadores ofensivos (bonus pasivos, herrerías, rey cura...).</summary>
    public static int CalcularDanyoAtacante(Card atacante, Card objetivo)
    {
        if (atacante == null || atacante.cardData == null) return 0;

        DamageableCardData cardDataAtacante = atacante.cardData as DamageableCardData;
        int danyo = cardDataAtacante?.ataque ?? 0;

        // Bonus propio de la carta atacante (arquero +1, guerrero +2, espía x2...)
        if (atacante.pasiva != null)
            danyo += atacante.pasiva.ModificarDanyoAtacante(objetivo);

        // Bonus de edificio aliado (Casa de constructor para estructuras / Herrería para monstruos)
        if (atacante.clickableObject != null && atacante.casilla != null)
        {
            if (atacante.cardData is StructureCardData)
                danyo += BonusDanyoEstructuras(atacante.clickableObject.propietarioP1);
            else if (atacante.cardData is MonsterCardData)
                danyo += BonusDanyoMonstruos(atacante.clickableObject.propietarioP1);
        }

        // Multiplicador Rey cura (x3 si el atacante está adyacente a uno)
        danyo *= MultiplicadorDanyoReyCura(atacante);

        // Bonus por habilidades activas
        danyo += atacante.bonusDanyoProximoAtaque;
        danyo *= atacante.multDanyoProximoAtaque;
        danyo *= atacante.multDanyoIndefinido;
        if (atacante.espiaActivoProximoAtaque && objetivo != null && objetivo.cardData != null && objetivo.cardData.nombre == "Castillo")
            danyo *= 3;

        return danyo;
    }

    // Aplica daño a una carta respetando sus habilidades, y la destruye si llega a 0
    public static void AplicarDanyo(Cell cell, int danyo)
    {
        if (!cell.ocupada || cell.cartaActual == null) return;
        // Invulnerabilidad absoluta: Torre protectora o Castillo falso
        if (EsInvulnerableATodo(cell.cartaActual)) return;
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

    // Aplica daño en área a partir de una casilla central, con opciones para filtrar por monstruos o aliados
    public static void AplicarDanyoAreaGeneral(Card atacante, Cell centro, int danyo, int radio, bool soloMonstruos = false, bool afectarAliados = false)
    {
        if (atacante == null || centro == null) return;
        bool esP1 = atacante.clickableObject.propietarioP1;

        for (int dr = -radio; dr <= radio; dr++)
        {
            for (int dc = -radio; dc <= radio; dc++)
            {
                if (dr == 0 && dc == 0) continue;
                int r = centro.row + dr, c = centro.col + dc;
                if (r < 0 || r >= Board.Instance.rows || c < 0 || c >= Board.Instance.columns) continue;
                Cell cell = Board.Instance.cells[r, c];
                if (!cell.ocupada || cell.cartaActual == null) continue;
                
                // Si no se permite afectar aliados y la carta es del mismo propietario, ignorarla
                if (!afectarAliados && cell.cartaActual.clickableObject.propietarioP1 == esP1) continue;
                
                if (soloMonstruos && !(cell.cartaActual.cardData is MonsterCardData)) continue;
                if (!soloMonstruos && !(cell.cartaActual.cardData is MonsterCardData) &&
                    !(cell.cartaActual.cardData is StructureCardData)) continue;
                    
                AplicarDanyo(cell, danyo);
            }
        }
    }

    // Realiza un ataque en área utilizando las estadísticas del atacante (usado por Mago, Tanque y Torreta destructora)
    public static void EjecutarAtaqueArea(Card atacante, Cell centro, int radio)
    {
        if (atacante == null || centro == null) return;
        bool esP1 = atacante.clickableObject.propietarioP1;

        for (int dr = -radio; dr <= radio; dr++)
        {
            for (int dc = -radio; dc <= radio; dc++)
            {
                if (dr == 0 && dc == 0) continue; // El centro ya ha recibido el golpe normal
                int r = centro.row + dr, c = centro.col + dc;
                if (r < 0 || r >= Board.Instance.rows || c < 0 || c >= Board.Instance.columns) continue;
                Cell cell = Board.Instance.cells[r, c];
                if (!cell.ocupada || cell.cartaActual == null) continue;
                if (cell.cartaActual.clickableObject.propietarioP1 == esP1) continue; // No dañar aliados
                
                int danyoFinal = CalcularDanyoAtacante(atacante, cell.cartaActual);
                AplicarDanyo(cell, danyoFinal);
            }
        }
    }

    // ─── CREACIÓN DE HABILIDADES PASIVAS ───────────────────────────────────────
    public static PassiveAbility Crear(string nombre, Card portador)
    {
        PassiveAbility habilidadPasiva = nombre switch
        {
            // ── Monstruos ──────────────────────────────────────────────────────
            "Guerrero oscuro"     => new PassiveGuerreroOscuro(),
            "Guerrero acorazado"  => new PassiveGuerreroAcorazado(),
            "Mago"                => new PassiveMago(),
            "Esqueleto gigante"   => new PassiveEsqueletoGigante(),
            "Cura oscuro"         => new PassiveCuraOscuro(),
            "Dragón de agua"      => new PassiveDragonAgua(),
            "Dragón de fuego"     => new PassiveDragonFuego(),
            "Ninja"               => new PassiveNinja(),
            "Cura protector"      => new PassiveCuraProtector(),
            "Arquero largo"       => new PassiveArqueroLargo(),
            "Bebé dragón"         => new PassiveBebeDragon(),
            "Tanque"              => new PassiveTanque(),
            "Caballero"           => new PassiveCaballero(),
            "Arquero reforzado"   => new PassiveArqueroReforzado(),
            "Cura"                => new PassiveCura(),
            "Arquero"             => new PassiveArquero(),
            "Guerrero"            => new PassiveGuerrero(),
            "Esqueleto quemado"   => new PassiveEsqueletoQuemado(),
            "Espía"               => new PassiveEspia(),
            "Esqueleto"           => new PassiveEsqueleto(),
            // ── Estructuras ────────────────────────────────────────────────────
            "Castillo"            => new PassiveCastillo(),
            "Torre infernal"      => new PassiveTorreInfernal(),
            "Torre protectora"    => new PassiveTorreProtectora(),
            "Torreta destructora" => new PassiveTorretaDestructora(),
            "Muro reforzado"      => new PassiveMuroReforzado(),
            "Torre mágica"        => new PassiveTorreMagica(),
            "Casa de constructor" => new PassiveCasaConstructor(),
            "Herrería"            => new PassiveHerreria(),
            "Castillo falso"      => new PassiveCastilloFalso(),
            "Torreta"             => new PassiveTorreta(),
            "Muro"                => new PassiveMuro(),
            // ── Legendarios ────────────────────────────────────────────────────
            "Rey guerrero"        => new PassiveReyGuerrero(),
            "Rey cura"            => new PassiveReyCura(),
            "Rey dragón"          => new PassiveReyDragon(),
            "Rey arquero"         => new PassiveReyArquero(),
            "Rey esqueleto"       => new PassiveReyEsqueleto(),
            _                     => null
        };
        if (habilidadPasiva != null) habilidadPasiva.portador = portador;
        return habilidadPasiva;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// HABILIDADES PASIVAS DE MONSTRUOS
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Guerrero oscuro: el daño recibido se reduce en 1.</summary>
public class PassiveGuerreroOscuro : PassiveAbility
{
    public override int OnRecibirDanyo(int danyo) => Mathf.Max(0, danyo - 1);
}

/// <summary>Guerrero acorazado: cada turno recupera 2 PV (sin superar el máximo).</summary>
public class PassiveGuerreroAcorazado : PassiveAbility
{
    public override void OnTurnoInicio()
    {
        if (portador.cardData is DamageableCardData d)
        {
            int nuevaVida = Mathf.Min(d.vida + 2, d.vidaMaxima);
            if (nuevaVida != d.vida) portador.UpdateVida(nuevaVida);
        }
    }
}

/// <summary>Mago: sus ataques afectan a todos los enemigos en un área de 3x3
/// alrededor del objetivo principal.</summary>
public class PassiveMago : PassiveAbility
{
    public override void OnDespuesDeAtacar(Card objetivo)
    {
        EjecutarAtaqueArea(portador, objetivo.casilla, 1);
    }
}

/// <summary>Esqueleto gigante: al morir, los monstruos y estructuras enemigos
/// adyacentes (8 casillas) pierden 10 PV.</summary>
public class PassiveEsqueletoGigante : PassiveAbility
{
    public override void OnMorir() => AplicarDanyoAreaGeneral(portador, portador.casilla, 10, 1);
}

/// <summary>Cura oscuro: puede atacar a enemigos o curar aliados con el mismo botón.</summary>
public class PassiveCuraOscuro : PassiveAbility
{
    public override bool PuedeAtacarAliados  => true;
    public override bool PuedeAtacarEnemigos => true;
    // La lógica de curación se gestiona en Board.ConfirmarAccionCarta al detectar objetivo aliado.
}

/// <summary>Dragón de agua: no puede ser atacado por monstruos con alcance menor a 3.</summary>
public class PassiveDragonAgua : PassiveAbility
{
    public override bool PuedeSerAtacadoPor(Card atacante)
        => (atacante.cardData as DamageableCardData)?.alcance >= 3;
}

/// <summary>Dragón de fuego: no puede ser atacado por monstruos con alcance menor a 3.</summary>
public class PassiveDragonFuego : PassiveAbility
{
    public override bool PuedeSerAtacadoPor(Card atacante)
        => (atacante.cardData as DamageableCardData)?.alcance >= 3;
}

/// <summary>Ninja: puede atacar dos veces por turno. El contador se resetea al inicio de turno.</summary>
public class PassiveNinja : PassiveAbility
{
    public int ataquesTurno = 0;

    public override void OnTurnoInicio() => ataquesTurno = 0;

    public override void OnDespuesDeAtacar(Card objetivo)
    {
        ataquesTurno++;
        if (ataquesTurno < 2)
            portador.clickableObject.ultimoAtaque = 0; // Permite el segundo ataque
    }
}

/// <summary>Cura protector: en vez de atacar, cura vida a monstruos aliados.</summary>
public class PassiveCuraProtector : PassiveAbility
{
    public override bool PuedeAtacarEnemigos => false;
    public override bool PuedeAtacarAliados  => true;
}

/// <summary>Arquero largo: tras atacar a un objetivo, puede dañar a un segundo monstruo
/// enemigo que esté en línea recta (horizontal, vertical o diagonal) con el primero
/// y dentro del alcance del arquero. Pueden existir huecos entre los dos.</summary>
public class PassiveArqueroLargo : PassiveAbility
{
    public override void OnDespuesDeAtacar(Card objetivo)
    {
        if (portador == null || portador.casilla == null || objetivo == null || objetivo.casilla == null) return;
        
        // Solo se activa si se ataca a un monstruo, si es a una estructura no hace nada
        if (!(objetivo.cardData is MonsterCardData)) return;

        int dr = objetivo.casilla.row - portador.casilla.row;
        int dc = objetivo.casilla.col - portador.casilla.col;

        Debug.Log($"[ArqueroLargo] Atacando a {objetivo.name}. dr={dr}, dc={dc}");

        // Solo líneas rectas: horizontal, vertical o diagonal (|dr|==|dc|)
        if (dr != 0 && dc != 0 && Mathf.Abs(dr) != Mathf.Abs(dc)) 
        {
            Debug.Log($"[ArqueroLargo] Ataque no lineal. Se cancela pasiva.");
            return;
        }

        int normR = dr == 0 ? 0 : (dr > 0 ? 1 : -1);
        int normC = dc == 0 ? 0 : (dc > 0 ? 1 : -1);
        int alcance = (portador.cardData as DamageableCardData)?.alcance ?? 0;
        int ataqueBase = (portador.cardData as DamageableCardData)?.ataque ?? 0;
        bool esP1   = portador.clickableObject.propietarioP1;

        Debug.Log($"[ArqueroLargo] Pasiva activada. normR={normR}, normC={normC}, alcance={alcance}, ataqueBase={ataqueBase}");

        for (int dist = 1; dist <= alcance; dist++)
        {
            int tr = portador.casilla.row + normR * dist;
            int tc = portador.casilla.col + normC * dist;
            
            if (tr < 0 || tr >= Board.Instance.rows || tc < 0 || tc >= Board.Instance.columns) 
            {
                Debug.Log($"[ArqueroLargo] Fin de la linea (fuera del tablero) en dist={dist}");
                break;
            }

            Cell cell = Board.Instance.cells[tr, tc];

            // El objetivo principal ya recibió el daño normal del ataque, lo saltamos
            if (tr == objetivo.casilla.row && tc == objetivo.casilla.col)
            {
                Debug.Log($"[ArqueroLargo] dist={dist} ({tr},{tc}) es el objetivo principal. Ignorando.");
                continue;
            }

            // Si hay un enemigo MONSTRUO en la trayectoria, recibe el daño pasivo y la flecha se detiene
            if (cell.ocupada && cell.cartaActual != null)
            {
                if (cell.cartaActual.clickableObject.propietarioP1 != esP1 &&
                    cell.cartaActual.cardData is MonsterCardData)
                {
                    // Calculamos el daño total con los bonus actuales (Herrería, Rey Cura...)
                    int danyoFinal = CalcularDanyoAtacante(portador, cell.cartaActual);

                    Debug.Log($"[ArqueroLargo] ¡Es monstruo enemigo! Aplicando {danyoFinal} de daño.");
                    AplicarDanyo(cell, danyoFinal);
                    break; // Solo un objetivo extra por ataque
                }
            }
        }
    }
}

/// <summary>Bebé dragón: no puede ser atacado por monstruos con alcance menor a 2.</summary>
public class PassiveBebeDragon : PassiveAbility
{
    public override bool PuedeSerAtacadoPor(Card atacante)
        => (atacante.cardData as DamageableCardData)?.alcance >= 2;
}

/// <summary>Tanque: al morir, los monstruos y estructuras enemigos adyacentes
/// (8 casillas) pierden 5 PV.</summary>
public class PassiveTanque : PassiveAbility
{
    public override void OnMorir() => AplicarDanyoAreaGeneral(portador, portador.casilla, 5, 1);
}

/// <summary>Caballero: cada turno recupera 1 PV (sin superar el máximo).</summary>
public class PassiveCaballero : PassiveAbility
{
    public override void OnTurnoInicio()
    {
        if (portador.cardData is DamageableCardData d)
        {
            int nuevaVida = Mathf.Min(d.vida + 1, d.vidaMaxima);
            if (nuevaVida != d.vida) portador.UpdateVida(nuevaVida);
        }
    }
}

/// <summary>Arquero reforzado: el daño recibido se reduce en 1.</summary>
public class PassiveArqueroReforzado : PassiveAbility
{
    public override int OnRecibirDanyo(int danyo) => Mathf.Max(0, danyo - 1);
}

/// <summary>Cura: solo puede curar (no atacar), seleccionando monstruos aliados.</summary>
public class PassiveCura : PassiveAbility
{
    public override bool PuedeAtacarEnemigos => false;
    public override bool PuedeAtacarAliados  => true;
}

/// <summary>Arquero: hace 1 punto de daño más a monstruos.</summary>
public class PassiveArquero : PassiveAbility
{
    public override int ModificarDanyoAtacante(Card objetivo)
        => (objetivo != null && objetivo.cardData is MonsterCardData) ? 1 : 0;
}

/// <summary>Guerrero: hace 2 puntos de daño más a estructuras.</summary>
public class PassiveGuerrero : PassiveAbility
{
    public override int ModificarDanyoAtacante(Card objetivo)
        => (objetivo != null && objetivo.cardData is StructureCardData) ? 2 : 0;
}

/// <summary>Esqueleto quemado: pierde 1 PV por turno (efecto permanente desde el momento
/// en que se coloca; se gestiona con DanyoEfecto infinito).</summary>
public class PassiveEsqueletoQuemado : PassiveAbility
{
    public override void OnColocar()
    {
        if (portador.cardData is DamageableCardData d)
        {
            // Evitar duplicar el efecto si ya lo tiene (por ejemplo, tras moverse)
            if (!d.efectosDanyo.Exists(e => e.nombre == "Esqueleto quemado"))
                d.efectosDanyo.Add(new DanyoEfecto("Esqueleto quemado", 1, -1));
        }
    }
}

/// <summary>Espía: hace el doble de daño a monstruos.</summary>
public class PassiveEspia : PassiveAbility
{
    public override int ModificarDanyoAtacante(Card objetivo)
    {
        if (objetivo == null || objetivo.cardData == null || portador == null || portador.cardData == null)
            return 0;
        if (objetivo.cardData is MonsterCardData)
            return (portador.cardData as DamageableCardData)?.ataque ?? 0; // +ataque = x2
        return 0;
    }
}

/// <summary>Esqueleto: pierde 1 PV por turno (igual que Esqueleto quemado).</summary>
public class PassiveEsqueleto : PassiveAbility
{
    public override void OnColocar()
    {
        if (portador.cardData is DamageableCardData d)
        {
            if (!d.efectosDanyo.Exists(e => e.nombre == "Esqueleto"))
                d.efectosDanyo.Add(new DanyoEfecto("Esqueleto", 1, -1));
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// HABILIDADES PASIVAS DE ESTRUCTURAS
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Castillo: al destruirse, el propietario pierde la partida.</summary>
public class PassiveCastillo : PassiveAbility
{
    public override void OnMorir()
    {
        WinManager.Instance?.FinPartida(portador.clickableObject.propietarioP1);
    }
}

/// <summary>Torre infernal: puede atacar 5 veces por turno; cada ataque dentro del mismo
/// turno hace 1 punto de daño más que el anterior. El contador se resetea cada turno.</summary>
public class PassiveTorreInfernal : PassiveAbility
{
    public int ataquesTurno = 0; // Contador de ataques realizados en este turno
    public int ataquesMaximos = 5; // Contador de ataques máximos permitidos en este turno

    public override void OnTurnoInicio()
    {
        ataquesTurno = 0;
        ataquesMaximos = 5;
    }

    /// <summary>Daño extra acumulado: en el primer ataque +0, en el segundo +1, etc.</summary>
    public override int ModificarDanyoAtacante(Card objetivo) => ataquesTurno;

    public override void OnDespuesDeAtacar(Card objetivo)
    {
        ataquesTurno++;
        if (ataquesTurno < ataquesMaximos)
            portador.clickableObject.ultimoAtaque = 0; // Permite otro ataque
    }
}

/// <summary>Torre protectora: en vez de atacar, cura vida a monstruos aliados.
/// Los monstruos aliados adyacentes son invulnerables (ni ataques ni hechizos los dañan).
/// La invulnerabilidad se comprueba con PassiveAbility.EsInvulnerablePorTorreProtectora.</summary>
public class PassiveTorreProtectora : PassiveAbility
{
    public override bool PuedeAtacarEnemigos => false;
    public override bool PuedeAtacarAliados  => true;
}

/// <summary>Torreta destructora: hace 3 puntos de daño más a monstruos.</summary>
public class PassiveTorretaDestructora : PassiveAbility
{
    public override int ModificarDanyoAtacante(Card objetivo)
        => (objetivo != null && objetivo.cardData is MonsterCardData) ? 3 : 0;
}

/// <summary>Muro reforzado: el daño recibido por turno se reduce en 3.</summary>
public class PassiveMuroReforzado : PassiveAbility
{
    public int reduccionAplicada = 0;

    public override void OnTurnoInicio() => reduccionAplicada = 0;

    public override int OnRecibirDanyo(int danyo)
    {
        if (reduccionAplicada < 3)
        {
            int reduccion = Mathf.Min(3 - reduccionAplicada, danyo);
            reduccionAplicada += reduccion;
            return danyo - reduccion;
        }
        return danyo;
    }
}

/// <summary>Torre mágica: puede atacar 3 veces por turno. El contador se resetea cada turno.</summary>
public class PassiveTorreMagica : PassiveAbility
{
    public int ataquesTurno = 0;

    public override void OnTurnoInicio() => ataquesTurno = 0;

    public override void OnDespuesDeAtacar(Card objetivo)
    {
        ataquesTurno++;
        if (ataquesTurno < 3)
            portador.clickableObject.ultimoAtaque = 0; // Permite otro ataque
    }
}

/// <summary>Casa de constructor: todos los edificios aliados hacen 1 punto más de daño.
/// Su presencia se detecta via PassiveAbility.BonusDanyoEdificio().</summary>
public class PassiveCasaConstructor : PassiveAbility { }

/// <summary>Herrería: todos los edificios aliados hacen 1 punto más de daño.
/// Su presencia se detecta via PassiveAbility.BonusDanyoEdificio().</summary>
public class PassiveHerreria : PassiveAbility { }

/// <summary>Castillo falso: mientras esté en pie, el castillo aliado es invulnerable
/// a ataques y hechizos. Si hay varios, el efecto se acumula (sigue activo mientras
/// quede al menos uno). Se comprueba con PassiveAbility.EsCastilloInvulnerable().</summary>
public class PassiveCastilloFalso : PassiveAbility { }

/// <summary>Torreta: no puede atacar a edificios (solo a monstruos).</summary>
public class PassiveTorreta : PassiveAbility
{
    public override bool PuedeAtacar(Card objetivo)
        => !(objetivo.cardData is StructureCardData);
}

/// <summary>Muro: el daño recibido por turno se reduce en 1.</summary>
public class PassiveMuro : PassiveAbility
{
    public bool reduccionAplicada = false;

    public override void OnTurnoInicio() => reduccionAplicada = false;

    public override int OnRecibirDanyo(int danyo)
    {
        if (!reduccionAplicada)
        {
            reduccionAplicada = true;
            return Mathf.Max(0, danyo - 1);
        }
        return danyo;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// HABILIDADES PASIVAS DE MONSTRUOS LEGENDARIOS
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Rey guerrero: cada turno recupera 3 PV y hace el doble de daño a estructuras.</summary>
public class PassiveReyGuerrero : PassiveAbility
{
    public override void OnTurnoInicio()
    {
        if (portador.cardData is DamageableCardData d)
        {
            int nuevaVida = Mathf.Min(d.vida + 3, d.vidaMaxima);
            if (nuevaVida != d.vida) portador.UpdateVida(nuevaVida);
        }
    }

    public override int ModificarDanyoAtacante(Card objetivo)
    {
        if (objetivo == null || objetivo.cardData == null || portador == null || portador.cardData == null)
            return 0;
        if (objetivo.cardData is StructureCardData)
            return (portador.cardData as DamageableCardData)?.ataque ?? 0; // +ataque = x2
        return 0;
    }
}

/// <summary>Rey cura: en vez de atacar, cura vida a monstruos aliados.
/// Los monstruos adyacentes aliados son inmunes a hechizos y hacen el triple de daño.
/// La inmunidad se comprueba con PassiveAbility.EsInmuneHechizo().
/// El multiplicador se comprueba con PassiveAbility.MultiplicadorDanyoReyCura().</summary>
public class PassiveReyCura : PassiveAbility
{
    public override bool PuedeAtacarEnemigos => false;
    public override bool PuedeAtacarAliados  => true;
}

/// <summary>Rey dragón: no puede ser atacado por monstruos con alcance menor a 4.
/// Además recupera 3 PV al inicio de cada turno.</summary>
public class PassiveReyDragon : PassiveAbility
{
    public override void OnTurnoInicio()
    {
        if (portador.cardData is DamageableCardData d)
        {
            int nuevaVida = Mathf.Min(d.vida + 3, d.vidaMaxima);
            if (nuevaVida != d.vida) portador.UpdateVida(nuevaVida);
        }
    }

    public override bool PuedeSerAtacadoPor(Card atacante)
        => (atacante.cardData as DamageableCardData)?.alcance >= 4;
}

/// <summary>Rey arquero: el daño recibido se reduce en 3.</summary>
public class PassiveReyArquero : PassiveAbility
{
    public override int OnRecibirDanyo(int danyo) => Mathf.Max(0, danyo - 3);
}

/// <summary>Rey esqueleto: al inicio de cada turno recupera toda su vida.
/// Al morir, los monstruos y estructuras enemigos en un área de 5x5 pierden 20 PV.</summary>
public class PassiveReyEsqueleto : PassiveAbility
{
    public override void OnTurnoInicio()
    {
        if (portador.cardData is DamageableCardData d && d.vida < d.vidaMaxima)
            portador.UpdateVida(d.vidaMaxima);
    }

    public override void OnMorir() => AplicarDanyoAreaGeneral(portador, portador.casilla, 20, 2);
}
