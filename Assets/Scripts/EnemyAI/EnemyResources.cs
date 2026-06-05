using UnityEngine;
using System;
using PlayerShootingSystem;
using TK_Shared._3DPlayerMovement;
using UnityEngine.AI;

namespace EnemySystem
{
    public class EnemyResources : MonoBehaviour, ICharacter
    {
        public static event Action<EnemyResources> OnEnemyDied;

        float _maxHealth;

        float _currentHealth;
        EnemyBrain _brain;
        AIAnimationController _animController;

        public bool IsDead => _currentHealth <= 0;
        
        [Header("Combat Settings")]
        public Gun currentGun;
        [SerializeField] internal Transform gunPivot;
        [Space(10)]
        [SerializeField] internal Gun glockWeapon;
        [SerializeField] internal Gun shotgunWeapon;
        
        [Tooltip("Total amount of bullets, specified in magazines of current weapon")]
        [SerializeField] internal int totalMagazines = 1;

        internal void Init()
        {
            _brain = GetComponent<EnemyBrain>();
            _maxHealth = _brain.Config.maxHealth;
            _currentHealth = _maxHealth;
           _animController = GetComponent<AIAnimationController>();
        }

        public void SwitchWeapon(WeaponType weaponType)
        {
            currentGun.gameObject.SetActive(false);
            
            currentGun = weaponType switch
            {
                WeaponType.Shotgun => shotgunWeapon,
                WeaponType.Pistol => glockWeapon,
                _ => glockWeapon // Default fallback
            };
            
            currentGun.gameObject.SetActive(true);
        }

        public void Damage(float amount)
        {
            if (IsDead) return;

            _currentHealth = Mathf.Clamp(_currentHealth - amount, 0, _maxHealth);

            if (IsDead)
            {
                Die();
            }
            else
            {
                // Alert the brain if we were shot from stealth
                _brain.AlertSystem.ForceAlertSpike();
            }
        }

        public void StealthKill()
        {
            if (IsDead) return;
            _currentHealth = 0;
            Die();
        }

        void Die()
        {
            OnEnemyDied?.Invoke(this);
            Lobotomize();
            _animController.SetRagdoll(true);
            GetComponent<Collider>().enabled = false;
            int deadLayer = LayerMask.NameToLayer("EnemyFainted");
            SetLayerRecursively(gameObject, deadLayer);
            //Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            //rb.AddForceAtPosition(Vector3.back * 2, transform.position + new Vector3(0, 0.5f, 0), ForceMode.Impulse);
            // uncomment if is supposed to disappear, maybe add some delay
            // Destroy(gameObject);
        }
        
        void SetLayerRecursively(GameObject obj, int newLayer)
        {
            obj.layer = newLayer;
            
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, newLayer);
            }
        }

        public void Lobotomize()
        {
            MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
            foreach (var script in scripts)
            {
                script.StopAllCoroutines();
                script.enabled = false;
            }
            
            NavMeshAgent agent = GetComponent<NavMeshAgent>();
            if (agent != null) 
            {
                agent.enabled = false;
            }
        }
    }
}