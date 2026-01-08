using UnityEngine;
using UnityEngine.Events;

public class Menu : MonoBehaviour
{
    public void ExitGame()
    {
        Debug.Log("Quit!");
        Application.Quit();
    }
}
