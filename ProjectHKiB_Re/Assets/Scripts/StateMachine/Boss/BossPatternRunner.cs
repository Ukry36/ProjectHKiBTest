using System;
using StateMachine;
using UnityEngine;

/// <summary>
/// 보스 패턴 타임라인의 현재 실행 결과를 나타낸다.
/// Decision에서 실행 중·완료·취소 상태를 명확하게 구분할 때 사용한다.
/// </summary>
public enum BossPatternRunResult
{
    None,
    Running,
    Completed,
    Cancelled
}

/// <summary>
/// StateAction이 보스 패턴 실행 상태를 시작·취소·조회하기 위한 인터페이스다.
/// 실제 시간 진행은 StateController의 스케일 시간 틱을 사용한다.
/// </summary>
public interface IBossPatternRunner : IInitializable
{
    BossPatternSO CurrentPattern { get; }
    BossPatternRunResult Result { get; }
    bool IsRunning { get; }
    bool Play(BossPatternSO pattern);
    void Cancel();
    bool Matches(BossPatternSO pattern, BossPatternRunResult result);
}

/// <summary>
/// BossPatternSO의 상대 지연 타임라인을 StateController의 TimingUpdated로 실행한다.
/// DOTween과 코루틴을 사용하지 않아 일시정지와 State 수명 규칙을 StateMachine과 공유한다.
/// </summary>
public sealed class BossPatternRunner : InterfaceModule, IBossPatternRunner
{
    private const int MaxEntriesPerFrame = 1024;

    [Tooltip("이 패턴 실행기가 시간을 받고 StateAction을 실행할 소유 StateController.")]
    [NaughtyAttributes.ReadOnly, SerializeField]
    private StateController _owner;

    [Tooltip("현재 실행 중이거나 가장 최근 종료된 BossPatternSO.")]
    [NaughtyAttributes.ReadOnly, SerializeField]
    private BossPatternSO _currentPattern;

    [Tooltip("현재 패턴을 시작한 State. 이 State를 벗어나면 남은 타임라인을 취소한다.")]
    [NaughtyAttributes.ReadOnly, SerializeField]
    private StateSO _startingState;

    [Tooltip("현재 패턴의 다음 실행 대상 타임라인 인덱스.")]
    [NaughtyAttributes.ReadOnly, SerializeField]
    private int _entryIndex;

    [Tooltip("다음 타임라인 항목 또는 후딜레이에 누적된 스케일 시간.")]
    [NaughtyAttributes.ReadOnly, SerializeField]
    private float _elapsedTime;

    [Tooltip("현재 패턴의 실행 결과.")]
    [NaughtyAttributes.ReadOnly, SerializeField]
    private BossPatternRunResult _result;

    [Tooltip("StateController 이벤트 구독이 완료됐는지 나타낸다.")]
    private bool _initialized;

    [Tooltip("Action 실행 도중 패턴이 교체 또는 취소됐는지 구분하는 실행 버전.")]
    private int _runVersion;

    public BossPatternSO CurrentPattern => _currentPattern;
    public BossPatternRunResult Result => _result;
    public bool IsRunning => _result == BossPatternRunResult.Running;

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
    /// 지정한 패턴을 현재 State 소유의 새 타임라인으로 시작한다.
    /// 이전 패턴이 실행 중이었다면 남은 Action을 호출하지 않고 취소한다.
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
        _entryIndex = 0;
        _elapsedTime = 0f;
        _result = BossPatternRunResult.Running;
        _runVersion++;
        return true;
    }

    /// <summary>
    /// 현재 패턴의 남은 타임라인을 실행하지 않고 취소 결과로 종료한다.
    /// 이미 완료·취소됐거나 실행 전이면 기록을 변경하지 않는다.
    /// </summary>
    public void Cancel()
    {
        if (!IsRunning) return;

        _result = BossPatternRunResult.Cancelled;
        _runVersion++;
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
    /// StateController가 전달한 Time.deltaTime으로 타임라인과 후딜레이를 진행한다.
    /// 한 프레임의 초과 시간은 다음 항목으로 넘겨 낮은 프레임에서도 순서를 보존한다.
    /// </summary>
    private void UpdatePattern(float deltaTime)
    {
        if (!IsRunning || _currentPattern == null) return;

        _elapsedTime += Mathf.Max(0f, deltaTime);
        int runVersion = _runVersion;
        int processedCount = 0;
        BossPatternTimelineEntry[] timeline = _currentPattern.Timeline;
        int entryCount = timeline?.Length ?? 0;

        while (_entryIndex < entryCount && processedCount < MaxEntriesPerFrame)
        {
            BossPatternTimelineEntry entry = timeline[_entryIndex];
            float delay = entry != null ? entry.Delay : 0f;
            if (_elapsedTime < delay) return;

            _elapsedTime -= delay;
            _entryIndex++;
            processedCount++;
            ExecuteEntry(entry);

            if (!IsRunning || runVersion != _runVersion) return;
        }

        if (processedCount >= MaxEntriesPerFrame && _entryIndex < entryCount)
        {
            Debug.LogWarning(
                "[BossPatternRunner] 한 프레임에 타임라인 항목을 1024개 이상 처리하지 않도록 중단했습니다.",
                this);
            return;
        }

        if (_entryIndex >= entryCount && _elapsedTime >= _currentPattern.RecoveryDuration)
        {
            _elapsedTime -= _currentPattern.RecoveryDuration;
            _result = BossPatternRunResult.Completed;
            _runVersion++;
        }
    }

    /// <summary>
    /// 한 타임라인 항목에 들어 있는 Action들을 배열 순서대로 실행한다.
    /// 빈 항목과 빈 Action은 대기 또는 구분 용도로 허용한다.
    /// </summary>
    private void ExecuteEntry(BossPatternTimelineEntry entry)
    {
        StateAction[] actions = entry?.Actions;
        if (actions == null) return;

        for (int i = 0; i < actions.Length; i++)
        {
            actions[i]?.Act(_owner);
            if (!IsRunning) return;
        }
    }

    /// <summary>
    /// 패턴을 시작한 State가 다른 State로 바뀌면 남은 예약 Action을 취소한다.
    /// 이미 자연 완료된 패턴의 Completed 결과는 유지한다.
    /// </summary>
    private void HandleStateChanging(StateSO previousState, StateSO nextState)
    {
        if (IsRunning && previousState == _startingState)
            Cancel();
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
    /// 공유된 컨트롤러가 파괴된 실행기 참조를 계속 호출하지 않게 한다.
    /// </summary>
    private void OnDestroy()
    {
        if (!_initialized || _owner == null) return;

        _owner.TimingUpdated -= UpdatePattern;
        _owner.TimingStopped -= HandleTimingStopped;
        _owner.StateChanging -= HandleStateChanging;
        _initialized = false;
    }
}
