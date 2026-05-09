using UnityEngine;

public interface IHearingTarget
{
    Vector3 GetHearingPosition();
    
    void OnSoundHeard(Vector3 soundOrigin, float baseVolume, float distance, float range, bool capAlertLevel, AnimationCurve falloffCurve);
}