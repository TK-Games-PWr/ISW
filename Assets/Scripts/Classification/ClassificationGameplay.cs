using System;
using UnityEngine;

public class ClassificationGameplay : MonoBehaviour
{
    public static ClassificationGameplay Instance { get; private set; }

    public enum ClassificationCategories
    {
        None,
        Assassin,
        Thief,
        Rambo,
        Tactical,
        Ninja
    }
    
    ClassificationCategories _currentCategory;
    ClassificationCategories _lastCategory;

    void Awake()
    {
        if(Instance && Instance != this) Destroy(gameObject);
        Instance = this;
    }

    public void Reset()
    {
        _currentCategory = ClassificationCategories.None;
        _lastCategory = _currentCategory;
    }

    public void SetCategory(string category)
    {
        if (!Enum.TryParse(category, out _currentCategory))
        {
            Debug.LogError($"{category} is not a valid category");
            return;
        }
        
        Debug.Log("Current category: " +  Enum.GetName(typeof(ClassificationCategories), _currentCategory));
    }

    void AdaptWorld()
    {
        if (_currentCategory == _lastCategory) return;
        _lastCategory = _currentCategory;

        switch (_currentCategory)
        {
            
        }
    }
}
