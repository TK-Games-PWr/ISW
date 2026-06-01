using System;
using UnityEngine;
using UnityEngine.Pool;

public class BulletImpactManager : MonoBehaviour
{
    public static BulletImpactManager Instance { get; private set; }
    
    public enum ImpactType
    {
        Ground,
        Flesh
    }
    
    [SerializeField]
    PooledParticle groundHitPrefab;
    [SerializeField]
    PooledParticle fleshHitPrefab;

    ObjectPool<PooledParticle> groundImpactPool;
    ObjectPool<PooledParticle> fleshImpactPool;

    void Awake()
    {
        if(Instance && Instance != this) Destroy(gameObject);
        Instance = this;
    }

    void Start()
    {
        Reset();
    }
    
    public void Reset()
    {
        groundImpactPool = new ObjectPool<PooledParticle>(
            createFunc: () => 
            {
                // Create the object and give it a reference to this pool
                PooledParticle instance = Instantiate(groundHitPrefab);
                instance.Initialize(groundImpactPool); 
                return instance;
            },
            actionOnGet: (ps) => ps.gameObject.SetActive(true),
            actionOnRelease: (ps) => ps.gameObject.SetActive(false),
            actionOnDestroy: (ps) => Destroy(ps.gameObject),
            collectionCheck: false,
            defaultCapacity: 20,
            maxSize: 50
        );
        fleshImpactPool = new ObjectPool<PooledParticle>(
            createFunc: () => 
            {
                // Create the object and give it a reference to this pool
                PooledParticle instance = Instantiate(fleshHitPrefab);
                instance.Initialize(fleshImpactPool); 
                return instance;
            },
            actionOnGet: (ps) => ps.gameObject.SetActive(true),
            actionOnRelease: (ps) => ps.gameObject.SetActive(false),
            actionOnDestroy: (ps) => Destroy(ps.gameObject),
            collectionCheck: false,
            defaultCapacity: 20,
            maxSize: 50
        );
    }
    
    public void SpawnImpact(Vector3 hitPoint, Vector3 hitNormal, ImpactType impactType)
    {
        PooledParticle impact;
        switch (impactType)
        {
            case ImpactType.Ground:
                impact = groundImpactPool.Get();
                break;
            case ImpactType.Flesh:
                impact = fleshImpactPool.Get();
                break;
            default:
                impact = groundImpactPool.Get();
                break;
        }
        
        impact.transform.position = hitPoint;
        impact.transform.forward = hitNormal; 
        
        impact.Play();
    }
}
