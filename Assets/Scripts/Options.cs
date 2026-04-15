using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;

namespace OptionsSystem
{
    public static class Options
    {
        public static bool IsCrouchHold => CrouchMode == 0;
        public static bool IsLeanHold => LeanMode == 0;

        internal static int CrouchMode; // 0 hold, 1 toggle
        internal static int LeanMode; // 0 hold, 1 toggle

        internal static float MasterVolume;
        internal static float SfxVolume;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static async void Initialize()
        {
            ReloadPrefs();

            AudioMixer mixer = Resources.Load<AudioMixer>("AudioMixer");

            if (mixer == null)
            {
                Debug.LogError("AudioMixer not found in Resources folder!");
                return;
            }

            await Task.Yield();

            RefreshMixer(mixer);
        }

        public static void ReloadPrefs()
        {
            CrouchMode = PlayerPrefs.GetInt("crouchMode", 0);
            LeanMode = PlayerPrefs.GetInt("leanMode", 0);

            MasterVolume = PlayerPrefs.GetFloat("masterVolume", 0.7f);
            SfxVolume = PlayerPrefs.GetFloat("sfxVolume", 0.7f);
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
}
