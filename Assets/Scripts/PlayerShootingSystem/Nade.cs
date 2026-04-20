using System.Collections;
using JetBrains.Annotations;
using PlayerShootingSystem;
using TK_Shared._3DPlayerMovement;
using UnityEngine;
using UnityEngine.Serialization;

public class Nade : MonoBehaviour, IThrowable
{
    
    [SerializeField] [CanBeNull] AudioSource explosionSound;
    [SerializeField] [CanBeNull] ParticleSystem explosionParticle;

    public void Thrown(ThrowableInfo info)
    {
        StartCoroutine(CookCoroutine(info.radius, info.flatDamage, info.delay, info.workingLayerMask));
    }
    IEnumerator CookCoroutine(float blastRadius, int flatDamage, float delay, LayerMask damageLayerMask)
     {
         yield return new WaitForSecondsRealtime(delay);
         Explode(blastRadius,flatDamage, damageLayerMask);
         yield return new WaitForSecondsRealtime(2f);
         Destroy(gameObject);
     }


     void Explode(float blastRadius, int flatDamage, LayerMask damageLayerMask)
     {
         Vector3 explosionPoint=transform.position;
         Collider[] hitColliders=Physics.OverlapSphere(explosionPoint,blastRadius,damageLayerMask,QueryTriggerInteraction.Ignore);
         if (explosionParticle)
         {
             explosionParticle.gameObject.transform.parent = null;
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
                 //float distance = Vector3.Distance(explosionPoint, col.bounds.center);
                 //float falloff = Mathf.Clamp01(distance / blastRadius); // 0 → close, 1 → edge
                 float finalDamage = flatDamage;
                 
                 //Debug.Log(col.name + ", distance: " + distance + ", finalDamage: " + finalDamage);
                 
                 damageable.Damage(finalDamage);
             }
         }

     }
      
    public void Cook()
    {
        
    }
}
