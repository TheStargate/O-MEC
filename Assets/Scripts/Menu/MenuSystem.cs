using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuSystem : MonoBehaviour
{
    public void Jugar()
    {
        // Indica que quiere empezar una nueva partida al entrar en la escena
        PlayerPrefs.SetInt("CargarPartida", 0);
        // Carga la escena del juego
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void CargarPartidaGuardada()
    {
        // Indica que quiere cargar la partida guardada al entrar en la escena
        PlayerPrefs.SetInt("CargarPartida", 1);
        // Carga la escena del juego
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void Salir()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();  
    }
    
}
