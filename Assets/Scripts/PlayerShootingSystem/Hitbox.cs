using EnemySystem;
using UnityEngine;
using UnityEngine.Serialization;

namespace PlayerShootingSystem
{

    public enum HitboxType { Body, Head }
    
    public class EnemyHitbox : MonoBehaviour
    {
        [FormerlySerializedAs("enemyHealth")] [SerializeField]
        EnemyResources enemyResources;
        public HitboxType hitboxType = HitboxType.Body;

        public float GetDamageMultiplier() => hitboxType switch
        {
            HitboxType.Head => 2.5f,
            _ => 1f
        };

        public EnemyResources GetEnemyHealth() => enemyResources;
    }
}
