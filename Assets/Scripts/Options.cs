using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;

public static class Options
{
    public static bool IsCrouchHold => CrouchMode == 0;
    public static bool IsLeanHold => LeanMode == 0;
    
    public static int CrouchMode => PlayerPrefs.GetInt("crouchMode", 0); // 0 hold, 1 toggle
    public static int LeanMode => PlayerPrefs.GetInt("leanMode", 0); // 0 hold, 1 toggle
    
    public static float MasterVolume => PlayerPrefs.GetFloat("masterVolume", 0.7f);
    public static float SfxVolume => PlayerPrefs.GetFloat("sfxVolume", 0.7f);
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static async void InitializeAudio()
    {
        AudioMixer mixer = Resources.Load<AudioMixer>("AudioMixer"); 

        if (mixer == null)
        {
            Debug.LogError("AudioMixer not found in Resources folder!");
            return;
        }
        
        await Task.Yield();

        RefreshMixer(mixer);
    }

    static void RefreshMixer(AudioMixer mixer)
    {
        if (mixer == null) return;

        ApplyVolume(mixer, "masterVolume", MasterVolume);
        ApplyVolume(mixer, "sfxVolume", SfxVolume);
    }

    static void ApplyVolume(AudioMixer mixer, string key, float value)
    {
        float dB = value > 0.0001f ? Mathf.Log10(value) * 20 : -80f;
        mixer.SetFloat(key, dB);
    }
}
