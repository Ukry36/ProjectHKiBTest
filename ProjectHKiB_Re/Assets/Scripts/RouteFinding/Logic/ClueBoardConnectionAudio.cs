using UnityEngine;

/// <summary>
/// 단서 보드에서 관계를 이었을 때의 효과음. 성공은 "얻었다"는 신호라 단서 획득음을 그대로 쓰고,
/// 실패(관계 없는 쌍)는 노이즈 계열을 쓰도록 설계했다.
///
/// [소리를 고르는 곳은 여기가 아니다] 어떤 클립을 쓸지는 ClueSystemSettings(= 맵 DB 편집기 "기타 설정" 탭)가
/// 정한다. 이 클래스는 "언제 울리는가"만 안다 — 콘텐츠 작업자가 코드를 고치지 않고 소리를 바꿀 수 있어야 해서다.
///
/// [울리지 않는 경우] 설정이 비어 있거나, AudioManager가 아직 없거나(씬 초기화 전), 에디터 검증처럼
/// 플레이 중이 아닌 경우엔 조용히 건너뛴다. 소리가 없다고 판정·기록이 달라지면 안 된다.
/// </summary>
public static class ClueBoardConnectionAudio
{
    /// <summary>관계를 올바르게 이었을 때. 이미 이어진 선을 다시 눌러 보는 재열람에는 부르지 않는다.</summary>
    public static void PlaySuccess() => Play(ClueSystemSettings.ConnectionSuccessAudio);

    /// <summary>관계가 정의되지 않은 쌍을 이으려 했을 때(Unrelated 거절).</summary>
    public static void PlayFailure() => Play(ClueSystemSettings.ConnectionFailureAudio);

    private static void Play(AudioDataSO audio)
    {
        if (audio == null || !Application.isPlaying) return;

        AudioManager audioManager = GameManager.instance != null ? GameManager.instance.audioManager : null;
        if (audioManager == null) return;

        // 보드는 화면 UI라 위치가 없다 — ClueAcquireNotice와 같은 2D 재생(Vector3.zero) 규칙을 따른다.
        audioManager.PlayAudioOneShot(audio, ClueSystemSettings.ConnectionAudioVolume, Vector3.zero);
    }
}
