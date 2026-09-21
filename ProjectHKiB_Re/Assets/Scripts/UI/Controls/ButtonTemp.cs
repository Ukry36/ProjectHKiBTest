using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using TMPro;


[ExecuteInEditMode]
public class ButtonTemp : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler, ISubmitHandler
{
    // Content
    public Sprite buttonIcon;
    public string buttonText = "Button";

    // Resources
    public CanvasGroup normalCG;
    public CanvasGroup highlightCG;
    public CanvasGroup pressedCG;
    public CanvasGroup disabledCG;
    public TextMeshProUGUI normalText;
    public TextMeshProUGUI highlightedText;
    public TextMeshProUGUI pressedText;
    public TextMeshProUGUI disabledText;
    public Image normalImage;
    public Image highlightImage;
    public Image disabledImage;
    public Image pressedImage;
    public AudioSource soundSource;
    [SerializeField] private GameObject rippleParent;

    // Settings
    public bool isInteractable = true;
    public AudioClip hoverSound;
    public AudioClip clickSound;
    public bool useUINavigation = false;
    public Navigation.Mode navigationMode = Navigation.Mode.Automatic;
    public GameObject selectOnUp;
    public GameObject selectOnDown;
    public GameObject selectOnLeft;
    public GameObject selectOnRight;
    public bool wrapAround = false;
    public bool useRipple = true;
    [SerializeField] private AnimationSolution animationSolution = AnimationSolution.ScriptBased;

    // Events
    public UnityEvent onClick = new UnityEvent();
    public UnityEvent onHover = new UnityEvent();
    public UnityEvent onLeave = new UnityEvent();

    // Helpers
    bool isInitialized = false;
    Button targetButton;
    bool isPointerOn;
    bool isPointerDown;
    const int navHelper = 1;

    // Anim
    string hoverToPressed = "Hover to Pressed";
    string normalToPressed = "Normal to Pressed";
    string pressedToNormal = "Pressed to Normal";
    string hoverToNormal = "Hover to Normal";
    string normalToHover = "Normal to Hover";
    string pressedToHover = "Pressed to Hover";

    public enum AnimationSolution
    {
        Custom,
        ScriptBased,
        AnimationBased
    }

    void OnEnable()
    {
        if (!isInitialized) { Initialize(); }
        UpdateUI();
    }

    void OnDisable()
    {
        if (!isInteractable)
            return;

        if (disabledCG != null) { disabledCG.alpha = 0; }
        if (normalCG != null) { normalCG.alpha = 1; }
        if (highlightCG != null) { highlightCG.alpha = 0; }
        if (pressedCG != null) { pressedCG.alpha = 0; }
    }

    void Initialize()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) { return; }
