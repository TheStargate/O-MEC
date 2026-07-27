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
    private bool partidaTerminada = false; // Cuando termina la partida, se bloquean los paneles de acción
    private Transform objetivoActual; // Indica el objeto al que se quiere mover la cámara
    public static CameraController Instance { get; private set; } // Instancia de la propia cámara para comunicarse con otros scripts

    [SerializeField] private float velocidadDesplazamiento = 20f; // Velocidad a la que se mueve la cámara con el teclado en la visión de tablero
    [SerializeField] private float velocidadZoom = 200f; // Velocidad del zoom con la rueda del ratón en la visión de tablero
    [SerializeField] private float sensibilidadToque = 0.025f; // Sensibilidad del paneo táctil en la visión de tablero
    [SerializeField] private float zoomMinY = 10f; // Zoom mínimo en la visión de tablero
    [SerializeField] private float zoomMaxY = 60f; // Zoom máximo en la visión de tablero
    [SerializeField] private GameObject botonVolverVisionTablero; // Para volver a la posición original de la visión de tablero
    [SerializeField] private GameObject botonVerTablero; // Botón para entrar en visión de tablero
    [SerializeField] private Vector3 posicionVisionTableroDefault = new Vector3(0, 50, 50);
    [SerializeField] private Vector3 rotacionVisionTableroDefaultEuler = new Vector3(90f, 0f, 0f);
    [SerializeField] private float limiteMinX = -15f; // Desplazamiento X mínimo en la visión de tablero
    [SerializeField] private float limiteMaxX = 15f; // Desplazamiento X máximo en la visión de tablero
    [SerializeField] private float limiteMinZ = 30f; // Desplazamiento Z mínimo en la visión de tablero
    [SerializeField] private float limiteMaxZ = 70f; // Desplazamiento Z máximo en la visión de tablero

    void Start()
    {
        // Establece posiciones y rotaciones originales de la cámara
        posicionOriginalCamaraP1 = new Vector3(0, 35, 0);
        rotacionOriginalCamaraP1 = Quaternion.Euler(40f, 0f, 0f);
        posicionOriginalCamaraP2 = new Vector3(0, 35, 100);
        rotacionOriginalCamaraP2 = Quaternion.Euler(40f, 180f, 0f);
        MostrarPanelSegunObjeto(null); // No se muestra ningún panel
        botonVolverVisionTablero.SetActive(false);
        botonVerTablero.SetActive(!enVisionTablero);
        Instance = this;
    }

    void Update()
    {
        bool isPressed = false;
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            isPressed = true;
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            isPressed = true;

        if (isPressed)
            ClickIzquierdo();

        if (moverCamara)
        {
            // Mueve la cámara hacia el objetivo calculado y muestra el panel correspondiente cuando llega
            MoverCamara(objetivoPosicion, objetivoRotacion, () =>
            {
                moverCamara = false;

                if (UIManager?.canvas == null || partidaTerminada)
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
                enVisionTablero = false;
                botonVerTablero.SetActive(!enVisionTablero);
                MostrarPanelSegunObjeto(null); // No se muestra ningún panel
            });
        }

        if (enVisionTablero && !moverCamara && !volverAPosicionOriginal && !pausado)
            ControlarVisionTablero();
    }

    // Maneja el click izquierdo para mover la cámara a un objeto válido
    private void ClickIzquierdo()
    {
        if (UIManager == null || UIManager.canvas == null || UIManager.EstaSobreUI())
            return;

        // Hace un raycast para comprobar si se ha clickado una carta u objeto clickable válido
        Vector2 screenPos = Vector2.zero;
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
        }
        else if (Mouse.current != null)
        {
            screenPos = Mouse.current.position.ReadValue();
        }

        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        Card carta = hit.transform.GetComponent<Card>();
            
        if (carta != null && (!bloqueado || carta.casilla.GetColor() == Color.violet))
            UIManager.SetCartaSeleccionada(carta); // Marca la carta como seleccionada

        if (bloqueado && (carta == null || (carta.casilla.GetColor() != Color.green && carta.casilla.GetColor() != Color.violet)))
            return; // En la visión de tablero no se pueden clickar cartas que no estén resaltadas en verde o violeta

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
        if (!partidaTerminada) botonVolverVisionTablero.SetActive(false);
        botonVerTablero.SetActive(false);
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
            if (!partidaTerminada) botonVolverVisionTablero.gameObject.SetActive(false);

            MostrarPanelSegunObjeto(null); // No se muestra ningún panel
        }
    }

    // Vuelve a la posición y rotación por defecto de la visión de tablero según el turno actual
    public void VolverAPosicionVisionTablero()
    {
        objetivoPosicion = posicionVisionTableroDefault;
        
        if (TurnManager.turnoP1)
            objetivoRotacion = Quaternion.Euler(rotacionVisionTableroDefaultEuler);
        else
            objetivoRotacion = Quaternion.Euler(rotacionVisionTableroDefaultEuler.x, rotacionVisionTableroDefaultEuler.y + 180f, rotacionVisionTableroDefaultEuler.z);
        
        moverCamara = true;
        enVisionTablero = true;
        botonVerTablero.SetActive(!enVisionTablero);

        if (!partidaTerminada) botonVolverVisionTablero.SetActive(false);
    }

    // Controla el movimiento y el zoom de la cámara cuando está en visión completa del tablero.
    private void ControlarVisionTablero()
    {
        if (Camera.main == null)
            return;

        Vector2 inputMovimiento = KeyboardManager.Instance != null ? KeyboardManager.Instance.movimientoCamara : Vector2.zero;
        Vector3 movimiento = new Vector3(inputMovimiento.x, 0f, inputMovimiento.y);

        // Si hay movimiento por teclado, desplaza la cámara en la dirección correspondiente.
        if (movimiento.sqrMagnitude > 0f)
        {
            Vector3 direccionVertical = Vector3.forward;
            Vector3 direccionHorizontal = Vector3.right;

            if (!TurnManager.turnoP1)
            {
                direccionVertical = -direccionVertical;
                direccionHorizontal = -direccionHorizontal;
            }

            Vector3 desplazamiento = (direccionVertical * movimiento.z + direccionHorizontal * movimiento.x) * velocidadDesplazamiento * Time.deltaTime;
            Vector3 nuevaPos = Camera.main.transform.position + desplazamiento;

            nuevaPos.x = Mathf.Clamp(nuevaPos.x, limiteMinX, limiteMaxX);
            nuevaPos.z = Mathf.Clamp(nuevaPos.z, limiteMinZ, limiteMaxZ);

            Camera.main.transform.position = nuevaPos;

            botonVolverVisionTablero.SetActive(true);
        }

        // Contar toques activos para móviles
        int activeTouches = 0;
        if (Touchscreen.current != null)
        {
            foreach (var t in Touchscreen.current.touches)
            {
                if (t.press.isPressed)
                    activeTouches++;
            }
        }

        // Movimiento con un solo dedo en móviles (equivalente a WASD en PC)
        if (!CardUI.estaArrastrando && activeTouches == 1)
        {
            var t0 = Touchscreen.current.primaryTouch;
            Vector2 delta = t0.delta.ReadValue();

            // Solo si hay movimiento apreciable (evita pequeños temblores)
            if (delta.sqrMagnitude > 1f)
            {
                // Invertir ejes: arrastrar en pantalla mueve la cámara hacia el lado contrario
                Vector3 direccionVertical = TurnManager.turnoP1 ? Vector3.forward : Vector3.back;
                Vector3 direccionHorizontal = TurnManager.turnoP1 ? Vector3.right : Vector3.left;

                // Movemos la cámara restando el delta para que actúe como un "arrastre" de la superficie
                Vector3 desplazamiento = (direccionVertical * (-delta.y) + direccionHorizontal * (-delta.x)) * sensibilidadToque;
                Vector3 nuevaPos = Camera.main.transform.position + desplazamiento;

                nuevaPos.x = Mathf.Clamp(nuevaPos.x, limiteMinX, limiteMaxX);
                nuevaPos.z = Mathf.Clamp(nuevaPos.z, limiteMinZ, limiteMaxZ);

                Camera.main.transform.position = nuevaPos;
                botonVolverVisionTablero.SetActive(true);
            }
        }

        if (Mouse.current != null)
        {
            Vector2 scroll = Mouse.current.scroll.ReadValue();

            // Si hay movimiento de la rueda, aumenta o reduce el zoom.
            if (scroll.y != 0f)
            {
                Vector3 direccionZoom = Camera.main.transform.forward;
                Vector3 nuevaPosicion = Camera.main.transform.position + direccionZoom * (scroll.y * velocidadZoom * Time.deltaTime);

                if (nuevaPosicion.y >= zoomMinY && nuevaPosicion.y <= zoomMaxY)
                    Camera.main.transform.position = nuevaPosicion;

                botonVolverVisionTablero.SetActive(true);
            }
        }

        // Control de zoom con dos dedos para móviles
        if (!CardUI.estaArrastrando && activeTouches >= 2)
        {
            UnityEngine.InputSystem.Controls.TouchControl t0 = null;
            UnityEngine.InputSystem.Controls.TouchControl t1 = null;

            foreach (var t in Touchscreen.current.touches)
            {
                if (t.press.isPressed)
                {
                    if (t0 == null) t0 = t;
                    else if (t1 == null) { t1 = t; break; }
                }
            }

            if (t0 != null && t1 != null)
            {
                Vector2 pos0 = t0.position.ReadValue();
                Vector2 pos1 = t1.position.ReadValue();

                Vector2 prevPos0 = pos0 - t0.delta.ReadValue();
                Vector2 prevPos1 = pos1 - t1.delta.ReadValue();

                float delta = Vector2.Distance(pos0, pos1) - Vector2.Distance(prevPos0, prevPos1);

                if (Mathf.Abs(delta) > 0.05f)
                {
                    Vector3 direccionZoom = Camera.main.transform.forward;
                    Vector3 nuevaPosicion = Camera.main.transform.position + direccionZoom * (delta * 0.05f);

                    if (nuevaPosicion.y >= zoomMinY && nuevaPosicion.y <= zoomMaxY)
                        Camera.main.transform.position = nuevaPosicion;

                    botonVolverVisionTablero.SetActive(true);
                }
            }
        }
    }

    // Muestra el panel del objeto clickable seleccionado (o ninguno si se indica null)
    public void MostrarPanelSegunObjeto(ClickableObject clickeable)
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
                    
                    GameObject botonHabilidad = panelMonstruo.transform.Find("Hability").gameObject;
                    if (data.costeHabilidad > 0 && !clickeable.habilidadUsada && clickeable.turnoColocado < TurnManager.numTurno && data.costeHabilidad <= TurnManager.energiaDisponible)
                        botonHabilidad.SetActive(true);
                    else
                        botonHabilidad.SetActive(false);
                    if (clickeable.ultimoAtaque < TurnManager.numTurno && !clickeable.usado && data.alcance > 0 && data.ataque > 0)
                    {
                        GameObject botonAtacar = panelMonstruo.transform.Find("Attack").gameObject;
                        botonAtacar.SetActive(true); // Muestra el botón de atacar si es posible
                        ActualizarTextoAtaque(botonAtacar, carta.pasiva);
                    }
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
                    Card carta = objetivoActual.GetComponent<Card>();
                    StructureCardData data = carta.cardData as StructureCardData;
                    
                    GameObject botonHabilidad = panelEstructura.transform.Find("Hability").gameObject;
                    if (data.costeHabilidad > 0 && !clickeable.habilidadUsada && clickeable.turnoColocado < TurnManager.numTurno && data.costeHabilidad <= TurnManager.energiaDisponible)
                        botonHabilidad.SetActive(true);
                    else
                        botonHabilidad.SetActive(false);
                    if (clickeable.ultimoAtaque < TurnManager.numTurno && !clickeable.usado)
                    {
                        GameObject botonAtacar = panelEstructura.transform.Find("Attack").gameObject;
                        botonAtacar.SetActive(true); // Muestra el botón de atacar si es posible
                        ActualizarTextoAtaque(botonAtacar, carta.pasiva);
                    }
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
                {
                    panelTrampa?.SetActive(true);
                    Card carta = objetivoActual.GetComponent<Card>();
                    TrapCardData data = carta.cardData as TrapCardData;

                    panelTrampa.transform.Find("View")?.gameObject.SetActive(true);

                    GameObject botonHabilidad = panelTrampa.transform.Find("Hability").gameObject;
                    if (data.costeHabilidad > 0 && !clickeable.habilidadUsada && clickeable.turnoColocado < TurnManager.numTurno && data.costeHabilidad <= TurnManager.energiaDisponible)
                        botonHabilidad.SetActive(true);
                    else
                        botonHabilidad.SetActive(false);
                }
                else
                    panelConfirmar?.SetActive(true);
                break;
            case TipoObjeto.MonstruoLeg:
                if (TurnManager.turnoP1 == clickeable.propietarioP1)
                {
                    panelMonstruo?.SetActive(true);
                    Card carta = objetivoActual.GetComponent<Card>();
                    MonsterCardData data = carta.cardData as MonsterCardData;
                    
                    GameObject botonHabilidad = panelMonstruo.transform.Find("Hability").gameObject;
                    if (data.costeHabilidad > 0 && !clickeable.habilidadUsada && clickeable.turnoColocado < TurnManager.numTurno && data.costeHabilidad <= TurnManager.energiaDisponible)
                        botonHabilidad.SetActive(true);
                    else
                        botonHabilidad.SetActive(false);
                    if (clickeable.ultimoAtaque < TurnManager.numTurno && !clickeable.usado && data.alcance > 0 && data.ataque > 0)
                    {
                        GameObject botonAtacar = panelMonstruo.transform.Find("Attack").gameObject;
                        botonAtacar.SetActive(true); // Muestra el botón de atacar si es posible
                        ActualizarTextoAtaque(botonAtacar, carta.pasiva);
                    }
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
        botonVerTablero.SetActive(!enVisionTablero);

        MostrarPanelSegunObjeto(null); // No se muestra ningún panel
    }

    // Refresca el panel actual mostrado (para actualizar el botón de Habilidad cuando se modifica la energía disponible)
    public void RefrescarPanelActual()
    {
        MostrarPanelSegunObjeto(objetivoActual != null ? objetivoActual.GetComponent<ClickableObject>() : null);
    }

    // Llamado al terminar la partida: oculta todos los paneles de acción y evita que vuelvan a aparecer
    public void FinalizarPartida()
    {
        partidaTerminada = true;
        bloqueado = false;
        objetivoActual = null;
        MostrarPanelSegunObjeto(null); // Oculta panelConfirmar y todos los demás paneles
        botonVolverVisionTablero?.SetActive(true); // Permite resetear la cámara libremente al terminar la partida
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
        UIManager.visorCentral.gameObject.SetActive(false);
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

    // Actualiza el texto del botón "Atacar" según si la pasiva permite curar, atacar o ambas
    private void ActualizarTextoAtaque(GameObject boton, PassiveAbility pasiva)
    {
        string texto = "Atacar";
        if (pasiva != null && pasiva.PuedeAtacarAliados)
            texto = pasiva.PuedeAtacarEnemigos ? "Atacar / Curar" : "Curar";

        var textoBoton = boton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (textoBoton != null) textoBoton.text = texto;
    }
}
