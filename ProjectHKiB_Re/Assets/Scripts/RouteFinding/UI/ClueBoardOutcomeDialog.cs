using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static RouteFinding.UI.RouteUiKit;

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
    //
    // [거절 코멘트] 관계 없는 쌍의 거절 코멘트(ClueBoardRejectionComment)도 같은 카드로 보여 준다(PresentComment). 결과가
    // 아니므로 큐·키·OnPresented·viewed를 전혀 거치지 않고 즉시 덮어 띄우며, "확인"으로 닫으면 큐에 남은 결과가 이어진다.
    // 거절은 보드가 조작 가능한 상태(= 아무것도 떠 있지 않을 때)에서만 일어나므로 떠 있는 결과를 가리는 일은 없다.
    public sealed class ClueBoardOutcomeDialog : MonoBehaviour
    {
        /// <summary>결과 하나를 실제로 화면에 띄웠을 때.</summary>
        public event Action<ClueBoardOutcomePresentation> OnPresented;

        /// <summary>검증/진단용 — 마지막으로 띄운 거절 코멘트와 그 횟수. 결과(Current)와 별개다.</summary>
        public ClueBoardRejectionComment CurrentComment { get; private set; }
        public int CommentPresentedCount { get; private set; }

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
            // 프리팹 우선: OutcomeDialog/Overlay/Card/Title/Body/Footer/BtnClose를 이름으로 찾고 없을 때만 만든다.
            _root = ClueBoardUiKit.Child(parent, "OutcomeDialog", out bool created);
            if (created) StretchFull(_root);

            // 오버레이와 카드는 형제다. 카드를 오버레이의 자식으로 두면 카드 안 클릭이 부모 Button(닫기)까지
            // 올라가 본문을 눌러도 닫힌다.
            RectTransform overlayRect = ClueBoardUiKit.Child(_root, "Overlay", out created);
            if (created) StretchFull(overlayRect);
            var overlay = ClueBoardUiKit.Ensure<Image>(overlayRect.gameObject);
            if (created) overlay.color = new Color(0f, 0f, 0f, 0.55f);
            overlay.raycastTarget = true; // 결과를 닫기 전까지 보드 조작을 막는다
            var overlayButton = ClueBoardUiKit.Ensure<Button>(overlayRect.gameObject);
            overlayButton.targetGraphic = overlay;
            overlayButton.transition = Selectable.Transition.None;
            overlayButton.onClick.RemoveAllListeners();
            overlayButton.onClick.AddListener(Dismiss);

            RectTransform card = ClueBoardUiKit.Child(_root, "Card", out created);
            if (created)
            {
                card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 0.5f);
                card.sizeDelta = new Vector2(220f, 110f);
            }
            var cardBg = ClueBoardUiKit.Ensure<Image>(card.gameObject);
            if (created) cardBg.color = new Color(0.10f, 0.12f, 0.18f, 0.98f);
            cardBg.raycastTarget = true; // 카드가 레이캐스트를 삼켜 뒤의 오버레이 닫기가 눌리지 않는다

            RectTransform titleRect = ClueBoardUiKit.Child(card, "Title", out created);
            if (created)
            {
                titleRect.anchorMin = new Vector2(0f, 1f);
                titleRect.anchorMax = Vector2.one;
                titleRect.pivot = new Vector2(0.5f, 1f);
                titleRect.sizeDelta = new Vector2(-16f, 20f);
                titleRect.anchoredPosition = new Vector2(0f, -8f);
            }
            _title = ClueBoardUiKit.Text(titleRect, created, font, 9f, new Color(0.98f, 0.90f, 0.45f), TextAlignmentOptions.Midline);

            RectTransform bodyRect = ClueBoardUiKit.Child(card, "Body", out created);
            if (created)
            {
                bodyRect.anchorMin = Vector2.zero;
                bodyRect.anchorMax = Vector2.one;
                bodyRect.offsetMin = new Vector2(10f, 30f);
                bodyRect.offsetMax = new Vector2(-10f, -30f);
            }
            _body = ClueBoardUiKit.Text(bodyRect, created, font, 7f, Color.white, TextAlignmentOptions.TopLeft);
            _body.enableWordWrapping = true;
            _body.overflowMode = TextOverflowModes.Ellipsis;

            RectTransform footerRect = ClueBoardUiKit.Child(card, "Footer", out created);
            if (created)
            {
                footerRect.anchorMin = new Vector2(0f, 0f);
                footerRect.anchorMax = new Vector2(1f, 0f);
                footerRect.pivot = new Vector2(0.5f, 0f);
                footerRect.sizeDelta = new Vector2(-16f, 12f);
                footerRect.anchoredPosition = new Vector2(0f, 18f);
            }
            _footer = ClueBoardUiKit.Text(footerRect, created, font, 5.5f, new Color(0.70f, 0.74f, 0.80f), TextAlignmentOptions.MidlineLeft);

            RectTransform closeRect = ClueBoardUiKit.Child(card, "BtnClose", out created);
            if (created)
            {
                closeRect.anchorMin = new Vector2(0.5f, 0f);
                closeRect.anchorMax = new Vector2(0.5f, 0f);
                closeRect.pivot = new Vector2(0.5f, 0f);
                closeRect.sizeDelta = new Vector2(60f, 14f);
                closeRect.anchoredPosition = new Vector2(0f, 4f);
            }
            var closeImage = ClueBoardUiKit.Ensure<Image>(closeRect.gameObject);
            if (created) closeImage.color = new Color(0.20f, 0.28f, 0.42f);
            var closeButton = ClueBoardUiKit.Ensure<Button>(closeRect.gameObject);
            closeButton.targetGraphic = closeImage;
            closeButton.transition = Selectable.Transition.None;
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Dismiss);
            RectTransform closeLabel = ClueBoardUiKit.Child(closeRect, "Label", out created);
            if (created) StretchFull(closeLabel);
            ClueBoardUiKit.Text(closeLabel, created, font, 7f, Color.white, TextAlignmentOptions.Midline, "확인");

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

        /// <summary>
        /// 거절 코멘트를 즉시 띄운다. 결과 큐와 무관하며 기록을 남기지 않는다. 결과가 떠 있는 중이면(오버레이가 입력을 막고
        /// 있어 원래 일어날 수 없는 경우) 결과를 가리지 않도록 무시한다.
        /// </summary>
        public bool PresentComment(ClueBoardRejectionComment comment)
        {
            if (comment == null) return false;
            if (!PresentMessage(ClueSystemSettings.RejectionTitle, comment.text, "연관 없음")) return false;
            CurrentComment = comment;
            return true;
        }

        /// <summary>검증/진단용 — 지금 떠 있는 일반 메시지 본문(결과 카드면 null).</summary>
        public string CurrentMessageText { get; private set; }

        /// <summary>
        /// 결과가 아닌 일반 메시지(연결 코멘트 등)를 같은 카드로 띄운다. 큐·기록을 거치지 않으며, 결과가 떠 있는 중이면
        /// 거부한다. "확인"으로 닫으면 큐에 남은 결과가 이어진다.
        /// </summary>
        public bool PresentMessage(string title, string body, string footer)
        {
            if (_root == null || string.IsNullOrEmpty(body)) return false;
            if (Current != null) return false;

            CurrentComment = null;
            CurrentMessageText = body;
            _title.text = string.IsNullOrEmpty(title) ? ClueSystemSettings.RejectionTitle : title;
            _body.text = body;
            _footer.text = footer ?? "";
            _root.SetAsLastSibling();
            _root.gameObject.SetActive(true);
            CommentPresentedCount++;
            return true;
        }

        /// <summary>큐와 화면을 비운다(패널을 닫을 때). 기록은 건드리지 않는다.</summary>
        public void Clear()
        {
            _queue.Clear();
            _queuedKeys.Clear();
            Current = null;
            CurrentComment = null;
            CurrentMessageText = null;
            if (_root != null) _root.gameObject.SetActive(false);
        }

        /// <summary>"확인" — 지금 결과(또는 코멘트)를 닫고 큐의 다음 결과를 띄운다.</summary>
        public void Dismiss()
        {
            Current = null;
            CurrentComment = null;
            CurrentMessageText = null;
            if (_root != null) _root.gameObject.SetActive(false);
            ShowNext();
        }

        private void ShowNext()
        {
            if (_queue.Count == 0 || _root == null) return;
            ClueBoardOutcomePresentation next = _queue.Dequeue();
            _queuedKeys.Remove(next.outcome.boardId + "|" + ClueBoardOutcomeCatalog.Key(next.outcome.kind, next.outcome.outcomeId));
            Current = next;
            CurrentComment = null;
            CurrentMessageText = null;

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

    }
}
