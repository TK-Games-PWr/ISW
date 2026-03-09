using UnityEngine;
using System.Collections;
using UnityEngine.AI; // Needed to stop agent during reload

public class EnemyCombat : MonoBehaviour
{
    [Header("Combat Settings")]
    [SerializeField] float weaponRange = 20f;
    [SerializeField] float optimalCombatDistancePct = 0.7f;
    [SerializeField] int maxAmmo = 15;
    [SerializeField] float reloadTime = 1.7f;

    internal float WeaponRange => weaponRange;
    internal float OptimalDistance => weaponRange * optimalCombatDistancePct;
    internal bool IsReloading { get; private set; } = false;

    private int currentAmmo;
    private NavMeshAgent agent;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        currentAmmo = maxAmmo;
    }

    internal void TryCombatAction()
    {
        if (currentAmmo > 0)
        {
            Shoot();
        }
        else if (!IsReloading)
        {
            StartCoroutine(ReloadRoutine());
        }
    }

    private void Shoot()
    {
        // currentAmmo--;
        // Debug.Log($"{gameObject.name} is shooting!");
    }

    private IEnumerator ReloadRoutine()
    {
        IsReloading = true;
        agent.isStopped = true; 
        
        yield return new WaitForSeconds(reloadTime);

        currentAmmo = maxAmmo;
        IsReloading = false;
        agent.isStopped = false;
    }
}