#endif
        if (TryGetComponent<Animator>(out var tempAnimator))
        {
            if (animationSolution == AnimationSolution.ScriptBased) Destroy(tempAnimator);
        }
        else if (animationSolution == AnimationSolution.AnimationBased)
        {
            gameObject.AddComponent<Animator>();
        }

        if (gameObject.GetComponent<Image>() == null)
        {
            Image raycastImg = gameObject.AddComponent<Image>();
            raycastImg.color = new Color(0, 0, 0, 0);
            raycastImg.raycastTarget = true;
        }

        if (normalCG == null) { normalCG = new GameObject().AddComponent<CanvasGroup>(); normalCG.gameObject.AddComponent<RectTransform>(); normalCG.transform.SetParent(transform); normalCG.gameObject.name = "Normal"; }
        if (highlightCG == null) { highlightCG = new GameObject().AddComponent<CanvasGroup>(); highlightCG.gameObject.AddComponent<RectTransform>(); highlightCG.transform.SetParent(transform); highlightCG.gameObject.name = "Highlight"; }
        if (pressedCG == null) { pressedCG = new GameObject().AddComponent<CanvasGroup>(); pressedCG.gameObject.AddComponent<RectTransform>(); pressedCG.transform.SetParent(transform); pressedCG.gameObject.name = "PressedCG"; }
        if (disabledCG == null) { disabledCG = new GameObject().AddComponent<CanvasGroup>(); disabledCG.gameObject.AddComponent<RectTransform>(); disabledCG.transform.SetParent(transform); disabledCG.gameObject.name = "Disabled"; }

        if (useRipple && rippleParent != null) { rippleParent.SetActive(false); }
        else if (!useRipple && rippleParent != null) { Destroy(rippleParent); }

        if (targetButton == null)
        {
            if (gameObject.GetComponent<Button>() == null) { targetButton = gameObject.AddComponent<Button>(); }
            else { targetButton = GetComponent<Button>(); }

            targetButton.transition = Selectable.Transition.None;

            Navigation customNav = new Navigation();
            customNav.mode = Navigation.Mode.None;
            targetButton.navigation = customNav;
        }
        if (useUINavigation) { AddUINavigation(); }

        isInitialized = true;
    }

    public void UpdateUI()
    {
        if (normalCG != null && isInteractable) { normalCG.alpha = 1; }
        if (disabledCG != null && !isInteractable) { disabledCG.alpha = 1; }
        if (highlightCG != null) { highlightCG.alpha = 0; }
        if (pressedCG != null) { pressedCG.alpha = 0; }

        if (!Application.isPlaying || !gameObject.activeInHierarchy) { return; }
        if (!isInteractable) { StartCoroutine(nameof(SetDisabled)); }
        else if (isInteractable && disabledCG.alpha == 1) { StartCoroutine(nameof(SetNormal)); }
    }

    public void SetText(string text) { buttonText = text; UpdateUI(); }
    public void SetIcon(Sprite icon) { buttonIcon = icon; UpdateUI(); }

    public void SetInteractable(bool value)
    {
        isInteractable = value;

        if (!gameObject.activeInHierarchy) { return; }
        if (!isInteractable) PlayDisableAnimation();
        else if (isInteractable && disabledCG.alpha == 1) PlayNormalAnimation();
    }

    public void AddUINavigation()
    {
        if (targetButton == null)
            return;

        targetButton.transition = Selectable.Transition.None;
        Navigation customNav = new Navigation
        {
            mode = navigationMode
        };

        if (navigationMode == Navigation.Mode.Vertical || navigationMode == Navigation.Mode.Horizontal) { customNav.wrapAround = wrapAround; }
        else if (navigationMode == Navigation.Mode.Explicit) { StartCoroutine(nameof(InitUINavigation), customNav); return; }

        targetButton.navigation = customNav;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        isPointerDown = false;
        if (!isInteractable || eventData.button != PointerEventData.InputButton.Left) { return; }
        if (soundSource != null) { soundSource.PlayOneShot(clickSound); }

        // Invoke click actions
        onClick.Invoke();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isInteractable) { return; }
        PlayPressAnimation();
        isPointerDown = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isInteractable) { return; }
        PlayHoverAnimation();
        isPointerDown = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isInteractable) { return; }
        if (soundSource != null) { soundSource.PlayOneShot(hoverSound); }

        if (isPointerDown) PlayPressAnimation();
        else PlayHoverAnimation();

        onHover.Invoke();

        isPointerOn = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isInteractable) { return; }
        PlayNormalAnimation();

        onLeave.Invoke();

        isPointerOn = false;
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (!isInteractable) { return; }
        PlayHoverAnimation();
        if (useUINavigation) { onHover.Invoke(); }
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (!isInteractable) { return; }
        PlayNormalAnimation();
        if (useUINavigation) { onLeave.Invoke(); }
    }

    public void OnSubmit(BaseEventData eventData)
    {
        if (!isInteractable) { return; }
        PlayNormalAnimation();
        onClick.Invoke();
    }

    public void PlayDisableAnimation()
    {
        SetDisabled();
    }

    public void PlayNormalAnimation()
    {
        if (animationSolution == AnimationSolution.ScriptBased) SetNormal();
        if (animationSolution == AnimationSolution.AnimationBased)
        {
            if (isPointerDown) gameObject.GetComponent<Animator>().Play(pressedToNormal);
            else gameObject.GetComponent<Animator>().Play(hoverToNormal);
        }
    }

    public void PlayPressAnimation()
    {
        if (animationSolution == AnimationSolution.ScriptBased) SetPressed();
        if (animationSolution == AnimationSolution.AnimationBased)
        {
            if (isPointerOn) gameObject.GetComponent<Animator>().Play(hoverToPressed);
            else gameObject.GetComponent<Animator>().Play(normalToPressed);
        }
    }

    public void PlayHoverAnimation()
    {
        if (animationSolution == AnimationSolution.ScriptBased) SetHighlight();
        if (animationSolution == AnimationSolution.AnimationBased)
        {
            if (isPointerDown) gameObject.GetComponent<Animator>().Play(pressedToHover);
            else gameObject.GetComponent<Animator>().Play(normalToHover);
        }
    }

    public void SetNormal()
    {
        normalCG.alpha = 1;
        highlightCG.alpha = 0;
        disabledCG.alpha = 0;
        pressedCG.alpha = 0;
    }

    public void SetHighlight()
    {
        normalCG.alpha = 0;
        highlightCG.alpha = 1;
        disabledCG.alpha = 0;
        pressedCG.alpha = 0;
    }

    public void SetDisabled()
    {
        normalCG.alpha = 0;
        highlightCG.alpha = 0;
        disabledCG.alpha = 1;
        pressedCG.alpha = 0;
    }

    public void SetPressed()
    {
        normalCG.alpha = 0;
        highlightCG.alpha = 0;
        disabledCG.alpha = 0;
        pressedCG.alpha = 1;
    }

    IEnumerator InitUINavigation(Navigation nav)
    {
        yield return new WaitForSecondsRealtime(navHelper);

        if (selectOnUp != null) { nav.selectOnUp = selectOnUp.GetComponent<Selectable>(); }
        if (selectOnDown != null) { nav.selectOnDown = selectOnDown.GetComponent<Selectable>(); }
        if (selectOnLeft != null) { nav.selectOnLeft = selectOnLeft.GetComponent<Selectable>(); }
        if (selectOnRight != null) { nav.selectOnRight = selectOnRight.GetComponent<Selectable>(); }

        targetButton.navigation = nav;
    }
}