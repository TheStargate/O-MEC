using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Board : MonoBehaviour
{
    public int rows = 8;
    public int columns = 10;
    public float spacingX = 3.4f; // Distancia horizontal entre las casillas
    public float spacingZ = 5f; // Distancia vertical entre las casillas
    public Cell cellPrefab; // Prefab para instanciar las casillas
    public UIManager UIManager; // Para gestionar los cambios en la interfaz
    public static Board Instance { get; private set; } // Instancia del propio tablero para comunicarse con otros scripts
    public Cell[,] cells; // Casillas del tablero
    private bool seleccionandoCasilla = false; // Indica si se está seleccionando una casilla para moverse
    private bool seleccionandoAtaque = false; // Indica si se está seleccionando una casilla para atacar
    private Cell casillaOriginal; // Casilla que indica la carta que se ha seleccionado para moverse o atacar
    private Cell casillaSeleccionada; // Casilla objetivo donde mover o atacar

    void Start()
    {
        Instance = this;
        cells = new Cell[rows, columns];

        // Calcula el offset para centrar el tablero en torno a su posición
        Vector3 origen = transform.position - new Vector3((columns - 1) * spacingX / 2f, -0.1f, (rows - 1) * spacingZ / 2f);

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                // Coloca e instancia las casillas a partir del offset calculado
                Vector3 posicion = origen + new Vector3(col * spacingX, 0, row * spacingZ);
                Cell cell = Instantiate(cellPrefab, posicion, Quaternion.identity, transform);
                cell.transform.localScale = new Vector3(0.1f, 1f, 0.125f); // Reduce un plane a 1x1 unidades (bugs raros)
                cell.name = $"Cell_{row}_{col}";
                cell.row = row;
                cell.col = col;
                cells[row, col] = cell;
            }
        }
    }

    void Update()
    {
        if (!seleccionandoCasilla)
        {
            return;
        }
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {  // Hace un raycast si se hace click para seleccionar una casilla para moverse / atacar
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (CameraController.Instance.enVisionTablero && Physics.Raycast(ray, out RaycastHit hit) && !UIManager.EstaSobreUI())
            {
                Cell cell = hit.collider.GetComponent<Cell>();
                Card card = hit.collider.GetComponent<Card>();
                if (cell != null && (cell.GetColor() == Color.blue || cell.GetColor() == Color.orange))
                { // Selecciona la casilla si está disponible para moverse / atacar (color azul / naranja)
                    ActivarMovimiento(seleccionandoAtaque);
                    cell.SetColor(Color.green);
                    casillaSeleccionada = cell;
                }
                else if (card != null && card.casilla != null && card.casilla.GetColor() == Color.orange)
                { // Selecciona la casilla si se selecciona una carta disponible para ser atacada
                    ActivarMovimiento(seleccionandoAtaque);
                    card.casilla.SetColor(Color.green);
                    casillaSeleccionada = card.casilla;
                }
            }
        }
    }

    // Confirma una acción de movimiento / ataque de la carta
    public void ConfirmarAccionCarta()
    {
        // Vuelve a poner la cámara en su posición original y desactiva la visión de tablero
        CameraController.Instance.enVisionTablero = true;
        DesactivarMovimiento();
        CameraController.Instance.enVisionTablero = false;

        // Obtenemos la carta seleccionada para mover / atacar
        Card carta = UIManager.GetCartaSeleccionada();

        if (casillaSeleccionada != null)
        {
            if (!seleccionandoAtaque)
            { // Mover
                // Actualizar turno del último movimiento
                carta.clickableObject.ultimoMovimiento = TurnManager.numTurno;
                MonsterCardData mCardData = carta.cardData as MonsterCardData;

                if (casillaSeleccionada.cartaActual.cardData.tipo == CardType.Trampa)
                { // Si se mueve la carta a una trampa, se activa y produce daño
                    TrapCardData trapCardData = casillaSeleccionada.cartaActual.cardData as TrapCardData;
                    UIManager.visorCentral.sprite = trapCardData.imagenCarta;
                    UIManager.visorCentral.gameObject.SetActive(true);
                    carta.UpdateVida(mCardData.vida -= trapCardData.ataque);
                    casillaSeleccionada.LiberarCasilla(false);
                    CameraController.Instance.MantenerVisor(); // Mantenemos el visor para mostrar la trampa activada
                }

                // La carta se mueve (se ocupa la nueva casilla y se libera la anterior)
                casillaSeleccionada.OcuparCasilla(carta);
                casillaOriginal.LiberarCasilla(true);
                carta = casillaSeleccionada.cartaActual;
                carta.clickableObject.usado = false;
                carta.clickableObject.actualizarResaltado();
                if (mCardData.vida <= 0) // Comprueba si ha muerto por trampa
                    casillaSeleccionada.LiberarCasilla(false);
            }
            else
            { // Atacar
                // Actualizar turno del último ataque
                carta.clickableObject.ultimoAtaque = TurnManager.numTurno;

                // Si es una estructura, marcar la carta como usada
                if (carta.cardData is StructureCardData)
                    carta.clickableObject.usar();

                // Produce daño a la carta seleccionada para ser atacada
                DamageableCardData cardData = casillaSeleccionada.cartaActual.cardData as DamageableCardData;
                DamageableCardData cardDataAtacante = carta.cardData as DamageableCardData;
                casillaSeleccionada.cartaActual.UpdateVida(cardData.vida -= cardDataAtacante.ataque);
                if (cardData.vida <= 0)
                    casillaSeleccionada.LiberarCasilla(false); // Destruye la carta si se queda sin vida
            }
            if (carta.clickableObject.ultimoMovimiento == TurnManager.numTurno && carta.clickableObject.ultimoAtaque == TurnManager.numTurno)
                carta.clickableObject.usar(); // Marca la carta como usada si no quedan acciones disponibles
        }
    }

    // Resalta en verde todas las casillas ocupadas por cartas del jugador rival.
    public void ResaltarCasillasVerde()
    {
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                Cell casilla = cells[row, col];
                if (casilla.ocupada && casilla.cartaActual.clickableObject.propietarioP1 != TurnManager.turnoP1)
                    casilla.SetColor(Color.violet);
            }
        }
    }

    // Activa la visión de tablero para seleccionar una casilla para moverse / atacar
    public void ActivarMovimiento(bool ataque)
    {
        seleccionandoCasilla = true;
        seleccionandoAtaque = ataque;
        casillaSeleccionada = null;

        // Establece la casilla seleccionada originalmente
        casillaOriginal = UIManager.GetCartaSeleccionada().casilla;


        // Según si se quiere mover / atacar se calcula una distancia distinta
        if (ataque)
            DistanciaManhattan();
        else
            DistanciaBFS();

    }

    // Calcula la distancia Manhattan a partir de los datos de casillaOriginal
    private void DistanciaManhattan()
    {
        // Obtener coordenadas de la casilla origen
        int origenRow = casillaOriginal.row;
        int origenCol = casillaOriginal.col;


        // Datos de la carta seleccionada
        DamageableCardData cardData = UIManager.GetCartaSeleccionada().cardData as DamageableCardData;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                Cell casilla = cells[row, col];

                // Calcular distancia Manhattan
                int distancia = Mathf.Abs(row - origenRow) + Mathf.Abs(col - origenCol);

                if (distancia <= cardData.alcance)
                { 
                    if (casilla.ocupada)
                    {
                        if ((row == origenRow && col == origenCol) || casilla.cartaActual.cardData.tipo == CardType.Trampa || casilla.cartaActual.clickableObject.propietarioP1 == TurnManager.turnoP1)
                            casilla.SetColor(Color.red); // Marca en rojo si hay una trampa
                        else
                            casilla.SetColor(Color.orange); // Marca en naranja si hay una carta que se puede atacar
                    }
                    else
                        casilla.SetColor(Color.yellow); // Marca en amarillo si no hay ninguna carta para atacar
                }
                else
                { // Se bloquea la casilla si está demasiado lejos
                    casilla.Bloquear();
                }
            }
        }
    }

    // Calcula la distancia BFS a partir de los datos de casillaOriginal
    private void DistanciaBFS()
    {
        // Primero encuentra el castillo enemigo para no permitir moverse a las casillas traseras
        Cell[] casillasCastillo = new Cell[3];
        int filaCastillo = -1;
        int columnaCastillo = -1;

        // Determina la fila trasera según el turno del jugador
        int filaTrasera = -1;
        if (TurnManager.turnoP1)
            filaTrasera = 1;

        foreach (var cell in cells)
        {
            if (cell.cartaActual.cardData.nombre.Equals("Castillo") && cell.cartaActual.clickableObject.propietarioP1 != TurnManager.turnoP1)
            {
                filaCastillo = cell.row;
                columnaCastillo = cell.col;
            }

            if (filaCastillo != -1 && columnaCastillo != -1)
            {
                casillasCastillo[0] = cells[filaCastillo + filaTrasera, columnaCastillo - 1];
                casillasCastillo[1] = cells[filaCastillo + filaTrasera, columnaCastillo];
                casillasCastillo[2] = cells[filaCastillo + filaTrasera, columnaCastillo + 1];
                break;
            }
        }

        // Datos de la carta seleccionada para moverse
        MonsterCardData monsterData = UIManager.GetCartaSeleccionada().cardData as MonsterCardData;

        // Calcula la distancia BFS con una cola y marcando las visitadas
        Queue<(Cell cell, int distancia)> cola = new Queue<(Cell, int)>();
        HashSet<Cell> visitadas = new HashSet<Cell>();

        cola.Enqueue((casillaOriginal, 0));
        visitadas.Add(casillaOriginal);

        while (cola.Count > 0)
        {
            var (casilla, distancia) = cola.Dequeue();

            if (distancia > monsterData.velocidad)
            { // Bloquea la casilla si está demasiado lejos
                casilla.Bloquear();
                continue;
            }

            // Pinta la casilla
            if (distancia == 0)
                casilla.SetColor(Color.lightCyan); // Casilla de origen
            else if (casilla.ocupada && casilla.cartaActual.cardData.tipo == CardType.Trampa && casilla.cartaActual.clickableObject.propietarioP1 != TurnManager.turnoP1)
                casilla.SetColor(Color.orange); // Se puede pisar la trampa
            else if (casilla.ocupada)
                casilla.SetColor(Color.red); // No se puede pasar
            else if (casilla == casillasCastillo[0] || casilla == casillasCastillo[1] || casilla == casillasCastillo[2])
                casilla.SetColor(Color.red); // No se puede mover detrás del castillo enemigo
            else
                casilla.SetColor(Color.blue); // Válido para moverse

            // No continuar por casillas ocupadas del otro jugador
            if (casilla.ocupada && distancia != 0 && casilla.cartaActual.clickableObject.propietarioP1 != TurnManager.turnoP1)
                continue;

            // Explorar casillas vecinas (arriba, abajo, izquierda, derecha)
            Vector2Int[] direcciones = {
                new Vector2Int(1, 0),
                new Vector2Int(-1, 0),
                new Vector2Int(0, 1),
                new Vector2Int(0, -1)
            };

            foreach (var dir in direcciones)
            {
                int newRow = casilla.row + dir.x;
                int newCol = casilla.col + dir.y;
                
                if (EsCoordenadaValida(newRow, newCol))
                {
                    Cell vecina = cells[newRow, newCol];
                    if (!visitadas.Contains(vecina))
                    { // Si la casilla no está visitada, se añade a la cola
                        cola.Enqueue((vecina, distancia + 1));
                        visitadas.Add(vecina);
                    }
                }
            }
        }

        // Bloquea todas las casillas no visitadas
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                if (!visitadas.Contains(cells[row, col]))
                    cells[row, col].Bloquear();
            }
        }
    }

    // Comprueba que no se salga del tablero
    private bool EsCoordenadaValida(int row, int col)
    {
        return row >= 0 && row < rows && col >= 0 && col < columns;
    }

    // Desactiva la visión de tablero para seleccionar una casilla para moverse / atacar (o la activa si no estaba puesta)
    public void DesactivarMovimiento()
    {
        // Para poder desactivar las acciones del tablero, la cámara debe estar en visión tablero
        if (CameraController.Instance.enVisionTablero)
        {
            seleccionandoCasilla = false;

            foreach (Cell casilla in cells)
            {
                casilla.ResetColor();
            }
        }
        else if (seleccionandoCasilla)
        { // Activa la visión de tablero si no lo estaba antes (al pulsar "volver" mirando un objeto clickable)
            CameraController.Instance.VisionTablero(true);
        }
    }
}
