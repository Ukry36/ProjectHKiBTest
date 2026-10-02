using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static RouteFinding.UI.RouteUiKit;

namespace RouteFinding.UI
{
    // 보드의 해금 노드를 클릭하면 뜨는 단서 설명 팝업. 결과 다이얼로그(ClueBoardOutcomeDialog)와 같은 카드 구성이지만
    // 큐·기록이 없는 단순 읽기 창이다. 닫기 버튼이나 카드 바깥(오버레이)을 누르면 닫힌다.
    //
    // [입력] 오버레이가 레이캐스트를 받아 열려 있는 동안 보드 드래그·선 클릭이 막힌다 — 바깥을 누르면 그 클릭으로 닫히고,
    // 그 클릭은 보드에 전달되지 않는다(닫으면서 엉뚱한 드래그가 시작되지 않게).
    // [내용] 이름 / 유형 · 감정 / 설명(description) + 본문(content) + 글 매체 블록. 사진·영상·소리 매체는 도감 카드 몫이라
    // 여기서는 종류만 한 줄로 알린다.
    public sealed class ClueBoardClueInfoPopup : MonoBehaviour
    {
        private RectTransform _root;
        private Image _icon;
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _subtitle;
        private TextMeshProUGUI _body;
        private TMP_FontAsset _font;
        private float _titleLeftWithIcon = 38f; // 프리팹의 제목 왼쪽 여백(아이콘 있을 때). 없으면 아이콘 폭만큼 당긴다.

        public bool IsShowing => _root != null && _root.gameObject.activeSelf;
        public ClueData Current { get; private set; }

        /// <summary>검증/진단용 — 지금까지 띄운 횟수.</summary>
        public int ShownCount { get; private set; }

        public void Initialize(RectTransform parent, TMP_FontAsset font)
        {
            _font = font;
            // 프리팹 우선: ClueInfoPopup/Overlay/Card/Icon/Title/Subtitle/Body/BtnClose를 이름으로 찾고 없을 때만 만든다.
            _root = ClueBoardUiKit.Child(parent, "ClueInfoPopup", out bool created);
            if (created) StretchFull(_root);

            // 오버레이와 카드는 형제 — 카드 안 클릭이 오버레이(닫기)까지 올라가지 않게.
            RectTransform overlayRect = ClueBoardUiKit.Child(_root, "Overlay", out created);
            if (created) StretchFull(overlayRect);
            var overlay = ClueBoardUiKit.Ensure<Image>(overlayRect.gameObject);
            if (created) overlay.color = new Color(0f, 0f, 0f, 0.45f);
            overlay.raycastTarget = true;
            var overlayButton = ClueBoardUiKit.Ensure<Button>(overlayRect.gameObject);
            overlayButton.targetGraphic = overlay;
            overlayButton.transition = Selectable.Transition.None;
            overlayButton.onClick.RemoveAllListeners();
            overlayButton.onClick.AddListener(Hide);

            RectTransform card = ClueBoardUiKit.Child(_root, "Card", out created);
            if (created)
            {
                card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 0.5f);
                card.sizeDelta = new Vector2(230f, 130f);
            }
            var cardBg = ClueBoardUiKit.Ensure<Image>(card.gameObject);
            if (created) cardBg.color = new Color(0.10f, 0.12f, 0.18f, 0.98f);
            cardBg.raycastTarget = true;

            RectTransform iconRect = ClueBoardUiKit.Child(card, "Icon", out created);
            if (created)
            {
                iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 1f);
                iconRect.pivot = new Vector2(0f, 1f);
                iconRect.anchoredPosition = new Vector2(8f, -8f);
                iconRect.sizeDelta = new Vector2(24f, 24f);
            }
            _icon = ClueBoardUiKit.Ensure<Image>(iconRect.gameObject);
            _icon.preserveAspect = true;
            _icon.raycastTarget = false;

            RectTransform titleRect = ClueBoardUiKit.Child(card, "Title", out created);
            if (created)
            {
                titleRect.anchorMin = new Vector2(0f, 1f);
                titleRect.anchorMax = Vector2.one;
                titleRect.pivot = new Vector2(0.5f, 1f);
                titleRect.offsetMin = new Vector2(38f, -22f);
                titleRect.offsetMax = new Vector2(-30f, -7f);
            }
            _title = ClueBoardUiKit.Text(titleRect, created, font, 9f, new Color(0.98f, 0.94f, 0.80f), TextAlignmentOptions.MidlineLeft);
            _titleLeftWithIcon = titleRect.offsetMin.x;

            RectTransform subRect = ClueBoardUiKit.Child(card, "Subtitle", out created);
            if (created)
            {
                subRect.anchorMin = new Vector2(0f, 1f);
                subRect.anchorMax = Vector2.one;
                subRect.pivot = new Vector2(0.5f, 1f);
                subRect.offsetMin = new Vector2(38f, -34f);
                subRect.offsetMax = new Vector2(-30f, -22f);
            }
            _subtitle = ClueBoardUiKit.Text(subRect, created, font, 5.5f, new Color(0.70f, 0.74f, 0.80f), TextAlignmentOptions.MidlineLeft);

