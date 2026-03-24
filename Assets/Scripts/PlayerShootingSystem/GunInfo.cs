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
        
        public float recoilUpward = 30f;
        public float recoilHorizontal = 15f;
        public float spread = 0.01f;
        public float movementSpreadPenalty = 0.05f;
        public float reloadTime = 1f;
    }
}
