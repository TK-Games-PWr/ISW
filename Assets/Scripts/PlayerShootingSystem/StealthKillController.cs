using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class StealthKillController : MonoBehaviour
{
    public float killDistance = 2.5f;
    public float behindThreshold = -0.5f; // -1 is exactly behind, 0 is exactly to the side
    private Camera playerCamera;

    public bool canStealthKill { get; private set; }
    private EnemySystem.EnemyHealth enemy;

    private void Start()
    {
        playerCamera = LevelManager.Instance.playerCamera;
    }

    private void Update()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, killDistance))
        {
            if (hit.transform.TryGetComponent(out enemy))
            {
                Vector3 dirFromEnemyToPlayer = (transform.position - enemy.transform.position).normalized;
                float dotProduct = Vector3.Dot(enemy.transform.forward, dirFromEnemyToPlayer);
                canStealthKill = dotProduct < behindThreshold && !enemy.IsDead;
                return;
            }
        }

        canStealthKill = false;
    }

    public void OnInteractInput(InputValue inputValue)
    {
        if (inputValue.isPressed)
        {
            TryStealthKill();
        }
    }

    void TryStealthKill()
    {
        if (canStealthKill)
        {
            enemy.StealthKill();
        }
    }
}