            RectTransform bodyRect = ClueBoardUiKit.Child(card, "Body", out created);
            if (created)
            {
                bodyRect.anchorMin = Vector2.zero;
                bodyRect.anchorMax = Vector2.one;
                bodyRect.offsetMin = new Vector2(8f, 20f);
                bodyRect.offsetMax = new Vector2(-8f, -36f);
            }
            _body = ClueBoardUiKit.Text(bodyRect, created, font, 6.5f, Color.white, TextAlignmentOptions.TopLeft);
            _body.enableWordWrapping = true;
            _body.overflowMode = TextOverflowModes.Ellipsis;

            RectTransform closeRect = ClueBoardUiKit.Child(card, "BtnClose", out created);
            if (created)
            {
                closeRect.anchorMin = closeRect.anchorMax = closeRect.pivot = new Vector2(1f, 1f);
                closeRect.anchoredPosition = new Vector2(-6f, -6f);
                closeRect.sizeDelta = new Vector2(18f, 12f);
            }
            var closeImage = ClueBoardUiKit.Ensure<Image>(closeRect.gameObject);
            if (created) closeImage.color = new Color(0.20f, 0.28f, 0.42f);
            var closeButton = ClueBoardUiKit.Ensure<Button>(closeRect.gameObject);
            closeButton.targetGraphic = closeImage;
            closeButton.transition = Selectable.Transition.None;
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Hide);
            RectTransform closeLabel = ClueBoardUiKit.Child(closeRect, "Label", out created);
            if (created) StretchFull(closeLabel);
            ClueBoardUiKit.Text(closeLabel, created, font, 6.5f, Color.white, TextAlignmentOptions.Midline, "닫기");

            _root.gameObject.SetActive(false);
        }

        public void Show(ClueData clue)
        {
            if (clue == null || _root == null) return;
            Current = clue;
            _title.text = string.IsNullOrWhiteSpace(clue.name) ? clue.id : clue.name;

            var sub = new StringBuilder();
            string type = ClueTypeConfig.GetDisplayName(clue.classification);
            if (!string.IsNullOrEmpty(type))
            {
                sub.Append(type);
                if (clue.classification != null) sub.Append(" · ").Append(ClueTypeConfig.GetSubtypeName(clue.classification.subtype));
            }
            if (ClueEmotionTagConfig.IsSelectable(clue.emotionTag))
                sub.Append(sub.Length > 0 ? "  |  " : "").Append(ClueEmotionTagConfig.GetDisplayName(clue.emotionTag));
            if (!string.IsNullOrWhiteSpace(clue.source))
                sub.Append(sub.Length > 0 ? "  |  " : "").Append("출처: ").Append(clue.source.Trim());
            _subtitle.text = sub.ToString();

            var body = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(clue.description)) body.Append(clue.description.Trim());
            if (!string.IsNullOrWhiteSpace(clue.content))
                body.Append(body.Length > 0 ? "\n\n" : "").Append(clue.content.Trim());
            int mediaCount = 0;
            if (clue.mediaBlocks != null)
                foreach (ClueMediaBlock block in clue.mediaBlocks)
                {
                    if (block == null) continue;
                    if (block.kind == ClueMediaKind.Text && !string.IsNullOrWhiteSpace(block.text))
                        body.Append(body.Length > 0 ? "\n\n" : "").Append(block.text.Trim());
                    else if (block.kind != ClueMediaKind.Text) mediaCount++;
                }
            if (mediaCount > 0)
                body.Append(body.Length > 0 ? "\n\n" : "").Append($"<color=#B0B6C0>(사진·영상·소리 매체 {mediaCount}개 — 도감에서 볼 수 있다)</color>");
            _body.text = body.Length > 0 ? body.ToString() : "<color=#B0B6C0>(설명 없음)</color>";

            Sprite icon = ClueTypeIconSet.ResolveIcon(clue);
            _icon.sprite = icon;
            _icon.gameObject.SetActive(icon != null);
            float left = icon != null ? _titleLeftWithIcon : Mathf.Max(4f, _titleLeftWithIcon - _icon.rectTransform.sizeDelta.x - 4f);
            _title.rectTransform.offsetMin = new Vector2(left, _title.rectTransform.offsetMin.y);
            _subtitle.rectTransform.offsetMin = new Vector2(left, _subtitle.rectTransform.offsetMin.y);

            _root.SetAsLastSibling();
            _root.gameObject.SetActive(true);
            ShownCount++;
        }

        public void Hide()
        {
            Current = null;
            if (_root != null) _root.gameObject.SetActive(false);
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
