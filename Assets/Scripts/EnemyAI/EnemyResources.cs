using UnityEngine;
using System;
using EnemyAI;
using TK_Shared._3DPlayerMovement;
using UnityEngine.AI;

namespace EnemySystem
{
    public class EnemyResources : MonoBehaviour, ICharacter
    {
        public static event Action<EnemyResources> OnEnemyDied;

        [SerializeField] float maxHealth = 75f;

        float currentHealth;
        AICore brain;
        AIAnimationController animController;

        public bool IsDead => currentHealth <= 0;

        void Awake()
        {
            currentHealth = maxHealth;
            brain = GetComponent<AICore>();
           animController = GetComponent<AIAnimationController>();

        }

        public void Damage(float amount)
        {
            if (IsDead) return;

            currentHealth = Mathf.Clamp(currentHealth - amount, 0, maxHealth);

            if (IsDead)
            {
                Die();
            }
            else
            {
                // Alert the brain if we were shot from stealth
                brain.ForceAlertSpike();
            }
        }

        public void StealthKill()
        {
            if (IsDead) return;
            currentHealth = 0;
            Die();
        }

        void Die()
        {
            OnEnemyDied?.Invoke(this);
            Lobotomize();
            animController.SetRagdoll(true);
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