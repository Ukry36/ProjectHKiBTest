using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RouteFinding.UI
{
    /// <summary>실제 신규 획득 이벤트를 한 장씩 보여 주는 독립 토스트입니다.</summary>
    public sealed class ClueAcquireNotice : MonoBehaviour
    {
        [SerializeField] private RectTransform _noticeRoot;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _title;
        [SerializeField] private TextMeshProUGUI _message;
        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private AudioDataSO _acquireAudio;
        [SerializeField, Range(0f, 1f)] private float _audioVolume = 1f;
        [SerializeField] private bool _soundOnly;
        [SerializeField, Min(1)] private int _maxQueueLength = 8;
        [SerializeField, Min(0f)] private float _displaySeconds = 2.5f;
        [SerializeField, Min(0f)] private float _fadeSeconds = .2f;

        private readonly Queue<ClueData> _queue = new();
        private RouteProgressState _subscribedProgress;
        private Coroutine _displayRoutine;

        public int PendingCount => _queue.Count;

        private void Awake()
        {
            if (_noticeRoot == null) _noticeRoot = transform as RectTransform;
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            if (_font != null)
            {
                if (_title != null) _title.font = _font;
                if (_message != null) _message.font = _font;
            }
            SetVisible(false);
        }

        private void Update() => TrySubscribe();

        private void OnDestroy()
        {
            if (_subscribedProgress != null) _subscribedProgress.OnClueAcquired -= HandleClueAcquired;
            _subscribedProgress = null;
        }

        private void TrySubscribe()
        {
            RouteProgressState progress = RouteModule.Instance?.Progress;
            if (progress == null || ReferenceEquals(progress, _subscribedProgress)) return;
            if (_subscribedProgress != null) _subscribedProgress.OnClueAcquired -= HandleClueAcquired;
            _subscribedProgress = progress;
            _subscribedProgress.OnClueAcquired += HandleClueAcquired;
        }

        private void HandleClueAcquired(ClueData clue)
        {
            PlaySound();
            if (_soundOnly || clue == null) return;
            if (_queue.Count >= Mathf.Max(1, _maxQueueLength))
            {
                Debug.LogWarning("[ClueAcquireNotice] 토스트 큐가 가득 차 새 알림을 생략했습니다.");
                return;
            }
            _queue.Enqueue(clue);
            if (_displayRoutine == null) _displayRoutine = StartCoroutine(DisplayQueue());
        }

        private IEnumerator DisplayQueue()
        {
            while (_queue.Count > 0)
            {
                ClueData clue = _queue.Dequeue();
                ApplyClue(clue);
                yield return Fade(0f, 1f);
                yield return new WaitForSecondsRealtime(_displaySeconds);
                yield return Fade(1f, 0f);
            }
            _displayRoutine = null;
        }

        private void ApplyClue(ClueData clue)
        {
            if (_title != null) _title.text = string.IsNullOrEmpty(clue.name) ? clue.id : clue.name;
            if (_message != null) _message.text = "단서 획득";
            // 단서별 대표 아이콘 → 유형 기본 아이콘(Resources/ClueTypeIcons.asset) 순. 둘 다 없으면 자리를 접는다.
            Sprite sprite = ClueTypeIconSet.ResolveIcon(clue);
            if (_icon != null)
            {
                _icon.sprite = sprite;
                // 아이콘 주소가 없거나 로드 실패하면 빈 칸을 남기지 않는 것이 기획의 '아이콘 자리 생략'이다.
                _icon.gameObject.SetActive(sprite != null);
            }
        }

        private IEnumerator Fade(float from, float to)
        {
            SetVisible(true);
            if (_fadeSeconds <= 0f)
            {
                _canvasGroup.alpha = to;
                SetVisible(to > 0f);
                yield break;
            }
            float elapsed = 0f;
            while (elapsed < _fadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / _fadeSeconds));
                yield return null;
            }
            _canvasGroup.alpha = to;
            SetVisible(to > 0f);
        }

        private void SetVisible(bool visible)
        {
            // 인스펙터 배선을 빼먹은 개발 씬에서도 자기 자신을 꺼 버리면 코루틴이 중단된다.
            if (_noticeRoot != null && _noticeRoot.gameObject != gameObject) _noticeRoot.gameObject.SetActive(visible);
            if (_canvasGroup != null) _canvasGroup.alpha = visible ? _canvasGroup.alpha : 0f;
        }

        private void PlaySound()
        {
            AudioManager audioManager = GameManager.instance != null ? GameManager.instance.audioManager : null;
            if (audioManager != null && _acquireAudio != null)
                audioManager.PlayAudioOneShot(_acquireAudio, _audioVolume, Vector3.zero);
        }
    }
}
