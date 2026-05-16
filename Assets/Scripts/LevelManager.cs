using EnemySystem;
using PlayerShootingSystem;
using TK_Shared._3DPlayerMovement;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }
    
    [SerializeField] GameObject DeathScreen;
    [SerializeField] GameObject SuccessScreen;

    public GameObject player;
    public Camera playerCamera;
    private CinemachineCamera flyingCamera;
    [Header("Camera Settings")]
    [Tooltip("Select the layers that the camera should not pass through")]
    public LayerMask camObstacleLayers;
    [Tooltip("Target FOV of camera in end screen")]
    public float targetFOV = 100f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        DontDestroyOnLoad(this);
    }

    private void Start()
    {
        Reset();
    }

    void Reset()
    {
        DeathScreen.SetActive(false);
        SuccessScreen.SetActive(false);
        BulletImpactManager.Instance.Reset();
    }

    public void OnPlayerDeath()
    {
        if(DeathScreen == null) return;
        DeathScreen.SetActive(true);
        EndGame();
    }

    public void Win()
    {
        SuccessScreen.SetActive(true);
        EndGame();
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

    private void EndGame()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        // disabling player
        player.GetComponent<PlayerActionsController>().enabled = false;
        player.GetComponent<PlayerShootingController>().StopAllCoroutines();
        player.GetComponent<PlayerShootingController>().enabled = false;
        player.GetComponent<PlayerInput>().enabled = false;
        player.GetComponent<HeadBobbing>().StopAllCoroutines();
        player.GetComponent<HeadBobbing>().enabled = false;
        
        // disabling enemies
        EnemyResources[] enemies = FindObjectsByType<EnemyResources>(FindObjectsSortMode.None);
        foreach (var e in enemies)
        {
            e.Lobotomize();
        }
        
        // floating camera woo fancy
        GameObject camObj = new GameObject();
        camObj.transform.position = player.transform.position - player.transform.forward * 5f + Vector3.up * 8f;
        GameObject camTarget = new GameObject();
        camTarget.transform.position = player.transform.position + player.transform.forward * 1f + Vector3.up * 0.5f;

        camObj.AddComponent<CinemachineHardLookAt>();
        
        flyingCamera = camObj.AddComponent<CinemachineCamera>();
        flyingCamera.Target = new CameraTarget { TrackingTarget = camTarget.transform };
        var lensSettings = flyingCamera.Lens;
        lensSettings.FieldOfView = targetFOV;
        flyingCamera.Lens = lensSettings;
        
        // obstacle avoidance
        CinemachineDeoccluder camDeoccluder = camObj.AddComponent<CinemachineDeoccluder>();
        camDeoccluder.CollideAgainst = camObstacleLayers;
        camDeoccluder.MinimumDistanceFromTarget = 1f;
        
        var avoidanceSettings = camDeoccluder.AvoidObstacles;
        avoidanceSettings.Enabled = true;
        avoidanceSettings.Strategy = CinemachineDeoccluder.ObstacleAvoidance.ResolutionStrategy.PullCameraForward;
        camDeoccluder.AvoidObstacles = avoidanceSettings;

        flyingCamera.Priority = 10;
    }
    
    public void RestartLevel()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
        Reset();
    }
}
