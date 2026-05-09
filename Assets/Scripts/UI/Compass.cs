using System;
using System.Numerics;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class Compass : MonoBehaviour
{
    [SerializeField] private Transform playerTransform;
    [SerializeField] private RectTransform arrowRect;
    
    private Transform exitPoint;

    public void Start()
    {
        exitPoint = FinishZone.Instance.transform;
    }

    void Update()
    {
        Vector3 worldDirection = exitPoint.position - playerTransform.position;
        Vector3 localDirection = playerTransform.InverseTransformDirection(worldDirection);
        float angle = Mathf.Atan2(localDirection.z, localDirection.x) * Mathf.Rad2Deg;
        arrowRect.rotation = Quaternion.Euler(0, 0, angle);
    }
    
}
