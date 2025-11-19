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

    public void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        //OpenWindow(0);
        GameManager.instance.inputManager.onMenu += OnOpenMenu;
        GameManager.instance.inputManager.onMENUCancel += OnCloseWindow;
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

    public void OnExitPressed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (context.started)
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

    public void OnOpenMenu(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (context.started)
        {
            OpenWindow(defaultPauseWindowIndex);
            GameManager.instance.inputManager.MENUMode();
        }
    }

    public void OnCloseWindow(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (canExit) CloseWindow();
            if (openedWindows.Count < 1)
                GameManager.instance.inputManager.PLAYMode();
        }
    }

}