using UnityEngine;
using System;
using TK_Shared._3DPlayerMovement;

namespace EnemySystem
{
    public class EnemyHealth : MonoBehaviour, ICharacter
    {
        public static event Action<EnemyHealth> OnEnemyDied;

        [SerializeField] float maxHealth = 75f;

        private float currentHealth;
        private AICore brain;

        internal bool IsDead => currentHealth <= 0;

        private void Awake()
        {
            currentHealth = maxHealth;
            brain = GetComponent<AICore>();
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
            if (brain.currentState != AICore.AIState.Combat && brain.currentAlertLevel == AICore.AlertLevel.None)
            {
                currentHealth = 0;
                Die();
            }
        }

        private void Die()
        {
            OnEnemyDied?.Invoke(this);
            Destroy(gameObject);
        }
    }
}