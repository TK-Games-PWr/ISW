using TK_Shared._3DPlayerMovement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public bool isPaused = false;
    public PlayerActionsController playerActionsController;
    
    public void TogglePauseMenu()
    {
        isPaused = !isPaused;
        gameObject.SetActive(isPaused);
        Time.timeScale = isPaused ? 0f : 1f;
        Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isPaused;
        playerActionsController.OnGamePaused(isPaused);
    }
    
    public void LoadScene(string name)
    {
        if (!MainMenuController.Scenes.ContainsKey(name))
        {
            Debug.LogError("Scene not found: " + name);
            return;
        }
        SceneManager.LoadScene(MainMenuController.Scenes[name]);
        Destroy(gameObject);
    }
    
    public void RestartLevel()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }
}
