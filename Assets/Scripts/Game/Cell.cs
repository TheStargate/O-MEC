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
        cartaActual.Setup(cartaPrefab.cardData);
        cartaActual.transform.localScale = new Vector3(0.285f, 1, 0.445f); // Reestablece la escala original
        cartaActual.casilla = this;
        cartaActual.transform.SetParent(this.transform); // Establecer la carta como hijo de la casilla
        if (!TurnManager.turnoP1) cartaActual.transform.Rotate(0, 180, 0); // Rotar 180 grados si es del jugador 2

        // Inicializar la habilidad pasiva de la carta (monstruos, legendarios y estructuras)
        cartaActual.pasiva = PassiveAbility.Crear(cartaActual.cardData.nombre, cartaActual);
        cartaActual.pasiva?.OnColocar();

        // Inicializar la habilidad activa de la carta
        cartaActual.activa = ActiveAbility.Crear(cartaActual.cardData.nombre, cartaActual);

        // Conserva los estados temporales del Card original (efectos por turno, bonus, invulnerabilidad, fuego, etc.)
        cartaActual.CopiarEstadoTemporalDesde(cartaPrefab);

        // Guardar el turno en el que se colocó la carta
        if (cartaActual.clickableObject != null)
            cartaActual.clickableObject.turnoColocado = TurnManager.numTurno;

        // Si se coloca el castillo, se dan 3 muros al jugador
        if (cartaActual.cardData.nombre.Equals("Castillo"))
            DeckManager.Instance.SpawnWalls();

        ocupada = true;
        cartaActual.RefrescarAtaqueUI();

        // Restaurar los backgrounds de vida y velocidad tras Setup() que los desactiva
        if (cartaActual.cardData is DamageableCardData dData)
            cartaActual.UpdateVida(dData.vida);
        if (cartaActual.cardData is MonsterCardData mData)
            cartaActual.UpdateVelocidad(mData.velocidad);

        return true;
    }

    // Elimina la carta de la casilla (si hay alguna)
    public void LiberarCasilla(bool movimiento)
    {
        if (cartaActual == null)
        {
            ocupada = false;
            cartaActual = null;
            return;
        }

        // Quitar la carta del tablero primero para evitar reentrada cuando una pasiva de muerte
        // daña a otra carta adyacente y esa otra carta intenta destruir a la primera en la misma cascada.
        Card cartaADestruir = cartaActual;
        ocupada = false;
        cartaActual = null;

        if (!movimiento)
        {
            cartaADestruir.pasiva?.OnMorir();

            // Las trampas no muestran el popup de calavera al destruirse, porque su destrucción
            // es parte del efecto de activación y no representa la muerte de una carta del tablero.
            if (cartaADestruir.cardData != null && cartaADestruir.cardData.tipo != CardType.Trampa)
                cartaADestruir.MostrarTextoMuerte();
        }

        // Si la carta se ha movido a otra casilla o es un Monstruo Legendario, no se pone en la pila de descartes
        if (cartaADestruir.cardData != null && cartaADestruir.cardData.tipo != CardType.MonstruoLeg && !movimiento && cartaADestruir.clickableObject != null)
            DeckManager.Instance.descartar(cartaADestruir.cardData, cartaADestruir.clickableObject.propietarioP1);

        Destroy(cartaADestruir.gameObject);
    }
}
