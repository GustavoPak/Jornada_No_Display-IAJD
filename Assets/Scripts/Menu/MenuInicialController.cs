
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuInicialController : MonoBehaviour
{
    public void Jogar()
    {
        SceneManager.LoadScene("SampleScene");
    }

    public void Sair()
    {
        Application.Quit();
    }
}
