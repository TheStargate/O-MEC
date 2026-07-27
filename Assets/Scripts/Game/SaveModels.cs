using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Contenedor principal de todos los datos necesarios para guardar el estado de una partida.
/// </summary>
[Serializable]
public class SaveData_Game
{
    public int numTurno;
    public bool turnoP1;
    public int energiaDisponible;
    public int bonusHerreriaActiva;
    public bool robadoDisponible;

    public List<string> deckP1 = new List<string>();
    public List<string> energyDeckP1 = new List<string>();
    public List<string> deckP2 = new List<string>();
    public List<string> energyDeckP2 = new List<string>();

    public List<string> discardP1 = new List<string>();
    public List<string> energyDiscardP1 = new List<string>();
    public List<string> discardP2 = new List<string>();
    public List<string> energyDiscardP2 = new List<string>();

    public List<string> handP1 = new List<string>();
    public List<string> handP2 = new List<string>();

    public List<SaveData_Cell> tablero = new List<SaveData_Cell>();

    // Estado de fin de partida
    public bool partidaTerminada = false; // True si la partida ya ha concluido
    public bool perdedorEsP1 = false;     // True si el perdedor es el Jugador 1

    // Nombres de los jugadores
    public string nombreP1 = "Jugador 1";
    public string nombreP2 = "Jugador 2";
}

/// <summary>
/// Representa el estado de una casilla individual en el tablero para su guardado.
/// </summary>
[Serializable]
public class SaveData_Cell
{
    public int row;
    public int col;
    public bool ocupada;
    public SaveData_Card carta; // Null si no está ocupada
}

/// <summary>
/// Representa un efecto de daño prolongado en el tiempo (como quemaduras o venenos).
/// </summary>
[Serializable]
public class SaveData_DanyoEfecto
{
    public string nombre;
    public int danyo;
    public int turnosRestantes;
}

/// <summary>
/// Datos detallados de una carta en juego, incluyendo sus estadísticas, modificadores y estado actual.
/// </summary>
[Serializable]
public class SaveData_Card
{
    public string nombrePrefab;
    public bool propietarioP1;
    
    // Estados de Turno / Uso
    public int ultimoMovimiento;
    public int ultimoAtaque;
    public bool usado;
    public bool habilidadUsada;
    public int turnoColocado;

    // Estadísticas Actuales (si aplican)
    public int vidaActual;
    public int ataqueActual;
    public int velocidadActual;
    public int turnosTrampaActuales;

    // Modificadores Activos
    public bool invulnerableHastaProximoTurno;
    public bool inmuneHechizosIndefinido;
    public int bonusDanyoProximoAtaque;
    public int multDanyoProximoAtaque;
    public bool areaProximoAtaque;
    public bool espiaActivoProximoAtaque;

    public int multDanyoIndefinido;
    public int bonusDanyoTrampa;
    public int multDanyoTrampa;
    public bool trampaAplicaAturdimiento;
    public bool trampaAplicaRalentizacion;
    public int trampaAplicaFuego;

    // Efectos de Daño Continuo
    public List<SaveData_DanyoEfecto> efectosDanyo = new List<SaveData_DanyoEfecto>();

    // Estado Interno de Pasivas
    public int pasiva_ataquesTurno;
    public int pasiva_ataquesMaximos;
    public int pasiva_reduccionAplicadaInt;
    public bool pasiva_reduccionAplicadaBool;
}
