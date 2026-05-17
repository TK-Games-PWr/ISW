using System.Collections.Generic;
using System.Linq;
using EnemySystem;
using Sirenix.OdinInspector;
using UnityEngine;

public class EnemyManager : SerializedMonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    public enum EnemyType
    {
        Normal,
        Rambenemy,
        CoverGuy
    }

    [SerializeField] Dictionary<EnemyType, int> _specialEnemyCount;
    
    [SerializeField]
    List<AICore> aiData;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance);
        }

        Instance = this;
    }

    void Start()
    {
        InitializeLevel();
    }

    void Reset()
    {
        aiData = FindObjectsByType<AICore>(FindObjectsSortMode.None).ToList();
    }

    public void InitializeLevel()
    {
        Reset();
        aiData.Shuffle();

        int enemiesIter = 0;
        
        foreach (var specialEnemy in _specialEnemyCount)
        {
            for (int count = 0; count < specialEnemy.Value; count++)
            {
                aiData[enemiesIter].ChangeEnemyType(specialEnemy.Key);
                Debug.Log("changed enemy " + aiData[enemiesIter].name + " type to " + specialEnemy.Key);
                enemiesIter++;
                if (enemiesIter >= aiData.Count) break;
            }
            if (enemiesIter >= aiData.Count) break;
        }
    }
}
