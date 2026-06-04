using UnityEngine;
using System;
using TK_Shared._3DPlayerMovement;
using UnityEngine.AI;

namespace EnemySystem
{
    public class EnemyResources : MonoBehaviour, ICharacter
    {
        public static event Action<EnemyResources> OnEnemyDied;

        [SerializeField] float maxHealth = 75f;

        float _currentHealth;
        AICore _brain;
        AIAnimationController _animController;

        public bool IsDead => _currentHealth <= 0;
        
        [Header("Combat Settings")]
        public PlayerShootingSystem.Gun currentGun;
        [SerializeField] internal Transform gunPivot;
        
        [Tooltip("Total amount of bullets, specified in magazines of current weapon")]
        [SerializeField] internal int totalMagazines = 1;

        void Awake()
        {
            _currentHealth = maxHealth;
            _brain = GetComponent<AICore>();
           _animController = GetComponent<AIAnimationController>();

        }

        public void Damage(float amount)
        {
            if (IsDead) return;

            _currentHealth = Mathf.Clamp(_currentHealth - amount, 0, maxHealth);

            if (IsDead)
            {
                Die();
            }
            else
            {
                // Alert the brain if we were shot from stealth
                _brain.ForceAlertSpike();
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