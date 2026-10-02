using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace RouteFinding.UI
{
    // 영상 단서를 보드 위에서 재생하는 오버레이. ClueBoardImagePreview와 같은 모양이지만, 사진과 달리
    // 영상은 RenderTexture를 잡으므로 "닫히면 반드시 멈춘다"가 계약이다.
    //
    // 재생 자체는 도감이 쓰는 ClueMediaVideoPlayer를 그대로 재사용한다(로딩은 ClueAttachmentService).
    // 플레이어를 이 오버레이의 루트에 붙여 두는 것이 핵심 — 루트를 SetActive(false)하면 플레이어의
    // OnDisable이 돌아 재생 정지와 렌더 타깃 해제가 함께 일어난다. 패널이 통째로 닫힐 때도 같다.
    public sealed class ClueBoardVideoPreview : MonoBehaviour
    {
        private RectTransform _root;
        private RawImage _surface;
        private TextMeshProUGUI _caption;
        private ClueMediaVideoPlayer _player;

        public bool IsShowing => _root != null && _root.gameObject.activeSelf;

        public void Initialize(RectTransform parent, TMP_FontAsset font)
        {
            _root = ClueBoardUiKit.Child(parent, "ClueVideoPreview", out bool created);
            if (created) ClueBoardUiKit.Stretch(_root);

            RectTransform overlay = ClueBoardUiKit.Child(_root, "Overlay", out created);
            if (created) ClueBoardUiKit.Stretch(overlay);
            var overlayImage = ClueBoardUiKit.Ensure<Image>(overlay.gameObject);
            if (created) overlayImage.color = new Color(0f, 0f, 0f, 0.78f);
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
                card.sizeDelta = new Vector2(260f, 172f);
            }
            var cardImage = ClueBoardUiKit.Ensure<Image>(card.gameObject);
            if (created) cardImage.color = new Color(0.05f, 0.06f, 0.09f, 0.98f);
            cardImage.raycastTarget = true;

            RectTransform screen = ClueBoardUiKit.Child(card, "Screen", out created);
            if (created)
            {
                screen.anchorMin = Vector2.zero; screen.anchorMax = Vector2.one;
                screen.offsetMin = new Vector2(10f, 25f); screen.offsetMax = new Vector2(-10f, -10f);
            }
            _surface = ClueBoardUiKit.Ensure<RawImage>(screen.gameObject);
            // 재생 전에는 검은 화면. ClueMediaVideoPlayer가 재생하면서 texture와 흰색을 직접 끼운다.
            _surface.color = Color.black;
            _surface.raycastTarget = false;

            RectTransform caption = ClueBoardUiKit.Child(card, "Caption", out created);
            if (created)
            {
                caption.anchorMin = new Vector2(0f, 0f); caption.anchorMax = new Vector2(1f, 0f);
                caption.offsetMin = new Vector2(10f, 6f); caption.offsetMax = new Vector2(-10f, 22f);
            }
            _caption = ClueBoardUiKit.Text(caption, created, font, 6f, new Color(0.80f, 0.82f, 0.88f), TextAlignmentOptions.MidlineLeft);

            _root.gameObject.SetActive(false);
            _player = ClueMediaVideoPlayer.AttachTo(_root.gameObject);
        }

        public void Show(VideoClip clip, string caption)
        {
            if (_root == null || clip == null) return;
            _caption.text = (string.IsNullOrWhiteSpace(caption) ? "영상" : caption) + " · 바깥을 누르면 닫기";
            _root.SetAsLastSibling();
            // 재생은 반드시 활성화 뒤에 건다 — 꺼진 오브젝트에 붙은 플레이어는 OnDisable 규칙상 바로 멈춘다.
            _root.gameObject.SetActive(true);
            _player.Toggle(clip, _surface, null, OnPlaybackError);
        }

        public void Hide()
        {
            if (_root == null) return;
            // SetActive(false)만으로도 플레이어의 OnDisable이 정지·해제를 하지만, 순서를 코드에 드러내 둔다.
            _player?.Stop();
            _surface.texture = null;
            _surface.color = Color.black;
            _root.gameObject.SetActive(false);
        }

        // 코덱·손상 파일 때문에 주소를 찾고도 재생이 실패할 수 있다. 창을 닫지 않고 자막만 바꿔 알린다.
        private void OnPlaybackError(string message)
        {
            if (_caption != null) _caption.text = "영상을 재생할 수 없습니다 — " + message;
            if (_surface != null) { _surface.texture = null; _surface.color = Color.black; }
        }
    }
}
