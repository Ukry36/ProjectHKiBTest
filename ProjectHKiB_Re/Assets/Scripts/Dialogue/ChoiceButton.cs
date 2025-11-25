using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class ChoiceButton : MonoBehaviour, ISelectHandler, IPointerEnterHandler
{
    public RectTransform cursorArrow;

    [Header("Refs")]
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private GameObject lockIcon;           // If It's Lock선택(잠금 아이콘)
    [SerializeField] private CanvasGroup canvasGroup;       // 선택(페이드/잠금 시 사용)

    [Header("Options")]
    [SerializeField] private bool autoSelectOnShow = false; // Show 시 자동 포커스
    [SerializeField] private bool autoPlayClickSound = false;
    [SerializeField] private AudioSource sfxSource;         // 선택(클릭 SFX)
    [SerializeField] private AudioClip clickClip;

    // 내부 상태
    private Action<int> onClick;
    private int nextIndex = -1;
    private bool locked = false;

    #region Unity lifecycle
    private void Reset()
    {
        if (!button) button = GetComponent<Button>();
        if (!label)  label  = GetComponentInChildren<TextMeshProUGUI>(true);
        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        if (!button) button = GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(HandleClick);
    }
    #endregion


    //한 번의 선택지 바인딩: 텍스트/점프 인덱스/콜백

    public void Setup(string text, int nextLineIndex, Action<int> onClicked)
    {
        if (label) label.text = text;
        nextIndex = nextLineIndex;
        onClick   = onClicked;

        SetLocked(false);
        SetInteractable(true);
        Show();

        // 중복 등록 방지(안전)
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(HandleClick);
    }

    // 버튼 클릭 처리(잠금/비활성 시 무시)
    private void HandleClick()
    {
        if (locked || !button || !button.interactable) return;

        if (autoPlayClickSound && sfxSource && clickClip)
            sfxSource.PlayOneShot(clickClip);

        // 캡처된 nextIndex 전달
        onClick?.Invoke(nextIndex);
    }

    public void SetText(string text)
    {
        if (label) label.text = text;
    }

    public void SetInteractable(bool value)
    {
        if (button) button.interactable = value && !locked;

        if (canvasGroup)
        {
            // 비활성/잠금 시 살짝 흐리게
            canvasGroup.alpha = (value && !locked) ? 1f : 0.6f;
            canvasGroup.blocksRaycasts = value && !locked;
        }
    }
 
    public void SetLocked(bool value, string lockedTextSuffix = "")
    {
        locked = value;

        if (lockIcon) lockIcon.SetActive(value);
        if (label && value && !string.IsNullOrEmpty(lockedTextSuffix))
            label.text += lockedTextSuffix;   // 예: " (잠김)"

        SetInteractable(!value);
    }

    public void Show()
    {
        gameObject.SetActive(true);
        if (autoSelectOnShow) Focus();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    /// 키보드/패드 네비게이션 포커스
    public void Focus()
    {
        if (!button) return;
        EventSystem.current?.SetSelectedGameObject(button.gameObject);
    }

    // 마우스 오버 시 자동 포커스(키/패드 네비와 충돌 없이 자연스럽게)
    public void OnPointerEnter(PointerEventData eventData) => Focus();
    
    public void OnSelect(BaseEventData eventData)
    {
        if (cursorArrow == null) return;

        cursorArrow.gameObject.SetActive(true);

        // 화살표를 버튼 왼쪽으로 이동
        Vector3 pos = transform.position;
        pos.x -= 2f;
        cursorArrow.position = pos;
    }

    
}

