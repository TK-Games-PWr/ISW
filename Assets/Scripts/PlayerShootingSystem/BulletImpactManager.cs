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
    
    [SerializeField] private PooledParticle groundHitPrefab;
    [SerializeField] private PooledParticle fleshHitPrefab;
    
    private ObjectPool<PooledParticle> groundImpactPool;
    private ObjectPool<PooledParticle> fleshImpactPool;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    private void Start()
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
