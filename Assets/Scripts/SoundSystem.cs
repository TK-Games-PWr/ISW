using System;
using System.Collections.Generic;
using UnityEngine;

public class SoundSystem : MonoBehaviour
{
    [SerializeField] LayerMask occludingLayers;
    
    public static SoundSystem Instance { get; private set; }

    readonly List<IHearingTarget> _activeListeners = new();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Call this in OnEnable()
    public void RegisterListener(IHearingTarget listener)
    {
        if (!_activeListeners.Contains(listener))
            _activeListeners.Add(listener);
    }

    // Call this in OnDisable() / OnDestroy()
    public void UnregisterListener(IHearingTarget listener)
    {
        _activeListeners.Remove(listener);
    }

    /// <summary>
    /// Call this from the player, explosions, or moving objects.
    /// </summary>
    /// <param name="capAlertLevel">If true then maximum alert level of target can't exceed "Alerted" state from this sound, so enemy won't enter combat.</param>
    public void BroadcastSound(Vector3 soundOrigin, float baseVolume, float range, bool capAlertLevel = true, AnimationCurve falloffCurve = null)
    {
        float sqrRange = range * range;
        
        for (int i = _activeListeners.Count - 1; i >= 0; i--)
        {
            IHearingTarget listener = _activeListeners[i];
            
            Vector3 directionToListener = listener.GetHearingPosition() - soundOrigin;
            float sqrDistance = directionToListener.sqrMagnitude;

            // faster distance check using sqrMagnitude
            if (sqrDistance <= sqrRange)
            {
                float actualDistance = Mathf.Sqrt(sqrDistance);

                Debug.DrawLine(soundOrigin, listener.GetHearingPosition(), Color.red, 1f);
                
                if (Physics.Linecast(soundOrigin + new Vector3(0, 0.1f, 0), listener.GetHearingPosition(), occludingLayers))
                {
                    Debug.Log("Occluding audio by 50%");
                    baseVolume /= 2;
                }
                
                listener.OnSoundHeard(soundOrigin, baseVolume, actualDistance, range, capAlertLevel, falloffCurve);
            }
        }
    }
}
