using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RouteFinding.UI
{
    // 사진 단서를 보드 위에서 확대해 보는 읽기 전용 오버레이. 바깥 클릭/닫기 모두 원래 보드 조작으로 전달되지 않는다.
    public sealed class ClueBoardImagePreview : MonoBehaviour
    {
        private RectTransform _root;
        private Image _image;
        private TextMeshProUGUI _caption;

        public bool IsShowing => _root != null && _root.gameObject.activeSelf;

        public void Initialize(RectTransform parent, TMP_FontAsset font)
        {
            _root = ClueBoardUiKit.Child(parent, "ClueImagePreview", out bool created);
            if (created) ClueBoardUiKit.Stretch(_root);
            RectTransform overlay = ClueBoardUiKit.Child(_root, "Overlay", out created);
            if (created) ClueBoardUiKit.Stretch(overlay);
            var overlayImage = ClueBoardUiKit.Ensure<Image>(overlay.gameObject);
            if (created) overlayImage.color = new Color(0f, 0f, 0f, 0.72f);
            overlayImage.raycastTarget = true;
            var closeOverlay = ClueBoardUiKit.Ensure<Button>(overlay.gameObject);
            closeOverlay.targetGraphic = overlayImage;
            closeOverlay.transition = Selectable.Transition.None;
            closeOverlay.onClick.RemoveAllListeners();
            closeOverlay.onClick.AddListener(Hide);

            RectTransform card = ClueBoardUiKit.Child(_root, "Card", out created);
            if (created)
            {
                card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 0.5f);
                card.sizeDelta = new Vector2(260f, 180f);
            }
            var cardImage = ClueBoardUiKit.Ensure<Image>(card.gameObject);
            if (created) cardImage.color = new Color(0.06f, 0.07f, 0.10f, 0.98f);
            cardImage.raycastTarget = true;

            RectTransform imageRect = ClueBoardUiKit.Child(card, "Image", out created);
            if (created)
            {
                imageRect.anchorMin = Vector2.zero; imageRect.anchorMax = Vector2.one;
                imageRect.offsetMin = new Vector2(10f, 25f); imageRect.offsetMax = new Vector2(-10f, -10f);
            }
            _image = ClueBoardUiKit.Ensure<Image>(imageRect.gameObject);
            _image.preserveAspect = true;
            _image.raycastTarget = false;

            RectTransform caption = ClueBoardUiKit.Child(card, "Caption", out created);
            if (created)
            {
                caption.anchorMin = new Vector2(0f, 0f); caption.anchorMax = new Vector2(1f, 0f);
                caption.offsetMin = new Vector2(10f, 6f); caption.offsetMax = new Vector2(-10f, 22f);
            }
            _caption = ClueBoardUiKit.Text(caption, created, font, 6f, new Color(0.80f, 0.82f, 0.88f), TextAlignmentOptions.MidlineLeft);
            _root.gameObject.SetActive(false);
        }

        public void Show(Sprite sprite, string caption)
        {
            if (_root == null || sprite == null) return;
            _image.sprite = sprite;
            _caption.text = string.IsNullOrWhiteSpace(caption) ? "사진 · 클릭하면 닫기" : caption + " · 클릭하면 닫기";
            _root.SetAsLastSibling();
            _root.gameObject.SetActive(true);
        }

        public void Hide() { if (_root != null) _root.gameObject.SetActive(false); }
    }
}
