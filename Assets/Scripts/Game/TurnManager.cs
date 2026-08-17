using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class TurnManager : MonoBehaviour
{
    public static bool turnoP1 = true; // Indica si es el turno del jugador 1
    public static bool robadoDisponible = false; // Indica si el jugador puede robar cartas
    public static int numTurno = 1; // Número de turnos que lleva la partida
    public static int energiaDisponible; // Energía lista para gastarse en el turno actual
    public static int bonusHerreriaActiva = 0; // Bonus global temporal por habilidad activa de la Herrería
    private static ClickableObject[] clickables; // Lista de objetos clickables actualmente

    [SerializeField] private TextMeshProUGUI textoTurnoUI; // Texto que muestra el turno actual
    public static TextMeshProUGUI textoTurno; // Texto accesible estáticamente para actualizar el turno

    void Awake()
    {
        turnoP1 = true;
        robadoDisponible = false;
        numTurno = 1;
        energiaDisponible = 0;
        bonusHerreriaActiva = 0;
        clickables = null;
        textoTurno = null;
    }

    void Start()
    {
        textoTurno = textoTurnoUI;

        bool cargandoPartida = PlayerPrefs.GetInt("CargarPartida", 0) == 1;
        if (!cargandoPartida)
        {
            // Reinicia el estado global en una nueva partida
            turnoP1 = true;
            numTurno = 1;
            energiaDisponible = 0;
            bonusHerreriaActiva = 0;
            robadoDisponible = false;
            CardUI.cartaUISeleccionada = null;
            CardUI.estaArrastrando = false;

            if (UIManager.visorCentral != null)
            {
                UIManager.visorCentral.gameObject.SetActive(false);
                UIManager.visorCentral.sprite = null;
            }
            if (UIManager.botonEnergia != null)
                UIManager.botonEnergia.gameObject.SetActive(false);
            if (UIManager.textoEnergia != null)
                UIManager.textoEnergia.SetText("0");

            if (PlayerNameManager.Instance != null)
                PlayerNameManager.Instance.CargarNombres("Jugador 1", "Jugador 2");
        }

        // Al inicio se muestra la mano del jugador 1
        DeckManager.Instance.handPanelP1.gameObject.SetActive(true);
        DeckManager.Instance.handPanelP2.gameObject.SetActive(false);

        // Asegura que la mano se refresca y no queda ninguna carta de color incorrecto
        Board.RefrescarResaltados();

        // Pedir el nombre a los jugadores al inicio de la partida
        if (PlayerNameManager.Instance != null && !cargandoPartida)
        {
            if (PlayerNameManager.Instance.NombreP1 == "Jugador 1")
                PlayerNameManager.Instance.PedirNombreP1();
            else if (PlayerNameManager.Instance.NombreP2 == "Jugador 2")
                PlayerNameManager.Instance.PedirNombreP2();
        }

        ActualizarTextoTurno();
    }

    public static void ActualizarTextoTurno()
    {
        string nombreTurno = turnoP1
            ? (PlayerNameManager.Instance?.NombreP1 ?? "Jugador 1")
            : (PlayerNameManager.Instance?.NombreP2 ?? "Jugador 2");
        string turno = $"Turno de\n{nombreTurno}";

        textoTurno?.SetText(turno);
    }

    // Pasa el turno de un jugador al otro
    public static void CambiarTurno()
    {
        numTurno++;
        turnoP1 = !turnoP1;
        energiaDisponible = 0;

        // Reset de bonus globales que duran hasta el próximo turno
        bonusHerreriaActiva = 0;

        ActualizarTextoTurno();

        // Adaptar los elementos de la interfaz al nuevo turno
        UIManager.visorCentral.gameObject.SetActive(false);
        UIManager.botonEnergia.gameObject.SetActive(false);
        UIManager.textoEnergia.SetText(energiaDisponible.ToString());
        DeckManager.Instance.recolocarBarajas(true);

        // Los jugadores pueden clickar objetos a partir de su segundo turno
        if (numTurno > 2)
        {
            // Roba automáticamente las cartas de la baraja
            robadoDisponible = true;
            DeckManager.Instance.SpawnCard();

            // Resetear objetos clickables del jugador actual
            clickables = FindObjectsByType<ClickableObject>(FindObjectsSortMode.None);
            foreach (ClickableObject clickable in clickables)
            {
                clickable.usado = false; // Permite que los objetos se puedan usar en el nuevo turno

                if (clickable.propietarioP1 == turnoP1)
                {
                    // Actualiza las cartas del juguador que se pueden usar en el nuevo turno
                    Card carta = clickable.GetComponent<Card>();
                    if (carta != null)
                    {
                        // Limpia estados temporales de habilidades
                        carta.ResetBuffsTurno();

                        // Actualiza efectos por turno activos
                        carta.UpdateEfectos();
                        if (carta == null) continue; // La carta ha muerto, pasamos a la siguiente

                        // Decrementar aturdimiento si el objeto está aturdido
                        if (clickable.aturdido > 0)
                            clickable.aturdido--;

                        // Si sigue aturdido, mantener la carta como usada (gris y no interactuable) al inicio del turno
                        if (clickable.aturdido > 0)
                            clickable.usado = true;

                        // Dispara la habilidad pasiva de inicio de turno
                        carta.pasiva?.OnTurnoInicio();

                        if (carta.cardData is MonsterCardData mData && (mData.alcance <= 0 || mData.ataque <= 0) && mData.velocidad <= 0)
                            clickable.usado = true; // Si es un monstruo que no puede atacar ni moverse, no se puede usar
                        else if (carta.cardData is StructureCardData sData && (sData.alcance <= 0 || sData.ataque <= 0))
                            clickable.usado = true; // Si es una estructura que no puede atacar, no se puede usar
                        else if (carta.cardData is TrapCardData)
                        { // Si es una trampa ya colocada, no se puede usar y se actualizan sus turnos restantes
                            clickable.usado = true;
                            carta.UpdateTurnos();
                        }
                    }
                }
                clickable.actualizarResaltado();
            }
        }

        // Mostrar la mano del jugador actual (y resaltar las cartas que se puedan usar)
        GameObject panel;
        if (turnoP1)
        {
            DeckManager.Instance.handPanelP1.gameObject.SetActive(true);
            DeckManager.Instance.handPanelP2.gameObject.SetActive(false);
            panel = GameObject.Find("Hand Panel P1");
        }
        else
        {
            DeckManager.Instance.handPanelP1.gameObject.SetActive(false);
            DeckManager.Instance.handPanelP2.gameObject.SetActive(true);
            panel = GameObject.Find("Hand Panel P2");
        }
        if (panel != null)
        {
            CardSorter sorter = panel.GetComponent<CardSorter>();
            if (numTurno > 2)
                sorter?.Resaltar();
        }

        // Auto-guardado al terminar de cambiar el turno (a partir del segundo turno)
        if (numTurno > 1 && SaveManager.Instance != null)
            SaveManager.Instance.GuardarPartida();
    }
}
