using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

// 단서 본문 영상 블록(ClueMediaKind.Video) 재생 담당 — ClueAttachmentAudioPlayer와 같은 패턴으로,
// 붙은 GameObject에 VideoPlayer 하나를 만들어 재사용한다. 한 화면에서 영상이 여러 개 동시에 돌지
// 않고, 재생 상태는 onPlayingChanged 콜백으로 알려 호출부가 버튼 라벨을 바꾸는 데 쓴다.
//
// [수명 관리] 영상은 소리와 달리 RenderTexture라는 GPU 자원을 하나 더 잡는다. 그래서 정지 시점이
// 세 군데 있다:
//   1) 카드를 다른 단서로 바꿀 때        — 호출부가 Stop()
//   2) 도감 패널이 닫힐 때(비활성화)     — OnDisable()
//   3) 카드 자체가 파괴될 때             — OnDestroy()에서 RenderTexture까지 해제
// RenderTexture는 필요한 순간에만 만들고, 정지하면 화면(RawImage)에서 떼어낸 뒤 Release한다 —
// 안 그러면 도감을 여닫을 때마다 안 쓰는 렌더 타깃이 쌓인다.
//
// VideoClip 자체의 로딩/캐시는 ClueAttachmentService.LoadVideo가 맡는다(Addressables 핸들 소유권도
// 그쪽에 있다) — 이 컴포넌트는 "재생과 표면"만 책임진다.
public class ClueMediaVideoPlayer : MonoBehaviour
{
    private VideoPlayer _player;
    private RenderTexture _target;
    private RawImage _surface;
    private Action<bool> _onPlayingChanged;
    private Action<string> _onError;

    // 영상 원본 해상도가 커도 카드 안에서는 작게 보이므로, 렌더 타깃은 실제 클립 크기가 아니라
    // 이 상한으로 잘라 만든다(세로는 클립 비율에 맞춰 계산).
    private const int MaxTargetWidth = 512;

    public static ClueMediaVideoPlayer AttachTo(GameObject host)
    {
        var existing = host.GetComponent<ClueMediaVideoPlayer>();
        return existing != null ? existing : host.AddComponent<ClueMediaVideoPlayer>();
    }

    public bool IsPlaying => _player != null && _player.isPlaying;

    // 같은 영상을 다시 누르면 정지, 다른 영상을 누르면 이전 것을 멈추고 새로 재생한다
    // (ClueAttachmentAudioPlayer.Toggle과 같은 규칙).
    public void Toggle(VideoClip clip, RawImage surface, Action<bool> onPlayingChanged, Action<string> onError = null)
    {
        if (clip == null || surface == null) return;

        EnsurePlayer();
        bool sameClipPlaying = _player.isPlaying && _player.clip == clip && _surface == surface;
        Stop();
        if (sameClipPlaying) return;

        int width = Mathf.Max(1, Mathf.Min(MaxTargetWidth, (int)clip.width));
        int height = clip.width > 0
            ? Mathf.Max(1, Mathf.RoundToInt(width * (float)clip.height / clip.width))
            : Mathf.Max(1, (int)clip.height);
        _target = new RenderTexture(width, height, 0) { name = "ClueVideoTarget" };

        _surface = surface;
        _surface.texture = _target;
        _surface.color = Color.white;

        _player.clip = clip;
        _player.targetTexture = _target;
        _player.Play();

        _onPlayingChanged = onPlayingChanged;
        _onError = onError;
        onPlayingChanged?.Invoke(true);
    }

    public void Stop()
    {
        if (_player != null)
        {
            if (_player.isPlaying) _player.Stop();
            _player.targetTexture = null;
            _player.clip = null; // 클립 참조를 남기면 Addressables 해제 후에도 이 컴포넌트가 붙잡고 있게 된다
        }
        ReleaseTarget();

        var callback = _onPlayingChanged;
        _onPlayingChanged = null;
        _onError = null;
        callback?.Invoke(false);
    }

    // 창이 닫히면(부모가 비활성화되면) 재생을 멈추고 렌더 타깃도 놓는다 — 오디오 쪽과 달리
    // 유니티가 알아서 회수해주지 않는 자원이라 여기서 반드시 해제해야 한다.
    private void OnDisable() => Stop();

    private void OnDestroy()
    {
        _onPlayingChanged = null;
        ReleaseTarget();
    }

    private void ReleaseTarget()
    {
        if (_surface != null && _surface.texture == _target) _surface.texture = null;
        _surface = null;

        if (_target == null) return;
        _target.Release();
        // 에디터에서 플레이를 멈추는 순간에도 이 경로가 돌 수 있다 — 그때 Destroy를 쓰면
        // "Destroy may not be called from edit mode" 경고가 뜨므로 상황에 맞는 쪽을 고른다.
        if (Application.isPlaying) Destroy(_target); else DestroyImmediate(_target);
        _target = null;
    }

    private void EnsurePlayer()
    {
        if (_player != null) return;
        _player = gameObject.GetComponent<VideoPlayer>();
        if (_player == null) _player = gameObject.AddComponent<VideoPlayer>();
        _player.playOnAwake = false;
        _player.isLooping = false;
        _player.renderMode = VideoRenderMode.RenderTexture;
        // 소리는 ClueAttachmentAudioPlayer가 담당한다 — 영상까지 오디오를 물면 두 경로가 한 화면에서
        // 겹쳐 나므로, 여기서는 영상만 재생하고 트랙은 끈다(음성이 필요한 콘텐츠가 생기면 재검토).
        _player.audioOutputMode = VideoAudioOutputMode.None;
        _player.loopPointReached -= HandleLoopPoint;
        _player.loopPointReached += HandleLoopPoint;
        _player.errorReceived -= HandleError;
        _player.errorReceived += HandleError;
    }

    // 끝까지 재생되면 스스로 정지 상태로 되돌린다 — 버튼 라벨이 "■ 정지"로 굳는 것을 막는다
    // (ClueAttachmentAudioPlayer.WatchEnd와 같은 목적. 이쪽은 콜백이 있어 코루틴이 필요 없다).
    private void HandleLoopPoint(VideoPlayer source) => Stop();

    // Addressable에서 클립을 찾았더라도 플랫폼 코덱·손상 파일 때문에 실제 재생은 실패할 수 있다.
    // 이 경우에도 카드 전체를 닫지 않고 해당 행만 대체 문구로 바꾼다.
    private void HandleError(VideoPlayer source, string message)
    {
        var report = _onError;
        Stop();
        report?.Invoke(string.IsNullOrWhiteSpace(message) ? "알 수 없는 재생 오류" : message);
    }
}
