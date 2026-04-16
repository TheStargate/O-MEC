using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Unity.VisualScripting;
using System.Collections.Generic;

public class CardUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] public Card cartaPrefab; // Prefab de referencia de la carta que se va a mostrar
    private Canvas canvas; // Interfaz del jugador
    private RectTransform rectTransform; // Se usa para mover / arrastrar la carta con el ratón
    private CanvasGroup canvasGroup; // Para controlar la carta al arrastrarla
    private Vector2 posicionInicial; // Posición inicial de la carta en la mano del jugador
    [SerializeField] public Image imagenUI; // Imagen de la carta que se muestra
    [SerializeField] public Sprite spriteReverso; // Parte de atrás de la carta
    private Cell casillaAnterior = null; // Indica la casilla anterior seleccionada para colocar la carta arrastrada
    private bool girada; // Indica si se debe mostrar la parte de atrás de la carta
    public static CardUI cartaUISeleccionada; // Indica la carta seleccionada de la mano del jugador
    private CardSorter sorter; // Para ordenar y resaltar las cartas de la mano
    private List<Cell> casillasAreaActual = new List<Cell>(); // Casillas que se resaltan al arrastrar una carta que actúa en un área

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
    }

    // Configura la carta UI con datos indicados y le asigna el prefab 3D
    public void Setup(CardData data)
    {
        cartaPrefab = Instantiate(cartaPrefab, transform);
        cartaPrefab.Setup(data);
        name = data.nombre;

        // Pone la carta girada si el resto también lo están
        if ((TurnManager.turnoP1 && UIManager.giradasP1) || (!TurnManager.turnoP1 && UIManager.giradasP2))
        {
            imagenUI.sprite = spriteReverso;
            girada = true;
        }
        else
        {
            imagenUI.sprite = data.imagenCarta;
            girada = false;
        }

        GameObject panel;
        if (TurnManager.turnoP1)
            panel = GameObject.Find("Hand Panel P1");
        else
            panel = GameObject.Find("Hand Panel P2");
        if (panel != null)
        {
            sorter = panel.GetComponent<CardSorter>();
            sorter?.Ordenar(); // Ordena la nueva carta
        }
    }

    // Oculta o muestra la carta en el centro de la interfaz al hacer click en ella
    public void OnPointerClick(PointerEventData eventData)
    {
        if (UIManager.visorCentral != null)
        {
            // Oculta la carta clickada si ya se estaba mostrando en el visor central
            if (UIManager.visorCentral.sprite == imagenUI.sprite)
                UIManager.visorCentral.gameObject.SetActive(!UIManager.visorCentral.gameObject.activeSelf);
            else
            { // Muestra la imagen de la carta clickada en el visor central
                UIManager.visorCentral.sprite = imagenUI.sprite;
                UIManager.visorCentral.gameObject.SetActive(true);
            }
        }

        if (UIManager.visorCentral.gameObject.activeSelf)
        { // Establece la carta como seleccionada
            cartaUISeleccionada = this;
            if (cartaPrefab.cardData.tipo == CardType.Energia)
                UIManager.botonEnergia.gameObject.SetActive(true);
            else
                UIManager.botonEnergia.gameObject.SetActive(false);
        }
        else
        { // No indica carta seleccionada si se ha ocultado
            cartaUISeleccionada = null;
            UIManager.botonEnergia.gameObject.SetActive(false);
        }
    }

    // Configura la carta al empezar a arrastrarla y resalta las casillas disponibles para colocarla
    public void OnBeginDrag(PointerEventData eventData)
    {
        posicionInicial = rectTransform.anchoredPosition; // Guarda la posición inicial
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.6f; // Añade algo de transparencia a la carta

        // Oculta el visor central
        if (UIManager.visorCentral != null)
        {
            UIManager.visorCentral.gameObject.SetActive(false);
            cartaUISeleccionada = null;
        }

        // RESALTAR CASILLAS DISPONIBLES

        // Se busca el castillo del jugador actual
        Cell[] casillasCastillo = new Cell[3]; // Casillas donde se pueden generar monstruos
        int filaCastillo = -1;
        int columnaCastillo = -1;
        bool castilloEncontrado = false;

        // Determina la fila trasera según el turno del jugador
        int filaTrasera = -1;
        if (!TurnManager.turnoP1)
            filaTrasera = 1;

        // Se recorren todas las casillas del tablero
        foreach (var cell in Board.Instance.cells)
        {
            cell.Bloquear(); // Por defecto se bloquean todas las casillas

            if (cell.cartaActual.cardData.nombre.Equals("Castillo") && cell.cartaActual.clickableObject.propietarioP1 == TurnManager.turnoP1)
            { // Guarda la posición del castillo
                filaCastillo = cell.row;
                columnaCastillo = cell.col;
            }

            if (filaCastillo != -1 && columnaCastillo != -1)
            { // Si se ha encontrado el castillo, se guardan las casillas traseras para poder generar monstruos
                casillasCastillo[0] = Board.Instance.cells[filaCastillo + filaTrasera, columnaCastillo - 1];
                casillasCastillo[1] = Board.Instance.cells[filaCastillo + filaTrasera, columnaCastillo];
                casillasCastillo[2] = Board.Instance.cells[filaCastillo + filaTrasera, columnaCastillo + 1];
                castilloEncontrado = true;
            }
        }

        // Si el jugador no tiene energía suficiente, no puede tirar la carta
        if (TurnManager.numTurno > 2 && TurnManager.energiaDisponible < cartaPrefab.cardData.costoEnergia)
            return;

        // Si es un monstruo, se resaltan solo las tres casillas detrás del castillo
        if ((cartaPrefab.cardData.tipo == CardType.Monstruo || cartaPrefab.cardData.tipo == CardType.MonstruoLeg) && castilloEncontrado)
        {
            foreach (var cell in casillasCastillo)
            {
                if (cell.ocupada)
                    cell.SetColor(Color.red);
                else
                    cell.Resaltar(Color.lightBlue);
            }
        }
        else if (cartaPrefab.cardData.tipo == CardType.Estructura || cartaPrefab.cardData.tipo == CardType.Trampa)
        {
            // Si es una estructura o trampa, se resaltan solo las casillas de la mitad del tablero del jugador
            foreach (var cell in Board.Instance.cells)
            {
                // El castillo solo se puede colocar en la segunda fila y no en los bordes del tablero ni dónde haya casillas ocupadas alrededor
                if (cartaPrefab.cardData.nombre.Equals("Castillo"))
                {

                    int filaDisponible = 1;
                    if (!TurnManager.turnoP1)
                        filaDisponible = Board.Instance.cells.GetLength(0) - 2;

                    if (cell.row == filaDisponible && cell.col > 0 && cell.col < Board.Instance.columns - 1)
                    {

                        // Comprueba si hay casillas ocupadas alrededor
                        bool hayOcupadas = false;
                        for (int i = -1; i <= 1; i++)
                        {
                            for (int j = -1; j <= 1; j++)
                            {
                                int nuevaFila = cell.row + i;
                                int nuevaColumna = cell.col + j;

                                if (nuevaFila >= 0 && nuevaFila < Board.Instance.rows &&
                                    nuevaColumna >= 0 && nuevaColumna < Board.Instance.columns)
                                {
                                    if (Board.Instance.cells[nuevaFila, nuevaColumna].ocupada)
                                    {
                                        hayOcupadas = true;
                                        break;
                                    }
                                }
                            }
                            if (hayOcupadas) break;
                        }

                        // Si no hay casillas ocupadas alrededor, resaltar la casilla para poder colocar el castillo
                        if (hayOcupadas)
                            cell.SetColor(Color.red);
                        else
                            cell.Resaltar(Color.lightBlue);
                    }
                }
                else if (TurnManager.turnoP1 && cell.row < Board.Instance.cells.GetLength(0) / 2)
                { // Mitad correspondiente al jugador 1
                    if (cell.ocupada)
                        cell.SetColor(Color.red);
                    else
                        cell.Resaltar(Color.lightBlue);
                }
                else if (!TurnManager.turnoP1 && cell.row >= Board.Instance.cells.GetLength(0) / 2)
                { // Mitad correspondiente al jugador 2
                    if (cell.ocupada)
                        cell.SetColor(Color.red);
                    else
                        cell.Resaltar(Color.lightBlue);
                }
            }

            if (castilloEncontrado)
            {
                // Si se quiere colocar una estructura o trampa, NO resaltar las tres casillas detrás del castillo
                foreach (var cell in casillasCastillo)
                {
                    cell.Bloquear();
                }
            }
        }
        else if (cartaPrefab.cardData.tipo == CardType.Hechizo)
        { // Los hechizos se pueden lanzar en cualquier lugar del tablero
            foreach (var cell in Board.Instance.cells)
            {
                cell.Resaltar(Color.lightBlue);
            }
            CameraController.Instance.VisionTablero(false);
        }
    }

    // Actualiza las casillas y la posición de la carta seleccionada al arrastrarla
    public void OnDrag(PointerEventData eventData)
    {
        // Actualiza la posición de la carta arrastrada según el movimiento del ratón
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;

        // Convierte la posición del ratón en un "raycast"
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        { // Comprueba si el rayo ha colisionado con algo (una casilla o una carta)
            Cell casilla = hit.collider.GetComponent<Cell>();

            // Los hechizos pueden apuntar a casillas ocupadas
            bool esHechizo = cartaPrefab.cardData.tipo == CardType.Hechizo;

            // Si el rayo golpea la carta encima de la casilla (y es un hechizo), se selecciona la casilla
            if (casilla == null && esHechizo)
            {
                Card cardGolpeada = hit.collider.GetComponent<Card>();
                if (cardGolpeada != null && cardGolpeada.casilla != null)
                    casilla = cardGolpeada.casilla;
            }

            // Hay una casilla seleccionada que no está ocupada ni bloqueada
            bool casillaMarcable = casilla != null && !casilla.bloqueado &&
                                   (esHechizo || !casilla.ocupada);

            if (casillaMarcable)
            { // Hay una casilla válida bajo el cursor
                if (casilla != casillaAnterior)
                { // Actualiza la casilla seleccionada
                    if (casillaAnterior != null) // Restablece el color de la anterior casilla seleccionada
                        casillaAnterior.Resaltar(Color.lightBlue);

                    LimpiarResaltadoArea();
                    casilla.Resaltar(Color.green); // Marca la nueva casilla seleccionada en verde
                    casillaAnterior = casilla;

                    if (esHechizo && (cartaPrefab.cardData as SpellCardData).actuaEnArea)
                    { // Si es un hechizo de área, resalta el área
                        ResaltarArea(casilla);
                    }
                }
            }
            else if (casillaAnterior != null && !casillaAnterior.bloqueado)
            { // Reestablece el color de la casilla seleccionada anteriormente (si no estaba bloqueada)
                casillaAnterior.Resaltar(Color.lightBlue);
                LimpiarResaltadoArea();
                casillaAnterior = null; // Ya no hay ninguna casilla seleccionada
            }
        }
        else
        { // No hay nada seleccionado
            if (casillaAnterior != null && !casillaAnterior.bloqueado)
            { // Reestablece el color de la casilla seleccionada anteriormente (si no estaba bloqueada)
                casillaAnterior.Resaltar(Color.lightBlue);
                LimpiarResaltadoArea();
                casillaAnterior = null; // Ya no hay ninguna casilla seleccionada
            }
        }
    }

    // Limpia el resaltado de las casillas del área seleccionada
    private void LimpiarResaltadoArea()
    {
        foreach (Cell c in casillasAreaActual)
        {
            if (c != null && c.GetColor() != Color.white) c.Resaltar(Color.lightBlue);
        }
        casillasAreaActual.Clear();
    }

    // Resalta el área alrededor de la casilla seleccionada
    private void ResaltarArea(Cell centro)
    {
        SpellCardData data = cartaPrefab.cardData as SpellCardData;
        int radio = data.radioArea;

        for (int df = -radio; df <= radio; df++)
        {
            for (int dc = -radio; dc <= radio; dc++)
            {
                if (df == 0 && dc == 0) continue; // El centro ya está resaltado en verde en OnDrag

                int nf = centro.row + df;
                int nc = centro.col + dc;

                if (nf >= 0 && nf < Board.Instance.rows && nc >= 0 && nc < Board.Instance.columns)
                { // Comprueba si la casilla está dentro del tablero
                    Cell adyacente = Board.Instance.cells[nf, nc];
                    if (adyacente != null)
                    { // Si la casilla está ocupada y tiene una carta que se puede dañar, se resalta en naranja
                        if (adyacente.ocupada && adyacente.cartaActual != null && adyacente.cartaActual.cardData is DamageableCardData)
                            adyacente.Resaltar(new Color(1f, 0.5f, 0f)); // Naranja
                        else
                            adyacente.Resaltar(Color.yellow); // Si no, se resalta en amarillo
                        
                        casillasAreaActual.Add(adyacente);
                    }
                }
            }
        }
    }


    // Coloca la carta o la devuelve a la mano del jugador al dejar de arrastrarla
    public void OnEndDrag(PointerEventData eventData)
    {

        CameraController.Instance.VolverAPosicionOriginal();

        // Restablece la transparencia y el bloqueo de raycasts de la carta
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        foreach (var cell in Board.Instance.cells)
        { // Restablece los colores de las casillas del tablero
            cell.ResetColor();
        }

        // Si había una casilla seleccionada válida, se coloca la carta allí
        if (casillaAnterior != null)
        {
            if (cartaPrefab.cardData.tipo == CardType.Hechizo)
            { // Los hechizos simplemente se lanzan, no se colocan

                SpellManager efecto = SpellManager.Crear(cartaPrefab.cardData as SpellCardData);
                if (efecto == null)
                {
                    Debug.LogWarning($"[CardUI] No hay SpellManager para '{cartaPrefab.cardData.nombre}'.");
                    rectTransform.anchoredPosition = posicionInicial;
                    casillaAnterior = null;
                    return;
                }

                if (!efecto.Lanzar(casillaAnterior))
                { // Si no se lanza el hechizo con éxito, devuelve la carta a la mano
                    rectTransform.anchoredPosition = posicionInicial;
                    casillaAnterior = null;
                    return;
                }

                // Muestra el hechizo en el visor central y pausa el juego antes de lanzarlo
                UIManager.visorCentral.sprite = imagenUI.sprite;
                UIManager.visorCentral.gameObject.SetActive(true);
                CameraController.Instance.MantenerVisor();
            }
            else
            { // Ocupa la casilla seleccionada con la carta
                casillaAnterior.OcuparCasilla(cartaPrefab);
            }
            if (TurnManager.numTurno > 2)
            { // A partir del segundo turno de cada jugador, se gasta la energía necesaria para colocar la carta
                TurnManager.energiaDisponible -= cartaPrefab.cardData.costoEnergia;
                UIManager.textoEnergia.SetText(TurnManager.energiaDisponible.ToString());
                sorter.Resaltar();
            }
            Destroy(this.gameObject); // Elimina la carta de la mano del jugador
            return;
        }

        // Si no se ha colocado la carta, vuelve a su posición original en la mano del jugador
        rectTransform.anchoredPosition = posicionInicial;
        casillaAnterior = null; // Ya no hay ninguna casilla seleccionada
    }

    public void GirarCarta()
    {
        if (girada)
            imagenUI.sprite = cartaPrefab.cardData.imagenCarta;
        else
            imagenUI.sprite = spriteReverso;

        girada = !girada;
    }

}
