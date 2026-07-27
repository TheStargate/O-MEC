using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Muestra un panel al inicio del primer turno de cada jugador para que introduzcan su nombre.
/// </summary>
public class PlayerNameManager : MonoBehaviour
{
    public static PlayerNameManager Instance { get; private set; }

    [SerializeField] private GameObject panelNombre;       // Panel de introducción de nombre
    [SerializeField] private TextMeshProUGUI textoPrompt;  // "Jugador X, introduce tu nombre:"
    [SerializeField] private TMP_InputField inputNombre;   // Campo de texto
    [SerializeField] private Button botonConfirmar;        // Botón para confirmar el nombre

    public string NombreP1 { get; private set; } = "Jugador 1";
    public string NombreP2 { get; private set; } = "Jugador 2";

    private bool esperandoP1 = false;
    private bool esperandoP2 = false;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        if (panelNombre != null)
            panelNombre.SetActive(false);
    }

    /// <summary>
    /// Muestra el panel de nombre para el Jugador 1 (llamado al inicio del turno 1).
    /// </summary>
    public void PedirNombreP1()
    {
        if (panelNombre == null) return;
        esperandoP1 = true;
        esperandoP2 = false;

        if (textoPrompt != null) textoPrompt.text = "Jugador 1, introduce tu nombre:";
        if (inputNombre != null) inputNombre.text = NombreP1 == "Jugador 1" ? "" : NombreP1;

        panelNombre.SetActive(true);
    }

    /// <summary>
    /// Muestra el panel de nombre para el Jugador 2 (llamado al inicio del turno 2).
    /// </summary>
    public void PedirNombreP2()
    {
        if (panelNombre == null) return;
        esperandoP1 = false;
        esperandoP2 = true;

        if (textoPrompt != null) textoPrompt.text = "Jugador 2, introduce tu nombre:";
        if (inputNombre != null) inputNombre.text = NombreP2 == "Jugador 2" ? "" : NombreP2;

        panelNombre.SetActive(true);
    }

    /// <summary>
    /// Confirma el nombre introducido en el panel de nombres
    /// </summary>
    public void ConfirmarNombre()
    {
        string nombre = inputNombre != null ? inputNombre.text.Trim() : "";

        if (esperandoP1)
        {
            NombreP1 = string.IsNullOrEmpty(nombre) ? "Jugador 1" : nombre;
            esperandoP1 = false;
        }
        else if (esperandoP2)
        {
            NombreP2 = string.IsNullOrEmpty(nombre) ? "Jugador 2" : nombre;
            esperandoP2 = false;
        }

        if (panelNombre != null)
            panelNombre.SetActive(false);

    }

    /// <summary>
    /// Restaura los nombres desde el archivo de guardado.
    /// </summary>
    public void CargarNombres(string nombreP1, string nombreP2)
    {
        NombreP1 = string.IsNullOrEmpty(nombreP1) ? "Jugador 1" : nombreP1;
        NombreP2 = string.IsNullOrEmpty(nombreP2) ? "Jugador 2" : nombreP2;
    }
}
