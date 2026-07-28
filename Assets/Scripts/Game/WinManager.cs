using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Gestiona el fin de partida: activa la visión de tablero con casillas en violeta,
/// oculta las manos y muestra el texto del ganador.
/// </summary>
public class WinManager : MonoBehaviour
{
    public static WinManager Instance { get; private set; }

    public TextMeshProUGUI textoGanador; // Texto que indica qué jugador ha ganado
    [SerializeField] private Image fondoGanador; // Fondo del texto del ganador
    public bool partidaTerminada = false; // Para evitar que el método se llame dos veces
    public bool perdedorEsP1 = false;

    void Awake()
    {
        if (Instance == null) {
            Instance = this;
            fondoGanador?.gameObject.SetActive(false);
        }
        else
            Destroy(gameObject);
    }

    /// <summary>
    /// Llamado cuando un castillo es destruido.
    /// El propietario del castillo destruido es el perdedor.
    /// </summary>
    /// <param name="perdedorEsP1">True si el que pierde es el Jugador 1.</param>
    public void FinPartida(bool perdedorEsP1)
    {
        if (partidaTerminada) return;
        partidaTerminada = true;

        fondoGanador?.gameObject.SetActive(true);

        string nombreGanador = perdedorEsP1
            ? (PlayerNameManager.Instance?.NombreP2 ?? "Jugador 2")
            : (PlayerNameManager.Instance?.NombreP1 ?? "Jugador 1");
        string ganador = $"GANADOR: {nombreGanador}";

        if (textoGanador != null)
            textoGanador.text = ganador;

        // Ocultar las manos de ambos jugadores
        if (DeckManager.Instance != null)
        {
            if (DeckManager.Instance.handPanelP1 != null)
                DeckManager.Instance.handPanelP1.gameObject.SetActive(false);
            if (DeckManager.Instance.handPanelP2 != null)
                DeckManager.Instance.handPanelP2.gameObject.SetActive(false);
        }

        // Ocultar todos los paneles de acción y activar la visión libre de tablero
        if (CameraController.Instance != null)
        {
            CameraController.Instance.FinalizarPartida();
            CameraController.Instance.VisionTablero(false);
        }

        // Resaltar en violeta las casillas ocupadas por el bando perdedor
        if (Board.Instance != null)
            Board.Instance.ResaltarCasillasVioleta();

        Debug.Log($"[WinManager] Fin de partida. {ganador}");
    }
}
