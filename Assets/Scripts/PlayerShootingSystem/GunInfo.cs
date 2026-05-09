using Sirenix.OdinInspector;
using UnityEngine;

namespace PlayerShootingSystem
{
    [CreateAssetMenu(fileName = "GunInfo", menuName = "Scripts/PlayerShootingSystem/GunInfo")]
    public class GunInfo : ScriptableObject
    {
        public Sprite icon;
        public int flatDamage;
        [Tooltip("Emit range of shooting audio")] public float audioEmitRange = 20f;
        [Tooltip("How loud gun shooting is.")] [HideIf("@this.audioEmitRange <= 0f")] public float fireVolume = 1f;
        [HideIf("@this.audioEmitRange <= 0f")] public AnimationCurve fireVolumeCurve = AnimationCurve.Linear(0, 1, 1, 0);
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
