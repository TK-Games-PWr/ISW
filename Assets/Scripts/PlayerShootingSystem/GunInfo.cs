using Sirenix.OdinInspector;
using UnityEngine;

namespace PlayerShootingSystem
{
    [CreateAssetMenu(fileName = "GunInfo", menuName = "Scripts/PlayerShootingSystem/GunInfo")]
    public class GunInfo : ScriptableObject
    {
        public int flatDamage;
        [HideIf("isMelee")]
        public bool isAutomatic;
        [HideIf("isAutomatic")]
        public bool isMelee;
        [HideIf("isMelee")]
        public float fireRate;
        [HideIf("isMelee")]
        public AmmoType ammoType;
        [HideIf("isMelee")]
        public int maxAmmo;
        [HideIf("isMelee")]
        public AnimationCurve damageFalloff;
        
        [HideIf("isMelee")]
        public float recoilUpward = 30f;
        [HideIf("isMelee")]
        public float recoilHorizontal = 15f;
        [HideIf("isMelee")]
        public float spread = 0.01f;
        [HideIf("isMelee")]
        public float movementSpreadPenalty = 0.05f;
        [HideIf("isMelee")]
        public float reloadTime = 1f;
    }
}
