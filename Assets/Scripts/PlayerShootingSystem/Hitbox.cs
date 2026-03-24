using EnemySystem;
using UnityEngine;
namespace PlayerShootingSystem
{

    public enum HitboxType { Body, Head }
    
    public class EnemyHitbox : MonoBehaviour
    {
        [SerializeField]
        EnemyHealth enemyHealth;
        public HitboxType hitboxType = HitboxType.Body;

        public float GetDamageMultiplier() => hitboxType switch
        {
            HitboxType.Head => 2.5f,
            _ => 1f
        };

        public EnemyHealth GetEnemyHealth() => enemyHealth;
    }
}
