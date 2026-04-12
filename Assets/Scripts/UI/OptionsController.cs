using System;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class OptionsController : MonoBehaviour
{
    [SerializeField] TMP_Dropdown leanModeDropdown;
    [SerializeField] TMP_Dropdown crouchModeDropdown;

    [Header("Audio")]
    [SerializeField] AudioMixer mixer;
    [SerializeField] Slider masterSlider;
    [SerializeField] Slider sfxSlider;
    
    [SerializeField] bool acknowledgedNotSettingReferences;

    void Start()
    {
        // Set defaults
        if (!CheckPrefs()) SetDefaults();
        
        SetAudioMixer("masterVolume", PlayerPrefs.GetFloat("masterVolume"));
        SetAudioMixer("sfxVolume", PlayerPrefs.GetFloat("sfxVolume"));

        if (!crouchModeDropdown)
        {
            if (!acknowledgedNotSettingReferences) Debug.LogWarning("Some references not set in Options menu, settings won't work. Ignore it if only loading is needed");
            return;
        }

        // Get values
        crouchModeDropdown.value = PlayerPrefs.GetInt("crouchMode");
        leanModeDropdown.value = PlayerPrefs.GetInt("leanMode");

        masterSlider.value = PlayerPrefs.GetFloat("masterVolume");
        sfxSlider.value = PlayerPrefs.GetFloat("sfxVolume");

        // Add listeners
        crouchModeDropdown.onValueChanged.AddListener(val => { SetPlayerPref("crouchMode", val); });
        leanModeDropdown.onValueChanged.AddListener(val => { SetPlayerPref("leanMode", val); });
        masterSlider.onValueChanged.AddListener(val => { SetAudioMixer("masterVolume", val); });
        sfxSlider.onValueChanged.AddListener(val => { SetAudioMixer("sfxVolume", val); });
    }

    void OnDestroy()
    {
        if (!crouchModeDropdown) return;
        crouchModeDropdown.onValueChanged.RemoveAllListeners();
        leanModeDropdown.onValueChanged.RemoveAllListeners();
        masterSlider.onValueChanged.RemoveAllListeners();
        sfxSlider.onValueChanged.RemoveAllListeners();
    }

    public void SetDefaults()
    {
        SetPlayerPref("crouchMode", 0); // 0 hold, 1 toggle
        SetPlayerPref("leanMode", 0); // 0 hold, 1 toggle
        SetAudioMixer("masterVolume", 0.7f);
        SetAudioMixer("sfxVolume", 1f);
    }

    void SetAudioMixer(string key, float value)
    {
        mixer.SetFloat(key, Mathf.Log10(value) * 20);
        PlayerPrefs.SetFloat(key, value);
    }

    public void SetPlayerPref(string key, int value)
    {
        PlayerPrefs.SetInt(key, value);
    }

    public void SetPlayerPref(string key, float value)
    {
        PlayerPrefs.SetFloat(key, value);
    }

    /// <summary>
    /// Returns true if all player prefs are set, false otherwise
    /// </summary>
    bool CheckPrefs()
    {
        String[] expectedPrefs = { "crouchMode", "leanMode", "masterVolume", "sfxVolume" };

        bool allPrefsPresent = true;
        foreach (var pref in expectedPrefs)
        {
            if (!PlayerPrefs.HasKey(pref)) allPrefsPresent = false;
        }

        return allPrefsPresent;
    }
}