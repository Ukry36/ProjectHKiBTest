using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class UIManager : MonoBehaviour
{
    [Serializable]
    public class WindowItem
    {
        public string name;
        public GameObject window;
        public UnityEngine.UI.Button initButton;
        public bool isPopup;
        public bool useHotkey;
        public EnumManager.InputType hotkey;
        public UnityEvent OnWindowShow;
        public UnityEvent OnWindowHide;
    }

    public List<WindowItem> windows;

    public List<WindowItem> openedWindows;

    public int defaultPauseWindowIndex;
    public bool canExit = true;

    public void Awake()
    {
        Initialize();
    }

    public void Initialize()
    {
        //OpenWindow(0);
    }

    public void Update()
    {
        if (GameManager.instance.inputManager.CancelInput) OnExitPressed();
    }

    public void OpenWindow(string name)
    {
        if (windows == null) return;
        OpenWindow(windows.Find((a) => a.name == name));
    }

    public void OpenWindow(int index)
    {
        if (windows == null) return;
        if (index >= windows.Count) return;
        OpenWindow(windows[index]);
    }

    public void OpenWindow(WindowItem window)
    {
        if (window == null) return;
        if (!window.isPopup) CloseWindow();
        window.window.SetActive(true);
        openedWindows.Add(window);
        if (window.initButton) window.initButton.Select();
    }

    public void CloseWindow()
    {
        if (openedWindows.Count < 1) return;
        openedWindows[^1]?.window.SetActive(false);
        openedWindows.Remove(openedWindows[^1]);
    }

    public void CloseAllWindows()
    {
        while (openedWindows.Count > 0) CloseWindow();
    }

    public void OnExitPressed()
    {
        if (openedWindows.Count < 1)
        {
            Debug.Log("menu");
            OpenWindow(defaultPauseWindowIndex);
        }
        else
        {
            Debug.Log("close");
            if (canExit) CloseWindow();
        }
    }

}