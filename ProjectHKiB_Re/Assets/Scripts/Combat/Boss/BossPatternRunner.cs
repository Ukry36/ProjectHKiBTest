using System.Collections.Generic;
using Gameplay;
using StateMachine;
using UnityEngine;

/// <summary>
/// 보스 패턴 타임라인의 현재 실행 결과를 나타낸다.
/// 특수행동 결과와 분리되어 패턴 자체의 실행·클리어 여부만 표현한다.
/// </summary>
public enum BossPatternRunResult
{
    None,
    Running,
    Completed,
    Cancelled,
    Failed
}

/// <summary>
/// 보스전 전체에서 패턴 진입 횟수와 정상 클리어 횟수 중 어떤 값을 읽을지 나타낸다.
/// 내부 타임라인 재시도는 Started에 포함하지 않고 AttemptCount로 별도 관리한다.
/// </summary>
public enum BossPatternCountType
{
    Started,
    Cleared
}

/// <summary>
/// StateAction과 Decision이 보스 패턴, 특수행동, 페이즈 실행 이력을 다루는 계약이다.
/// 모든 시간 진행은 StateController의 스케일 시간 틱을 사용한다.
/// </summary>
public interface IBossPatternRunner : IInitializable
{
    BossPatternSO CurrentPattern { get; }
    StateMachineSO CurrentPhase { get; }
    BossPatternRunResult Result { get; }
    bool IsRunning { get; }
    int AttemptCount { get; }
    void ResetBattleProgress();
    void BeginPhase(StateMachineSO phase);
    bool Play(BossPatternSO pattern);
    void Cancel();
    bool Complete(BossPatternSO pattern);
    bool Fail(BossPatternSO pattern);
    bool Restart(BossPatternSO pattern);
    bool ReportSpecialActionProgress(BossPatternSO pattern, string specialActionId, int amount);
    bool ObserveSpecialActionSignal(
        BossPatternSO pattern,
        string specialActionId,
        string signalKey,
        bool isActive,
        int amount);
    bool ResolveSpecialAction(
        BossPatternSO pattern,
        string specialActionId,
        BossSpecialActionResult result,
        string failureReasonId);
    int GetSpecialActionProgress(string specialActionId);
    BossSpecialActionResult GetSpecialActionResult(string specialActionId);
    string GetSpecialActionFailureReason(string specialActionId);
    int CountSpecialActionResults(BossSpecialActionResult result);
    BossPatternRunResult GetPhasePatternResult(StateMachineSO phase, BossPatternSO pattern);
    bool WasPatternCleared(BossPatternSO pattern);
    int GetPatternCount(BossPatternSO pattern, BossPatternCountType countType);
    bool Matches(BossPatternSO pattern, BossPatternRunResult result);
}

