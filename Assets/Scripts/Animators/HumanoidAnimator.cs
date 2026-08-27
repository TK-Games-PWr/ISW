using System;
using UnityEngine;

// General script for all common humanoid animations
public class HumanoidAnimator : MonoBehaviour
{
    [Header("General")]
    Transform _currentLeftGrip;
    Transform _currentRightGrip;
    
    [Tooltip("Transform target for right hand when idle")]
    [SerializeField] Transform rHandIdleTarget;
    [Tooltip("Transform target for left hand when idle")]
    [SerializeField] Transform lHandIdleTarget;
    
    [SerializeField] Transform rHandTarget;
    [SerializeField] Transform lHandTarget;
    
    [Header("Setup")]
    Animator _anim;
    public LayerMask groundLayer;
    public float footOffsetUp = 0.13f;
    public float footOffsetForward = 0.2f;
    
    // Limits how high or low the leg will stretch to find the floor
    public float raycastUpDistance = 0.5f; 
    public float raycastDownDistance = 1.0f;

    void Start() 
    { 
        _anim = GetComponent<Animator>(); 
    }

    void LateUpdate()
    {
        if (_currentRightGrip != null)
        {
            rHandTarget.SetPositionAndRotation(_currentRightGrip.position, _currentRightGrip.rotation);
        }
        if (_currentLeftGrip != null)
        {
            lHandTarget.SetPositionAndRotation(_currentLeftGrip.position, _currentLeftGrip.rotation);
        }
    }
    
    void OnAnimatorIK(int layerIndex)
    {
        if (!_anim) return;
        
        AdaptFootToGround(AvatarIKGoal.LeftFoot);
        AdaptFootToGround(AvatarIKGoal.RightFoot);
    }
    
    public void SetLeftHandTarget(Transform t = null)
    {
        if (t != null)
        {
            _currentLeftGrip = t;
        }
        else
        {
            _currentRightGrip = lHandIdleTarget;
        }
    }
    
    public void SetRightHandTarget(Transform t = null)
    {
        if (t != null)
        {
            _currentRightGrip = t;
        }
        else
        {
            _currentRightGrip = rHandIdleTarget;
        }
    }

    void AdaptFootToGround(AvatarIKGoal foot)
    {
        _anim.SetIKPositionWeight(foot, 1f);
        _anim.SetIKRotationWeight(foot, 1f);
        Vector3 animatedFootPos = _anim.GetIKPosition(foot);
        Quaternion animatedFootRot = _anim.GetIKRotation(foot);
        
        Vector3 rayStart = animatedFootPos + (Vector3.up * raycastUpDistance);
        float totalRayLength = raycastUpDistance + raycastDownDistance;

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, totalRayLength, groundLayer))
        {
            Vector3 toeVector = animatedFootRot * Vector3.forward * footOffsetForward;
            float toeDrop = Mathf.Min(0f, toeVector.y);
            Vector3 finalFootPos = hit.point;
            
            finalFootPos.y += footOffsetUp - toeDrop; 
            
            _anim.SetIKPosition(foot, finalFootPos);
            
            Quaternion floorRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            _anim.SetIKRotation(foot, floorRotation * animatedFootRot);
        }
    }
}
