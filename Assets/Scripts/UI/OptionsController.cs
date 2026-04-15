using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace OptionsSystem
{
    public class OptionsController : MonoBehaviour
    {
        [SerializeField] TMP_Dropdown leanModeDropdown;
        [SerializeField] TMP_Dropdown crouchModeDropdown;
        [SerializeField] GameObject tutorialPanel;

        [Header("Audio")]
        [SerializeField] AudioMixer mixer;
        [SerializeField] Slider masterSlider;
        [SerializeField] Slider sfxSlider;

        void Start()
        {
            // Get values
            crouchModeDropdown.value = Options.CrouchMode;
            leanModeDropdown.value = Options.LeanMode;

            masterSlider.value = Options.MasterVolume;
            sfxSlider.value = Options.SfxVolume;

            // Add listeners
            crouchModeDropdown.onValueChanged.AddListener(val => { SetPlayerPref("crouchMode", val); });
            leanModeDropdown.onValueChanged.AddListener(val => { SetPlayerPref("leanMode", val); });
            masterSlider.onValueChanged.AddListener(val => { SetAudioMixer("masterVolume", val); });
            sfxSlider.onValueChanged.AddListener(val => { SetAudioMixer("sfxVolume", val); });
        }

        void OnDestroy()
        {
            crouchModeDropdown.onValueChanged.RemoveAllListeners();
            leanModeDropdown.onValueChanged.RemoveAllListeners();
            masterSlider.onValueChanged.RemoveAllListeners();
            sfxSlider.onValueChanged.RemoveAllListeners();
        }

        void SetAudioMixer(string key, float value)
        {
            float dB = value > 0.0001f ? Mathf.Log10(value) * 20 : -80f;
            mixer.SetFloat(key, dB);
            PlayerPrefs.SetFloat(key, value);
        }

        public void SetPlayerPref(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
            Options.ReloadPrefs();
        }

        public void SetPlayerPref(string key, float value)
        {
            PlayerPrefs.SetFloat(key, value);
            Options.ReloadPrefs();
        }

        public void ShowTutorialPanel()
        {
            tutorialPanel.SetActive(true);
        }
    }
}