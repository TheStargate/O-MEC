using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    private float velocidadMovimiento = 10f; // Velocidad de la cámara
    public UIManager UIManager; // Para gestionar los cambios en la interfaz

    // Paneles para mostrar los menús de acciones al seleccionar objetos clickables
    [SerializeField] private GameObject panelMonstruo;
    [SerializeField] private GameObject panelEstructura;
    [SerializeField] private GameObject panelHechizo;
    [SerializeField] private GameObject panelTrampa;
    [SerializeField] private GameObject panelEnergia;
    [SerializeField] private GameObject panelBaraja;
    [SerializeField] private GameObject panelMenu;
    [SerializeField] private GameObject panelConfirmar;
    [SerializeField] private GameObject panelPausa; // Para cuando el juego está pausado

    // Objetos Deck y Menu para acceso por teclado
    [SerializeField] private ClickableObject deckP1;
    [SerializeField] private ClickableObject deckP2;
    [SerializeField] private ClickableObject menuP1;
    [SerializeField] private ClickableObject menuP2;

    // Paneles referentes a las manos de cada jugador
    [SerializeField] private Transform handPanelP1;
    [SerializeField] private Transform handPanelP2;
    [SerializeField] private GameObject botonGirar; // Para girar las cartas

    private Vector3 objetivoPosicion; // Posición a la que se quiere mover la cámara al clickar un objeto
    private Quaternion objetivoRotacion; // Rotación a la que se quiere poner la cámara al clickar un objeto

    // Posiciones y rotaciones originales de la cámara de cada jugador
    private Vector3 posicionOriginalCamaraP1;
    private Vector3 posicionOriginalCamaraP2;
    private Quaternion rotacionOriginalCamaraP1;
    private Quaternion rotacionOriginalCamaraP2;

    private bool moverCamara = false; // Indica si se está moviendo la cámara
    private bool bloqueado = false; // Bloquea la cámara si está en visión completa del tablero
    private bool volverAPosicionOriginal = false; // Indica si la cámarra se está moviendo a su posición original
    public bool enVisionTablero = false; // Indica que la cámara está en visión completa del tablero
    private bool pausado; // Indica si el juego está pausado
    private Transform objetivoActual; // Indica el objeto al que se quiere mover la cámara
    public static CameraController Instance; // Instancia de la propia cámara para comunicarse con otros scripts

    void Start()
    {
        // Establece posiciones y rotaciones originales de la cámara
        posicionOriginalCamaraP1 = new Vector3(0, 35, 0);
        rotacionOriginalCamaraP1 = Quaternion.Euler(40f, 0f, 0f);
        posicionOriginalCamaraP2 = new Vector3(0, 35, 100);
        rotacionOriginalCamaraP2 = Quaternion.Euler(40f, 180f, 0f);
        MostrarPanelSegunObjeto(null); // No se muestra ningún panel
        Instance = this;
    }

    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
            ClickIzquierdo();

        if (moverCamara)
        {
            // Mueve la cámara hacia el objetivo calculado y muestra el panel correspondiente cuando llega
            MoverCamara(objetivoPosicion, objetivoRotacion, () =>
            {
                moverCamara = false;

                if (UIManager?.canvas == null)
                    return;

                if (bloqueado) // Si la cámara estaba bloqueada, solo se puede confirmar para volver a la visión de tablero
                    panelConfirmar?.SetActive(true);
                else
                    MostrarPanelSegunObjeto(objetivoActual != null ? objetivoActual.GetComponent<ClickableObject>() : null);
            });
        }

        if (volverAPosicionOriginal)
        {
            // Gestiona el retorno de la cámara a su posición original
            objetivoPosicion = TurnManager.turnoP1 ? posicionOriginalCamaraP1 : posicionOriginalCamaraP2;
            objetivoRotacion = TurnManager.turnoP1 ? rotacionOriginalCamaraP1 : rotacionOriginalCamaraP2;

            MoverCamara(objetivoPosicion, objetivoRotacion, () =>
            {
                volverAPosicionOriginal = false;
                objetivoActual = null;
                MostrarPanelSegunObjeto(null); // No se muestra ningún panel
            });
        }
    }

    // Maneja el click izquierdo para mover la cámara a un objeto válido
    private void ClickIzquierdo()
    {
        if (UIManager == null || UIManager.canvas == null || UIManager.EstaSobreUI())
            return;

        // Hace un raycast para comprobar si se ha clickado una carta u objeto clickable válido
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        Card carta = hit.transform.GetComponent<Card>();
        if (carta != null && !bloqueado)
            UIManager.SetCartaSeleccionada(carta); // Marca la carta como seleccionada

        if (bloqueado && (carta == null || carta.clickableObject.propietarioP1 == TurnManager.turnoP1 || carta.casilla.GetColor() != Color.green))
            return; // En la visión de tablero no se pueden clickar cartas del jugador actual o cartas que no estén resaltadas en verde

        ClickableObject clickeable = hit.transform.GetComponent<ClickableObject>();
        if (clickeable == null)
            return;

        if ((clickeable.tipoObjeto == TipoObjeto.Baraja || clickeable.tipoObjeto == TipoObjeto.Menu) && clickeable.propietarioP1 != TurnManager.turnoP1)
            return; // No se pueden seleccionar barajas o menús de otro jugador

        if (clickeable.tipoObjeto == TipoObjeto.Baraja && !TurnManager.robadoDisponible)
            return; // No se pueden seleccionar barajas si no se permite robar

        if (clickeable.tipoObjeto == TipoObjeto.Menu && TurnManager.numTurno <= 2)
        {
            if ((TurnManager.turnoP1 && handPanelP1.childCount > 0) || (!TurnManager.turnoP1 && handPanelP2.childCount > 0))
                return; // En el primer turno de cada jugador, no se puede seleccionar el menú hasta colocar todas las cartas
        }

        // Después de validar, se marca el objeto clickado como objetivo para mover la cámara
        objetivoActual = hit.transform;

        // Calcula la posición y rotación objetivo de la cámara para el objeto seleccionado.
        Vector3 offset = clickeable.offsetDesdeEsteObjeto;
        if (!clickeable.propietarioP1)
            offset.z *= -1; // Si es del jugador contrario, hay que ver el objeto desde el otro lado

        objetivoPosicion = objetivoActual.position + offset;
        Vector3 direccion = (objetivoActual.position - objetivoPosicion).normalized;
        objetivoRotacion = Quaternion.LookRotation(direccion);

        moverCamara = true;
        volverAPosicionOriginal = false;
        enVisionTablero = false;
        MostrarPanelSegunObjeto(null); // No se muestra ningún panel
    }

    // Mueve la cámara hacia el destino indicado
    void MoverCamara(Vector3 destino, Quaternion rotacionDestino, System.Action onComplete)
    {
        if (!pausado) // Oculta el visor central si el juego no está pausado
            UIManager.visorCentral.gameObject.SetActive(false);
        UIManager.botonEnergia.gameObject.SetActive(false);

        // Mueve la cámara hacia el destino indicado
        Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, destino, Time.deltaTime * velocidadMovimiento);
        Camera.main.transform.rotation = Quaternion.Lerp(Camera.main.transform.rotation, rotacionDestino, Time.deltaTime * velocidadMovimiento);

        // Indica si la cámara ha llegado a su destino
        bool posicionLista = Vector3.Distance(Camera.main.transform.position, destino) < 0.1f;
        bool rotacionLista = Quaternion.Angle(Camera.main.transform.rotation, rotacionDestino) < 1f;

        if (posicionLista && rotacionLista)
            onComplete?.Invoke(); // Ejecuta el método establecido al llegar al destino

    }

    // Lleva la cámara a su posición original
    public void VolverAPosicionOriginal()
    {
        if (!volverAPosicionOriginal)
        {
            volverAPosicionOriginal = true;
            moverCamara = false;
            bloqueado = false;

            MostrarPanelSegunObjeto(null); // No se muestra ningún panel
        }
    }

    // Muestra el panel del objeto clickable seleccionado (o ninguno si se indica null)
    void MostrarPanelSegunObjeto(ClickableObject clickeable)
    {
        // Oculta todos
        panelMonstruo?.SetActive(false);
        panelEstructura?.SetActive(false);
        panelHechizo?.SetActive(false);
        panelTrampa?.SetActive(false);
        panelEnergia?.SetActive(false);
        panelBaraja?.SetActive(false);
        panelMenu?.SetActive(false);
        panelConfirmar?.SetActive(false);
        if (!pausado)
            panelPausa?.SetActive(false);

        if (clickeable == null) return;

        switch (clickeable.tipoObjeto)
        {
            case TipoObjeto.Monstruo:
                if (TurnManager.turnoP1 == clickeable.propietarioP1)
                { // Si la carta es del jugador actual, mostrar opciones disponibles para usarla
                    panelMonstruo?.SetActive(true);
                    Card carta = objetivoActual.GetComponent<Card>();
                    MonsterCardData data = carta.cardData as MonsterCardData;
                    panelMonstruo.transform.Find("Hability").gameObject.SetActive(true);
                    if (clickeable.ultimoAtaque < TurnManager.numTurno && !clickeable.usado && data.alcance > 0 && data.ataque > 0)
                        panelMonstruo.transform.Find("Attack").gameObject.SetActive(true); // Muestra el botón de atacar si es posible
                    else
                        panelMonstruo.transform.Find("Attack").gameObject.SetActive(false);
                    if (clickeable.ultimoMovimiento < TurnManager.numTurno && !clickeable.usado && data.velocidad > 0)
                        panelMonstruo.transform.Find("Move").gameObject.SetActive(true); // Muestra el botón de mover si es posible
                    else
                        panelMonstruo.transform.Find("Move").gameObject.SetActive(false);
                }
                else
                    panelConfirmar?.SetActive(true);
                break;
            case TipoObjeto.Estructura:
                if (TurnManager.turnoP1 == clickeable.propietarioP1)
                { // Si la carta es del jugador actual, mostrar opciones disponibles para usarla
                    panelEstructura?.SetActive(true);
                    if (clickeable.ultimoAtaque < TurnManager.numTurno && !clickeable.usado)
                        panelEstructura.transform.Find("Attack").gameObject.SetActive(true); // Muestra el botón de atacar si es posible
                    else
                        panelEstructura.transform.Find("Attack").gameObject.SetActive(false);
                }
                else
                    panelConfirmar?.SetActive(true);
                break;
            case TipoObjeto.Hechizo:
                if (TurnManager.turnoP1 == clickeable.propietarioP1)
                    panelHechizo?.SetActive(true);
                else
                    panelConfirmar?.SetActive(true);
                break;
            case TipoObjeto.Trampa:
                if (TurnManager.turnoP1 == clickeable.propietarioP1)
                    panelTrampa?.SetActive(true);
                else
                    panelConfirmar?.SetActive(true);
                break;
            case TipoObjeto.MonstruoLeg:
                if (TurnManager.turnoP1 == clickeable.propietarioP1)
                {
                    panelMonstruo?.SetActive(true);
                    Card carta = objetivoActual.GetComponent<Card>();
                    MonsterCardData data = carta.cardData as MonsterCardData;
                    panelMonstruo.transform.Find("Hability").gameObject.SetActive(false);
                    if (clickeable.ultimoAtaque < TurnManager.numTurno && !clickeable.usado && data.alcance > 0 && data.ataque > 0)
                        panelMonstruo.transform.Find("Attack").gameObject.SetActive(true);
                    else
                        panelMonstruo.transform.Find("Attack").gameObject.SetActive(false);
                    if (clickeable.ultimoMovimiento < TurnManager.numTurno && !clickeable.usado && data.velocidad > 0)
                        panelMonstruo.transform.Find("Move").gameObject.SetActive(true);
                    else
                        panelMonstruo.transform.Find("Move").gameObject.SetActive(false);
                }
                else
                    panelConfirmar?.SetActive(true);
                break;
            case TipoObjeto.Energia:
                if (TurnManager.turnoP1 == clickeable.propietarioP1)
                    panelEnergia?.SetActive(true);
                else
                    panelConfirmar?.SetActive(true);
                break;
            case TipoObjeto.Baraja:
                panelBaraja?.SetActive(true);
                break;
            case TipoObjeto.Menu:
                panelMenu?.SetActive(true);
                break;
        }
    }

    // Pone la cámara en visión completa del tablero.
    // mostrarPanel: si es false, la cámara se mueve sin bloquear ni mostrar panelConfirmar al llegar
    public void VisionTablero(bool mostrarPanel = true)
    {
        enVisionTablero = true;
        bloqueado = mostrarPanel;

        objetivoPosicion = new Vector3(0, 50, 50);
        if (TurnManager.turnoP1)
            objetivoRotacion = Quaternion.Euler(90f, 0f, 0f);
        else
            objetivoRotacion = Quaternion.Euler(90f, 0f, 180f);

        moverCamara = true;
        volverAPosicionOriginal = false;

        MostrarPanelSegunObjeto(null); // No se muestra ningún panel
    }

    // Muestra el visor central y pone en pausa el juego
    public void MantenerVisor()
    {
        pausado = true;
        Time.timeScale = 0f;
        botonGirar.SetActive(false);
        panelPausa.SetActive(true);
    }

    // Oculta el visor central y quita la pausa del juego
    public void OcultarVisor()
    {
        pausado = false;
        Time.timeScale = 1f;
        botonGirar.SetActive(true);
        panelPausa.SetActive(false);
    }

    // Mueve la cámara a la baraja según el turno actual
    public void MoverAlDeck()
    {
        ClickableObject deckActual = TurnManager.turnoP1 ? deckP1 : deckP2;
        if (deckActual == null)
            return;

        // Valida que se puede acceder a la baraja
        if (!TurnManager.robadoDisponible)
            return;

        objetivoActual = deckActual.transform;

        // Calcula la posición y rotación objetivo igual que en ClickIzquierdo
        Vector3 offset = deckActual.offsetDesdeEsteObjeto;
        if (!deckActual.propietarioP1)
            offset.z *= -1;

        objetivoPosicion = objetivoActual.position + offset;
        Vector3 direccion = (objetivoActual.position - objetivoPosicion).normalized;
        objetivoRotacion = Quaternion.LookRotation(direccion);

        moverCamara = true;
        volverAPosicionOriginal = false;
        enVisionTablero = false;
        MostrarPanelSegunObjeto(null);
    }

    // Mueve la cámara al Menu según el turno actual
    public void MoverAlMenu()
    {
        ClickableObject menuActual = TurnManager.turnoP1 ? menuP1 : menuP2;
        if (menuActual == null)
            return;

        // Valida que se puede acceder al menú
        if (TurnManager.numTurno <= 2)
        {
            if ((TurnManager.turnoP1 && handPanelP1.childCount > 0) || (!TurnManager.turnoP1 && handPanelP2.childCount > 0))
                return;
        }

        objetivoActual = menuActual.transform;

        // Calcula la posición y rotación objetivo igual que en ClickIzquierdo
        Vector3 offset = menuActual.offsetDesdeEsteObjeto;
        if (!menuActual.propietarioP1)
            offset.z *= -1;

        objetivoPosicion = objetivoActual.position + offset;
        Vector3 direccion = (objetivoActual.position - objetivoPosicion).normalized;
        objetivoRotacion = Quaternion.LookRotation(direccion);

        moverCamara = true;
        volverAPosicionOriginal = false;
        enVisionTablero = false;
        MostrarPanelSegunObjeto(null);
    }
}
