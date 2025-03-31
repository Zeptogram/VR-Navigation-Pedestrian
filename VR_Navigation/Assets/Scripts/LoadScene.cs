using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor.SceneManagement;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadScene : MonoBehaviour
{
    private List<string> paths;
    private GameObject editMenu;
    private GameObject editButton;

    // Start is called before the first frame update
    void Start()
    {
        editMenu = GameObject.Find("SceneEdit");
        paths = new List<string>();      
        editButton = gameObject.GetNamedChild("EditButton");
        editMenu.SetActive(false);
        editButton.SetActive(false);
        List<String> scenes = CollectScenePaths("Assets/Scenes");
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        foreach (String scene in scenes)
        {
            TMP_Dropdown.OptionData optionData = new TMP_Dropdown.OptionData(scene);
            options.Add(optionData);
        }
        if (options.Count != 0)
        {
            this.GetComponentInChildren<TMP_Dropdown>().AddOptions(options);
        }
    }

    public void loadScene() 
    {
        TMP_Dropdown dropdown = FindFirstObjectByType<TMP_Dropdown>();
        string scene = dropdown.options[dropdown.value].text;
        if (scene.Equals("New Scene"))
        {
            //TODO
        }
        else
        {
            string path = "";
            foreach (string i in paths)
            {
                String name = i.Split("\\")[1];
                if (name.Substring(0, name.Length - 6).Equals(scene))
                {
                    path = i.Replace("\\", "/");
                }
            }
            //SceneManager.LoadScene(scene);
            EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters());
        }
    }

    public void editScene() {
        editMenu.SetActive(true);
        gameObject.SetActive(false);
    }

    public void onChanged()
    {
        TMP_Dropdown dropdown = FindFirstObjectByType<TMP_Dropdown>();
        string scene = dropdown.options[dropdown.value].text;
        if (SceneManager.GetActiveScene().name.Equals(scene))
        {
            editButton.SetActive(true);
        }
        else
        {
            editButton.SetActive(false);
        }
    }

    private List<string> CollectScenePaths(string rootPath)
    {
        List<string> scenes = new List<string>();
        string[] files = Directory.GetFiles(rootPath);
        for (int i = 0; i < files.Length; ++i)
        {
            if (files[i].EndsWith(".unity"))
            {
                paths.Add(files[i]);
                String name = files[i].Split("\\")[1];
                scenes.Add(name.Substring(0, name.Length - 6));
            }
        }

        string[] directories = Directory.GetDirectories(rootPath);
        for (int i = 0; i < directories.Length; ++i)
        {
            CollectScenePaths(directories[i]);
        }

        return scenes;
    }
}
