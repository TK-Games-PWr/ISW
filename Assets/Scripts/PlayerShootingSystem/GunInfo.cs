using UnityEngine;

namespace PlayerShootingSystem
{
    [CreateAssetMenu(fileName = "GunInfo", menuName = "Scripts/PlayerShootingSystem/GunInfo")]
    public class GunInfo : ScriptableObject
    {
        public int flatDamage;
        public bool isAutomatic;
        public bool isExplosive;
        public float blastRadius;
        public float fireRate;
        public AnimationCurve damageFalloff;
    }
}
