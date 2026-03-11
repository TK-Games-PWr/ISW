using UnityEngine;

namespace PlayerShootingSystem
{
    [CreateAssetMenu(fileName = "GunInfo", menuName = "Scripts/PlayerShootingSystem/GunInfo")]
    public class GunInfo : ScriptableObject
    {
        public int flatDamage;
        public bool isAutomatic;
        public bool isExplosive;
        public bool isMelee;
        public float blastRadius;
        public float fireRate;
        public AmmoType ammoType;
        public int maxAmmo;
        public AnimationCurve damageFalloff;
    }
}
