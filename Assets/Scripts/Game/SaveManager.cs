using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

/// <summary>
/// Gestor encargado de guardar y restaurar el estado completo de la partida en disco (formato JSON).
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }
    private bool cargandoPartida;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        cargandoPartida = PlayerPrefs.GetInt("CargarPartida", 0) == 1;
    }

    private System.Collections.IEnumerator Start()
    {
        // Comprueba si el jugador ha pulsado "Cargar Partida" en el menú
        if (cargandoPartida)
        {
            // Resetea la bandera para futuras ejecuciones
            PlayerPrefs.SetInt("CargarPartida", 0);
            
            // Espera al final del frame para asegurarse de que Board, DeckManager, etc. han terminado su Start()
            yield return new WaitForEndOfFrame();
            
            CargarPartida();
            cargandoPartida = false;
        }
    }

    /// <summary>
    /// Guarda el estado actual de la partida en el archivo savegame.json.
    /// </summary>
    public void GuardarPartida()
    {
        SaveData_Game save = new SaveData_Game();

        // TurnManager
        save.numTurno = TurnManager.numTurno;
        save.turnoP1 = TurnManager.turnoP1;
        save.energiaDisponible = TurnManager.energiaDisponible;
        save.bonusHerreriaActiva = TurnManager.bonusHerreriaActiva;
        save.robadoDisponible = TurnManager.robadoDisponible;

        // WinManager (Final de partida)
        if (WinManager.Instance != null)
        {
            save.partidaTerminada = WinManager.Instance.partidaTerminada;
            save.perdedorEsP1 = WinManager.Instance.perdedorEsP1;
        }

        // PlayerNameManager (Nombres de los jugadores)
        if (PlayerNameManager.Instance != null)
        {
            save.nombreP1 = PlayerNameManager.Instance.NombreP1;
            save.nombreP2 = PlayerNameManager.Instance.NombreP2;
        }

        // DeckManager (Mazos y descartes)
        DeckManager dm = DeckManager.Instance;
        save.deckP1 = dm.deckP1.Select(c => c.nombre).ToList();
        save.energyDeckP1 = dm.energyDeckP1.Select(c => c.nombre).ToList();
        save.deckP2 = dm.deckP2.Select(c => c.nombre).ToList();
        save.energyDeckP2 = dm.energyDeckP2.Select(c => c.nombre).ToList();

        save.discardP1 = dm.descartadasP1.Select(c => c.nombre).ToList();
        save.energyDiscardP1 = dm.energiasDescartadasP1.Select(c => c.nombre).ToList();
        save.discardP2 = dm.descartadasP2.Select(c => c.nombre).ToList();
        save.energyDiscardP2 = dm.energiasDescartadasP2.Select(c => c.nombre).ToList();

        // Manos
        foreach (Transform child in dm.handPanelP1)
        {
            CardUI cardUI = child.GetComponent<CardUI>();
            if (cardUI != null && cardUI.cartaPrefab != null && cardUI.cartaPrefab.cardData != null)
                save.handP1.Add(cardUI.cartaPrefab.cardData.nombre);
        }
        foreach (Transform child in dm.handPanelP2)
        {
            CardUI cardUI = child.GetComponent<CardUI>();
            if (cardUI != null && cardUI.cartaPrefab != null && cardUI.cartaPrefab.cardData != null)
                save.handP2.Add(cardUI.cartaPrefab.cardData.nombre);
        }

        // Tablero
        foreach (Cell cell in Board.Instance.cells)
        {
            SaveData_Cell saveCell = new SaveData_Cell
            {
                row = cell.row,
                col = cell.col,
                ocupada = cell.ocupada
            };

            if (cell.ocupada && cell.cartaActual != null)
            {
                Card carta = cell.cartaActual;
                SaveData_Card saveCard = new SaveData_Card
                {
                    nombrePrefab = carta.cardData.nombre,
                    propietarioP1 = carta.clickableObject.propietarioP1,
                    ultimoMovimiento = carta.clickableObject.ultimoMovimiento,
                    ultimoAtaque = carta.clickableObject.ultimoAtaque,
                    usado = carta.clickableObject.usado,
                    habilidadUsada = carta.clickableObject.habilidadUsada,
                    turnoColocado = carta.clickableObject.turnoColocado,
                    
                    invulnerableHastaProximoTurno = carta.invulnerableHastaProximoTurno,
                    inmuneHechizosIndefinido = carta.inmuneHechizosIndefinido,
                    bonusDanyoProximoAtaque = carta.bonusDanyoProximoAtaque,
                    multDanyoProximoAtaque = carta.multDanyoProximoAtaque,
                    areaProximoAtaque = carta.areaProximoAtaque,
                    espiaActivoProximoAtaque = carta.espiaActivoProximoAtaque,
                    
                    multDanyoIndefinido = carta.multDanyoIndefinido,
                    bonusDanyoTrampa = carta.bonusDanyoTrampa,
                    multDanyoTrampa = carta.multDanyoTrampa,
                    trampaAplicaAturdimiento = carta.trampaAplicaAturdimiento,
                    trampaAplicaRalentizacion = carta.trampaAplicaRalentizacion,
                    trampaAplicaFuego = carta.trampaAplicaFuego
                };

                if (carta.cardData is DamageableCardData dmgData)
                {
                    saveCard.vidaActual = dmgData.vida;
                    saveCard.ataqueActual = dmgData.ataque;
                    
                    foreach (var efecto in dmgData.efectosDanyo)
                    {
                        saveCard.efectosDanyo.Add(new SaveData_DanyoEfecto
                        {
                            nombre = efecto.nombre,
                            danyo = efecto.danyo,
                            turnosRestantes = efecto.turnosRestantes
                        });
                    }
                }
                
                if (carta.cardData is MonsterCardData mData)
                    saveCard.velocidadActual = mData.velocidad;
                    
                if (carta.cardData is TrapCardData tData)
                    saveCard.turnosTrampaActuales = tData.turnos;

                // Extraer estados internos
                if (carta.pasiva is PassiveNinja pNinja)
                    saveCard.pasiva_ataquesTurno = pNinja.ataquesTurno;
                if (carta.pasiva is PassiveTorreInfernal pTorre)
                {
                    saveCard.pasiva_ataquesTurno = pTorre.ataquesTurno;
                    saveCard.pasiva_ataquesMaximos = pTorre.ataquesMaximos;
                }
                if (carta.pasiva is PassiveTorreMagica pMagica)
                    saveCard.pasiva_ataquesTurno = pMagica.ataquesTurno;
                if (carta.pasiva is PassiveMuroReforzado pMuroR)
                    saveCard.pasiva_reduccionAplicadaInt = pMuroR.reduccionAplicada;
                if (carta.pasiva is PassiveMuro pMuro)
                    saveCard.pasiva_reduccionAplicadaBool = pMuro.reduccionAplicada;

                saveCell.carta = saveCard;
            }
            save.tablero.Add(saveCell);
        }

        string json = JsonUtility.ToJson(save, true);
        string path = Path.Combine(Application.persistentDataPath, "savegame.json");
        File.WriteAllText(path, json);
        Debug.Log("Partida guardada en: " + path);
    }

    /// <summary>
    /// Guarda el estado actual de la partida y regresa a la escena del menú principal.
    /// </summary>
    public void GuardarYSalirAlMenu()
    {
        GuardarPartida();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
    }

    /// <summary>
    /// Carga y restaura el estado de la partida desde el archivo savegame.json.
    /// </summary>
    public void CargarPartida()
    {
        string path = Path.Combine(Application.persistentDataPath, "savegame.json");
        if (!File.Exists(path))
        {
            Debug.LogWarning("No hay partida guardada.");
            return;
        }

        string json = File.ReadAllText(path);
        SaveData_Game save = JsonUtility.FromJson<SaveData_Game>(json);

        // Restaurar nombres de jugadores
        PlayerNameManager.Instance?.CargarNombres(save.nombreP1, save.nombreP2);

        // Restaurar el estado de turno antes de crear las cartas en mano
        TurnManager.turnoP1 = save.turnoP1;
        TurnManager.numTurno = save.numTurno;
        TurnManager.energiaDisponible = save.energiaDisponible;
        TurnManager.bonusHerreriaActiva = save.bonusHerreriaActiva;
        TurnManager.robadoDisponible = save.robadoDisponible;

        // Limpieza
        DeckManager dm = DeckManager.Instance;
        
        // Destruye las cartas en mano
        foreach (Transform child in dm.handPanelP1) Destroy(child.gameObject);
        foreach (Transform child in dm.handPanelP2) Destroy(child.gameObject);
        
        // Destruye las cartas en el tablero y vacía las celdas
        foreach (Cell cell in Board.Instance.cells)
        {
            if (cell.ocupada && cell.cartaActual != null)
            {
                Destroy(cell.cartaActual.gameObject);
            }
            cell.ocupada = false;
            cell.cartaActual = null;
        }

        // Limpia los descartes
        dm.descartadasP1.Clear();
        dm.energiasDescartadasP1.Clear();
        dm.descartadasP2.Clear();
        dm.energiasDescartadasP2.Clear();

        // Limpia los mazos
        dm.deckP1.Clear();
        dm.energyDeckP1.Clear();
        dm.deckP2.Clear();
        dm.energyDeckP2.Clear();

        // Restaura los mazos y descartes
        foreach (string cardName in save.deckP1) dm.deckP1.Enqueue(dm.GetCardDataByName(cardName));
        foreach (string cardName in save.energyDeckP1) dm.energyDeckP1.Enqueue(dm.GetCardDataByName(cardName));
        foreach (string cardName in save.deckP2) dm.deckP2.Enqueue(dm.GetCardDataByName(cardName));
        foreach (string cardName in save.energyDeckP2) dm.energyDeckP2.Enqueue(dm.GetCardDataByName(cardName));
        
        foreach (string cardName in save.discardP1) dm.descartadasP1.Enqueue(dm.GetCardDataByName(cardName));
        foreach (string cardName in save.energyDiscardP1) dm.energiasDescartadasP1.Enqueue(dm.GetCardDataByName(cardName));
        foreach (string cardName in save.discardP2) dm.descartadasP2.Enqueue(dm.GetCardDataByName(cardName));
        foreach (string cardName in save.energyDiscardP2) dm.energiasDescartadasP2.Enqueue(dm.GetCardDataByName(cardName));

        // Restaura las manos
        bool p1Active = dm.handPanelP1.gameObject.activeSelf;
        bool p2Active = dm.handPanelP2.gameObject.activeSelf;
        dm.handPanelP1.gameObject.SetActive(true);
        dm.handPanelP2.gameObject.SetActive(true);

        foreach (string cardName in save.handP1)
        {
            CardUI nuevaCarta = Instantiate(dm.cartaPrefab, dm.handPanelP1);
            nuevaCarta.Setup(dm.GetCardDataByName(cardName));
        }
        foreach (string cardName in save.handP2)
        {
            CardUI nuevaCarta = Instantiate(dm.cartaPrefab, dm.handPanelP2);
            nuevaCarta.Setup(dm.GetCardDataByName(cardName));
        }

        dm.handPanelP1.gameObject.SetActive(p1Active);
        dm.handPanelP2.gameObject.SetActive(p2Active);

        // Restaura el tablero
        foreach (SaveData_Cell saveCell in save.tablero)
        {
            Cell cell = Board.Instance.cells[saveCell.row, saveCell.col];
            if (saveCell.ocupada && saveCell.carta != null)
            {
                SaveData_Card saveCard = saveCell.carta;
                CardData dataOriginal = dm.GetCardDataByName(saveCard.nombrePrefab);
                if (dataOriginal == null) continue;

                Card carta = Instantiate(dm.cartaPrefab.cartaPrefab, cell.transform.position + Vector3.up * 0.1f, Quaternion.Euler(0, 180, 0));
                carta.Setup(dataOriginal);
                carta.transform.localScale = new Vector3(0.285f, 1, 0.445f);
                carta.casilla = cell;
                carta.transform.SetParent(cell.transform);
                if (!saveCard.propietarioP1) carta.transform.Rotate(0, 180, 0);

                // Inicializa la pasiva y la activa
                carta.pasiva = PassiveAbility.Crear(carta.cardData.nombre, carta);
                carta.activa = ActiveAbility.Crear(carta.cardData.nombre, carta);

                // Restaura el estado
                carta.clickableObject.asignarAutomaticamente = false;
                carta.clickableObject.propietarioP1 = saveCard.propietarioP1;
                carta.clickableObject.ultimoMovimiento = saveCard.ultimoMovimiento;
                carta.clickableObject.ultimoAtaque = saveCard.ultimoAtaque;
                carta.clickableObject.usado = saveCard.usado;
                carta.clickableObject.habilidadUsada = saveCard.habilidadUsada;
                carta.clickableObject.turnoColocado = saveCard.turnoColocado;

                carta.invulnerableHastaProximoTurno = saveCard.invulnerableHastaProximoTurno;
                carta.inmuneHechizosIndefinido = saveCard.inmuneHechizosIndefinido;
                carta.bonusDanyoProximoAtaque = saveCard.bonusDanyoProximoAtaque;
                carta.multDanyoProximoAtaque = saveCard.multDanyoProximoAtaque;
                carta.areaProximoAtaque = saveCard.areaProximoAtaque;
                carta.espiaActivoProximoAtaque = saveCard.espiaActivoProximoAtaque;

                carta.multDanyoIndefinido = saveCard.multDanyoIndefinido;
                carta.bonusDanyoTrampa = saveCard.bonusDanyoTrampa;
                carta.multDanyoTrampa = saveCard.multDanyoTrampa;
                carta.trampaAplicaAturdimiento = saveCard.trampaAplicaAturdimiento;
                carta.trampaAplicaRalentizacion = saveCard.trampaAplicaRalentizacion;
                carta.trampaAplicaFuego = saveCard.trampaAplicaFuego;

                if (carta.cardData is DamageableCardData dmgData)
                {
                    dmgData.vida = saveCard.vidaActual;
                    carta.UpdateVida(saveCard.vidaActual);
                    dmgData.ataque = saveCard.ataqueActual;
                    
                    dmgData.efectosDanyo.Clear();
                    foreach (var efecto in saveCard.efectosDanyo)
                    {
                        dmgData.efectosDanyo.Add(new DanyoEfecto(efecto.nombre, efecto.danyo, efecto.turnosRestantes));
                    }
                }

                if (carta.cardData is MonsterCardData mData)
                    carta.UpdateVelocidad(saveCard.velocidadActual);
                
                if (carta.cardData is TrapCardData tData)
                    tData.turnos = saveCard.turnosTrampaActuales;

                // Restaura estados internos de la pasiva
                if (carta.pasiva is PassiveNinja pNinja) pNinja.ataquesTurno = saveCard.pasiva_ataquesTurno;
                if (carta.pasiva is PassiveTorreInfernal pTorre)
                {
                    pTorre.ataquesTurno = saveCard.pasiva_ataquesTurno;
                    pTorre.ataquesMaximos = saveCard.pasiva_ataquesMaximos;
                }
                if (carta.pasiva is PassiveTorreMagica pMagica) pMagica.ataquesTurno = saveCard.pasiva_ataquesTurno;
                if (carta.pasiva is PassiveMuroReforzado pMuroR) pMuroR.reduccionAplicada = saveCard.pasiva_reduccionAplicadaInt;
                if (carta.pasiva is PassiveMuro pMuro) pMuro.reduccionAplicada = saveCard.pasiva_reduccionAplicadaBool;

                carta.RefrescarAtaqueUI();

                cell.cartaActual = carta;
                cell.ocupada = true;
            }
        }

        // Restaura el turno y la UI global
        if (UIManager.textoEnergia != null)
            UIManager.textoEnergia.SetText(TurnManager.energiaDisponible.ToString());

        // Muestra u oculta los paneles de mano según a quién le toque el turno
        dm.handPanelP1.gameObject.SetActive(TurnManager.turnoP1);
        dm.handPanelP2.gameObject.SetActive(!TurnManager.turnoP1);

        TurnManager.ActualizarTextoTurno();

        if (UIManager.textoEnergia != null)
            UIManager.textoEnergia.SetText(TurnManager.energiaDisponible.ToString());
            
        // Actualiza el resaltado de todas las cartas ahora que el turno y las propiedades son correctos
        foreach (Cell cell in Board.Instance.cells)
        {
            if (cell.ocupada && cell.cartaActual != null && cell.cartaActual.clickableObject != null)
            {
                cell.cartaActual.clickableObject.actualizarResaltado();
            }
        }
        if (CameraController.Instance != null)
        {
            CameraController.Instance.VolverAPosicionOriginal();
        }

        // Si la partida ya estaba terminada al guardarla, restaura el estado de fin de partida
        if (save.partidaTerminada && WinManager.Instance != null)
        {
            WinManager.Instance.FinPartida(save.perdedorEsP1);
        }
        else
        {
            // Resalta la mano si la partida sigue en curso
            if (dm.handPanelP1 != null)
            {
                CardSorter sorterP1 = dm.handPanelP1.GetComponent<CardSorter>();
                if (sorterP1 != null) sorterP1.Resaltar();
            }
            if (dm.handPanelP2 != null)
            {
                CardSorter sorterP2 = dm.handPanelP2.GetComponent<CardSorter>();
                if (sorterP2 != null) sorterP2.Resaltar();
            }
        }
        
        Debug.Log("Partida cargada exitosamente.");
    }
}
