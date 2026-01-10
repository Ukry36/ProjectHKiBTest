using System;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Serializable]
    public class WindowItem
    {
        public string name;
        public Window window;
        public bool useHotkey;
        public EnumManager.InputType hotkey;
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
        if (!window.window.isPopup) CloseWindow();
        //Debug.Log("Window opened: " + window.name);
        window.window.Open();
        openedWindows.Add(window);
        InitButton(false);
    }

    public void CloseWindow()
    {
        if (openedWindows.Count < 1) return;
        openedWindows[^1]?.window.Close();
        bool isClosedWindowPopup = openedWindows[^1].window.isPopup;
        openedWindows.Remove(openedWindows[^1]);
        //Debug.Log($"Window closed, remaining window stack: {openedWindows.Count}");
        if (openedWindows.Count < 1)
            GameManager.instance.inputManager.PLAYMode();
        else
            InitButton(isClosedWindowPopup);
    }

    public void CloseWindow(string name)
    {
        if (windows == null) return;
        WindowItem window = openedWindows.Find((a) => a.name == name);
        if (window != null)
        {
            window.window.Close();
            bool isClosedWindowPopup = window.window.isPopup;
            openedWindows.Remove(window);
            //Debug.Log($"Window closed: {window.name}, remaining window stack: {openedWindows.Count}");
            if (openedWindows.Count < 1)
                GameManager.instance.inputManager.PLAYMode();
            else
                InitButton(isClosedWindowPopup);
        }
    }

    public void InitButton(bool fromPopupClose)
    {
        WindowItem window = openedWindows[^1];
        window.window.SelectInitButton(fromPopupClose);
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
                OpenWindow(defaultPauseWindowIndex);
            else
                if (canExit) CloseWindow();
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