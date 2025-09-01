using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Michsky.MUIP
{
    public class WindowManager : MonoBehaviour
    {
        // Content
        public List<WindowItem> windows = new List<WindowItem>();

        // Settings
        public int currentWindowIndex = 0;
        private int currentButtonIndex = 0;
        private int newWindowIndex;
        public bool cullWindows = true;
        public bool initializeButtons = true;
        bool isInitialized = false;

        // Events
        [System.Serializable] public class WindowChangeEvent : UnityEvent<int> { }
        public WindowChangeEvent onWindowChange;

        // Hidden vars
        private GameObject currentWindow;
        private GameObject nextWindow;
        private GameObject currentButtonObj;
        private GameObject nextButtonObj;
        private SimpleUIActivator currentWindowActivator;
        private SimpleUIActivator nextWindowActivator;
        private ButtonManager currentButton;
        private ButtonManager nextButtonAnimator;

        [System.Serializable]
        public class WindowItem
        {
            public string windowName = "My Window";
            public GameObject windowObject;
            public GameObject buttonObject;
            public GameObject firstSelected;
            public UnityEvent OnWindowShow;
            public UnityEvent OnWindowHide;
        }

        void Awake()
        {
            if (windows.Count == 0)
                return;

            InitializeWindows();
        }

        void OnEnable()
        {
            if (isInitialized == true && nextWindowActivator == null)
            {
                currentWindowActivator.SetEnable();
                if (currentButton != null) { currentButton.PlayHoverAnimation(); }
            }

            else if (isInitialized == true && nextWindowActivator != null)
            {
                nextWindowActivator.SetEnable();
                if (nextButtonAnimator != null) { nextButtonAnimator.PlayHoverAnimation(); }
            }
        }

        public void InitializeWindows()
        {
            if (windows[currentWindowIndex].firstSelected != null) { EventSystem.current.firstSelectedGameObject = windows[currentWindowIndex].firstSelected; }
            if (windows[currentWindowIndex].buttonObject != null)
            {
                currentButtonObj = windows[currentWindowIndex].buttonObject;
                currentButton = currentButtonObj.GetComponent<ButtonManager>();
                currentButton.PlayHoverAnimation();
            }

            currentWindow = windows[currentWindowIndex].windowObject;
            currentWindowActivator = currentWindow.GetComponent<SimpleUIActivator>();
            currentWindowActivator.SetEnable();
            InvokeChangeWindowEvents();
            isInitialized = true;

            for (int i = 0; i < windows.Count; i++)
            {
                if (i != currentWindowIndex && cullWindows == true) { windows[i].windowObject.SetActive(false); }
                if (windows[i].buttonObject != null && initializeButtons == true)
                {
                    string tempName = windows[i].windowName;
                    if (windows[i].buttonObject.TryGetComponent(out ButtonManager tempButton))
                    {
                        tempButton.onClick.RemoveAllListeners();
                        tempButton.onClick.AddListener(() => OpenPanel(tempName));
                    }
                }
            }
        }

        public void InvokeChangeWindowEvents()
        {
            for (int i = 0; i < windows.Count; i++)
            {
                if (i == currentWindowIndex) windows[i].OnWindowShow.Invoke();
                else windows[i].OnWindowHide.Invoke();
            }
            onWindowChange.Invoke(currentWindowIndex);
        }

        public void OpenFirstTab()
        {
            if (currentWindowIndex != 0)
            {
                currentWindow = windows[currentWindowIndex].windowObject;
                currentWindowActivator = currentWindow.GetComponent<SimpleUIActivator>();
                currentWindowActivator.SetDisable();

                if (windows[currentWindowIndex].buttonObject != null)
                {
                    currentButtonObj = windows[currentWindowIndex].buttonObject;
                    currentButton = currentButtonObj.GetComponent<ButtonManager>();
                    currentButton.PlayNormalAnimation();
                }

                currentWindowIndex = 0;
                currentButtonIndex = 0;

                currentWindow = windows[currentWindowIndex].windowObject;
                currentWindowActivator = currentWindow.GetComponent<SimpleUIActivator>();
                currentWindowActivator.SetEnable();

                if (windows[currentWindowIndex].firstSelected != null) { EventSystem.current.firstSelectedGameObject = windows[currentWindowIndex].firstSelected; }
                if (windows[currentButtonIndex].buttonObject != null)
                {
                    currentButtonObj = windows[currentButtonIndex].buttonObject;
                    currentButton = currentButtonObj.GetComponent<ButtonManager>();
                    currentButton.PlayHoverAnimation();
                }
                InvokeChangeWindowEvents();
            }

            else if (currentWindowIndex == 0)
            {
                currentWindow = windows[currentWindowIndex].windowObject;
                currentWindowActivator = currentWindow.GetComponent<SimpleUIActivator>();
                currentWindowActivator.SetEnable();

                if (windows[currentWindowIndex].firstSelected != null) { EventSystem.current.firstSelectedGameObject = windows[currentWindowIndex].firstSelected; }
                if (windows[currentButtonIndex].buttonObject != null)
                {
                    currentButtonObj = windows[currentButtonIndex].buttonObject;
                    currentButton = currentButtonObj.GetComponent<ButtonManager>();
                    currentButton.PlayHoverAnimation();
                }
            }
        }

        public void OpenWindow(string newWindow)
        {
            for (int i = 0; i < windows.Count; i++)
            {
                if (windows[i].windowName == newWindow)
                {
                    newWindowIndex = i;
                    break;
                }
            }

            if (newWindowIndex != currentWindowIndex)
            {
                currentWindow = windows[currentWindowIndex].windowObject;

                if (windows[currentWindowIndex].buttonObject != null)
                    currentButtonObj = windows[currentWindowIndex].buttonObject;

                currentWindowIndex = newWindowIndex;
                nextWindow = windows[currentWindowIndex].windowObject;
                nextWindow.SetActive(true);

                currentWindowActivator = currentWindow.GetComponent<SimpleUIActivator>();
                nextWindowActivator = nextWindow.GetComponent<SimpleUIActivator>();

                currentWindowActivator.SetDisable();
                nextWindowActivator.SetEnable();

                currentButtonIndex = newWindowIndex;

                if (windows[currentWindowIndex].firstSelected != null) { EventSystem.current.firstSelectedGameObject = windows[currentWindowIndex].firstSelected; }
                if (windows[currentButtonIndex].buttonObject != null)
                {
                    nextButtonObj = windows[currentButtonIndex].buttonObject;

                    currentButton = currentButtonObj.GetComponent<ButtonManager>();
                    nextButtonAnimator = nextButtonObj.GetComponent<ButtonManager>();

                    currentButton.PlayNormalAnimation();
                    nextButtonAnimator.PlayHoverAnimation();
                }

                InvokeChangeWindowEvents();
            }
        }

        // Old method
        public void OpenPanel(string newPanel)
        {
            OpenWindow(newPanel);
        }

        public void OpenWindowByIndex(int windowIndex)
        {
            for (int i = 0; i < windows.Count; i++)
            {
                if (windows[i].windowName == windows[windowIndex].windowName)
                {
                    OpenWindow(windows[windowIndex].windowName);
                    break;
                }
            }
        }

        public void NextWindow()
        {
            if (currentWindowIndex <= windows.Count - 2)
            {
                currentWindow = windows[currentWindowIndex].windowObject;
                currentWindow.gameObject.SetActive(true);

                if (windows[currentButtonIndex].buttonObject != null)
                {
                    currentButtonObj = windows[currentButtonIndex].buttonObject;
                    nextButtonObj = windows[currentButtonIndex + 1].buttonObject;

                    currentButton = currentButtonObj.GetComponent<ButtonManager>();
                    currentButton.PlayNormalAnimation();
                }

                currentWindowActivator = currentWindow.GetComponent<SimpleUIActivator>();
                currentWindowActivator.SetDisable();
                currentWindowIndex += 1;
                currentButtonIndex += 1;

                nextWindow = windows[currentWindowIndex].windowObject;
                nextWindow.gameObject.SetActive(true);

                nextWindowActivator = nextWindow.GetComponent<SimpleUIActivator>();
                nextWindowActivator.SetEnable();

                if (windows[currentWindowIndex].firstSelected != null) { EventSystem.current.firstSelectedGameObject = windows[currentWindowIndex].firstSelected; }
                if (nextButtonObj != null)
                {
                    nextButtonAnimator = nextButtonObj.GetComponent<ButtonManager>();
                    nextButtonAnimator.PlayHoverAnimation();
                }

                onWindowChange.Invoke(currentWindowIndex);
            }
        }

        public void PrevWindow()
        {
            if (currentWindowIndex >= 1)
            {

                currentWindow = windows[currentWindowIndex].windowObject;
                currentWindow.gameObject.SetActive(true);

                if (windows[currentButtonIndex].buttonObject != null)
                {
                    currentButtonObj = windows[currentButtonIndex].buttonObject;
                    nextButtonObj = windows[currentButtonIndex - 1].buttonObject;

                    currentButton = currentButtonObj.GetComponent<ButtonManager>();
                    currentButton.PlayNormalAnimation();
                }

                currentWindowActivator = currentWindow.GetComponent<SimpleUIActivator>();
                currentWindowActivator.SetDisable();

                currentWindowIndex -= 1;
                currentButtonIndex -= 1;

                nextWindow = windows[currentWindowIndex].windowObject;
                nextWindow.gameObject.SetActive(true);

                nextWindowActivator = nextWindow.GetComponent<SimpleUIActivator>();
                nextWindowActivator.SetEnable();

                if (windows[currentWindowIndex].firstSelected != null) { EventSystem.current.firstSelectedGameObject = windows[currentWindowIndex].firstSelected; }
                if (nextButtonObj != null)
                {
                    nextButtonAnimator = nextButtonObj.GetComponent<ButtonManager>();
                    nextButtonAnimator.PlayHoverAnimation();
                }

                onWindowChange.Invoke(currentWindowIndex);
            }
        }

        public void ShowCurrentWindow()
        {
            if (nextWindowActivator == null) { currentWindowActivator.SetEnable(); }
            else { nextWindowActivator.SetEnable(); }
        }

        public void HideCurrentWindow()
        {
            if (nextWindowActivator == null) { currentWindowActivator.SetDisable(); }
            else { nextWindowActivator.SetDisable(); }
        }

        public void ShowCurrentButton()
        {
            if (nextButtonAnimator == null) { currentButton.PlayHoverAnimation(); }
            else { nextButtonAnimator.PlayHoverAnimation(); }
        }

        public void HideCurrentButton()
        {
            if (nextButtonAnimator == null) { currentButton.PlayNormalAnimation(); }
            else { nextButtonAnimator.PlayNormalAnimation(); }
        }

        public void AddNewItem()
        {
            WindowItem window = new WindowItem();

            if (windows.Count != 0 && windows[windows.Count - 1].windowObject != null)
            {
                int tempIndex = windows.Count - 1;

                GameObject tempWindow = windows[tempIndex].windowObject.transform.parent.GetChild(tempIndex).gameObject;
                GameObject newWindow = Instantiate(tempWindow, new Vector3(0, 0, 0), Quaternion.identity) as GameObject;

                newWindow.transform.SetParent(windows[tempIndex].windowObject.transform.parent, false);
                newWindow.gameObject.name = "New Window " + tempIndex.ToString();

                window.windowName = "New Window " + tempIndex.ToString();
                window.windowObject = newWindow;

                if (windows[tempIndex].buttonObject != null)
                {
                    GameObject tempButton = windows[tempIndex].buttonObject.transform.parent.GetChild(tempIndex).gameObject;
                    GameObject newButton = Instantiate(tempButton, new Vector3(0, 0, 0), Quaternion.identity) as GameObject;

                    newButton.transform.SetParent(windows[tempIndex].buttonObject.transform.parent, false);
                    newButton.gameObject.name = "New Window " + tempIndex.ToString();

                    window.buttonObject = newButton;
                }
            }

            windows.Add(window);
        }

    }
}