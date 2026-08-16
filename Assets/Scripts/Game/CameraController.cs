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
    [SerializeField] private GameObject panelMenu;
    [SerializeField] private GameObject panelConfirmar;
    [SerializeField] private GameObject panelVolver;
    [SerializeField] private GameObject panelPausa; // Para cuando el juego está pausado

    // Objetos Deck y Menu para acceso por teclado
    [SerializeField] private ClickableObject deckP1;
    [SerializeField] private ClickableObject deckP2;
    [SerializeField] private ClickableObject menuP1;
    [SerializeField] private ClickableObject menuP2;

    // Paneles referentes a las manos de cada jugador
    [SerializeField] private Transform handPanelP1;
    [SerializeField] private Transform handPanelP2;

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
    private bool activarPanelVolver = false; // Si la visión del tablero se activa con casillas violeta, debe mostrar panelVolver en vez de panelConfirmar
    private bool volverAVisionTableroVioleta = false; // Si se sale de la vista violeta para enfocar una carta, volver debe restaurarla.
    public bool enVisionTablero = false; // Indica que la cámara está en visión completa del tablero
    private bool pausado; // Indica si el juego está pausado
    private bool partidaTerminada = false; // Cuando termina la partida, se bloquean los paneles de acción
    private Transform objetivoActual; // Indica el objeto al que se quiere mover la cámara
    private float cooldownRotacion180 = 0f; // Evita disparar la rotación 180º varias veces seguidas
    private bool vistaInvertida = false;    // Indica si la cámara ha sido girada 180º respecto a la posición por defecto
    public static CameraController Instance { get; private set; } // Instancia de la propia cámara para comunicarse con otros scripts

    [SerializeField] private float velocidadDesplazamiento = 20f; // Velocidad a la que se mueve la cámara con el teclado en la visión de tablero
    [SerializeField] private float velocidadZoom = 200f; // Velocidad del zoom con la rueda del ratón en la visión de tablero
    [SerializeField] private float sensibilidadToque = 0.025f; // Sensibilidad del paneo táctil en la visión de tablero
    [SerializeField] private float zoomMinY = 5f; // Zoom mínimo en la visión de tablero
    [SerializeField] private float zoomMaxY = 40f; // Zoom máximo en la visión de tablero
    [SerializeField] private GameObject botonVolverVisionTablero; // Para volver a la posición original de la visión de tablero
    [SerializeField] private Vector3 posicionVisionTablero = new Vector3(0, 40, 50);
    [SerializeField] private Vector3 rotacionVisionTableroDefaultEuler = new Vector3(90f, 0f, 0f);
    [SerializeField] private float limiteMinX = -15f; // Desplazamiento X mínimo en la visión de tablero
    [SerializeField] private float limiteMaxX = 15f; // Desplazamiento X máximo en la visión de tablero
    [SerializeField] private float limiteMinZ = 30f; // Desplazamiento Z mínimo en la visión de tablero
    [SerializeField] private float limiteMaxZ = 70f; // Desplazamiento Z máximo en la visión de tablero

    private float tiempoTactilVioleta = 0f; // Tiempo que lleva pulsado el toque en modo violeta
    private const float tiempoLongPressVioleta = 1f; // Tiempo necesario para long press en modo violeta (1 segundo)
    private bool fueClickLargoPorVioleta = false; // Indica si el último click fue un long press en violeta
    private bool yaProcesoLongPressVioleta = false; // Para evitar procesar el long press múltiples veces en el mismo toque

    void Start()
    {
        // Establece posiciones y rotaciones originales de la cámara
        posicionOriginalCamaraP1 = new Vector3(0, 20, 20);
        rotacionOriginalCamaraP1 = Quaternion.Euler(50f, 0f, 0f);
        posicionOriginalCamaraP2 = new Vector3(0, 20, 80);
        rotacionOriginalCamaraP2 = Quaternion.Euler(50f, 180f, 0f);
        MostrarPanelSegunObjeto(null); // No se muestra ningún panel
        botonVolverVisionTablero.SetActive(false);
        Instance = this;
    }

    void Update()
    {
        // Maneja el long press en modo violeta con touchscreen
        bool modoVioleta = bloqueado && activarPanelVolver;
        bool touchActivo = Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed;
        
        if (modoVioleta && touchActivo)
        {
            tiempoTactilVioleta += Time.deltaTime;
            if (tiempoTactilVioleta >= tiempoLongPressVioleta && !yaProcesoLongPressVioleta)
            {
                // Se han alcanzado 1 segundo: procesar el long press
                fueClickLargoPorVioleta = true;
                yaProcesoLongPressVioleta = true;
                ClickIzquierdo(); // Llamar directamente aquí cuando se alcanza el tiempo
            }
        }
        else if (!touchActivo || !modoVioleta)
        {
            // Soltar o cambiar de modo: resetear
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
            {
                tiempoTactilVioleta = 0f;
                fueClickLargoPorVioleta = false;
                yaProcesoLongPressVioleta = false;
            }
        }

        bool isPressed = false;
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            isPressed = true;
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            isPressed = true;

        if (isPressed && !(modoVioleta && Touchscreen.current != null))
            ClickIzquierdo(); // Solo llamar por click inicial si NO estamos en modo violeta con toque

        if (moverCamara)
        {
            // Mueve la cámara hacia el objetivo calculado y muestra el panel correspondiente cuando llega
            MoverCamara(objetivoPosicion, objetivoRotacion, () =>
            {
                moverCamara = false;

                if (UIManager?.canvas == null || partidaTerminada)
                    return;

                if (bloqueado)
                { // Si la cámara está bloqueada, solo se pude confirmar para volver a la visión de tablero
                    if (activarPanelVolver)
                        panelVolver?.SetActive(true);
                    else
                        panelConfirmar?.SetActive(true);
                }
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
                MostrarPanelSegunObjeto(null); // No se muestra ningún panel
            });
        }

        // Si el jugador mueve la cámara sin estar en visión tablero, se activa automáticamente
        if (!enVisionTablero && !moverCamara && !volverAPosicionOriginal && !pausado)
            DetectarInputParaVisionTablero();

        if (enVisionTablero && !moverCamara && !volverAPosicionOriginal && !pausado)
            ControlarVisionTablero();
    }

    // Maneja el click izquierdo para mover la cámara a un objeto válido
    private void ClickIzquierdo()
    {
        if (UIManager == null || UIManager.canvas == null || UIManager.EstaSobreUI())
            return;

        // En modo violeta, con touchscreen, se requiere un long press para hacer click en cartas
        bool modoVioleta = bloqueado && activarPanelVolver;
        bool isTouchscreen = Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed;
        if (modoVioleta && isTouchscreen && !fueClickLargoPorVioleta)
            return; // Ignorar clicks que no cumplan con long press en modo violeta con toque

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

        if (carta != null && !bloqueado)
            UIManager.SetCartaSeleccionada(carta); // Marca la carta como seleccionada

        ClickableObject clickeable = hit.transform.GetComponent<ClickableObject>();
        bool permiteClickCartaEnVisionTablero = bloqueado && activarPanelVolver && carta != null;
        if (bloqueado && !permiteClickCartaEnVisionTablero && (carta == null || carta.casilla.GetColor() != Color.green))
            return; // En la visión de tablero normal sólo se permiten clicks sobre las casillas verdes. Con la vista violeta, una carta también puede abrirse normalmente

        if (clickeable == null)
            return;

        if (clickeable.tipoObjeto == TipoObjeto.Menu && clickeable.propietarioP1 != TurnManager.turnoP1)
            return; // No se pueden seleccionar el menú de otro jugador

        if (clickeable.tipoObjeto == TipoObjeto.Menu && TurnManager.numTurno <= 2)
        {
            if ((TurnManager.turnoP1 && handPanelP1.childCount > 0) || (!TurnManager.turnoP1 && handPanelP2.childCount > 0))
                return; // En el primer turno de cada jugador, no se puede seleccionar el menú hasta colocar todas las cartas
        }

        // Si la cámara ya está enfocada en este mismo objeto (segundo click) y es de tipo carta,
        // muestra su imagen e información en el visor central
        bool esTipoCarta = clickeable.tipoObjeto == TipoObjeto.Monstruo   ||
                           clickeable.tipoObjeto == TipoObjeto.MonstruoLeg ||
                           clickeable.tipoObjeto == TipoObjeto.Estructura  ||
                           clickeable.tipoObjeto == TipoObjeto.Hechizo     ||
                           clickeable.tipoObjeto == TipoObjeto.Trampa;

        if (!moverCamara && objetivoActual == hit.transform && esTipoCarta && carta != null)
        {
            bool visorActivo = false;
            if (UIManager.visorCentral != null)
            {
                if (UIManager.visorCentral.sprite == carta.cardData.imagenCarta)
                    UIManager.visorCentral.gameObject.SetActive(!UIManager.visorCentral.gameObject.activeSelf);
                else
                {
                    UIManager.visorCentral.sprite = carta.cardData.imagenCarta;
                    UIManager.visorCentral.gameObject.SetActive(true);
                }
                visorActivo = UIManager.visorCentral.gameObject.activeSelf;
            }

            // Muestra la consola de información detallada de la carta
            if (UIManager.textoInfoCarta != null)
            {
                if (visorActivo)
                {
                    string info = UIManager.GenerarInfoCarta(carta);
                    UIManager.textoInfoCarta.text = info;
                    UIManager.textoInfoCarta.transform.parent.parent.parent.gameObject.SetActive(!string.IsNullOrEmpty(info));
                }
                else
                {
                    UIManager.textoInfoCarta.transform.parent.parent.parent.gameObject.SetActive(false);
                }
            }

            return; // No vuelve a mover la cámara
        }

        // Si se estaba en la vista de tablero violeta y se pulsa una carta, hay que salir del bloqueo
        // del tablero para que muestre el menú normal de la carta, pero recordando ese estado para
        // restaurarlo al pulsar volver.
        if (bloqueado && activarPanelVolver && carta != null)
        {
            volverAVisionTableroVioleta = true;
            bloqueado = false;
            fueClickLargoPorVioleta = false; // Resetear el long press tras procesar el click
        }

        // Después de validar, se marca el objeto clickado como objetivo para mover la cámara
        objetivoActual = hit.transform;
        activarPanelVolver = false;

        // Calcula la posición y rotación objetivo de la cámara para el objeto seleccionado.
        Vector3 offset = clickeable.offsetDeObjetoClickable;
        if (!clickeable.propietarioP1)
            offset.z *= -1; // Si es del jugador contrario, hay que ver el objeto desde el otro lado

        objetivoPosicion = objetivoActual.position + offset;
        Vector3 direccion = (objetivoActual.position - objetivoPosicion).normalized;
        objetivoRotacion = Quaternion.LookRotation(direccion);

        moverCamara = true;
        volverAPosicionOriginal = false;
        enVisionTablero = false;
        if (!partidaTerminada) botonVolverVisionTablero.SetActive(false);
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
        bool rotacionLista = Quaternion.Angle(Camera.main.transform.rotation, rotacionDestino) < 0.001f;

        if (posicionLista && rotacionLista)
            onComplete?.Invoke(); // Ejecuta el método establecido al llegar al destino

    }

    // Lleva la cámara a su posición original
    public void VolverAPosicionOriginal()
    {
        if (volverAVisionTableroVioleta)
        {
            volverAVisionTableroVioleta = false;
            objetivoActual = null;
            moverCamara = false;
            bloqueado = true;
            activarPanelVolver = true;
            enVisionTablero = true;
            VisionTablero(true);
            Board.Instance?.ResaltarCasillasVioleta();
            return;
        }

        if (!volverAPosicionOriginal)
        {
            volverAPosicionOriginal = true;
            moverCamara = false;
            bloqueado = false;
            activarPanelVolver = false;
            if (!partidaTerminada) botonVolverVisionTablero.gameObject.SetActive(false);
            volverAVisionTableroVioleta = false;
            MostrarPanelSegunObjeto(null); // No se muestra ningún panel
        }
    }

    // Vuelve a enfocar la carta que acaba de atacar o moverse para continuar con esa carta
    public void VolverACarta(Card carta)
    {
        if (carta == null || carta.clickableObject == null || carta.transform == null)
            return;

        UIManager?.SetCartaSeleccionada(carta);
        objetivoActual = carta.transform;
        activarPanelVolver = false;

        Vector3 offset = carta.clickableObject.offsetDeObjetoClickable;
        if (!carta.clickableObject.propietarioP1)
            offset.z *= -1;

        objetivoPosicion = carta.transform.position + offset;
        Vector3 direccion = (carta.transform.position - objetivoPosicion).normalized;
        objetivoRotacion = Quaternion.LookRotation(direccion);

        moverCamara = true;
        volverAPosicionOriginal = false;
        enVisionTablero = false;
        if (!partidaTerminada) botonVolverVisionTablero.SetActive(false);
        MostrarPanelSegunObjeto(null);
    }

    public void ResetearEstadoVisionTableroVioleta()
    {
        volverAVisionTableroVioleta = false;
    }

    // Vuelve a la posición y rotación por defecto de la visión de tablero según el turno actual
    public void VolverAPosicionVisionTablero()
    {
        objetivoPosicion = posicionVisionTablero;
        
        if (TurnManager.turnoP1)
            objetivoRotacion = Quaternion.Euler(rotacionVisionTableroDefaultEuler);
        else
            objetivoRotacion = Quaternion.Euler(rotacionVisionTableroDefaultEuler.x, rotacionVisionTableroDefaultEuler.y + 180f, rotacionVisionTableroDefaultEuler.z);
        
        moverCamara = true;
        enVisionTablero = true;

        if (!partidaTerminada) botonVolverVisionTablero.SetActive(false);
    }

    // Detecta si el jugador está intentando mover la cámara estando en la posición por defecto para activar automáticamente la visión del tablero
    private void DetectarInputParaVisionTablero()
    {
        // Solo actua si la cámara está en la posición por defecto (no mirando ningún objeto)
        if (objetivoActual != null)
            return;

        bool hayInput = false;

        // WASD / flechas de teclado
        if (KeyboardManager.Instance != null && KeyboardManager.Instance.movimientoCamara.sqrMagnitude > 0f)
            hayInput = true;

        // Rueda del ratón (scroll)
        if (!hayInput && Mouse.current != null && Mouse.current.scroll.ReadValue().y != 0f)
            hayInput = true;

        // Detección táctil en móvil (solo se activa con 2 o más dedos)
        if (!hayInput && Touchscreen.current != null)
        {
            int activeTouches = 0;
            foreach (var t in Touchscreen.current.touches)
                if (t.press.isPressed) activeTouches++;

            // Dos o más dedos en móvil
            if (!CardUI.estaArrastrando && activeTouches >= 2)
                hayInput = true;
        }

        if (hayInput)
        {
            // Activa la visión completa del tablero con casillas resaltadas en violeta y muestra el panel de volver.
            volverAVisionTableroVioleta = false;
            activarPanelVolver = true;
            VisionTablero();
            Board.Instance?.ResaltarCasillasVioleta();
        }
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
            // Las direcciones se invierten si el turno es de P2 o si la cámara ha sido girada 180º
            bool perspectivaNormal = TurnManager.turnoP1 != vistaInvertida;
            Vector3 direccionVertical   = perspectivaNormal ? Vector3.forward : Vector3.back;
            Vector3 direccionHorizontal = perspectivaNormal ? Vector3.right   : Vector3.left;

            float zoomFactor = Mathf.InverseLerp(zoomMinY, zoomMaxY, Camera.main.transform.position.y);
            float velocidadActual = velocidadDesplazamiento * Mathf.Lerp(0.5f, 1.8f, zoomFactor);

            Vector3 desplazamiento = (direccionVertical * movimiento.z + direccionHorizontal * movimiento.x) * velocidadActual * Time.deltaTime;
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
                // También se invierte si la cámara ha sido girada 180º con Q/E
                bool perspectivaNormal = TurnManager.turnoP1 != vistaInvertida;
                Vector3 direccionVertical   = perspectivaNormal ? Vector3.forward : Vector3.back;
                Vector3 direccionHorizontal = perspectivaNormal ? Vector3.right   : Vector3.left;

                float zoomFactor = Mathf.InverseLerp(zoomMinY, zoomMaxY, Camera.main.transform.position.y);
                float sensibilidadActual = sensibilidadToque * Mathf.Lerp(0.5f, 1.8f, zoomFactor);

                // Movemos la cámara restando el delta para que actúe como un "arrastre" de la superficie
                Vector3 desplazamiento = (direccionVertical * (-delta.y) + direccionHorizontal * (-delta.x)) * sensibilidadActual;
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

        // Control de zoom con dos dedos para móviles + detección de giro brusco 180º
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

                // Zoom (pinch)
                float deltaPinch = Vector2.Distance(pos0, pos1) - Vector2.Distance(prevPos0, prevPos1);
                if (Mathf.Abs(deltaPinch) > 0.05f)
                {
                    Vector3 direccionZoom = Camera.main.transform.forward;
                    Vector3 nuevaPosicion = Camera.main.transform.position + direccionZoom * (deltaPinch * 0.05f);

                    if (nuevaPosicion.y >= zoomMinY && nuevaPosicion.y <= zoomMaxY)
                        Camera.main.transform.position = nuevaPosicion;

                    botonVolverVisionTablero.SetActive(true);
                }

                // Rotación brusca con dos dedos: si detecta un giro rápido rota la cámara 180º
                float anguloActual  = Mathf.Atan2(pos1.y - pos0.y, pos1.x - pos0.x) * Mathf.Rad2Deg;
                float anguloAnterior = Mathf.Atan2(prevPos1.y - prevPos0.y, prevPos1.x - prevPos0.x) * Mathf.Rad2Deg;
                float deltaAngulo = Mathf.DeltaAngle(anguloAnterior, anguloActual);

                if (cooldownRotacion180 <= 0f && Mathf.Abs(deltaAngulo) > 5f)
                    RotarCamara180();
            }
        }

        // Rotación 180º de la cámara con Q o E (teclado)
        if (cooldownRotacion180 <= 0f && Keyboard.current != null &&
            (Keyboard.current.qKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame))
            RotarCamara180();

        // Decrementar cooldown de rotación
        if (cooldownRotacion180 > 0f)
            cooldownRotacion180 -= Time.deltaTime;
    }

    // Rota la cámara 180º alrededor del centro del tablero (cambia de perspectiva)
    private void RotarCamara180()
    {
        cooldownRotacion180 = 0.1f; // 0.1 segundos de cooldown para evitar dobles activaciones
        vistaInvertida = !vistaInvertida; // Alterna el estado de inversión

        Vector3 posActual = Camera.main.transform.position;
        float centroZ = (limiteMinZ + limiteMaxZ) / 2f; // Centro del tablero en Z

        // Reflejar la posición alrededor del centro del tablero (x=0, z=centroZ)
        objetivoPosicion = new Vector3(-posActual.x, posActual.y, 2f * centroZ - posActual.z);

        // Seleccionar la rotación exacta predefinida según el turno y el estado de inversión
        // perspectivaNormal = true: rotación P1 (Euler 90,0,0)
        // perspectivaNormal = false: rotación P2 (Euler 90,0,180)
        bool perspectivaNormal = TurnManager.turnoP1 != vistaInvertida;
        objetivoRotacion = perspectivaNormal
            ? Quaternion.Euler(90f, 0f, 0f)
            : Quaternion.Euler(90f, 0f, 180f);

        moverCamara = true;
        botonVolverVisionTablero.SetActive(true);
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
        panelMenu?.SetActive(false);
        panelConfirmar?.SetActive(false);
        panelVolver?.SetActive(false);
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
                    panelVolver?.SetActive(true);
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
                    panelVolver?.SetActive(true);
                break;
            case TipoObjeto.Hechizo:
                if (TurnManager.turnoP1 == clickeable.propietarioP1)
                    panelHechizo?.SetActive(true);
                else
                    panelVolver?.SetActive(true);
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
                    panelVolver?.SetActive(true);
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
                    panelVolver?.SetActive(true);
                break;
            case TipoObjeto.Energia:
                if (TurnManager.turnoP1 == clickeable.propietarioP1)
                    panelEnergia?.SetActive(true);
                else
                    panelVolver?.SetActive(true);
                break;
            case TipoObjeto.Menu:
                panelMenu?.SetActive(true);
                break;
        }
    }

    // Según la velocidad / alcance de la carta, se aplica una cantidad de zoom a la cámara para mostrar hasta donde puede llegar
    private float CalcularDistanciaFocoCarta(Card carta)
    {
        if (carta == null)
            return 20f;

        int valorBase = 0;
        bool atacando = Board.Instance != null && Board.Instance.EstaSeleccionandoAtaque;

        if (carta.cardData is MonsterCardData monsterData)
            valorBase = atacando ? monsterData.alcance : monsterData.velocidad;
        else if (carta.cardData is DamageableCardData damageData)
            valorBase = damageData.alcance;

        // Si supera 4, la cámara no necesita enfocarse en la carta y debe centrarse en el tablero como en la vista normal.
        if (valorBase > 4)
            return -1f;

        if (valorBase <= 1) return 10f;
        if (valorBase == 2) return 20f;
        if (valorBase == 3) return 30f;
        return 40f;
    }

    // Para saber si ya hay una carta para enfocar
    public bool TieneObjetivoEnfocado(Transform objetivo)
    {
        return objetivo != null && objetivoActual != null && objetivoActual == objetivo;
    }

    // Pone la cámara en visión completa del tablero.
    // mostrarPanel: si es false, la cámara se mueve sin bloquear ni mostrar panelConfirmar al llegar.
    // objetivoEnfocado: si se indica, la cámara se centra en ese objeto antes de la vista general del tablero.
    public void VisionTablero(bool mostrarPanel = true, Transform objetivoEnfocado = null)
    {
        enVisionTablero = true;
        bloqueado = mostrarPanel;
        vistaInvertida = false; // Siempre se empieza con la orientación por defecto del jugador

        if (objetivoEnfocado != null)
        {
            Card cartaEnfocada = objetivoEnfocado.GetComponent<Card>();
            float distanciaFoco = CalcularDistanciaFocoCarta(cartaEnfocada);

            if (distanciaFoco > 0f)
            {
                objetivoActual = objetivoEnfocado;
                Vector3 offset = new Vector3(0f, distanciaFoco, 0f);
                objetivoPosicion = objetivoEnfocado.position + offset;
            }
            else
            {
                objetivoActual = null;
                objetivoPosicion = posicionVisionTablero;
            }
        }
        else
        {
            objetivoActual = null;
            objetivoPosicion = posicionVisionTablero;
        }

        if (TurnManager.turnoP1)
            objetivoRotacion = Quaternion.Euler(90f, 0f, 0f);
        else
            objetivoRotacion = Quaternion.Euler(90f, 0f, 180f);

        moverCamara = true;
        volverAPosicionOriginal = false;

        if (!mostrarPanel)
            activarPanelVolver = false;

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
        activarPanelVolver = false;
        MostrarPanelSegunObjeto(null); // Oculta panelConfirmar y todos los demás paneles
        botonVolverVisionTablero?.SetActive(true); // Permite resetear la cámara libremente al terminar la partida
    }
    
    // Muestra el visor central y pone en pausa el juego
    public void MantenerVisor()
    {
        pausado = true;
        Time.timeScale = 0f;
        panelPausa.SetActive(true);
    }

    // Oculta el visor central y quita la pausa del juego
    public void OcultarVisor()
    {
        UIManager.visorCentral.gameObject.SetActive(false);
        pausado = false;
        Time.timeScale = 1f;
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
        Vector3 offset = deckActual.offsetDeObjetoClickable;
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
        Vector3 offset = menuActual.offsetDeObjetoClickable;
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
