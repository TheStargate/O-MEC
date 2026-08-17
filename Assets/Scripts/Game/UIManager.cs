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
            bool overVisorOInfo = false;
            if (EventSystem.current != null && graphicRaycaster != null)
            {
                PointerEventData pointerData = new PointerEventData(EventSystem.current);
                if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
                    pointerData.position = Touchscreen.current.primaryTouch.position.ReadValue();
                else if (Mouse.current != null)
                    pointerData.position = Mouse.current.position.ReadValue();

                List<RaycastResult> results = new List<RaycastResult>();
                graphicRaycaster.Raycast(pointerData, results);

                GameObject infoContainer = textoInfoCarta != null ? textoInfoCarta.transform.parent.parent.parent.gameObject : null;

                foreach (RaycastResult result in results)
                {
                    if (result.gameObject == null) continue;

                    if (result.gameObject.GetComponentInParent<Button>() != null)
                    {
                        overUIButton = true;
                        break;
                    }

                    if (visorCentral != null && (result.gameObject == visorCentral.gameObject || result.gameObject.transform.IsChildOf(visorCentral.transform)))
                    {
                        overVisorOInfo = true;
                        break;
                    }

                    if (infoContainer != null && (result.gameObject == infoContainer || result.gameObject.transform.IsChildOf(infoContainer.transform)))
                    {
                        overVisorOInfo = true;
                        break;
                    }
                }
            }

            if (!overUIButton && !overVisorOInfo)
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

    // Devuelve la carta seleccionada de la mano del jugador
    public Card GetCartaSeleccionada()
    {
        return carta;
    }

    // Gasta la carta seleccionada para obtener energía disponible para usar.
    // Si la carta no es de energía o Monstruo Legendario, se obtiene la mitad de su coste (redondeado hacia abajo).
    public void ActualizarEnergia()
    {
        if (CardUI.cartaUISeleccionada == null || CardUI.cartaUISeleccionada.cartaPrefab == null)
            return;

        CardData data = CardUI.cartaUISeleccionada.cartaPrefab.cardData;
        if (data == null)
            return;

        int energiaGanada = (data.tipo == CardType.Energia || data.tipo == CardType.MonstruoLeg) ? data.costoEnergia : Mathf.FloorToInt(data.costoEnergia / 2f);

        if (DeckManager.Instance != null)
        {
            if (data.tipo == CardType.Energia)
                DeckManager.Instance.descartarEnergia();
            else if(data.tipo != CardType.MonstruoLeg)
                DeckManager.Instance.descartar(data, TurnManager.turnoP1);
        }

        // Actualiza la energía disponible
        TurnManager.energiaDisponible += energiaGanada;
        textoEnergia.SetText(TurnManager.energiaDisponible.ToString());

        // Destruye la carta utilizada
        Destroy(CardUI.cartaUISeleccionada.gameObject);
        visorCentral.gameObject.SetActive(false);
        botonEnergia.gameObject.SetActive(false);

        // Actualiza el resaltado de la mano y del tablero con la nueva energía disponible
        Board.RefrescarResaltados();
        CameraController.Instance.RefrescarPanelActual();
    }

    // Genera el texto de información detallada de una carta del tablero para mostrar en la consola
    public static string GenerarInfoCarta(Card carta)
    {
        if (carta == null || carta.cardData == null) return "";

        var sb = new System.Text.StringBuilder();
        CardData data = carta.cardData;
        ClickableObject co = carta.clickableObject;

        // CABECERA
        sb.AppendLine($"<b>── {data.nombre.ToUpper()} ──</b>");
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

            sb.AppendLine($"Vida: {mData.vida} / {mData.vidaMaxima}");
            sb.AppendLine($"Ataque: {ataqueTotal}" + (ataqueTotal != mData.ataque ? " (modificado)" : ""));
            sb.AppendLine($"Velocidad: {mData.velocidad}" + (velocidadModificada ? " (modificado)" : ""));
            sb.AppendLine($"Alcance: {mData.alcance}");
        }
        else if (data is StructureCardData sData)
        {
            int ataqueTotal = PassiveAbility.CalcularDanyoAtacante(carta, null);
            sb.AppendLine($"Vida: {sData.vida} / {sData.vidaMaxima}");
            sb.AppendLine($"Ataque: {ataqueTotal}" + (ataqueTotal != sData.ataque ? " (modificado)" : ""));
            sb.AppendLine($"Alcance: {sData.alcance}");
        }
        else if (data is TrapCardData tData)
        {
            sb.AppendLine($"Daño de trampa: {tData.ataque}");
            sb.AppendLine($"Durabilidad: {tData.turnos} / {tData.turnosMaximos} turnos");
        }
        sb.AppendLine();

        // MODIFICADORES ACTIVOS
        bool aturdido = co != null && (co.ultimoMovimiento >= TurnManager.numTurno + 2 || co.ultimoAtaque >= TurnManager.numTurno + 2);
        bool hayModificadores = aturdido ||
                                carta.invulnerableHastaProximoTurno ||
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
            sb.AppendLine("<b>[ MODIFICADORES ]</b>");
            if (aturdido) sb.AppendLine("😵 Aturdido hasta el próximo turno");
            if (carta.invulnerableHastaProximoTurno) sb.AppendLine("🛡️ Invulnerable hasta el próximo turno");
            if (carta.inmuneHechizosIndefinido) sb.AppendLine("✨ Inmunidad a hechizos indefinida");
            if (carta.multDanyoIndefinido != 1) sb.AppendLine($"⚔️ Multiplicador de daño indefinido: x{carta.multDanyoIndefinido}");
            if (carta.bonusDanyoProximoAtaque != 0) sb.AppendLine($"⚔️ Bonus del próximo ataque: +{carta.bonusDanyoProximoAtaque}");
            if (carta.multDanyoProximoAtaque != 1) sb.AppendLine($"⚔️ Multiplicador del próximo ataque: x{carta.multDanyoProximoAtaque}");
            if (carta.areaProximoAtaque) sb.AppendLine("💥 Próximo ataque en área");
            if (carta.espiaActivoProximoAtaque) sb.AppendLine("🕵 Bonus de triple de daño al castillo");
            if (carta.bonusDanyoTrampa != 0) sb.AppendLine($"💣 Bonus de daño: +{carta.bonusDanyoTrampa}");
            if (carta.multDanyoTrampa != 1) sb.AppendLine($"💣 Multiplicador de daño: x{carta.multDanyoTrampa}");
            if (carta.trampaAplicaAturdimiento) sb.AppendLine("😵 Aplica Aturdimiento");
            if (carta.trampaAplicaRalentizacion) sb.AppendLine("🐢 Aplica Ralentización");
            if (carta.trampaAplicaFuego > 0) sb.AppendLine($"🔥 Aplica Fuego ({carta.trampaAplicaFuego} daño por turno)");
            sb.AppendLine();
        }

        // EFECTOS DE DAÑO CONTINUO
        if (data is DamageableCardData dData && dData.efectosDanyo != null && dData.efectosDanyo.Count > 0)
        {
            sb.AppendLine("<b>[ DAÑO POR TURNO ]</b>");
            int danyoTotalEfectos = 0;
            foreach (var efecto in dData.efectosDanyo)
            {
                string turnos = efecto.turnosRestantes == -1 ? "∞ turnos" : $"{efecto.turnosRestantes} turnos rest.";
                sb.AppendLine($"🩸 {efecto.nombre}: -{efecto.danyo} vida ({turnos})");
                danyoTotalEfectos += efecto.danyo;
            }
            sb.AppendLine($"Total: {danyoTotalEfectos} daño por turno");
            sb.AppendLine();
        }

        return sb.ToString();
    }

}
