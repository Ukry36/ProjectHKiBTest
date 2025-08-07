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
        private Animator currentWindowAnimator;
        private Animator nextWindowAnimator;
        private ButtonManager currentButton;
        private ButtonManager nextButtonAnimator;

        // Helpers
        string windowFadeIn = "In";
        string windowFadeOut = "Out";
        float cachedStateLength;
        public bool altMode;

        [System.Serializable]
        public class WindowItem
        {
            public string windowName = "My Window";
            public GameObject windowObject;
            public GameObject buttonObject;
            public GameObject firstSelected;
        }

        void Awake()
        {
            if (windows.Count == 0)
                return;

            InitializeWindows();
        }

        void OnEnable()
        {
            if (isInitialized == true && nextWindowAnimator == null)
            {
                currentWindowAnimator.Play(windowFadeIn);
                if (currentButton != null) { currentButton.PlayHoverAnimation(); }
            }

            else if (isInitialized == true && nextWindowAnimator != null)
            {
                nextWindowAnimator.Play(windowFadeIn);
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
            currentWindowAnimator = currentWindow.GetComponent<Animator>();
            currentWindowAnimator.Play(windowFadeIn);
            onWindowChange.Invoke(currentWindowIndex);

            if (altMode == true) { cachedStateLength = 0.3f; }
            else { cachedStateLength = MUIPInternalTools.GetAnimatorClipLength(currentWindowAnimator, MUIPInternalTools.windowManagerStateName); }

            isInitialized = true;

            for (int i = 0; i < windows.Count; i++)
            {
                if (i != currentWindowIndex && cullWindows == true) { windows[i].windowObject.SetActive(false); }
                if (windows[i].buttonObject != null && initializeButtons == true)
                {
                    string tempName = windows[i].windowName;
                    ButtonManager tempButton = windows[i].buttonObject.GetComponent<ButtonManager>();

                    if (tempButton != null)
                    {
                        tempButton.onClick.RemoveAllListeners();
                        tempButton.onClick.AddListener(() => OpenPanel(tempName));
                    }
                }
            }
        }

        public void OpenFirstTab()
        {
            if (currentWindowIndex != 0)
            {
                currentWindow = windows[currentWindowIndex].windowObject;
                currentWindowAnimator = currentWindow.GetComponent<Animator>();
                currentWindowAnimator.Play(windowFadeOut);

                if (windows[currentWindowIndex].buttonObject != null)
                {
                    currentButtonObj = windows[currentWindowIndex].buttonObject;
                    currentButton = currentButtonObj.GetComponent<ButtonManager>();
                    currentButton.PlayNormalAnimation();
                }

                currentWindowIndex = 0;
                currentButtonIndex = 0;

                currentWindow = windows[currentWindowIndex].windowObject;
                currentWindowAnimator = currentWindow.GetComponent<Animator>();
                currentWindowAnimator.Play(windowFadeIn);

                if (windows[currentWindowIndex].firstSelected != null) { EventSystem.current.firstSelectedGameObject = windows[currentWindowIndex].firstSelected; }
                if (windows[currentButtonIndex].buttonObject != null)
                {
                    currentButtonObj = windows[currentButtonIndex].buttonObject;
                    currentButton = currentButtonObj.GetComponent<ButtonManager>();
                    currentButton.PlayHoverAnimation();
                }

                onWindowChange.Invoke(currentWindowIndex);
            }

            else if (currentWindowIndex == 0)
            {
                currentWindow = windows[currentWindowIndex].windowObject;
                currentWindowAnimator = currentWindow.GetComponent<Animator>();
                currentWindowAnimator.Play(windowFadeIn);

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
                if (cullWindows == true)
                    StopCoroutine("DisablePreviousWindow");

                currentWindow = windows[currentWindowIndex].windowObject;

                if (windows[currentWindowIndex].buttonObject != null)
                    currentButtonObj = windows[currentWindowIndex].buttonObject;

                currentWindowIndex = newWindowIndex;
                nextWindow = windows[currentWindowIndex].windowObject;
                nextWindow.SetActive(true);

                currentWindowAnimator = currentWindow.GetComponent<Animator>();
                nextWindowAnimator = nextWindow.GetComponent<Animator>();

                currentWindowAnimator.Play(windowFadeOut);
                nextWindowAnimator.Play(windowFadeIn);

                if (cullWindows == true)
                    StartCoroutine("DisablePreviousWindow");

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

                onWindowChange.Invoke(currentWindowIndex);
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
                if (cullWindows == true)
                    StopCoroutine("DisablePreviousWindow");

                currentWindow = windows[currentWindowIndex].windowObject;
                currentWindow.gameObject.SetActive(true);

                if (windows[currentButtonIndex].buttonObject != null)
                {
                    currentButtonObj = windows[currentButtonIndex].buttonObject;
                    nextButtonObj = windows[currentButtonIndex + 1].buttonObject;

                    currentButton = currentButtonObj.GetComponent<ButtonManager>();
                    currentButton.PlayNormalAnimation();
                }

                currentWindowAnimator = currentWindow.GetComponent<Animator>();
                currentWindowAnimator.Play(windowFadeOut);

                currentWindowIndex += 1;
                currentButtonIndex += 1;

                nextWindow = windows[currentWindowIndex].windowObject;
                nextWindow.gameObject.SetActive(true);

                nextWindowAnimator = nextWindow.GetComponent<Animator>();
                nextWindowAnimator.Play(windowFadeIn);

                if (cullWindows == true) { StartCoroutine("DisablePreviousWindow"); }
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
                if (cullWindows == true)
                    StopCoroutine("DisablePreviousWindow");

                currentWindow = windows[currentWindowIndex].windowObject;
                currentWindow.gameObject.SetActive(true);

                if (windows[currentButtonIndex].buttonObject != null)
                {
                    currentButtonObj = windows[currentButtonIndex].buttonObject;
                    nextButtonObj = windows[currentButtonIndex - 1].buttonObject;

                    currentButton = currentButtonObj.GetComponent<ButtonManager>();
                    currentButton.PlayNormalAnimation();
                }

                currentWindowAnimator = currentWindow.GetComponent<Animator>();
                currentWindowAnimator.Play(windowFadeOut);

                currentWindowIndex -= 1;
                currentButtonIndex -= 1;

                nextWindow = windows[currentWindowIndex].windowObject;
                nextWindow.gameObject.SetActive(true);

                nextWindowAnimator = nextWindow.GetComponent<Animator>();
                nextWindowAnimator.Play(windowFadeIn);

                if (cullWindows == true) { StartCoroutine("DisablePreviousWindow"); }
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
            if (nextWindowAnimator == null) { currentWindowAnimator.Play(windowFadeIn); }
            else { nextWindowAnimator.Play(windowFadeIn); }
        }

        public void HideCurrentWindow()
        {
            if (nextWindowAnimator == null) { currentWindowAnimator.Play(windowFadeOut); }
            else { nextWindowAnimator.Play(windowFadeOut); }
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

        IEnumerator DisablePreviousWindow()
        {
            yield return new WaitForSecondsRealtime(cachedStateLength);

            for (int i = 0; i < windows.Count; i++)
            {
                if (i == currentWindowIndex)
                    continue;

                windows[i].windowObject.SetActive(false);
            }
        }
    }
}