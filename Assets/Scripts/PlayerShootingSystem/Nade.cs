using System.Collections;
using JetBrains.Annotations;
using PlayerShootingSystem;
using TK_Shared._3DPlayerMovement;
using UnityEngine;
using UnityEngine.Serialization;

public class Nade : MonoBehaviour
{
    public Transform pin;
    [SerializeField] [CanBeNull] AudioSource explosionSound;
    [FormerlySerializedAs("gunParticle")] [SerializeField] [CanBeNull] ParticleSystem explosionParticle;

    void Explode(GunInfo  gunInfo, LayerMask damageLayerMask)
    {
        Vector3 explosionPoint=transform.position;
        Collider[] hitColliders=Physics.OverlapSphere(explosionPoint,gunInfo.blastRadius,damageLayerMask,QueryTriggerInteraction.Ignore);
        if (explosionParticle)
        {
            explosionParticle.Play();
        }

        if (explosionSound)
        {
            explosionSound.Play();
        }
        GetComponent<MeshRenderer>().enabled = false;
        foreach (Collider col in hitColliders)
        {
    
            if (col.TryGetComponent<ICharacter>(out var damageable))
            {
                float distance = Vector3.Distance(explosionPoint, col.bounds.center);
                float falloff = Mathf.Clamp01(distance / gunInfo.blastRadius); // 0 → close, 1 → edge
                float finalDamage = gunInfo.flatDamage * gunInfo.damageFalloff.Evaluate(falloff);
                
                //Debug.Log(col.name + ", distance: " + distance + ", finalDamage: " + finalDamage);
                
                damageable.Damage(finalDamage);
            }
        }

    }
        
    public IEnumerator CookCoroutine(GunInfo gunInfo, LayerMask damageLayerMask)
    {
        yield return new WaitForSecondsRealtime(3f);
        Explode(gunInfo,damageLayerMask);
        yield return new WaitForSecondsRealtime(4f);
        Destroy(gameObject);
    }
}