/// <summary>
/// BossPatternSO 타임라인을 실행하고 특수행동 결과를 보스전 전체에 누적한다.
/// 페이즈 재시작은 지역 패턴 이력만 지우며 전투 진행 기록은 명시적 Reset에서만 지운다.
/// </summary>
public sealed class BossPatternRunner : InterfaceModule, IBossPatternRunner,
    IGameplayEventReceiver, IGameplayEventReader
{
    private const int _maxStoredGameplayEvents = 32;

    /// <summary>
    /// 한 특수행동의 누적 횟수, 현재 결과와 실패 사유를 함께 보관한다.
    /// 실패 뒤 올바른 대응을 수행하면 성공으로 승격할 수 있지만 성공은 다시 실패하지 않는다.
    /// </summary>
    private sealed class SpecialActionRecord
    {
        public int Progress { get; set; }
        public BossSpecialActionResult Result { get; set; }
        public string FailureReasonId { get; set; } = string.Empty;
    }

    private const int _maxEntriesPerFrame = 1024;

    [Tooltip("이 패턴 실행기가 시간을 받고 StateAction을 실행할 소유 StateController.")]
    [NaughtyAttributes.ReadOnly, SerializeField]
    private StateController _owner;

    [Tooltip("현재 실행 중이거나 가장 최근 종료된 BossPatternSO.")]
    [NaughtyAttributes.ReadOnly, SerializeField]
    private BossPatternSO _currentPattern;

    [Tooltip("현재 실행 중인 Boss Phase StateMachineSO.")]
    [NaughtyAttributes.ReadOnly, SerializeField]
    private StateMachineSO _currentPhase;

    [Tooltip("현재 패턴을 시작한 State. 이 State를 벗어나면 남은 타임라인을 취소한다.")]
    [NaughtyAttributes.ReadOnly, SerializeField]
    private StateSO _startingState;

    [Tooltip("현재 패턴의 다음 실행 대상 타임라인 인덱스.")]
    [NaughtyAttributes.ReadOnly, SerializeField]
    private int _entryIndex;

    [Tooltip("다음 타임라인 항목 또는 반복 대기에 누적된 스케일 시간.")]
    [NaughtyAttributes.ReadOnly, SerializeField]
    private float _elapsedTime;

    [Tooltip("패턴 클리어 조건 충족 뒤 후딜레이에 누적한 스케일 시간.")]
    [NaughtyAttributes.ReadOnly, SerializeField]
    private float _recoveryElapsedTime;

    [Tooltip("현재 타임라인이 최소 한 사이클 끝났는지 나타낸다.")]
    [NaughtyAttributes.ReadOnly, SerializeField]
    private bool _timelineCompleted;

    [Tooltip("패턴 클리어 조건을 충족해 후딜레이를 진행 중인지 나타낸다.")]
    [NaughtyAttributes.ReadOnly, SerializeField]
    private bool _isRecovering;

    [Tooltip("현재 Play 요청의 타임라인 시도 횟수. 자동 반복 때 증가한다.")]
    [NaughtyAttributes.ReadOnly, SerializeField]
    private int _attemptCount;

    [Tooltip("현재 패턴 자체의 실행 결과.")]
    [NaughtyAttributes.ReadOnly, SerializeField]
    private BossPatternRunResult _result;

    [Tooltip("StateController 이벤트 구독이 완료됐는지 나타낸다.")]
    private bool _initialized;

    [Tooltip("Action 실행 중 패턴이 교체·재시작·취소됐는지 구분하는 실행 버전.")]
    private int _runVersion;

    [Tooltip("UpdateAction 조건이 여러 프레임 유지돼도 한 번만 횟수를 올리기 위한 신호 상태.")]
    private readonly Dictionary<string, bool> _specialActionSignalStates = new();

    [Tooltip("보스전 전체의 특수행동 ID별 누적 진행도와 성공·실패 기록.")]
    private readonly Dictionary<string, SpecialActionRecord> _specialActionRecords = new();

    [Tooltip("현재 페이즈 실행에서 PatternSO별 최근 결과. 페이즈 재시작 때만 초기화한다.")]
    private readonly Dictionary<BossPatternSO, BossPatternRunResult> _phasePatternResults = new();

    [Tooltip("보스전 시작 이후 PatternSO별 State 진입 실행 횟수. 내부 타임라인 재시도는 포함하지 않는다.")]
    private readonly Dictionary<BossPatternSO, int> _patternStartCounts = new();

    [Tooltip("보스전 시작 이후 PatternSO별 정상 클리어 횟수.")]
    private readonly Dictionary<BossPatternSO, int> _patternClearCounts = new();

    [Tooltip("현재 패턴 실행 중 전달된 최근 게임플레이 사건. 오래된 항목은 고정 개수에서 제거한다.")]
    private readonly List<GameplayEvent> _gameplayEvents = new();

    [Tooltip("공유 StateAction 대신 Runner가 보관하는 소비자 키별 마지막 사건 Sequence.")]
    private readonly Dictionary<string, long> _gameplayEventConsumerSequences = new();

    [Tooltip("직접 전달과 전역 알림에서 같은 사건이 두 번 저장되지 않게 하는 마지막 Sequence.")]
    private long _lastReceivedGameplayEventSequence;

    public BossPatternSO CurrentPattern => _currentPattern;
    public StateMachineSO CurrentPhase => _currentPhase;
    public BossPatternRunResult Result => _result;
    public bool IsRunning => _result == BossPatternRunResult.Running;
    public int AttemptCount => _attemptCount;

    /// <summary>
    /// StateController의 인터페이스 저장소에 보스 패턴 실행기를 등록한다.
    /// 실제 이벤트 구독은 첫 초기화 또는 첫 Play 요청 때 한 번만 수행한다.
    /// </summary>
    public override void Register(IInterfaceRegistable interfaceRegistable)
    {
        interfaceRegistable.RegisterInterface<IBossPatternRunner>(this);
        _owner = interfaceRegistable as StateController;
    }

    /// <summary>
    /// 소유 StateController를 확인하고 시간·State 변경·중단 이벤트를 구독한다.
    /// 여러 번 호출해도 중복 구독하지 않는다.
    /// </summary>
    public void Initialize()
    {
        if (_initialized) return;
        if (_owner == null) _owner = GetComponent<StateController>();

        if (_owner == null)
        {
            Debug.LogError($"{name}: BossPatternRunner는 같은 오브젝트의 StateController가 필요합니다.", this);
            return;
        }

        _owner.TimingUpdated += UpdatePattern;
        _owner.TimingStopped += HandleTimingStopped;
        _owner.StateChanging += HandleStateChanging;
        _initialized = true;
    }

    /// <summary>
    /// 보스와 직접 연결되지 않은 퍼즐·상호작용 사건도 관찰할 수 있게 전역 알림을 구독한다.
    /// 직접 참여 사건은 Sequence 중복 검사로 한 번만 저장된다.
    /// </summary>
    private void OnEnable()
    {
        GameplayEventDispatcher.Published -= ReceiveGameplayEvent;
        GameplayEventDispatcher.Published += ReceiveGameplayEvent;
    }

    /// <summary>
    /// 비활성 보스가 다른 전투의 사건을 계속 보관하지 않도록 전역 알림을 해제한다.
    /// 재활성화 시 OnEnable에서 안전하게 다시 연결된다.
    /// </summary>
    private void OnDisable()
    {
        GameplayEventDispatcher.Published -= ReceiveGameplayEvent;
    }

    /// <summary>
    /// 현재 패턴 실행 중 발생한 게임플레이 사건만 작은 런타임 목록에 보관한다.
    /// 보조 오브젝트의 Root Owner가 이 보스면 Dispatcher가 이 메서드를 호출한다.
    /// </summary>
    public void ReceiveGameplayEvent(GameplayEvent gameplayEvent)
    {
        if (!IsRunning || _isRecovering) return;
        if (gameplayEvent.Sequence <= _lastReceivedGameplayEventSequence) return;
        _lastReceivedGameplayEventSequence = gameplayEvent.Sequence;
        _gameplayEvents.Add(gameplayEvent);
        if (_gameplayEvents.Count > _maxStoredGameplayEvents)
            _gameplayEvents.RemoveAt(0);
    }

    /// <summary>
    /// 소비자 키가 아직 읽지 않은 다음 게임플레이 사건을 순서대로 반환한다.
    /// 키별 Sequence를 Runner가 소유하여 여러 보스와 공유 StateSO가 서로 간섭하지 않는다.
    /// </summary>
    public bool TryConsumeGameplayEvent(string consumerKey, out GameplayEvent gameplayEvent)
    {
        string key = string.IsNullOrWhiteSpace(consumerKey) ? "GameplayEvent" : consumerKey.Trim();
        _gameplayEventConsumerSequences.TryGetValue(key, out long lastSequence);

        for (int i = 0; i < _gameplayEvents.Count; i++)
        {
            if (_gameplayEvents[i].Sequence <= lastSequence) continue;
            gameplayEvent = _gameplayEvents[i];
            _gameplayEventConsumerSequences[key] = gameplayEvent.Sequence;
            return true;
        }

        gameplayEvent = default;
        return false;
    }

    /// <summary>
    /// 새 보스전 시작 시 모든 페이즈·패턴·특수행동 진행 기록을 초기화한다.
    /// 페이즈 전환이나 페이즈 재시작에서는 호출하지 않는다.
    /// </summary>
    public void ResetBattleProgress()
    {
        if (IsRunning) Cancel();
        _currentPattern = null;
        _currentPhase = null;
        _startingState = null;
        _result = BossPatternRunResult.None;
        _attemptCount = 0;
        _specialActionSignalStates.Clear();
        _specialActionRecords.Clear();
        _phasePatternResults.Clear();
        _patternStartCounts.Clear();
        _patternClearCounts.Clear();
        ResetGameplayEventState();
        ResetCycleState();
        _runVersion++;
    }

    /// <summary>
    /// 대상 페이즈를 현재 페이즈로 정하고 그 페이즈의 지역 패턴 실행 이력만 초기화한다.
    /// 특수행동 결과와 과거 패턴 클리어 이력은 다음 페이즈 조건을 위해 그대로 유지한다.
    /// </summary>
    public void BeginPhase(StateMachineSO phase)
    {
        if (phase == null)
        {
            Debug.LogError("[BossPatternRunner] 시작할 Boss Phase가 비어 있습니다.", this);
            return;
        }

        if (IsRunning) Cancel();
        _currentPhase = phase;
        _currentPattern = null;
        _startingState = null;
        _result = BossPatternRunResult.None;
        _attemptCount = 0;
        _specialActionSignalStates.Clear();
        ResetGameplayEventState();
        _phasePatternResults.Clear();
        ResetCycleState();
        _runVersion++;
    }

    /// <summary>
    /// 지정한 패턴을 현재 State 소유의 새 타임라인으로 시작한다.
    /// 보스전 전체의 특수행동 진행도는 유지하고 상승 에지 관찰 상태만 새로 만든다.
    /// </summary>
    public bool Play(BossPatternSO pattern)
    {
        Initialize();
        if (!_initialized || pattern == null)
        {
            Debug.LogError($"{name}: 실행할 BossPatternSO가 비어 있습니다.", this);
            return false;
        }

        if (IsRunning) Cancel();
        _currentPattern = pattern;
        _startingState = _owner.CurrentState;
        _specialActionSignalStates.Clear();
        ResetGameplayEventState();
        _attemptCount = 1;
        ResetCycleState();
        _result = BossPatternRunResult.Running;
        IncrementPatternCount(_patternStartCounts, pattern);
        if (_currentPhase != null) _phasePatternResults[pattern] = _result;
        _runVersion++;
        TryBeginRecovery(0f);
        return true;
    }

    /// <summary>
    /// 현재 패턴의 남은 타임라인을 실행하지 않고 취소 결과로 종료한다.
    /// 이미 종료됐거나 실행 전이면 기록을 변경하지 않는다.
    /// </summary>
    public void Cancel()
    {
        if (!IsRunning) return;
        _result = BossPatternRunResult.Cancelled;
        RecordPhasePatternResult(_result);
        _isRecovering = false;
        _runVersion++;
    }

    /// <summary>
    /// 설정된 클리어 조건과 관계없이 지정 패턴의 정상 완료 후딜레이를 시작한다.
    /// 컷신이나 외부 퍼즐 시스템이 패턴 클리어를 확정할 때 사용한다.
    /// </summary>
    public bool Complete(BossPatternSO pattern)
    {
        if (!MatchesRunningPattern(pattern)) return false;
        MarkUnresolvedSpecialActionsAsIgnored();
        BeginRecovery(0f);
        return true;
    }

    /// <summary>
    /// 지정 패턴을 클리어가 아닌 Failed 실행 결과로 종료한다.
    /// 특수행동 실패와 패턴 실패를 혼동하지 않고 별도 연출 흐름이 필요할 때만 사용한다.
    /// </summary>
    public bool Fail(BossPatternSO pattern)
    {
        if (!MatchesRunningPattern(pattern)) return false;
        MarkUnresolvedSpecialActionsAsIgnored();
        _result = BossPatternRunResult.Failed;
        RecordPhasePatternResult(_result);
        _isRecovering = false;
        _runVersion++;
        return true;
    }

    /// <summary>
    /// 현재 패턴의 타임라인만 처음부터 재시작한다.
    /// 특수행동 진행도와 결과는 보존하여 정답 행동을 할 때까지 반복할 수 있게 한다.
    /// </summary>
    public bool Restart(BossPatternSO pattern)
    {
        if (!MatchesRunningPattern(pattern)) return false;
        _specialActionSignalStates.Clear();
        ResetGameplayEventState();
        ResetCycleState();
        _attemptCount++;
        _runVersion++;
        return true;
    }

    /// <summary>
    /// 현재 패턴에 정의된 특수행동 횟수를 누적하고 필요 횟수 도달 시 성공으로 확정한다.
    /// 실패 기록이 있더라도 이후 올바른 대응으로 필요 횟수를 채우면 성공으로 승격한다.
    /// </summary>
    public bool ReportSpecialActionProgress(
        BossPatternSO pattern,
        string specialActionId,
        int amount)
    {
        if (!MatchesRunningPattern(pattern) || _isRecovering) return false;

        string normalizedId = BossSpecialActionDefinition.NormalizeId(specialActionId);
        if (!_currentPattern.TryGetSpecialAction(normalizedId, out BossSpecialActionDefinition definition))
        {
            Debug.LogWarning(
                $"[BossPatternRunner] '{_currentPattern.name}'에 특수행동 '{normalizedId}'가 정의되지 않았습니다.",
                this);
            return false;
        }

        SpecialActionRecord record = GetOrCreateSpecialActionRecord(normalizedId);
        if (record.Result == BossSpecialActionResult.Success) return false;
        record.Progress += Mathf.Max(1, amount);
        if (record.Progress >= definition.RequiredCount)
            SetSpecialActionResult(record, BossSpecialActionResult.Success, string.Empty);

        TryBeginRecovery(0f);
        return true;
    }

    /// <summary>
    /// 매 프레임 평가되는 조건의 false→true 순간에만 특수행동 횟수를 한 번 증가시킨다.
    /// 같은 충돌이나 위치 조건이 유지되는 동안 중복 집계되는 문제를 막는다.
    /// </summary>
    public bool ObserveSpecialActionSignal(
        BossPatternSO pattern,
        string specialActionId,
        string signalKey,
        bool isActive,
        int amount)
    {
        if (!MatchesRunningPattern(pattern) || _isRecovering) return false;

        string normalizedId = BossSpecialActionDefinition.NormalizeId(specialActionId);
        string normalizedSignalKey = string.IsNullOrWhiteSpace(signalKey)
            ? normalizedId
            : signalKey.Trim();
        string runtimeKey = $"{normalizedId}:{normalizedSignalKey}";
        bool wasActive = _specialActionSignalStates.TryGetValue(runtimeKey, out bool previous) && previous;
        _specialActionSignalStates[runtimeKey] = isActive;
        return isActive && !wasActive &&
               ReportSpecialActionProgress(pattern, normalizedId, Mathf.Max(1, amount));
    }

    /// <summary>
    /// 현재 패턴에 정의된 특수행동을 명시적으로 성공 또는 실패로 기록한다.
    /// 성공은 최종이며 실패는 재시도 중 이후 성공으로 덮어쓸 수 있다.
    /// </summary>
    public bool ResolveSpecialAction(
        BossPatternSO pattern,
        string specialActionId,
        BossSpecialActionResult result,
        string failureReasonId)
    {
        BossPatternSO targetPattern = pattern != null ? pattern : _currentPattern;
        if (targetPattern == null || result == BossSpecialActionResult.Unresolved) return false;

        string normalizedId = BossSpecialActionDefinition.NormalizeId(specialActionId);
        if (!targetPattern.TryGetSpecialAction(normalizedId, out BossSpecialActionDefinition definition))
        {
            Debug.LogWarning(
                $"[BossPatternRunner] '{targetPattern.name}'에 특수행동 '{normalizedId}'가 정의되지 않았습니다.",
                this);
            return false;
        }

        SpecialActionRecord record = GetOrCreateSpecialActionRecord(normalizedId);
        if (record.Result == BossSpecialActionResult.Success) return false;
        if (result == BossSpecialActionResult.Success)
            record.Progress = Mathf.Max(record.Progress, definition.RequiredCount);
        SetSpecialActionResult(record, result, failureReasonId);
        TryBeginRecovery(0f);
        return true;
    }

    /// <summary>
    /// 보스전 전체에서 지정 특수행동의 현재 누적 횟수를 반환한다.
    /// 아직 보고되지 않은 ID는 0을 반환한다.
    /// </summary>
    public int GetSpecialActionProgress(string specialActionId)
    {
        string normalizedId = BossSpecialActionDefinition.NormalizeId(specialActionId);
        return _specialActionRecords.TryGetValue(normalizedId, out SpecialActionRecord record)
            ? record.Progress
            : 0;
    }

    /// <summary>
    /// 보스전 전체에서 지정 특수행동의 현재 성공·실패 결과를 반환한다.
    /// 아직 기록되지 않은 ID는 Unresolved로 처리한다.
    /// </summary>
    public BossSpecialActionResult GetSpecialActionResult(string specialActionId)
    {
        string normalizedId = BossSpecialActionDefinition.NormalizeId(specialActionId);
        return _specialActionRecords.TryGetValue(normalizedId, out SpecialActionRecord record)
            ? record.Result
            : BossSpecialActionResult.Unresolved;
    }

    /// <summary>
    /// 실패한 특수행동에 기록된 최신 이유 ID를 반환한다.
    /// 대상이 성공·미결정이면 빈 문자열을 반환한다.
    /// </summary>
    public string GetSpecialActionFailureReason(string specialActionId)
    {
        string normalizedId = BossSpecialActionDefinition.NormalizeId(specialActionId);
        return _specialActionRecords.TryGetValue(normalizedId, out SpecialActionRecord record) &&
               record.Result == BossSpecialActionResult.Failure
            ? record.FailureReasonId
            : string.Empty;
    }

    /// <summary>
    /// 보스전 전체에서 선택한 성공·실패 결과의 개수를 센다.
    /// 성공 개수 기반 페이즈 분기를 만들 때 개별 ID Decision 수를 줄여 준다.
    /// </summary>
    public int CountSpecialActionResults(BossSpecialActionResult result)
    {
        int count = 0;
        foreach (SpecialActionRecord record in _specialActionRecords.Values)
            if (record.Result == result) count++;
        return count;
    }

    /// <summary>
    /// 현재 페이즈 실행에서 한 PatternSO의 최근 실행 결과를 반환한다.
    /// 페이즈가 다르거나 아직 실행되지 않은 패턴은 None을 반환한다.
    /// </summary>
    public BossPatternRunResult GetPhasePatternResult(
        StateMachineSO phase,
        BossPatternSO pattern)
    {
        if (_currentPhase == null || pattern == null ||
            (phase != null && phase != _currentPhase))
            return BossPatternRunResult.None;

        return _phasePatternResults.TryGetValue(pattern, out BossPatternRunResult result)
            ? result
            : BossPatternRunResult.None;
    }

    /// <summary>
    /// 보스전 시작 이후 지정 패턴이 한 번이라도 정상 클리어됐는지 반환한다.
    /// 페이즈가 바뀌거나 재시작돼도 이 기록은 유지된다.
    /// </summary>
    public bool WasPatternCleared(BossPatternSO pattern)
    {
        return pattern != null &&
               _patternClearCounts.TryGetValue(pattern, out int count) &&
               count > 0;
    }

    /// <summary>
    /// 보스전 전체에서 선택한 패턴의 State 진입 또는 정상 클리어 횟수를 반환한다.
    /// 기록이 없거나 PatternSO가 비어 있으면 0을 반환한다.
    /// </summary>
    public int GetPatternCount(BossPatternSO pattern, BossPatternCountType countType)
    {
        if (pattern == null) return 0;
        Dictionary<BossPatternSO, int> counts = countType == BossPatternCountType.Cleared
            ? _patternClearCounts
            : _patternStartCounts;
        return counts.TryGetValue(pattern, out int count) ? count : 0;
    }

    /// <summary>
    /// 현재 또는 가장 최근 패턴과 실행 결과가 요청값에 일치하는지 검사한다.
    /// pattern이 비어 있으면 패턴 종류를 무시하고 결과만 비교한다.
    /// </summary>
    public bool Matches(BossPatternSO pattern, BossPatternRunResult result)
    {
        return _result == result && (pattern == null || _currentPattern == pattern);
    }

    /// <summary>
    /// StateController 스케일 시간으로 타임라인, 반복 대기와 후딜레이를 진행한다.
    /// 한 프레임의 초과 시간은 다음 항목으로 넘겨 낮은 프레임에서도 순서를 보존한다.
    /// </summary>
    private void UpdatePattern(float deltaTime)
    {
        if (!IsRunning || _currentPattern == null) return;

        float safeDeltaTime = Mathf.Max(0f, deltaTime);
        if (_isRecovering)
        {
            UpdateRecovery(safeDeltaTime);
            return;
        }

        _elapsedTime += safeDeltaTime;
        int runVersion = _runVersion;
        int processedCount = 0;
        BossPatternTimelineEntry[] timeline = _currentPattern.Timeline;
        int entryCount = timeline?.Length ?? 0;

        while (_entryIndex < entryCount && processedCount < _maxEntriesPerFrame)
        {
            BossPatternTimelineEntry entry = timeline[_entryIndex];
            float delay = entry != null ? entry.Delay : 0f;
            if (_elapsedTime < delay) return;

            _elapsedTime -= delay;
            _entryIndex++;
            processedCount++;
            ExecuteEntry(entry);
            if (!IsRunning || runVersion != _runVersion || _isRecovering) return;
        }

        if (processedCount >= _maxEntriesPerFrame && _entryIndex < entryCount)
        {
            Debug.LogWarning(
                "[BossPatternRunner] 한 프레임에 타임라인 항목을 1024개 이상 처리하지 않도록 중단했습니다.",
                this);
            return;
        }

        if (_entryIndex < entryCount) return;
        _timelineCompleted = true;
        MarkUnresolvedSpecialActionsAsIgnored();
        if (TryBeginRecovery(_elapsedTime)) return;
        if (!_currentPattern.LoopTimelineUntilCleared) return;
        if (_elapsedTime < _currentPattern.TimelineLoopDelay) return;

        _elapsedTime -= _currentPattern.TimelineLoopDelay;
        _entryIndex = _currentPattern.TimelineLoopStartIndex;
        _timelineCompleted = false;
        _specialActionSignalStates.Clear();
        _attemptCount++;
    }

    /// <summary>
    /// 한 타임라인 항목에 들어 있는 Action들을 배열 순서대로 실행한다.
    /// Action이 패턴을 완료·실패·재시작하면 같은 항목의 남은 Action을 실행하지 않는다.
    /// </summary>
    private void ExecuteEntry(BossPatternTimelineEntry entry)
    {
        StateAction[] actions = entry?.Actions;
        if (actions == null) return;

        int runVersion = _runVersion;
        for (int i = 0; i < actions.Length; i++)
        {
            actions[i]?.Act(_owner);
            if (!IsRunning || runVersion != _runVersion || _isRecovering) return;
        }
    }

    /// <summary>
    /// PatternSO의 타임라인·필수 특수행동 조건이 맞으면 완료 후딜레이를 시작한다.
    /// 타임라인에서 남은 초과 시간은 후딜레이에 넘겨 프레임 의존 오차를 줄인다.
    /// </summary>
    private bool TryBeginRecovery(float carryTime)
    {
        if (!IsRunning || _isRecovering || _currentPattern == null) return false;

        bool requiredActionsCompleted = AreRequiredSpecialActionsCompleted();
        bool canComplete = _currentPattern.CompletionRule switch
        {
            BossPatternCompletionRule.RequiredSpecialActions => requiredActionsCompleted,
            BossPatternCompletionRule.TimelineAndRequiredSpecialActions =>
                _timelineCompleted && requiredActionsCompleted,
            _ => _timelineCompleted
        };

        if (!canComplete) return false;
        BeginRecovery(carryTime);
        return true;
    }

    /// <summary>
    /// 패턴 클리어 후딜레이를 시작하고 0초 설정이면 즉시 Completed로 마친다.
    /// 진행 중이던 타임라인은 더 실행하지 않는다.
    /// </summary>
    private void BeginRecovery(float carryTime)
    {
        if (!IsRunning || _isRecovering) return;
        MarkUnresolvedSpecialActionsAsIgnored();
        _isRecovering = true;
        _recoveryElapsedTime = Mathf.Max(0f, carryTime);
        _elapsedTime = 0f;
        UpdateRecovery(0f);
    }

    /// <summary>
    /// 완료 후딜레이를 누적하고 시간이 차면 패턴 클리어를 지역·전역 이력에 기록한다.
    /// 실행 버전을 올려 같은 프레임의 남은 타임라인 Action을 차단한다.
    /// </summary>
    private void UpdateRecovery(float deltaTime)
    {
        if (!_isRecovering || !IsRunning) return;
        _recoveryElapsedTime += Mathf.Max(0f, deltaTime);
        if (_recoveryElapsedTime < _currentPattern.RecoveryDuration) return;

        _result = BossPatternRunResult.Completed;
        RecordPhasePatternResult(_result);
        IncrementPatternCount(_patternClearCounts, _currentPattern);
        _isRecovering = false;
        _runVersion++;
    }

    /// <summary>
    /// 패턴 클리어에 필요로 표시된 모든 특수행동이 성공했는지 검사한다.
    /// 필수 특수행동 완료 규칙인데 해당 정의가 없으면 자동 완료하지 않는다.
    /// </summary>
    private bool AreRequiredSpecialActionsCompleted()
    {
        BossSpecialActionDefinition[] definitions = _currentPattern?.SpecialActions;
        if (definitions == null || definitions.Length == 0) return false;

        bool foundRequired = false;
        for (int i = 0; i < definitions.Length; i++)
        {
            BossSpecialActionDefinition definition = definitions[i];
            if (definition == null || !definition.RequiredForPatternClear) continue;
            foundRequired = true;
            if (GetSpecialActionResult(definition.Id) != BossSpecialActionResult.Success)
                return false;
        }

        return foundRequired;
    }

    /// <summary>
    /// 현재 패턴의 미결정 특수행동을 각 정의의 Ignored 실패로 기록한다.
    /// 명시적 실패나 성공은 유지하며 반복 패턴에서는 이후 성공으로 승격될 수 있다.
    /// </summary>
    private void MarkUnresolvedSpecialActionsAsIgnored()
    {
        BossSpecialActionDefinition[] definitions = _currentPattern?.SpecialActions;
        if (definitions == null) return;

        for (int i = 0; i < definitions.Length; i++)
        {
            BossSpecialActionDefinition definition = definitions[i];
            if (definition == null ||
                GetSpecialActionResult(definition.Id) != BossSpecialActionResult.Unresolved)
                continue;

            SpecialActionRecord record = GetOrCreateSpecialActionRecord(definition.Id);
            SetSpecialActionResult(
                record,
                BossSpecialActionResult.Failure,
                definition.UnansweredFailureReasonId);
        }
    }

    /// <summary>
    /// ID에 대응하는 특수행동 런타임 기록을 가져오고 없으면 기본 기록을 만든다.
    /// Dictionary 생성 규칙을 한곳에 모아 모든 Action과 자동 실패가 같은 값을 공유하게 한다.
    /// </summary>
    private SpecialActionRecord GetOrCreateSpecialActionRecord(string specialActionId)
    {
        string normalizedId = BossSpecialActionDefinition.NormalizeId(specialActionId);
        if (_specialActionRecords.TryGetValue(normalizedId, out SpecialActionRecord record))
            return record;

        record = new SpecialActionRecord();
        _specialActionRecords.Add(normalizedId, record);
        return record;
    }

    /// <summary>
    /// 패턴 횟수 Dictionary의 기존 값을 한 번 증가시키고 첫 기록은 1로 만든다.
    /// Play와 정상 완료 지점이 같은 규칙을 공유하도록 한곳에서 처리한다.
    /// </summary>
    private static void IncrementPatternCount(
        Dictionary<BossPatternSO, int> counts,
        BossPatternSO pattern)
    {
        if (counts == null || pattern == null) return;
        counts.TryGetValue(pattern, out int count);
        counts[pattern] = count + 1;
    }

    /// <summary>
    /// 특수행동 기록에 성공·실패와 정규화된 이유를 일관되게 반영한다.
    /// 성공 결과에는 과거 실패 이유를 남기지 않는다.
    /// </summary>
    private static void SetSpecialActionResult(
        SpecialActionRecord record,
        BossSpecialActionResult result,
        string failureReasonId)
    {
        record.Result = result;
        record.FailureReasonId = result == BossSpecialActionResult.Failure
            ? BossPatternSO.NormalizeFailureReasonId(failureReasonId)
            : string.Empty;
    }

    /// <summary>
    /// 현재 패턴과 요청 패턴이 일치하고 아직 Running 상태인지 공통 검사한다.
    /// pattern을 비우면 현재 실행 중인 패턴을 대상으로 허용한다.
    /// </summary>
    private bool MatchesRunningPattern(BossPatternSO pattern)
    {
        return IsRunning && _currentPattern != null &&
               (pattern == null || pattern == _currentPattern);
    }

    /// <summary>
    /// 한 타임라인 사이클과 완료 후딜레이의 런타임 값을 초기 상태로 되돌린다.
    /// PatternSO, 특수행동과 페이즈 이력은 호출 목적에 맞게 외부에서 관리한다.
    /// </summary>
    private void ResetCycleState()
    {
        _entryIndex = 0;
        _elapsedTime = 0f;
        _recoveryElapsedTime = 0f;
        _timelineCompleted = false;
        _isRecovering = false;
    }

    /// <summary>
    /// 이전 패턴이나 반복 시도에서 남은 사건과 소비 위치를 함께 제거한다.
    /// 새 시도는 시작 이후 실제로 발생한 사건만 SpecialAction으로 집계한다.
    /// </summary>
    private void ResetGameplayEventState()
    {
        _gameplayEvents.Clear();
        _gameplayEventConsumerSequences.Clear();
        _lastReceivedGameplayEventSequence = 0;
    }

    /// <summary>
    /// 현재 PatternSO의 실행 결과를 현재 페이즈 범위 이력에 기록한다.
    /// 페이즈가 시작되지 않았거나 Pattern이 비어 있으면 이력을 만들지 않는다.
    /// </summary>
    private void RecordPhasePatternResult(BossPatternRunResult result)
    {
        if (_currentPhase != null && _currentPattern != null)
            _phasePatternResults[_currentPattern] = result;
    }

    /// <summary>
    /// 패턴을 시작한 State가 다른 State로 바뀌면 남은 예약 Action을 취소한다.
    /// 이미 자연 완료·실패된 패턴의 결과는 다음 전이에서 읽을 수 있도록 유지한다.
    /// </summary>
    private void HandleStateChanging(StateSO previousState, StateSO nextState)
    {
        if (IsRunning && previousState == _startingState) Cancel();
    }

    /// <summary>
    /// StateController가 비활성화되어 시간 틱을 중단하면 현재 패턴도 취소한다.
    /// 재활성화 뒤 이전 타임라인이 뒤늦게 이어지지 않게 한다.
    /// </summary>
    private void HandleTimingStopped()
    {
        Cancel();
    }

    /// <summary>
    /// 컴포넌트가 제거될 때 StateController에 등록한 모든 이벤트를 해제한다.
    /// 공유 컨트롤러가 파괴된 실행기 참조를 계속 호출하지 않게 한다.
    /// </summary>
    private void OnDestroy()
    {
        GameplayEventDispatcher.Published -= ReceiveGameplayEvent;
        if (!_initialized || _owner == null) return;
        _owner.TimingUpdated -= UpdatePattern;
        _owner.TimingStopped -= HandleTimingStopped;
        _owner.StateChanging -= HandleStateChanging;
        _initialized = false;
    }
}
