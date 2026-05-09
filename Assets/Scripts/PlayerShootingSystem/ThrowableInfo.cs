using Sirenix.OdinInspector;
using UnityEngine;

namespace PlayerShootingSystem
{
    [CreateAssetMenu(fileName = "ThrowableInfo", menuName = "Scripts/PlayerShootingSystem/ThrowableInfo")]
    public class ThrowableInfo : ScriptableObject
    {
        public bool dealsDamage;
        [ShowIf("dealsDamage")]
        public int flatDamage;
        public float radius;
        [Tooltip("Emit range of explosion/activation audio")] public float audioEmitRange = 20f;
        [Tooltip("How loud explosion/activation is.")] [HideIf("@this.audioEmitRange <= 0f")] public float volume = 1f;
        [HideIf("@this.audioEmitRange <= 0f")] public AnimationCurve volumeCurve = AnimationCurve.Linear(0, 1, 1, 0);
        public AmmoType ammoType;
        public LayerMask workingLayerMask;
        public float delay;

        public GameObject throwablePrefab;
    }
}
