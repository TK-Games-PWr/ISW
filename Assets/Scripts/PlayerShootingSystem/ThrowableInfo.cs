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
        public AmmoType ammoType;
        public LayerMask workingLayerMask;
        public float delay;

        public GameObject throwablePrefab;
    }
}
