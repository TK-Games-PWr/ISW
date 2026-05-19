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
    public static Dictionary<string, int> Scenes = new Dictionary<string, int>();
    public GameObject mainMenu;
    public GameObject optionsMenu;

    void Start()
    {
        Time.timeScale = 1f;
        
        mainMenu.SetActive(true);
        optionsMenu.SetActive(false);
        
        foreach (var se in scenes)
        {
            Scenes[se.name] = se.index;
        }
    }

    public void LoadScene(string name)
    {
        if (!Scenes.ContainsKey(name))
        {
            Debug.LogError("Scene not found: " + name);
            return;
        }
        SceneManager.LoadScene(Scenes[name]);
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
