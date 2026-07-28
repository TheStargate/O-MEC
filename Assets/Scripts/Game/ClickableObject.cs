using UnityEngine;

public class ClickableObject : MonoBehaviour
{
    public TipoObjeto tipoObjeto = TipoObjeto.Ninguno;
    [HideInInspector] public Vector3 offsetObjeto = new Vector3(0, 7, -3f); // Posición relativa donde se debe quedar la cámara al seleccionar el objeto
    public bool propietarioP1; // Jugador al que pertenece el objeto
    public int ultimoAtaque = 0; // Turno en el que la carta realizó el último ataque
    public int ultimoMovimiento = 0; // Turno en el que la carta realizó el último movimiento
    public int turnoColocado = 0; // Turno en el que la carta fue colocada en el tablero
    public bool habilidadUsada = false; // Si la habilidad activa de la carta ya ha sido usada
    public bool usado; // Si el objeto ya ha sido usado, no se resalta ni permite atacar o moverse
    public Renderer renderizador; // Para controlar la textura y resaltado del objeto
    [HideInInspector] public bool asignarAutomaticamente = true; // Para no asignar todas las cartas al mismo jugador al cargar una partida en curso

    void Awake()
    {
        renderizador = GetComponent<Renderer>();
        usar(); // Al principio los objetos se marcan como usados
    }
    void Start()
    {
        // Se establecen los propietarios de las cartas
        if (tipoObjeto != TipoObjeto.Menu && tipoObjeto != TipoObjeto.Baraja && asignarAutomaticamente)
        {
            propietarioP1 = TurnManager.turnoP1;
        }
    }

    // Actualiza el resaltado de las cartas / objetos clickables
    public void actualizarResaltado()
    {
        // Resalta las cartas del jugador rival en rojo (y el castillo en amarillo)
        if (propietarioP1 != TurnManager.turnoP1 && GetComponent<Card>() != null)
        {
            if (GetComponent<Card>().cardData.nombre == "Castillo")
                renderizador.material.color = Color.lightGoldenRod;
            else
                renderizador.material.color = Color.lightPink;
        }
        // Resalta las cartas del jugador actual en blanco (o en gris si no se pueden usar)
        else if (tipoObjeto == TipoObjeto.Baraja && propietarioP1 != TurnManager.turnoP1)
            renderizador.material.color = Color.gray;
        else if (usado)
        {
            // Aunque esté "usada", puede seguir sin estar gris si su habilidad activa sigue disponible
            Card carta = GetComponent<Card>();
            bool puedeUsarHabilidad = carta != null &&
                                      carta.activa != null &&
                                      !habilidadUsada &&
                                      turnoColocado < TurnManager.numTurno && // No puede actuar el turno en que fue colocada
                                      carta.cardData.costeHabilidad <= TurnManager.energiaDisponible;
            renderizador.material.color = puedeUsarHabilidad ? Color.white : Color.gray;
        }
        else
            renderizador.material.color = Color.white;
    }

    // Establece el objeto como usado para este turno y actualiza su resaltado
    public void usar()
    {
        if (name != "Menu P1" && name != "Menu P2")
        {
            usado = true;
            actualizarResaltado();
        }
    }
}

// Lista de posibles objetos clickables
public enum TipoObjeto
{
    Ninguno,
    Menu,
    Baraja,
    Monstruo,
    Estructura,
    Hechizo,
    Trampa,
    MonstruoLeg,
    Energia
}