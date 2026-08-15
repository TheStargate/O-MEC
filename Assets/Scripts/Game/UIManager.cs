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

    private GraphicRaycaster graphicRaycaster;

    void Awake()
    {
        // Por defecto no hay ninguna carta seleccionada
        visorCentral = GameObject.Find("Selected Card").GetComponent<Image>();
        visorCentral.gameObject.SetActive(false);
        botonEnergia = GameObject.Find("Use Energy")?.GetComponent<Button>();
        botonEnergia.gameObject.SetActive(false);
        textoEnergia = GameObject.Find("Amount")?.GetComponent<TextMeshProUGUI>();
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
    }

    // Oculta el visor central de una carta
    private void OcultarVisorCentral()
    {
        if (visorCentral != null)
        {
            visorCentral.gameObject.SetActive(false);
            visorCentral.sprite = null;
        }

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

}
