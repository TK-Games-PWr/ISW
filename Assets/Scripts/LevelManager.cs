using PlayerShootingSystem;
using TK_Shared._3DPlayerMovement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }
    
    [SerializeField] GameObject DeathScreen;
    [SerializeField] GameObject SuccessScreen;

    GameObject player;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        DeathScreen.SetActive(false);
        SuccessScreen.SetActive(false);
        
        player = GameObject.FindGameObjectWithTag("Player");
    }

    public void OnPlayerDeath()
    {
        DeathScreen.SetActive(true);
        EndGame();
    }

    public void Win()
    {
        SuccessScreen.SetActive(true);
        EndGame();
    }

    private void EndGame()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        player.GetComponent<PlayerActionsController>().enabled = false;
        player.GetComponent<PlayerShootingController>().enabled = false;
        Debug.Log("time stop");
        Time.timeScale = 0; // TODO replace with proper death handling
    }
    
    public void RestartLevel()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }
}
