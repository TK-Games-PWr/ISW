using System;
using System.Collections;
using EnemySystem;
using JetBrains.Annotations;
using PlayerShootingSystem;
using TK_Shared._3DPlayerMovement;
using UnityEngine;
using UnityEngine.Serialization;

public class Nade : MonoBehaviour, IThrowable
{
    [SerializeField] [CanBeNull] AudioSource explosionSound;
    [SerializeField] [CanBeNull] ParticleSystem explosionParticle;
    int _enemyLayerIndex;

    void Awake()
    {
        _enemyLayerIndex = LayerMask.NameToLayer("Enemy");
    }

    public void Thrown(ThrowableInfo info)
    {
        StartCoroutine(CookCoroutine(info.radius, info.flatDamage, info.delay, info.workingLayerMask,
            info.audioEmitRange, info.volume, info.volumeCurve));
    }

    IEnumerator CookCoroutine(float blastRadius, int flatDamage, float delay, LayerMask damageLayerMask,
        float audioEmitRange, float volume, AnimationCurve volumeCurve)
    {
        yield return new WaitForSecondsRealtime(delay);
        Explode(blastRadius, flatDamage, damageLayerMask);
        EmitExplosionSound(audioEmitRange, volume, volumeCurve);
        yield return new WaitForSecondsRealtime(2f);
        Destroy(gameObject);
    }


    void Explode(float blastRadius, int flatDamage, LayerMask damageLayerMask)
    {
        Vector3 explosionPoint = transform.position;
        Collider[] hitColliders =
            Physics.OverlapSphere(explosionPoint, blastRadius, damageLayerMask, QueryTriggerInteraction.Ignore);
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

    void EmitExplosionSound(float audioEmitRange, float volume, AnimationCurve volumeCurve)
    {
        if (audioEmitRange < 0.01f) return;
        SoundSystem.Instance.BroadcastSound(
            transform.position, 
            volume, 
            audioEmitRange, 
            false, volumeCurve
        );
    }
}