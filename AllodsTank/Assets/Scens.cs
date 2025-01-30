using UnityEngine;
using UnityEngine.SceneManagement;

public class Scens : MonoBehaviour
{
    public void Game()
    {
        SceneManager.LoadScene("Game");
    }

    public void Settings()
    {

    }

    public void Developers()
    {

    }

    public void Exit()
    {
        Application.Quit();
    }
}
