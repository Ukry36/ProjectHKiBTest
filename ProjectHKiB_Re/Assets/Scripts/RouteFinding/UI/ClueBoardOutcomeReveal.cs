using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RouteFinding.UI
{
    // 비차단형 해몽 카드. 기존 OutcomeDialog와 선택적으로 교체되며, 호버 중에는 자동 페이드아웃을 멈춘다.
    //
    // [호버 판정은 카드 오브젝트가 직접 받는다] 이 컴포넌트는 패널 루트(_panelGO)에 붙는데, uGUI는 포인터가
    // 들어간 오브젝트의 **조상 전부**에 PointerEnter를 보내고 공통 조상에는 Exit를 보내지 않는다. 그래서 여기서
    // IPointerEnterHandler를 구현하면 포인터가 보드의 노드·배경·탭 어디에 있든 "호버 중"이 되어 카드가 영영
    // 안 꺼진다(실제로 그렇게 됐다). 판정은 카드 루트(_root)에 붙인 ClueBoardOutcomeRevealHover가 받아 넘겨준다.
    public sealed class ClueBoardOutcomeReveal : MonoBehaviour
    {
        public event Action<ClueBoardOutcomePresentation> OnPresented;

        [SerializeField] private Sprite _accentSprite;
        [SerializeField, Min(0.05f)] private float _fadeSeconds = 0.22f;
        [SerializeField, Min(0f)] private float _visibleSeconds = 3.5f;

        /// <summary>
        /// 표시 시간과 페이드 길이를 바꾼다. 이 컴포넌트는 런타임에 붙으므로 인스펙터에 잡히지 않는다 —
        /// 값은 ClueBoardPanel의 "해몽 표시" 항목이 소유하고 빌드할 때 여기로 밀어 넣는다(장식 스프라이트와 같은 규칙).
        /// </summary>
        public void SetTimings(float visibleSeconds, float fadeSeconds)
        {
            _visibleSeconds = Mathf.Max(0f, visibleSeconds);
            _fadeSeconds = Mathf.Max(0.05f, fadeSeconds);
        }

        private RectTransform _root;
        private CanvasGroup _group;
        private Image _accent;
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _body;
        private TextMeshProUGUI _footer;
        private Coroutine _routine;
        private bool _hovered;
        // 카드를 한 번 더 누르면 남은 표시 시간과 호버 유지를 건너뛰고 바로 페이드아웃한다.
        private bool _dismissed;
        private float _titleLeftWithAccent = 38f; // 장식이 있을 때의 제목 왼쪽 여백. 없으면 그만큼 당긴다(InfoPopup과 같은 규칙).
        private readonly Queue<ClueBoardOutcomePresentation> _queue = new();
        private readonly HashSet<string> _keys = new(StringComparer.Ordinal);

        public bool IsShowing => _root != null && _root.gameObject.activeSelf;
        public ClueBoardOutcomePresentation Current { get; private set; }
        public int QueuedCount => _queue.Count;

        public void SetAccentSprite(Sprite sprite)
        {
            _accentSprite = sprite;
            ApplyAccent();
        }

        public void Initialize(RectTransform parent, TMP_FontAsset font)
        {
            // [레이아웃은 ClueBoardClueInfoPopup과 동일하다] 루트는 화면을 덮고, 그 안의 Card가 중앙에
            // 230x130으로 뜬다. 다만 InfoPopup과 달리 **입력을 막는 Overlay를 두지 않는다** — 해몽 카드는
            // 비차단형이라 떠 있는 동안에도 보드를 계속 만질 수 있어야 한다.
            _root = ClueBoardUiKit.Child(parent, "OutcomeReveal", out bool created);
            if (created) ClueBoardUiKit.Stretch(_root);
            _group = ClueBoardUiKit.Ensure<CanvasGroup>(_root.gameObject);
            _group.blocksRaycasts = true;
            // 루트는 화면 전체를 덮으므로 레이캐스트를 받으면 안 된다 — 받으면 보드 클릭이 막히고,
            // 호버 판정도 보드 어디서나 참이 되어 카드가 사라지지 않는다.
            var rootImage = ClueBoardUiKit.Ensure<Image>(_root.gameObject);
            rootImage.color = new Color(0f, 0f, 0f, 0f);
            rootImage.raycastTarget = false;

            RectTransform card = ClueBoardUiKit.Child(_root, "Card", out created);
            if (created)
            {
                card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 0.5f);
                card.sizeDelta = new Vector2(230f, 130f);
            }
            var cardImage = ClueBoardUiKit.Ensure<Image>(card.gameObject);
            if (created) cardImage.color = new Color(0.10f, 0.12f, 0.18f, 0.98f);
            // 호버 판정과 레이캐스트는 카드에만 둔다(위 루트 주석 참고).
            cardImage.raycastTarget = true;
            var relay = ClueBoardUiKit.Ensure<ClueBoardOutcomeRevealHover>(card.gameObject);
            relay.onEnter = HandleCardEnter; // += 가 아니라 대입 — 프리팹 자식을 재사용해 다시 빌드해도 한 번만 묶인다
            relay.onExit = HandleCardExit;
            relay.onClick = HandleCardClick;

            // 장식은 InfoPopup의 아이콘 자리를 그대로 쓴다(좌상단 24x24).
            RectTransform accent = ClueBoardUiKit.Child(card, "Accent", out created);
            if (created)
            {
                accent.anchorMin = accent.anchorMax = new Vector2(0f, 1f);
                accent.pivot = new Vector2(0f, 1f);
                accent.anchoredPosition = new Vector2(8f, -8f);
                accent.sizeDelta = new Vector2(24f, 24f);
            }
            _accent = ClueBoardUiKit.Ensure<Image>(accent.gameObject);
            _accent.preserveAspect = true;
            _accent.raycastTarget = false;

            // 제목 / 보조줄 / 본문 = InfoPopup의 Title / Subtitle / Body와 같은 자리·같은 글자 크기.
            _title = MakeText(card, "Title", font, 9f, new Color(1f, 0.88f, 0.48f), TextAlignmentOptions.MidlineLeft,
                              new Vector2(38f, -22f), new Vector2(-30f, -7f), new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f));
            _footer = MakeText(card, "Footer", font, 5.5f, new Color(0.70f, 0.74f, 0.80f), TextAlignmentOptions.MidlineLeft,
                               new Vector2(38f, -34f), new Vector2(-30f, -22f), new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f));
            _body = MakeText(card, "Body", font, 6.5f, Color.white, TextAlignmentOptions.TopLeft,
                             new Vector2(8f, 20f), new Vector2(-8f, -36f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            _body.enableWordWrapping = true;
            _body.overflowMode = TextOverflowModes.Ellipsis;

            _titleLeftWithAccent = _title.rectTransform.offsetMin.x;
            ApplyAccent();
            _root.gameObject.SetActive(false);
        }

        public void Enqueue(IEnumerable<ClueBoardOutcomePresentation> presentations)
        {
            if (presentations == null) return;
            foreach (ClueBoardOutcomePresentation presentation in presentations)
                if (presentation?.outcome != null && presentation.reading != null && _keys.Add(Key(presentation))) _queue.Enqueue(presentation);
            if (!IsShowing) ShowNext();
        }

        public void Clear()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = null;
            _queue.Clear(); _keys.Clear(); Current = null; _hovered = false; _dismissed = false;
            if (_root != null) _root.gameObject.SetActive(false);
        }

        private void HandleCardEnter() { _hovered = true; if (_group != null) _group.alpha = 1f; }
        private void HandleCardExit() => _hovered = false;

        /// <summary>카드를 누르면 남은 대기를 건너뛰고 즉시 사라지기 시작한다(페이드는 그대로 탄다).</summary>
        private void HandleCardClick() => _dismissed = true;


        /// <summary>
        /// 해몽 결과가 없는 관계의 연결 코멘트처럼, 기록할 결과가 없는 한 줄짜리 카드를 띄운다.
        /// ClueBoardOutcomeDialog.PresentMessage와 같은 규칙이다 — 이미 카드가 떠 있으면 뒤로 미루지 않고 건너뛴다.
        /// 표시 방식이 Dialog/BoardReveal 중 무엇이든 같은 종류의 알림이 같은 모양으로 나오도록 짝을 맞춘 것이다.
        /// </summary>
        public bool PresentMessage(string title, string body, string footer)
        {
            if (_root == null || string.IsNullOrEmpty(body)) return false;
            if (IsShowing) return false;
            Current = null; // 결과가 아니므로 열람 기록(OnPresented)을 남기지 않는다
            ShowCard(string.IsNullOrEmpty(title) ? ClueSystemSettings.RejectionTitle : title, body, footer ?? "");
            return true;
        }

        private void ShowNext()
        {
            if (_root == null || _queue.Count == 0) return;
            Current = _queue.Dequeue(); _keys.Remove(Key(Current));
            DreamReading reading = Current.reading;
            ShowCard(string.IsNullOrWhiteSpace(reading.title) ? "해몽" : reading.title,
                     reading.interpretation ?? "",
                     "해몽 완료 · 올리면 유지 · 누르면 닫기" + (Current.isNew ? "" : " · 재열람"));
            OnPresented?.Invoke(Current);
        }

        private void ShowCard(string title, string body, string footer)
        {
            _dismissed = false; // 앞 카드를 눌러 닫았어도 다음 카드는 처음부터 다시 센다
            _title.text = title;
            _body.text = body;
            _footer.text = footer;
            _root.SetAsLastSibling(); _group.alpha = 0f; _root.gameObject.SetActive(true);
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(Present());
        }

        private IEnumerator Present()
        {
            yield return Fade(0f, 1f);
            float elapsed = 0f;
            // 호버 중에는 시간이 멈추지만, 눌러서 닫았으면 둘 다 건너뛴다.
            while (!_dismissed && elapsed < _visibleSeconds) { if (!_hovered) elapsed += Time.unscaledDeltaTime; yield return null; }
            while (!_dismissed && _hovered) yield return null;
            yield return Fade(1f, 0f);
            _routine = null; Current = null; _root.gameObject.SetActive(false); ShowNext();
        }

        private IEnumerator Fade(float from, float to)
        {
            float time = 0f;
            while (time < _fadeSeconds)
            {
                // 사라지는 중 호버가 들어오면 다시 불투명해진다 — 단, 눌러서 닫은 경우는 그대로 사라진다.
                if (!_dismissed && _hovered && to < from) { _group.alpha = 1f; yield return null; continue; }
                time += Time.unscaledDeltaTime; _group.alpha = Mathf.Lerp(from, to, time / _fadeSeconds); yield return null;
            }
            _group.alpha = to;
        }

        private void ApplyAccent()
        {
            if (_accent == null) return;
            // 정식 장식 아트는 아직 없다. 이 주소에 스프라이트를 두면 인스펙터 지정 없이도 잡히고,
            // 없으면 장식 자리를 숨긴다 — 아트 유무가 카드 동작을 바꾸지 않는다.
            // (2026-09-22: 이 자리에 있던 DreamReadingEmblem.png은 Codex가 생성한 임시 이미지라 제거했다.)
            if (_accentSprite == null) _accentSprite = Resources.Load<Sprite>("ClueArt/DreamReadingEmblem");
            _accent.sprite = _accentSprite;
            bool has = _accentSprite != null;
            _accent.gameObject.SetActive(has);
            // 장식이 없으면 빈 여백이 남지 않게 제목·보조줄을 왼쪽으로 당긴다(InfoPopup과 같은 처리).
            float left = has ? _titleLeftWithAccent : Mathf.Max(4f, _titleLeftWithAccent - _accent.rectTransform.sizeDelta.x - 4f);
            if (_title != null) _title.rectTransform.offsetMin = new Vector2(left, _title.rectTransform.offsetMin.y);
            if (_footer != null) _footer.rectTransform.offsetMin = new Vector2(left, _footer.rectTransform.offsetMin.y);
        }

        private static string Key(ClueBoardOutcomePresentation value) => value.outcome.boardId + "|" + ClueBoardOutcomeCatalog.Key(value.outcome.kind, value.outcome.outcomeId);
        private static TextMeshProUGUI MakeText(RectTransform parent, string name, TMP_FontAsset font, float size, Color color, TextAlignmentOptions alignment, Vector2 min, Vector2 max, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
        {
            RectTransform rect = ClueBoardUiKit.Child(parent, name, out bool created);
            if (created) { rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.pivot = pivot; rect.offsetMin = min; rect.offsetMax = max; }
            var text = ClueBoardUiKit.Text(rect, created, font, size, color, alignment);
            text.raycastTarget = false;
            return text;
        }
    }

    // 해몽 카드 루트에만 붙는 얇은 포인터 수신기. 카드가 SetActive(false)로 꺼질 때 Exit가 안 오는 경우를
    // 대비해 OnDisable에서도 호버를 풀어 준다 — 안 풀면 다음 카드가 "호버 중"으로 시작해 안 사라진다.
    //
    // **중첩 클래스로 만들지 말 것.** 유니티는 중첩 MonoBehaviour에 스크립트 에셋을 만들지 않아서,
    // 런타임 AddComponent는 되지만 이 컴포넌트가 붙은 계층을 프리팹으로 저장하면 m_Script가 비어
    // ("fileID: 0") missing script가 된다 — 인스펙터를 그릴 때 NaughtyAttributes가
    // "The target object is null" 오류를 뱉는다(2026-09-22 실제 발생). 보드의 다른 소형 컴포넌트
    // (ClueBoardNodeClick 등)가 전부 최상위 클래스인 것도 같은 이유다.
    public sealed class ClueBoardOutcomeRevealHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public Action onEnter;
        public Action onExit;
        public Action onClick;
        public void OnPointerEnter(PointerEventData eventData) => onEnter?.Invoke();
        public void OnPointerExit(PointerEventData eventData) => onExit?.Invoke();
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;
            onClick?.Invoke();
        }
        private void OnDisable() => onExit?.Invoke();
    }
}
