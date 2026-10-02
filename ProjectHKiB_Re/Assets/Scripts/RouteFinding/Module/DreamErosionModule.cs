using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// C07 꿈 잠식의 런타임 경계입니다. 보드가 Unrelated 거절/새 연결만 이 API로 전달하고,
/// 월드 배치와 강제 꿈 전환은 아직 명세가 없어 이벤트로만 외부에 요청합니다.
/// </summary>
public sealed class DreamErosionModule : MonoBehaviour
{
    private static DreamErosionModule _instance;
    private static bool _isQuitting;

    public static DreamErosionModule Instance
    {
        get
        {
            if (_instance == null && Application.isPlaying && !_isQuitting)
            {
                _instance = FindObjectOfType<DreamErosionModule>();
                if (_instance == null)
                    _instance = new GameObject(nameof(DreamErosionModule)).AddComponent<DreamErosionModule>();
            }
            return _instance;
        }
    }

    [SerializeField, Min(1)] private int _failuresPerLevel = 1;
    [SerializeField] private EffectAudioCue _levelOneAudio = new();
    [SerializeField] private EffectAudioCue _levelTwoAudio = new();
    [SerializeField] private EffectAudioCue _levelThreeAudio = new();

    private DreamErosionState _state;
    private RouteProgressState _subscribedProgress;
    private DreamErosionPresentation _presentation;

    public int Level => State.Level;
    public event Action<int> OnLevelChanged;
    public event Action<string, string, string> OnDreamExileRequested;
    public event Action OnRandomCluePairSpawnRequested;
    public event Action OnHallucinationScatterRequested;

    private DreamErosionState State => _state ??= new DreamErosionState(_failuresPerLevel);

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update() => TrySubscribe();
    private void OnApplicationQuit() => _isQuitting = true;

    private void OnDestroy()
    {
        _presentation?.Clear();
        if (_subscribedProgress != null) _subscribedProgress.OnClueAcquired -= HandleClueAcquired;
        _subscribedProgress = null;
        if (_instance == this) _instance = null;
    }

    public void RegisterConnectionFailure(string boardId, string firstNodeId, string secondNodeId)
    {
        int before = Level;
        State.RegisterConnectionFailure();
        if (before != Level) ApplyLevelChanged(boardId, firstNodeId, secondNodeId);
    }

    public void RegisterConnectionSuccess(string boardId, string relationId)
    {
        if (State.RegisterConnectionSuccess()) ApplyLevelChanged(boardId, relationId, null);
    }

    public void ForceLevel(int level)
    {
        if (State.ForceLevel(level)) ApplyLevelChanged(null, null, null);
    }

    /// <summary>월드가 실제 환각 단서를 획득 처리한 뒤 그 영속 ID를 기록하는 훅입니다.</summary>
    public void RegisterHallucinationClueAcquired(string clueId) => State.AddHallucinationClue(clueId);

    public IEnumerable<string> GetHallucinationNodeIds(ClueBoardDefinition board)
    {
        if (board?.slots == null) yield break;
        foreach (ClueBoardSlot slot in board.slots)
        {
            if (slot != null && State.HasHallucinationClue(slot.clueId))
                yield return slot.nodeId;
        }
    }

    public DreamErosionSaveData Export() => State.Export();

    public void Import(DreamErosionSaveData saved)
    {
        // 복원은 게임플레이 변화가 아니므로 OnLevelChanged/보상/강제추방은 절대 재발행하지 않는다.
        State.Import(saved);

        // 다만 잠식 단계는 **지속 상태**라 화면까지 0단계로 두면 안 된다 — 2단계에서 저장한 세이브를
        // 불러왔는데 화면이 멀쩡하면 플레이어는 잠식이 풀린 줄 안다. 단계 상승 순간의 충격 연출
        // (효과음·불협화음·슬로우모션)은 빼고 지속분만 현재 단계에 맞춘다.
        EnsurePresentation().RestoreLevel(Level);
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
        // 로드 복원은 이 이벤트를 내지 않으므로, 실제 새 단서만 잠식을 해제한다.
        if (State.RegisterClueAcquired()) ApplyLevelChanged(null, clue != null ? clue.id : null, null);
    }

    // 연출 컴포넌트는 모듈과 같은 오브젝트에 붙어 DontDestroyOnLoad를 함께 탄다(씬 전환에도 살아남는다).
    private DreamErosionPresentation EnsurePresentation() =>
        _presentation ??= GetComponent<DreamErosionPresentation>() ?? gameObject.AddComponent<DreamErosionPresentation>();

    private void ApplyLevelChanged(string boardId, string firstId, string secondId)
    {
        EnsurePresentation().ApplyLevel(Level, LevelAudioCue(Level));

        // 1~2단계는 화면·소리 연출이 전부다(위에서 이미 걸었다). 월드가 움직이는 것은 3단계뿐.
        if (Level == 3)
        {
            // 월드가 구독해 실제 꿈 전환/배치를 책임진다. 여기서 씬을 추측해 이동하면 세이브·맵 계약을 깨뜨린다.
            OnRandomCluePairSpawnRequested?.Invoke();
            OnHallucinationScatterRequested?.Invoke();
            OnDreamExileRequested?.Invoke(boardId, firstId, secondId);
        }
        OnLevelChanged?.Invoke(Level);
    }

    private EffectAudioCue LevelAudioCue(int level)
    {
        return level switch
        {
            1 => _levelOneAudio,
            2 => _levelTwoAudio,
            3 => _levelThreeAudio,
            _ => null,
        };
    }
}
