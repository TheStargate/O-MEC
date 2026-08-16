using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public Canvas canvas; // Interfaz del jugador
    private Card carta; // Carta de la mano del jugador actual que se ha seleccionado
    [SerializeField] private Transform handPanelP1; // Contiene las cartas de la mano del jugador 1
    [SerializeField] private Transform handPanelP2; // Contiene las cartas de la mano del jugador 2
    public static Image visorCentral; // Imagen de la carta seleccionada de la mano del jugador que aparece en el centro de la pantalla
    public static Button botonEnergia; // Botón que aparece al seleccionar una carta de energía para poder usarla
    public static TextMeshProUGUI textoEnergia; // Indica la cantidad de energía disponible para usar
    public static TextMeshProUGUI textoInfoCarta; // Cuadro de texto con la información de la carta seleccionada

    private GraphicRaycaster graphicRaycaster;

    void Awake()
    {
        // Por defecto no hay ninguna carta seleccionada
        visorCentral = GameObject.Find("Selected Card").GetComponent<Image>();
        visorCentral.gameObject.SetActive(false);
        botonEnergia = GameObject.Find("Use Energy")?.GetComponent<Button>();
        botonEnergia.gameObject.SetActive(false);
        textoEnergia = GameObject.Find("Amount")?.GetComponent<TextMeshProUGUI>();
        textoInfoCarta = GameObject.Find("Card Info Text")?.GetComponent<TextMeshProUGUI>();
        if (textoInfoCarta != null) textoInfoCarta.transform.parent.parent.parent.gameObject.SetActive(false);
        graphicRaycaster = canvas != null ? canvas.GetComponentInParent<GraphicRaycaster>() : null;
    }

    void Update()
    {
        bool pointerPressed = false;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            pointerPressed = true;
        else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            pointerPressed = true;

        // Si el visor central está activo y se pulsa fuera de cualquier botón, se oculta
        if (visorCentral != null && visorCentral.gameObject.activeSelf && pointerPressed && Time.timeScale != 0f)
        {
            bool overUIButton = false;
            if (EventSystem.current != null && graphicRaycaster != null)
            {
                PointerEventData pointerData = new PointerEventData(EventSystem.current);
                if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
                    pointerData.position = Touchscreen.current.primaryTouch.position.ReadValue();
                else if (Mouse.current != null)
                    pointerData.position = Mouse.current.position.ReadValue();

                List<RaycastResult> results = new List<RaycastResult>();
                graphicRaycaster.Raycast(pointerData, results);

                foreach (RaycastResult result in results)
                {
                    if (result.gameObject == null) continue;
                    if (result.gameObject.GetComponentInParent<Button>() != null)
                    {
                        overUIButton = true;
                        break;
                    }
                }
            }

            if (!overUIButton)
                OcultarVisorCentral();
        }

        // Sincroniza la visibilidad del contenedor de información con el visorCentral:
        // si el visor central se oculta desde cualquier script, ocultamos su panel padre
        if (textoInfoCarta != null && visorCentral != null)
        {
            if (!visorCentral.gameObject.activeSelf && textoInfoCarta.transform.parent.gameObject.activeSelf)
            {
                textoInfoCarta.transform.parent.parent.parent.gameObject.SetActive(false);
            }
        }
    }

    // Oculta el visor central de una carta
    private void OcultarVisorCentral()
    {
        if (visorCentral != null)
        {
            visorCentral.gameObject.SetActive(false);
            visorCentral.sprite = null;
        }

        if (textoInfoCarta != null)
            textoInfoCarta.transform.parent.parent.parent.gameObject.SetActive(false);

        CardUI.cartaUISeleccionada = null;
        if (botonEnergia != null)
            botonEnergia.gameObject.SetActive(false);
    }

    // Comprueba si el cursor está sobre un elemento de la UI
    public bool EstaSobreUI()
    {
        // Se obtiene la posición actual del ratón
        Vector2 screenPos = Vector2.zero;
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
        }
        else if (Mouse.current != null)
        {
            screenPos = Mouse.current.position.ReadValue();
        }

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPos
        };

        // Se hace un "raycast" para ver si intercepta algún objeto del canvas
        GraphicRaycaster raycaster = canvas.GetComponentInParent<GraphicRaycaster>();
        List<RaycastResult> resultados = new List<RaycastResult>();
        if (raycaster == null)
            return false;

        raycaster.Raycast(pointerData, resultados);

        foreach (RaycastResult resultado in resultados)
        {
            // Se comprueba si los objetos interceptados están visibles
            GameObject objeto = resultado.gameObject;
            if (objeto.gameObject.activeInHierarchy)
                return true;
        }

        return false;
    }

    // Establece la carta seleccionada de la mano del jugador al hacer click en ella
    public void SetCartaSeleccionada(Card c)
    {
        carta = c;
    }

    // Devuelve la carta seleccionada de la mano del jugadors
    public Card GetCartaSeleccionada()
    {
        return carta;
    }

    // Gasta la carta de energía seleccionada y actualiza la energía disponible para usar
    public void ActualizarEnergia()
    {
        CardData data = CardUI.cartaUISeleccionada.cartaPrefab.cardData;
        if (data.tipo == CardType.Energia)
        {
            // Actualiza la energía disponible
            TurnManager.energiaDisponible += data.costoEnergia;
            textoEnergia.SetText(TurnManager.energiaDisponible.ToString());

            // Destruye la carta de energía utilizada
            Destroy(CardUI.cartaUISeleccionada.gameObject);
            visorCentral.gameObject.SetActive(false);
            botonEnergia.gameObject.SetActive(false);

            // Actualiza el resaltado de la mano y del tablero con la nueva energía disponible
            Board.RefrescarResaltados();
            CameraController.Instance.RefrescarPanelActual();
        }
    }

    // Genera el texto de información detallada de una carta del tablero para mostrar en la consola
    public static string GenerarInfoCarta(Card carta)
    {
        if (carta == null || carta.cardData == null) return "";

        var sb = new System.Text.StringBuilder();
        CardData data = carta.cardData;
        ClickableObject co = carta.clickableObject;
        string propietario = (co != null)
            ? (co.propietarioP1 ? "Jugador 1" : "Jugador 2")
            : "?";

        // CABECERA
        sb.AppendLine($"<b>── {data.nombre.ToUpper()} ──</b>");
        sb.AppendLine($"Tipo: {data.tipo}   |   Propietario: {propietario}");
        sb.AppendLine($"Turno actual: {TurnManager.numTurno}");
        sb.AppendLine();

        // ESTADÍSTICAS BASE
        sb.AppendLine("<b>[ ESTADÍSTICAS ]</b>");
        if (data is MonsterCardData mData)
        {
            int ataqueTotal = PassiveAbility.CalcularDanyoAtacante(carta, null);
            
            // Obtener la velocidad original de la carta si DeckManager existe
            int velocidadOriginal = mData.velocidad;
            if (DeckManager.Instance != null)
            {
                MonsterCardData originalData = DeckManager.Instance.GetCardDataByName(data.nombre) as MonsterCardData;
                if (originalData != null)
                    velocidadOriginal = originalData.velocidad;
            }
            bool velocidadModificada = mData.velocidad != velocidadOriginal;

            sb.AppendLine($"  Vida:      {mData.vida} / {mData.vidaMaxima}");
            sb.AppendLine($"  Ataque:    {mData.ataque}" + (ataqueTotal != mData.ataque ? $" (modificado a <color=yellow>{ataqueTotal}</color>)" : ""));
            sb.AppendLine($"  Velocidad: {mData.velocidad}" + (velocidadModificada ? $" (modificado a <color=yellow>{mData.velocidad}</color>)" : "") + $"   |   Alcance: {mData.alcance}");
        }
        else if (data is StructureCardData sData)
        {
            int ataqueTotal = PassiveAbility.CalcularDanyoAtacante(carta, null);
            sb.AppendLine($"  Vida:   {sData.vida} / {sData.vidaMaxima}");
            sb.AppendLine($"  Ataque: {sData.ataque}" + (ataqueTotal != sData.ataque ? $" (modificado a <color=yellow>{ataqueTotal}</color>)" : ""));
            sb.AppendLine($"  Alcance: {sData.alcance}");
        }
        else if (data is TrapCardData tData)
        {
            sb.AppendLine($"  Daño de trampa: {tData.ataque}");
            sb.AppendLine($"  Durabilidad: {tData.turnos} / {tData.turnosMaximos} turnos");
        }
        if (data.costeHabilidad > 0)
            sb.AppendLine($"  Coste habilidad: {data.costeHabilidad} energía");
        sb.AppendLine();

        // ESTADO DE TURNO
        if (co != null)
        {
            sb.AppendLine("<b>[ ESTADO DE TURNO ]</b>");
            sb.AppendLine($"  Turno colocada:    {co.turnoColocado}");
            
            // Comprobación de Aturdimiento: si el último movimiento y el ataque están bloqueados más allá del turno actual + 1
            bool aturdido = co.ultimoMovimiento >= TurnManager.numTurno + 2 || co.ultimoAtaque >= TurnManager.numTurno + 2;
            if (aturdido)
                sb.AppendLine("  😵 <color=red><b>¡ATURDIDO!</b></color> (No puede moverse ni atacar este turno)");

            bool puedeMoverse = co.ultimoMovimiento < TurnManager.numTurno;
            bool puedeAtacar  = co.ultimoAtaque  < TurnManager.numTurno;
            sb.AppendLine($"  Puede moverse:  " + (puedeMoverse ? "<color=green>Sí</color>" : "<color=red>No</color>") + $"  (último movimiento en turno {co.ultimoMovimiento})");
            sb.AppendLine($"  Puede atacar:   " + (puedeAtacar  ? "<color=green>Sí</color>" : "<color=red>No</color>") + $"  (último ataque en turno {co.ultimoAtaque})");
            if (carta.activa != null)
                sb.AppendLine($"  Habilidad activa: " + (co.habilidadUsada ? "<color=red>YA USADA este turno</color>" : "<color=green>Disponible</color>"));
            sb.AppendLine($"  Usada (sin acciones): " + (co.usado ? "<color=gray>Sí</color>" : "No"));
            sb.AppendLine();
        }

        // MODIFICADORES ACTIVOS
        bool hayModificadores = carta.invulnerableHastaProximoTurno ||
                                carta.inmuneHechizosIndefinido ||
                                carta.bonusDanyoProximoAtaque != 0 ||
                                carta.multDanyoProximoAtaque != 1 ||
                                carta.areaProximoAtaque ||
                                carta.espiaActivoProximoAtaque ||
                                carta.multDanyoIndefinido != 1 ||
                                carta.bonusDanyoTrampa != 0 ||
                                carta.multDanyoTrampa != 1 ||
                                carta.trampaAplicaAturdimiento ||
                                carta.trampaAplicaRalentizacion ||
                                carta.trampaAplicaFuego > 0;

        if (hayModificadores)
        {
            sb.AppendLine("<b>[ MODIFICADORES ACTIVOS ]</b>");
            if (carta.invulnerableHastaProximoTurno) sb.AppendLine("  🛡 Invulnerable hasta el próximo turno");
            if (carta.inmuneHechizosIndefinido)      sb.AppendLine("  ✨ Inmunidad a hechizos indefinida");
            if (carta.multDanyoIndefinido != 1)      sb.AppendLine($"  ⚔ Multiplicador de daño indefinido: x{carta.multDanyoIndefinido}");
            if (carta.bonusDanyoProximoAtaque != 0)  sb.AppendLine($"  ⚔ Bonus del próximo ataque: +{carta.bonusDanyoProximoAtaque}");
            if (carta.multDanyoProximoAtaque != 1)   sb.AppendLine($"  ⚔ Multiplicador del próximo ataque: x{carta.multDanyoProximoAtaque}");
            if (carta.areaProximoAtaque)             sb.AppendLine("  💥 Próximo ataque en área");
            if (carta.espiaActivoProximoAtaque)      sb.AppendLine("  🕵 Bonus de triple de daño al castillo");
            if (carta.bonusDanyoTrampa != 0)         sb.AppendLine($"  💣 Bonus de daño: +{carta.bonusDanyoTrampa}");
            if (carta.multDanyoTrampa != 1)          sb.AppendLine($"  💣 Multiplicador de daño: x{carta.multDanyoTrampa}");
            if (carta.trampaAplicaAturdimiento)      sb.AppendLine("  😵 Aplica Aturdimiento");
            if (carta.trampaAplicaRalentizacion)     sb.AppendLine("  🐢 Aplica Ralentización");
            if (carta.trampaAplicaFuego > 0)         sb.AppendLine($"  🔥 Aplica Fuego ({carta.trampaAplicaFuego} daño por turno)");
            sb.AppendLine();
        }

        // EFECTOS DE DAÑO CONTINUO
        if (data is DamageableCardData dData && dData.efectosDanyo != null && dData.efectosDanyo.Count > 0)
        {
            sb.AppendLine("<b>[ EFECTOS DE DAÑO CONTINUO ]</b>");
            int danyoTotalEfectos = 0;
            foreach (var efecto in dData.efectosDanyo)
            {
                string turnos = efecto.turnosRestantes == -1 ? "∞ turnos" : $"{efecto.turnosRestantes} turnos rest.";
                sb.AppendLine($"  🩸 {efecto.nombre}: -{efecto.danyo} daño por turno ({turnos})");
                danyoTotalEfectos += efecto.danyo;
            }
            sb.AppendLine($"  Total daño continuo: -{danyoTotalEfectos} daño este turno");
            sb.AppendLine();
        }

        // HABILIDADES
        if (carta.pasiva != null || carta.activa != null)
        {
            sb.AppendLine("<b>[ HABILIDADES ]</b>");
            if (carta.pasiva != null)
                sb.AppendLine($"  Pasiva: {carta.pasiva.GetType().Name}");
            if (carta.activa != null)
                sb.AppendLine($"  Activa: {carta.activa.GetType().Name}" +
                    (data.costeHabilidad > 0 ? $" (coste: {data.costeHabilidad})" : ""));
        }

        return sb.ToString();
    }

}
