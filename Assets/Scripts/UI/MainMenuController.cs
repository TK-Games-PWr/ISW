using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public struct SceneEntry
{
    public string name;
    public int index;
}

public class MainMenuController : MonoBehaviour
{
    public SceneEntry[] scenes;
    Dictionary<string, int> _scenes = new Dictionary<string, int>();
    public GameObject mainMenu;
    public GameObject optionsMenu;

    void Start()
    {
        foreach (var se in scenes)
        {
            _scenes[se.name] = se.index;
        }
    }

    public void LoadScene(string name)
    {
        if (!_scenes.ContainsKey(name))
        {
            Debug.LogError("Scene not found: " + name);
            return;
        }
        SceneManager.LoadScene(_scenes[name]);
    }

    public void OptionsMenu()
    {
        mainMenu.SetActive(false);
        optionsMenu.SetActive(true);
    }

    public void MainMenu()
    {
        mainMenu.SetActive(true);
        optionsMenu.SetActive(false);
    }
    
    public void QuitGame()
    {
        Application.Quit();
    }
}
