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
    [SerializeField, Range(0f, 1f)] private float _levelThreeNoise = .35f;
    [SerializeField, Min(0f)] private float _levelThreeNoiseSeconds = 1.2f;

    private DreamErosionState _state;
    private RouteProgressState _subscribedProgress;

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
        // 복원은 게임플레이 변화가 아니므로 OnLevelChanged/화면 연출/강제추방을 절대 재발행하지 않는다.
        State.Import(saved);
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

    private void ApplyLevelChanged(string boardId, string firstId, string secondId)
    {
        switch (Level)
        {
            case 1: _levelOneAudio?.Play(); break;
            case 2: _levelTwoAudio?.Play(); break;
            case 3:
                _levelThreeAudio?.Play();
                ScreenEffectManager.Instance?.SetNoise(_levelThreeNoise, _levelThreeNoiseSeconds);
                // 월드가 구독해 실제 꿈 전환/배치를 책임진다. 여기서 씬을 추측해 이동하면 세이브·맵 계약을 깨뜨린다.
                OnRandomCluePairSpawnRequested?.Invoke();
                OnHallucinationScatterRequested?.Invoke();
                OnDreamExileRequested?.Invoke(boardId, firstId, secondId);
                break;
        }
        OnLevelChanged?.Invoke(Level);
    }
}
