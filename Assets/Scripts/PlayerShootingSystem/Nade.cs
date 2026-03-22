using System.Collections;
using JetBrains.Annotations;
using PlayerShootingSystem;
using TK_Shared._3DPlayerMovement;
using UnityEngine;

public class Nade : MonoBehaviour
{
    public Transform pin;
    [SerializeField] [CanBeNull] ParticleSystem gunParticle;

    void Explode(GunInfo  gunInfo, LayerMask damageLayerMask)
    {
        Vector3 explosionPoint=transform.position;
        Collider[] hitColliders=Physics.OverlapSphere(explosionPoint,gunInfo.blastRadius,damageLayerMask,QueryTriggerInteraction.Ignore);
        if (gunParticle)
        {
            gunParticle.Play();
        }
        GetComponent<MeshRenderer>().enabled = false;
        foreach (Collider col in hitColliders)
        {
    
            if (col.TryGetComponent<ICharacter>(out var damageable))
            {
                float distance = Vector3.Distance(explosionPoint, col.bounds.center);
                float falloff = 1f - (distance / gunInfo.blastRadius);
                float finalDamage = gunInfo.flatDamage * Mathf.Clamp01(falloff);

                damageable.Damage(finalDamage);
            }
        }

    }
        
    public IEnumerator CookCoroutine(GunInfo gunInfo, LayerMask damageLayerMask)
    {
        yield return new WaitForSecondsRealtime(3f);
        Explode(gunInfo,damageLayerMask);
        yield return new WaitForSecondsRealtime(2f);
        Destroy(gameObject);
    }
}
