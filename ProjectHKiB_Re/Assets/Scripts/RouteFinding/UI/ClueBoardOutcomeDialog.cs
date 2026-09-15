using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RouteFinding.UI
{
    // [C06] 관계 결과(해몽) 읽기 전용 다이얼로그 — ClueBoardPanel 위에 뜬다.
    //
    // 기존 해몽 카드(NotePanel.EnsureReadingCard)는 노트 창 안에 묶여 있어 보드 창에서 재사용할 수 없다. 같은 크기·
    // 색 구성을 따르되 보드 패널 안에 따로 만든다. 새 발행과 재열람이 같은 화면을 쓰고, 한 조작에 결과가 여럿이면
    // 큐로 하나씩 보여 준다(같은 식별자는 큐에 한 번만 들어간다).
    //
    // [입력] 오버레이가 레이캐스트를 막아 결과를 닫기 전까지 보드 조작(드래그·선 클릭·탭)을 받지 않는다. 입력
    // 모드(InputManager)나 플레이어 상태는 건드리지 않는다 — 그건 패널이 열릴 때 이미 MENU로 바뀌어 있다.
    //
    // [viewed] 결과를 실제로 화면에 띄운 순간 OnPresented를 발행한다. 패널이 그때 발행 기록의 viewed를 세워, 다음 열람에
    // 보류분으로 다시 뜨지 않게 한다("확인"을 누르지 않고 창을 닫아도 이미 본 것이다).
    public sealed class ClueBoardOutcomeDialog : MonoBehaviour
    {
        /// <summary>결과 하나를 실제로 화면에 띄웠을 때.</summary>
        public event Action<ClueBoardOutcomePresentation> OnPresented;

        private RectTransform _root;
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _body;
        private TextMeshProUGUI _footer;
        private TMP_FontAsset _font;

        private readonly Queue<ClueBoardOutcomePresentation> _queue = new();
        private readonly HashSet<string> _queuedKeys = new(StringComparer.Ordinal);

        public bool IsShowing => _root != null && _root.gameObject.activeSelf;
        public ClueBoardOutcomePresentation Current { get; private set; }
        public int QueuedCount => _queue.Count;

        /// <summary>검증/진단용 — 지금까지 화면에 띄운 횟수.</summary>
        public int PresentedCount { get; private set; }

        public void Initialize(RectTransform parent, TMP_FontAsset font)
        {
            _font = font;
            _root = NewRect(parent, "OutcomeDialog");
            StretchFull(_root);

            // 오버레이와 카드는 형제다. 카드를 오버레이의 자식으로 두면 카드 안 클릭이 부모 Button(닫기)까지
            // 올라가 본문을 눌러도 닫힌다.
            var overlayRect = NewRect(_root, "Overlay");
            StretchFull(overlayRect);
            var overlay = overlayRect.gameObject.AddComponent<Image>();
            overlay.color = new Color(0f, 0f, 0f, 0.55f);
            overlay.raycastTarget = true; // 결과를 닫기 전까지 보드 조작을 막는다
            var overlayButton = overlayRect.gameObject.AddComponent<Button>();
            overlayButton.targetGraphic = overlay;
            overlayButton.transition = Selectable.Transition.None;
            overlayButton.onClick.AddListener(Dismiss);

            var card = NewRect(_root, "Card");
            card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(220f, 110f);
            var cardBg = card.gameObject.AddComponent<Image>();
            cardBg.color = new Color(0.10f, 0.12f, 0.18f, 0.98f);
            cardBg.raycastTarget = true; // 카드가 레이캐스트를 삼켜 뒤의 오버레이 닫기가 눌리지 않는다

            var titleRect = NewRect(card, "Title");
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = Vector2.one;
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(-16f, 20f);
            titleRect.anchoredPosition = new Vector2(0f, -8f);
            _title = MakeText(titleRect, 9f, new Color(0.98f, 0.90f, 0.45f), TextAlignmentOptions.Midline);

            var bodyRect = NewRect(card, "Body");
            bodyRect.anchorMin = Vector2.zero;
            bodyRect.anchorMax = Vector2.one;
            bodyRect.offsetMin = new Vector2(10f, 30f);
            bodyRect.offsetMax = new Vector2(-10f, -30f);
            _body = MakeText(bodyRect, 7f, Color.white, TextAlignmentOptions.TopLeft);
            _body.enableWordWrapping = true;
            _body.overflowMode = TextOverflowModes.Ellipsis;

            var footerRect = NewRect(card, "Footer");
            footerRect.anchorMin = new Vector2(0f, 0f);
            footerRect.anchorMax = new Vector2(1f, 0f);
            footerRect.pivot = new Vector2(0.5f, 0f);
            footerRect.sizeDelta = new Vector2(-16f, 12f);
            footerRect.anchoredPosition = new Vector2(0f, 18f);
            _footer = MakeText(footerRect, 5.5f, new Color(0.70f, 0.74f, 0.80f), TextAlignmentOptions.MidlineLeft);

            var closeRect = NewRect(card, "BtnClose");
            closeRect.anchorMin = new Vector2(0.5f, 0f);
            closeRect.anchorMax = new Vector2(0.5f, 0f);
            closeRect.pivot = new Vector2(0.5f, 0f);
            closeRect.sizeDelta = new Vector2(60f, 14f);
            closeRect.anchoredPosition = new Vector2(0f, 4f);
            var closeImage = closeRect.gameObject.AddComponent<Image>();
            closeImage.color = new Color(0.20f, 0.28f, 0.42f);
            var closeButton = closeRect.gameObject.AddComponent<Button>();
            closeButton.targetGraphic = closeImage;
            closeButton.transition = Selectable.Transition.None;
            closeButton.onClick.AddListener(Dismiss);
            var closeLabel = NewRect(closeRect, "Label");
            StretchFull(closeLabel);
            MakeText(closeLabel, 7f, Color.white, TextAlignmentOptions.Midline).text = "확인";

            _root.gameObject.SetActive(false);
        }

        /// <summary>결과들을 큐에 넣고, 아무것도 떠 있지 않으면 첫 항목을 바로 띄운다. 같은 결과는 한 번만 들어간다.</summary>
        public void Enqueue(IEnumerable<ClueBoardOutcomePresentation> presentations)
        {
            if (presentations == null) return;
            foreach (ClueBoardOutcomePresentation presentation in presentations)
            {
                if (presentation?.outcome == null || presentation.reading == null) continue;
                string key = presentation.outcome.boardId + "|" + ClueBoardOutcomeCatalog.Key(presentation.outcome.kind, presentation.outcome.outcomeId);
                if (!_queuedKeys.Add(key)) continue;
                _queue.Enqueue(presentation);
            }
            if (!IsShowing) ShowNext();
        }

        /// <summary>큐와 화면을 비운다(패널을 닫을 때). 기록은 건드리지 않는다.</summary>
        public void Clear()
        {
            _queue.Clear();
            _queuedKeys.Clear();
            Current = null;
            if (_root != null) _root.gameObject.SetActive(false);
        }

        /// <summary>"확인" — 지금 결과를 닫고 큐의 다음 결과를 띄운다.</summary>
        public void Dismiss()
        {
            Current = null;
            if (_root != null) _root.gameObject.SetActive(false);
            ShowNext();
        }

        private void ShowNext()
        {
            if (_queue.Count == 0 || _root == null) return;
            ClueBoardOutcomePresentation next = _queue.Dequeue();
            _queuedKeys.Remove(next.outcome.boardId + "|" + ClueBoardOutcomeCatalog.Key(next.outcome.kind, next.outcome.outcomeId));
            Current = next;

            DreamReading reading = next.reading;
            _title.text = string.IsNullOrEmpty(reading.title) ? "해몽" : reading.title;
            _body.text = reading.interpretation ?? "";
            string kind = next.outcome.kind == ClueBoardOutcomeKind.Chain
                ? $"관계 사슬 완성 ({next.outcome.relationIds.Count}건)"
                : "관계 연결";
            _footer.text = $"{kind} · {next.outcome.outcomeId}" + (next.isNew ? "" : " · 재열람");

            _root.SetAsLastSibling();
            _root.gameObject.SetActive(true);
            PresentedCount++;
            OnPresented?.Invoke(next);
        }

        private TextMeshProUGUI MakeText(RectTransform parent, float size, Color color, TextAlignmentOptions align)
        {
            var text = parent.gameObject.AddComponent<TextMeshProUGUI>();
            if (_font != null) text.font = _font;
            text.fontSize = size;
            text.color = color;
            text.alignment = align;
            text.enableWordWrapping = false;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform NewRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
