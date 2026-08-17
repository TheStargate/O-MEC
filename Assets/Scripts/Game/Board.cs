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
    private bool seleccionandoHabilidad = false; // Indica si se están seleccionando objetivos para utilizar una habilidad
    private List<Card> objetivosHabilidad = new List<Card>(); // Objetivos seleccionados para aplicar la habilidad
    private Cell casillaOriginal; // Casilla que indica la carta que se ha seleccionado para moverse o atacar
    private Cell casillaSeleccionada; // Casilla objetivo donde mover o atacar
    public bool EstaSeleccionandoAtaque => seleccionandoAtaque;

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
        bool isPressed = false;
        Vector2 screenPos = Vector2.zero;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            isPressed = true;
            screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
        }
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            isPressed = true;
            screenPos = Mouse.current.position.ReadValue();
        }

        if (isPressed)
        {  // Hace un raycast si se hace click para seleccionar una casilla para moverse / atacar
            Ray ray = Camera.main.ScreenPointToRay(screenPos);
            if (CameraController.Instance.enVisionTablero && Physics.Raycast(ray, out RaycastHit hit) && !UIManager.EstaSobreUI())
            {
                Cell cell = hit.collider.GetComponent<Cell>();
                Card card = hit.collider.GetComponent<Card>();

                if (seleccionandoHabilidad)
                { // Selecciona objetivos para aplicar una habilidad
                    Card atacante = UIManager.GetCartaSeleccionada();
                    if (atacante == null || atacante.activa == null || atacante.casilla == null)
                        return;

                    Card target = card ?? (cell != null && cell.ocupada ? cell.cartaActual : null);

                    if (target != null && target.casilla != null)
                    { // Selecciona o deselecciona objetivos disponibles para aplicar una habilidad
                        if (target.casilla.GetColor() == Color.orange && atacante.activa.EsObjetivoValido(target))
                        { // Selecciona un nuevo objetivo válido
                            int maxObjetivos = atacante.activa.NumObjetivos;
                            if (maxObjetivos <= 1)
                            {
                                foreach (Card objetivoAnterior in new List<Card>(objetivosHabilidad))
                                {
                                    objetivosHabilidad.Remove(objetivoAnterior);
                                    if (objetivoAnterior.casilla != null && objetivoAnterior.casilla.GetColor() == Color.green)
                                        objetivoAnterior.casilla.SetColor(Color.orange);
                                }
                            }
                            else if (objetivosHabilidad.Count >= maxObjetivos)
                            {
                                // Si ya hay el máximo de objetivos, se deselecciona el más antiguo
                                Card objetivoAnterior = objetivosHabilidad[0];
                                objetivosHabilidad.RemoveAt(0);
                                if (objetivoAnterior.casilla != null && objetivoAnterior.casilla.GetColor() == Color.green)
                                    objetivoAnterior.casilla.SetColor(Color.orange);
                            }

                            if (objetivosHabilidad.Count < maxObjetivos)
                            {
                                objetivosHabilidad.Add(target);
                                target.casilla.SetColor(Color.green);
                            }
                        }
                    }
                }
                else
                {
                    Card cartaSeleccionada = UIManager.GetCartaSeleccionada();
                    if (cartaSeleccionada == null || cartaSeleccionada.casilla == null)
                        return;

                    if (cell != null && (cell.GetColor() == Color.blue || cell.GetColor() == Color.orange))
                    { // Solo puede haber una casilla activa en verde a la vez. Si ya había otra seleccionada,
                      // se reestablece su color original antes de marcar la nueva.
                        if (casillaSeleccionada != null && casillaSeleccionada != cell && casillaSeleccionada.GetColor() == Color.green)
                            casillaSeleccionada.SetColor(seleccionandoAtaque ? Color.orange : Color.blue);

                        cell.SetColor(Color.green);
                        casillaSeleccionada = cell;
                    }
                    else if (card != null && card.casilla != null && card.casilla.GetColor() == Color.orange)
                    { // Mismo caso: no puede quedar más de un objetivo verde a la vez.
                        if (casillaSeleccionada != null && casillaSeleccionada != card.casilla && casillaSeleccionada.GetColor() == Color.green)
                            casillaSeleccionada.SetColor(seleccionandoAtaque ? Color.orange : Color.blue);

                        card.casilla.SetColor(Color.green);
                        casillaSeleccionada = card.casilla;
                    }
                }
            }
        }
    }

    // Confirma una acción de movimiento / ataque de la carta (o una habilidad si se está seleccionando objetivos)
    public void ConfirmarAccionCarta()
    {
        // Si estamos en modo selección de habilidad, redirigir a ConfirmarHabilidad
        if (seleccionandoHabilidad)
        {
            ConfirmarHabilidad();
            return;
        }

        // Vuelve a poner la cámara en su posición original y desactiva la visión de tablero
        CameraController.Instance.enVisionTablero = true;
        DesactivarMovimiento();
        CameraController.Instance.enVisionTablero = false;

        // Obtenemos la carta seleccionada para mover / atacar
        Card carta = UIManager.GetCartaSeleccionada();

        if (carta == null || carta.clickableObject == null || casillaSeleccionada == null || casillaOriginal == null)
            return;

        if (!seleccionandoAtaque)
        { // Mover
          // Actualizar turno del último movimiento
            carta.clickableObject.ultimoMovimiento = TurnManager.numTurno;
            MonsterCardData mCardData = carta.cardData as MonsterCardData;

            if (casillaSeleccionada.cartaActual != null && casillaSeleccionada.cartaActual.cardData != null &&
                casillaSeleccionada.cartaActual.cardData.tipo == CardType.Trampa)
            { // Si se mueve la carta a una trampa, se activa y produce daño
                Card trampa = casillaSeleccionada.cartaActual;
                TrapCardData trapCardData = trampa.cardData as TrapCardData;
                UIManager.visorCentral.sprite = trapCardData.imagenCarta;
                UIManager.visorCentral.gameObject.SetActive(true);

                if (UIManager.textoInfoCarta != null)
                {
                    string info = UIManager.GenerarInfoCarta(trampa);
                    UIManager.textoInfoCarta.text = info;
                    UIManager.textoInfoCarta.transform.parent.parent.parent.gameObject.SetActive(!string.IsNullOrEmpty(info));
                }

                int danyoTrampa = trapCardData.ataque;
                danyoTrampa += trampa.bonusDanyoTrampa;
                danyoTrampa *= trampa.multDanyoTrampa;

                if (mCardData != null)
                    carta.UpdateVida(mCardData.vida - danyoTrampa);

                // Efectos de habilidades activas de trampas
                if (trampa.trampaAplicaAturdimiento)
                { // Aturde la carta (no podrá moverse ni atacar durante 1 turno)
                    carta.clickableObject.ultimoMovimiento = TurnManager.numTurno + 2;
                    carta.clickableObject.ultimoAtaque = TurnManager.numTurno + 2;
                }
                if (trampa.trampaAplicaRalentizacion && mCardData != null)
                { // Ralentiza la carta (su velocidad se reduce 1 punto permanentemente)
                    carta.UpdateVelocidad(mCardData.velocidad - 1);
                }
                if (trampa.trampaAplicaFuego > 0 && mCardData != null)
                { // Aplica fuego a la carta (1 punto de daño durante 3 turnos)
                    mCardData.efectosDanyo.Add(new DanyoEfecto("Quemadura", trampa.trampaAplicaFuego, 3));
                }

                casillaSeleccionada.LiberarCasilla(false);
                CameraController.Instance.MantenerVisor(); // Mantenemos el visor para mostrar la trampa activada
            }

            // La carta se mueve (se ocupa la nueva casilla y se libera la anterior)
            // Preservar el estado del ClickableObject que se resetearía con el nuevo Instantiate
            int turnoColocadoOriginal = carta.clickableObject.turnoColocado;
            int ultimoMovimientoOriginal = carta.clickableObject.ultimoMovimiento;
            int ultimoAtaqueOriginal = carta.clickableObject.ultimoAtaque;
            bool habilidadUsadaOriginal = carta.clickableObject.habilidadUsada;
            if (casillaSeleccionada.OcuparCasilla(carta))
            {
                casillaOriginal.LiberarCasilla(true);
                carta = casillaSeleccionada.cartaActual;
                if (carta != null && carta.clickableObject != null)
                {
                    carta.clickableObject.turnoColocado = turnoColocadoOriginal;
                    carta.clickableObject.ultimoMovimiento = ultimoMovimientoOriginal;
                    carta.clickableObject.ultimoAtaque = ultimoAtaqueOriginal;
                    carta.clickableObject.habilidadUsada = habilidadUsadaOriginal;
                    carta.clickableObject.usado = false;
                    carta.clickableObject.actualizarResaltado();
                }
                if (mCardData != null && mCardData.vida <= 0) // Comprueba si ha muerto por trampa
                    casillaSeleccionada.LiberarCasilla(false);
            }
        }
        else
        { // Atacar o Curar
          // Actualizar turno del último ataque
            carta.clickableObject.ultimoAtaque = TurnManager.numTurno;

            DamageableCardData cardDataAtacante = carta.cardData as DamageableCardData;
            Card objetivo = casillaSeleccionada.cartaActual;

            if (objetivo == null || objetivo.clickableObject == null)
                return;

            // Si el objetivo es un aliado, se cura en vez de atacar
            bool esAccionCuracion = carta.pasiva != null && carta.pasiva.PuedeAtacarAliados && objetivo.clickableObject.propietarioP1 == TurnManager.turnoP1;

            if (esAccionCuracion)
            {
                // Curar: restaura vida al aliado según el ataque del curador
                if (objetivo.cardData is DamageableCardData dDataAliado)
                {
                    int curacion = cardDataAtacante?.ataque ?? 0;
                    int nuevaVida = Mathf.Min(dDataAliado.vida + curacion, dDataAliado.vidaMaxima);
                    objetivo.UpdateVida(nuevaVida);
                    Debug.Log($"[Pasiva] {carta.name} cura {curacion} PV a {objetivo.name}.");
                }
                carta.pasiva.OnDespuesDeAtacar(objetivo);
            }
            else
            {
                // Atacar: se calcula el daño base y todos los modificadores ofensivos (bonus pasivos, herreria, rey cura...)
                DamageableCardData cardData = objetivo.cardData as DamageableCardData;
                int danyo = PassiveAbility.CalcularDanyoAtacante(carta, objetivo);
                // Reducción de daño del defensor
                danyo = objetivo.pasiva?.OnRecibirDanyo(danyo) ?? danyo;
                // Invulnerabilidad absoluta (daño 0)
                if (PassiveAbility.EsInvulnerableATodo(objetivo)) danyo = 0;

                // Aplica el daño a la carta seleccionada
                if (cardData != null)
                {
                    int nuevaVida = cardData.vida - danyo;
                    if (nuevaVida <= 0)
                        casillaSeleccionada.LiberarCasilla(false); // Destruye la carta si se queda sin vida
                    else
                        objetivo.UpdateVida(nuevaVida);
                }

                // Efectos secundarios de la habilidad pasiva del atacante (Mago en área, Arquero largo, Ninja 2 ataques...)
                carta.pasiva?.OnDespuesDeAtacar(objetivo);

                // Ataque en área si está activo (por habilidades activas como Tanque o Torreta destructora)
                if (carta.areaProximoAtaque) PassiveAbility.EjecutarAtaqueArea(carta, objetivo.casilla, 1);

                // Reseteo de buffs de habilidad activa que solo duran 1 ataque
                carta.multDanyoProximoAtaque = 1;
                carta.bonusDanyoProximoAtaque = 0;
                carta.areaProximoAtaque = false;
                carta.espiaActivoProximoAtaque = false;
                carta.RefrescarAtaqueUI();
            }

            // Las estructuras se marcan como usadas a no ser que la habilidad pasiva permita más ataques
            if (carta.cardData is StructureCardData && carta.clickableObject.ultimoAtaque == TurnManager.numTurno)
                carta.clickableObject.usar();
        }
        if (carta.clickableObject != null && carta.clickableObject.ultimoMovimiento == TurnManager.numTurno && carta.clickableObject.ultimoAtaque == TurnManager.numTurno)
            carta.clickableObject.usar(); // Marca la carta como usada si no quedan acciones disponibles

        if (CameraController.Instance != null)
            CameraController.Instance.VolverACarta(carta);
    }

    // Obtiene la carta seleccionada desde la UI y permite aplicar su habilidad activa
    public void ActivarHabilidad()
    {
        Card carta = UIManager.GetCartaSeleccionada();
        if (carta == null || carta.cardData == null || carta.casilla == null || carta.clickableObject == null) return;

        if (carta.activa == null) return;

        if (TurnManager.energiaDisponible < carta.cardData.costeHabilidad) return;

        if (carta.activa.RequiereObjetivo)
        { // La habilidad necesita que el jugador seleccione uno o varios objetivos en el tablero.
            seleccionandoCasilla = true;
            seleccionandoHabilidad = true;
            casillaOriginal = carta.casilla;
            objetivosHabilidad.Clear();

            // Marca en naranja los objetivos válidos para seleccionar
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    Cell casilla = cells[row, col];
                    int dist = Mathf.Abs(row - casillaOriginal.row) + Mathf.Abs(col - casillaOriginal.col);

                    // Si la habilidad respeta el alcance, filtrar por distancia Manhattan (igual que el ataque normal)
                    if (carta.activa.RespetaAlcance && dist > ((carta.cardData as DamageableCardData)?.alcance ?? 0))
                        casilla.Bloquear();
                    else if (dist == 0)
                        casilla.SetColor(Color.red);
                    else if (casilla.ocupada && casilla.cartaActual != null)
                    {
                        if (carta.activa.EsObjetivoValido(casilla.cartaActual))
                            casilla.SetColor(Color.orange);
                        else
                            casilla.SetColor(Color.red);
                    }
                    else
                        casilla.SetColor(Color.yellow);
                }
            }
            CameraController.Instance.VisionTablero(true, carta.transform);
        }
        else
        { // La habilidad se ejecuta de forma inmediata y no requiere seleciconar objetivos
            TurnManager.energiaDisponible -= carta.cardData.costeHabilidad;
            UIManager.textoEnergia.SetText(TurnManager.energiaDisponible.ToString());
            RefrescarResaltados();

            carta.clickableObject.habilidadUsada = true;
            carta.indicadorHabilidadUsada.SetActive(true);
            carta.activa.Ejecutar();

            if (carta.cardData.tipo == CardType.Trampa)
                carta.RestaurarTurnosTrampa();

            carta.clickableObject.actualizarResaltado();
            CameraController.Instance.MostrarPanelSegunObjeto(carta.clickableObject);
        }
    }

    // Comprueba los objetivos seleccionados para poder aplicar la habilidad
    public void ConfirmarHabilidad()
    {
        Card carta = UIManager.GetCartaSeleccionada();
        // Guarda los objetivos ANTES de DesactivarMovimiento (que los borra con Clear())
        List<Card> objetivosConfirmados = new List<Card>(objetivosHabilidad);

        CameraController.Instance.enVisionTablero = true;
        DesactivarMovimiento();
        CameraController.Instance.enVisionTablero = false;

        bool cantidadObjetivosValida = carta != null && carta.activa != null && carta.cardData != null &&
            objetivosConfirmados.Count >= carta.activa.MinObjetivos &&
            objetivosConfirmados.Count <= carta.activa.NumObjetivos;

        if (cantidadObjetivosValida)
        {
            // Si la selección es válida, se consume la energía y se ejecuta la habilidad con los objetivos elegidos.
            TurnManager.energiaDisponible -= carta.cardData.costeHabilidad;
            UIManager.textoEnergia.SetText(TurnManager.energiaDisponible.ToString());
            RefrescarResaltados();

            carta.clickableObject.habilidadUsada = true;
            carta.indicadorHabilidadUsada.SetActive(true);
            carta.activa.Ejecutar(objetivosConfirmados);

            if (carta.cardData.tipo == CardType.Trampa)
                carta.RestaurarTurnosTrampa();

            carta.clickableObject.actualizarResaltado();
            CameraController.Instance.MostrarPanelSegunObjeto(carta.clickableObject);
        }
    }

    // Limpia el resaltado violeta para no dejar casillas enemigas marcadas al salir del modo de visión del tablero.
    public void LimpiarResaltadoVioleta()
    {
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                Cell casilla = cells[row, col];
                if (casilla != null && casilla.GetColor() == Color.violet)
                    casilla.ResetColor();
            }
        }
    }

    // Resalta en violeta todas las casillas ocupadas por cartas del jugador rival.
    public void ResaltarCasillasVioleta()
    {
        LimpiarResaltadoVioleta();

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                Cell casilla = cells[row, col];
                if (casilla == null || !casilla.ocupada || casilla.cartaActual == null || casilla.cartaActual.clickableObject == null)
                    continue;

                if (casilla.cartaActual.clickableObject.propietarioP1 != TurnManager.turnoP1)
                    casilla.SetColor(Color.violet);
            }
        }
    }


    // Activa la visión de tablero para seleccionar una casilla para moverse / atacar
    public void ActivarMovimiento(bool ataque)
    {
        CameraController.Instance?.ResetearEstadoVisionTableroVioleta();
        seleccionandoCasilla = true;
        seleccionandoAtaque = ataque;
        casillaSeleccionada = null;

        Card cartaSeleccionada = UIManager.GetCartaSeleccionada();
        if (cartaSeleccionada == null || cartaSeleccionada.casilla == null || cartaSeleccionada.cardData == null)
            return;

        // Establece la casilla seleccionada originalmente
        casillaOriginal = cartaSeleccionada.casilla;

        // Si la cámara aún no está enfocada en la carta atacante, hay que centrarla
        // con el zoom calculado por CalcularDistanciaFocoCarta para que la vista de
        // selección se muestre como corresponde. Si ya está enfocada, no se repite el movimiento.
        bool yaEnfocadaLaCarta = CameraController.Instance != null &&
            CameraController.Instance.enVisionTablero &&
            CameraController.Instance.TieneObjetivoEnfocado(cartaSeleccionada.transform);

        if (!yaEnfocadaLaCarta)
            CameraController.Instance.VisionTablero(true, cartaSeleccionada.transform);

        // Según si se quiere mover / atacar se calcula una distancia distinta
        if (ataque)
            DistanciaManhattan();
        else
            DistanciaBFS();

    }

    // Calcula la distancia Manhattan a partir de los datos de casillaOriginal
    private void DistanciaManhattan()
    {
        if (casillaOriginal == null)
            return;

        // Obtener coordenadas de la casilla origen
        int origenRow = casillaOriginal.row;
        int origenCol = casillaOriginal.col;

        Card cartaSeleccionada = UIManager.GetCartaSeleccionada();
        if (cartaSeleccionada == null || cartaSeleccionada.cardData == null)
            return;

        // Datos de la carta seleccionada
        DamageableCardData cardData = cartaSeleccionada.cardData as DamageableCardData;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                Cell casilla = cells[row, col];

                // Calcular distancia Manhattan
                int distancia = Mathf.Abs(row - origenRow) + Mathf.Abs(col - origenCol);

                if (distancia <= cardData.alcance)
                {
                    if (casilla.ocupada && casilla.cartaActual != null)
                    {
                        bool esAliado = casilla.cartaActual.clickableObject.propietarioP1 == TurnManager.turnoP1; // La carta seleccionada es aliada
                        Card atacante = cartaSeleccionada; // Carta que se quiere mover/atacar

                        if ((row == origenRow && col == origenCol) || casilla.cartaActual.cardData.tipo == CardType.Trampa)
                            casilla.SetColor(Color.red); // Marca en rojo si es la misma casilla o está ocupada por una trampa

                        else if (esAliado && (atacante.pasiva?.PuedeAtacarAliados ?? false))
                        {
                            // Solo se pueden curar monstruos aliados (no estructuras)
                            if (casilla.cartaActual.cardData is MonsterCardData)
                                casilla.SetColor(Color.orange); // Marca en naranja si hay un monstruo que se puede curar
                            else
                                casilla.SetColor(Color.red);
                        }

                        else if (!esAliado && (atacante.pasiva?.PuedeAtacarEnemigos ?? true))
                        {
                            // Comprueba restricciones de la pasiva defensora (dragones) y del atacante (torreta)
                            bool defensaPuedeAtacarse = casilla.cartaActual.pasiva?.PuedeSerAtacadoPor(atacante) ?? true;
                            bool atacantePuedeAtacar = atacante.pasiva?.PuedeAtacar(casilla.cartaActual) ?? true;
                            // Comprueba invulnerabilidad por Torre protectora
                            bool invulnerable = PassiveAbility.EsInvulnerablePorTorreProtectora(casilla.cartaActual);
                            // Comprueba si la casilla atacada es el castillo y si está protegido por un Castillo falso
                            bool castilloProtegido = casilla.cartaActual.cardData.nombre == "Castillo" &&
                                PassiveAbility.EsCastilloInvulnerable(casilla.cartaActual.clickableObject.propietarioP1);

                            if (defensaPuedeAtacarse && atacantePuedeAtacar && !invulnerable && !castilloProtegido)
                                casilla.SetColor(Color.orange); // Marca en naranja si hay una carta que se puede atacar
                            else
                                casilla.SetColor(Color.red); // Marca en rojo si no se puede atacar
                        }
                        else
                            casilla.SetColor(Color.red); // Marca en rojo si no se puede atacar
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
            if (cell.cartaActual != null && cell.cartaActual.cardData.nombre.Equals("Castillo") && cell.cartaActual.clickableObject.propietarioP1 != TurnManager.turnoP1)
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

        Card cartaSeleccionada = UIManager.GetCartaSeleccionada();
        if (cartaSeleccionada == null || cartaSeleccionada.cardData == null || casillaOriginal == null)
            return;

        // Datos de la carta seleccionada para moverse
        MonsterCardData monsterData = cartaSeleccionada.cardData as MonsterCardData;
        if (monsterData == null)
            return;

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
    public bool EsCoordenadaValida(int row, int col)
    {
        return row >= 0 && row < rows && col >= 0 && col < columns;
    }

    // Desactiva la visión de tablero para seleccionar una casilla para moverse / atacar (o la activa si no estaba puesta)
    public void DesactivarMovimiento()
    {
        bool estabaEnVisionTablero = CameraController.Instance != null && CameraController.Instance.enVisionTablero;
        bool estabaSeleccionandoCasilla = seleccionandoCasilla;

        if (estabaEnVisionTablero)
        {
            foreach (Cell casilla in cells)
            {
                casilla.ResetColor();
            }

            seleccionandoCasilla = false;
            seleccionandoHabilidad = false;
            objetivosHabilidad.Clear();
            if (CameraController.Instance != null)
                CameraController.Instance.enVisionTablero = false;
            return;
        }

        if (estabaSeleccionandoCasilla && CameraController.Instance != null)
        { // Activa la visión de tablero si no lo estaba antes (al pulsar "volver" mirando un objeto clickable)
            Card cartaSeleccionada = UIManager.GetCartaSeleccionada();
            if (cartaSeleccionada != null && cartaSeleccionada.transform != null)
                CameraController.Instance.VisionTablero(true, cartaSeleccionada.transform);
            else
                CameraController.Instance.VisionTablero(true);
            seleccionandoCasilla = true; // Volver a activar la selección para poder hacer click en las casillas
        }
        else
        {
            foreach (Cell casilla in cells)
            {
                casilla.ResetColor();
            }
            seleccionandoCasilla = false;
            seleccionandoHabilidad = false;
            objetivosHabilidad.Clear();
        }
    }

    // Refresca el resaltado de todos los ClickableObject del tablero y las cartas de la mano del jugador actual.
    // Debe llamarse cada vez que cambie la energía disponible.
    public static void RefrescarResaltados()
    {
        foreach (ClickableObject co in FindObjectsByType<ClickableObject>(FindObjectsSortMode.None))
            co.actualizarResaltado();

        if (DeckManager.Instance == null) return;
        if (TurnManager.turnoP1)
            DeckManager.Instance.handPanelP1?.GetComponent<CardSorter>()?.Resaltar();
        else
            DeckManager.Instance.handPanelP2?.GetComponent<CardSorter>()?.Resaltar();
    }
}
