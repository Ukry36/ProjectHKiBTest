using System;
using System.Collections.Generic;
using UnityEditor.PackageManager.UI;
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

    public DialogueModule dialogueModule;

    public void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        //OpenWindow(0);
        GameManager.instance.inputManager.onMenu += OnOpenMenuInput;
        GameManager.instance.inputManager.onMENUCancel += OnCloseWindowInput;
        dialogueModule.onExitDialogue += () => { canExit = true; CloseWindow("Dialogue"); };
    }

    public void OnDestroy()
    {
        GameManager.instance.inputManager.onMenu -= OnOpenMenuInput;
        GameManager.instance.inputManager.onMENUCancel -= OnCloseWindowInput;
        dialogueModule.onExitDialogue -= () => { canExit = true; CloseWindow("Dialogue"); };
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
        Debug.Log("Window opened: " + window.name);
        window.window.SetActive(true);
        openedWindows.Add(window);
        if (window.initButton) window.initButton.Select();
    }

    public void CloseWindow()
    {
        if (openedWindows.Count < 1) return;
        openedWindows[^1]?.window.SetActive(false);
        openedWindows.Remove(openedWindows[^1]);
        Debug.Log($"Window closed, remaining window stack: {openedWindows.Count}");
        if (openedWindows.Count < 1)
            GameManager.instance.inputManager.PLAYMode();
    }

    public void CloseWindow(string name)
    {
        if (windows == null) return;
        WindowItem window = openedWindows.Find((a) => a.name == name);
        if (window != null)
        {
            window.window.SetActive(false);
            openedWindows.Remove(window);
            Debug.Log($"Window closed: {window.name}, remaining window stack: {openedWindows.Count}");
            if (openedWindows.Count < 1)
                GameManager.instance.inputManager.PLAYMode();
        }
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

    public void OnOpenMenuInput(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (context.started)
        {
            OpenWindow(defaultPauseWindowIndex);
            GameManager.instance.inputManager.MENUMode();
        }
    }

    public void OnCloseWindowInput(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (canExit) CloseWindow();
        }
    }

    public void StartDialogue(DialogueDataSO dialogueData)
    {
        OpenWindow("Dialogue");
        canExit = false;
        dialogueModule.StartDialogue(dialogueData);
    }

    public void ExitDialogue() => dialogueModule.ExitDialogue();
}