using UnityEngine;
using UnityEngine.Pool;

[RequireComponent(typeof(ParticleSystem))]
public class PooledParticle : MonoBehaviour
{
    private IObjectPool<PooledParticle> pool;
    private new ParticleSystem particleSystem;

    private void Awake()
    {
        particleSystem = GetComponent<ParticleSystem>();
    }
    
    public void Initialize(IObjectPool<PooledParticle> pool)
    {
        this.pool = pool;
    }

    public void Play()
    {
        particleSystem.Play();
    }
    
    private void OnParticleSystemStopped()
    {
        pool?.Release(this);
    }
}