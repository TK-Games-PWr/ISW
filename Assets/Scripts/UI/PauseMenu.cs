using TK_Shared._3DPlayerMovement;
using UnityEngine;

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
}
