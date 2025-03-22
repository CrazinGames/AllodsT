using UnityEngine;
using UnityEngine.SceneManagement;

public class Scens : MonoBehaviour
{
    public void Game() => SceneManager.LoadScene("Game");

    public void Settings() => SceneManager.LoadScene("");

    public void Developers() => SceneManager.LoadScene("");

    public void Exit() => Application.Quit();
}
