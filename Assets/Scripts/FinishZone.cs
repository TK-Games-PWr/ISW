using System;
using UnityEngine;

public class FinishZone : MonoBehaviour
{
    public static FinishZone Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            LevelManager.Instance.Win();
        }
    }
}
