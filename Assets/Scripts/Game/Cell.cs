using UnityEngine;

public class Cell : MonoBehaviour
{
    public bool ocupada = false; // Indica si hay una carta en la casilla
    public Card cartaActual; // Carta que hay colocada en la casilla
    private Renderer rend; // Para cambiar el color de la casilla
    public bool bloqueado = false; // Indica si se puede colocar una carta en la casilla
    public int row; // Posición en el tablero
    public int col; // Posición en el tablero


    void Awake()
    {
        rend = GetComponent<Renderer>();
    }

    public void Bloquear()
    {
        SetColor(Color.gray);
        bloqueado = true;
    }

    // Cambia el color de la casilla y la bloquea
    public void SetColor(Color color)
    {
        rend.material.color = color;
        bloqueado = true;
    }

    // Cambia el color de la casilla y la desbloquea
    public void Resaltar(Color color)
    {
        rend.material.color = color;
        bloqueado = false;
    }
    public Color GetColor()
    {
        return rend.material.color;
    }

    // Pone el color por defecto y desbloquea la casilla
    public void ResetColor()
    {
        SetColor(Color.white);
        bloqueado = false;
    }

    // Ocupa la casilla con la carta indicada
    public bool OcuparCasilla(Card cartaPrefab)
    {
        // Se comprueba si se puede poner la carta
        if (ocupada || bloqueado || cartaPrefab == null || cartaPrefab.cardData == null || cartaPrefab.clickableObject == null)
            return false;

        // Se instancia la carta
        cartaActual = Instantiate(cartaPrefab, transform.position + Vector3.up * 0.1f, Quaternion.Euler(0, 180, 0));
        cartaActual.cardData = cartaPrefab.cardData;
        cartaActual.transform.localScale = new Vector3(0.285f, 1, 0.445f); // Reestablece la escala original
        cartaActual.casilla = this;
        cartaActual.transform.SetParent(this.transform); // Establecer la carta como hijo de la casilla
        if (!TurnManager.turnoP1) cartaActual.transform.Rotate(0, 180, 0); // Rotar 180 grados si es del jugador 2

        // Inicializar la habilidad pasiva de la carta (monstruos, legendarios y estructuras)
        cartaActual.pasiva = PassiveAbility.Crear(cartaActual.cardData.nombre, cartaActual);
        cartaActual.pasiva?.OnColocar();

        // Inicializar la habilidad activa de la carta
        cartaActual.activa = ActiveAbility.Crear(cartaActual.cardData.nombre, cartaActual);

        // Guardar el turno en el que se colocó la carta
        if (cartaActual.clickableObject != null)
            cartaActual.clickableObject.turnoColocado = TurnManager.numTurno;

        // Si se coloca el castillo, se dan 3 muros al jugador
        if (cartaActual.cardData.nombre.Equals("Castillo"))
            DeckManager.Instance.SpawnWalls();

        ocupada = true;
        cartaActual.RefrescarAtaqueUI();

        return true;
    }

    // Elimina la carta de la casilla (si hay alguna)
    public void LiberarCasilla(bool movimiento)
    {
        if (cartaActual != null)
        {
            // Disparar la habilidad pasiva de muerte antes de destruir (solo si no se está moviendo la carta)
            if (!movimiento)
                cartaActual.pasiva?.OnMorir();

            // Si la carta se ha movido a otra casilla o es un Monstruo Legendario, no se pone en la pila de descartes
            if (cartaActual.cardData != null && cartaActual.cardData.tipo != CardType.MonstruoLeg && !movimiento && cartaActual.clickableObject != null)
                DeckManager.Instance.descartar(cartaActual.cardData, cartaActual.clickableObject.propietarioP1);
            Destroy(cartaActual.gameObject);
        }

        ocupada = false;
        cartaActual = null;
    }
}
