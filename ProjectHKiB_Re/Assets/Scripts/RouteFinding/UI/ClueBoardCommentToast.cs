using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RouteFinding.UI
{
    // 관계 없는 연결의 거절 코멘트("이 [A]와 [B]는 아무런 연관이 없어 보인다…")를 보드 하단에 잠깐 띄우는 토스트.
    //
    // 결과 다이얼로그(ClueBoardOutcomeDialog)와 달리 입력을 막지 않고 닫기 버튼도 없다 — 거절은 보상도 기록도 없는
    // 즉답이라 플레이어가 확인을 누르게 하면 시도할 때마다 손이 한 번씩 더 간다. 표시 시간이 지나면 스스로 사라지고,
    // 표시 중에 또 거절되면 문구와 시간을 새로 시작한다. 레이캐스트를 받지 않으므로 드래그·탭 조작이 그대로 된다.
    public sealed class ClueBoardCommentToast : MonoBehaviour
    {
        [SerializeField] private float _fadeSeconds = 0.25f;

        private RectTransform _root;
        private CanvasGroup _group;
        private TextMeshProUGUI _text;
        private float _remaining;
        private float _duration;

        public bool IsShowing => _root != null && _root.gameObject.activeSelf;
        public string CurrentText => IsShowing ? _text.text : null;

        /// <summary>검증/진단용 — 지금까지 띄운 횟수.</summary>
        public int ShownCount { get; private set; }

        public void Initialize(RectTransform parent, TMP_FontAsset font)
        {
            // 프리팹 우선: CommentToast/Text가 있으면 배치·색을 그대로 쓴다.
            _root = ClueBoardUiKit.Child(parent, "CommentToast", out bool created);
            if (created)
            {
                // 하단 가운데. 우측 하단 탭 줄(폭 58)을 피해 왼쪽으로 조금 치우친다.
                _root.anchorMin = new Vector2(0.5f, 0f);
                _root.anchorMax = new Vector2(0.5f, 0f);
                _root.pivot = new Vector2(0.5f, 0f);
                _root.anchoredPosition = new Vector2(-29f, 6f);
                _root.sizeDelta = new Vector2(220f, 16f);
            }
            var bg = ClueBoardUiKit.Ensure<Image>(_root.gameObject);
            if (created) bg.color = new Color(0.05f, 0.05f, 0.08f, 0.92f);
            bg.raycastTarget = false;
            _group = ClueBoardUiKit.Ensure<CanvasGroup>(_root.gameObject);
            _group.blocksRaycasts = false;
            _group.interactable = false;

            RectTransform textRect = ClueBoardUiKit.Child(_root, "Text", out bool textCreated);
            if (textCreated)
            {
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(6f, 1f);
                textRect.offsetMax = new Vector2(-6f, -1f);
            }
            _text = ClueBoardUiKit.Text(textRect, textCreated, font, 6.5f, new Color(0.92f, 0.90f, 0.85f), TextAlignmentOptions.Midline);
            _text.enableWordWrapping = true;
            _text.overflowMode = TextOverflowModes.Ellipsis;
            _root.gameObject.SetActive(false);
        }

        /// <summary>문구를 띄우고 seconds 뒤에 스스로 숨긴다. 이미 떠 있으면 문구와 시간을 새로 시작한다.</summary>
        public void Show(string text, float seconds)
        {
            if (_root == null || string.IsNullOrEmpty(text)) return;
            _text.text = text;
            _duration = Mathf.Max(0.05f, seconds);
            _remaining = _duration;
            _group.alpha = 1f;
            _root.SetAsLastSibling();
            _root.gameObject.SetActive(true);
            ShownCount++;
        }

        public void Hide()
        {
            _remaining = 0f;
            if (_root != null) _root.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!IsShowing) return;
            // 보드는 열려 있는 동안 게임이 멈춰 있을 수 있으므로 실시간으로 센다.
            _remaining -= Time.unscaledDeltaTime;
            if (_remaining <= 0f) { Hide(); return; }
            if (_fadeSeconds > 0f && _remaining < _fadeSeconds)
                _group.alpha = Mathf.Clamp01(_remaining / _fadeSeconds);
        }
    }
}
