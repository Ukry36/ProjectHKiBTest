using System.Collections.Generic;
using UnityEngine;
using System;
using StateMachine;

/// <summary>
/// 서브 StateMachine이 부모로 돌아올 때 남기는 종료 결과다.
/// 부모 State의 Decision이 정상 완료와 취소를 구분할 때 사용한다.
/// </summary>
public enum SubStateMachineResult
{
    None,
    Completed,
    Cancelled
}

public class StateController : InterfaceRegister
{
    #region Data
    /// <summary>
    /// 실행 중인 서브 StateMachine이 돌아갈 부모 실행 문맥을 보관한다.
    /// 부모 State는 재개하지 않고 종료한 뒤 지정된 Return State로 새로 진입한다.
    /// </summary>
    private sealed class StateMachineFrame
    {
        [Tooltip("서브 실행이 끝나면 복원할 부모 StateMachine.")]
        private readonly StateMachineSO _parentStateMachine;

        [Tooltip("부모 StateMachine 복원 뒤 새로 진입할 State.")]
        private readonly StateSO _returnState;

        [Tooltip("부모 StateMachine에서 사용하던 커스텀 변수 저장소.")]
        private readonly CustomVariableSets _parentVariables;

        public StateMachineSO ParentStateMachine => _parentStateMachine;
        public StateSO ReturnState => _returnState;
        public CustomVariableSets ParentVariables => _parentVariables;

        /// <summary>
        /// 부모 StateMachine과 반환 State, 변수 저장소를 하나의 불변 문맥으로 묶는다.
        /// 서브 완료 시 이 값을 사용하여 부모 실행 환경을 복원한다.
        /// </summary>
        public StateMachineFrame(
            StateMachineSO parentStateMachine,
            StateSO returnState,
            CustomVariableSets parentVariables)
        {
            _parentStateMachine = parentStateMachine;
            _returnState = returnState;
            _parentVariables = parentVariables;
        }
    }

    /// <summary>
    /// State Action 도중 StateMachine을 즉시 교체하지 않기 위한 예약 종류다.
    /// 예약은 다음 Update 시작 시 안전한 지점에서 한 번만 처리된다.
    /// </summary>
    private enum StateMachineHierarchyOperation
    {
        None,
        Start,
        Finish
    }

    /// <summary>
    /// 현재 State의 ExitState와 다음 State의 EnterState 사이로 넘어가기 전에 호출된다.
    /// 장시간 실행되는 Action이 자신이 시작된 State의 수명에 맞춰 정리할 때 사용한다.
    /// </summary>
    public event Action<StateSO, StateSO> StateChanging;

    /// <summary>
    /// StateMachine 내부의 Update 기반 예약 작업을 진행시키는 틱이다.
    /// StateAction처럼 MonoBehaviour가 아닌 실행 객체가 스케일 시간을 받을 때 사용한다.
    /// </summary>
    public event Action<float> TimingUpdated;

    /// <summary>
    /// 컨트롤러가 비활성화될 때 진행 중인 Update 기반 예약 작업에 정리를 알린다.
    /// 재활성화 뒤 이전 State의 예약이 이어지는 것을 방지한다.
    /// </summary>
    public event Action TimingStopped;

    [HideInInspector] public CustomVariableSets customVariables = new();
    [NaughtyAttributes.ReadOnly][SerializeField] protected StateSO _currentState;
    [NaughtyAttributes.ReadOnly][SerializeField] protected StateMachineSO _stateMachine;
    public StateSO CurrentState
    {
        get => _currentState;
        protected set
        {
            if (value != _currentState)
                _currentState = value;
            //Debug.Log(_currentState);
        }
    }
    public StateMachineSO StateMachine => _stateMachine;
    [HideInInspector] public List<bool> TransitionConditions = new(10);
    [HideInInspector] public List<Timer> Timers = new(10);

    [Tooltip("각 State 전이가 시작된 뒤 흐른 스케일 시간을 보관한다.")]
    private readonly List<float> _transitionElapsedTimes = new(10);

    [Tooltip("각 State 전이의 availableTime 처리가 끝났는지 보관한다.")]
    private readonly List<bool> _transitionAvailableTriggered = new(10);

    [Tooltip("각 State 전이의 disableTime 처리가 끝났는지 보관한다.")]
    private readonly List<bool> _transitionDisableTriggered = new(10);

    [Tooltip("현재 Update 기반 ActionSequence의 원본 항목을 보관한다.")]
    private ActionSequence[] _currentActionSequence;

    [Tooltip("현재 ActionSequence에서 다음에 실행할 항목의 인덱스다.")]
    private int _sequenceInt;

    [Tooltip("현재 ActionSequence 항목을 위해 누적한 스케일 시간이다.")]
    private float _actionSequenceElapsedTime;

    [Tooltip("현재 ActionSequence가 마지막 항목 뒤에 다시 반복되는지 나타낸다.")]
    private bool _loopActionSequence;

    [Tooltip("현재 ActionSequence에 0보다 큰 대기 시간이 하나라도 있는지 나타낸다.")]
    private bool _actionSequenceHasPositiveDelay;

    [Tooltip("현재 ActionSequence가 Update에서 진행 중인지 나타낸다.")]
    private bool _isActionSequenceRunning;

    [Tooltip("Action 실행 중 Sequence가 교체되었는지 구분하는 실행 버전이다.")]
    private int _actionSequenceVersion;

    [Tooltip("현재 실행 중인 서브 StateMachine들이 돌아갈 부모 문맥을 순서대로 보관한다.")]
    private readonly Stack<StateMachineFrame> _stateMachineFrames = new();

    [Tooltip("다음 Update 시작 시 처리할 StateMachine 계층 작업이다.")]
    private StateMachineHierarchyOperation _pendingHierarchyOperation;

    [Tooltip("예약된 시작 작업에서 실행할 서브 StateMachine이다.")]
    private StateMachineSO _pendingSubStateMachine;

    [Tooltip("예약된 서브 StateMachine의 선택적 시작 State다.")]
    private StateSO _pendingSubStartState;

    [Tooltip("예약된 서브 StateMachine이 종료된 뒤 부모에서 진입할 State다.")]
    private StateSO _pendingReturnState;

    [Tooltip("예약된 종료 작업이 부모에게 남길 결과다.")]
    private SubStateMachineResult _pendingSubStateMachineResult;

    [Tooltip("가장 최근에 부모로 반환된 서브 StateMachine이다.")]
    private StateMachineSO _lastFinishedSubStateMachine;

    [Tooltip("가장 최근에 부모로 반환된 서브 StateMachine의 종료 결과다.")]
    private SubStateMachineResult _lastSubStateMachineResult;

    [Tooltip("StateMachine 교체 도중 중첩 예약이 들어오는 것을 막는다.")]
    private bool _isProcessingHierarchyOperation;

    public int SubStateMachineDepth => _stateMachineFrames.Count;
    public bool IsSubStateMachineActive => _stateMachineFrames.Count > 0;
    public StateMachineSO LastFinishedSubStateMachine => _lastFinishedSubStateMachine;
    public SubStateMachineResult LastSubStateMachineResult => _lastSubStateMachineResult;

    #endregion

    #region Core
    public virtual void Awake()
    {
        // Timers는 StateSO.timerID로 접근하고 그 값이 0~9로 제한돼 있어(MinValue/MaxValue) 10개면 충분하다.
        // TransitionConditions와 전이 시간 목록은 전이 인덱스로 접근하므로 State에 따라 더 필요할 수
        // 있다. 부족한 만큼은 EnsureTransitionCapacity가 채운다.
        for (int i = 0; i < 10; i++)
        {
            TransitionConditions.Add(false);
            _transitionElapsedTimes.Add(0f);
            _transitionAvailableTriggered.Add(false);
            _transitionDisableTriggered.Add(false);
            Timers.Add(new());
        }
    }

    public virtual void Start()
    {
        Initialize();
    }

    public virtual void Initialize()
    {
        RegisterModules(transform);
    }

    /// <summary>
    /// StateMachine의 전이 타이머와 예약 Action을 매 프레임 갱신한다.
    /// 모든 시간은 Time.deltaTime을 사용하므로 게임 일시정지의 영향을 받는다.
    /// </summary>
    public void Update()
    {
        if (!enabled) return;

        ProcessPendingHierarchyOperation();
        if (!CurrentState) return;

        float deltaTime = Time.deltaTime;
        UpdateTransitionTimers(CurrentState.transitions, deltaTime);
        UpdateActionSequence(deltaTime);
        TimingUpdated?.Invoke(deltaTime);

        if (CurrentState)
            UpdateState();
    }

    /// <summary>
    /// 컨트롤러가 비활성화되면 현재 State가 만든 Update 기반 예약을 정리한다.
    /// 풀에서 다시 초기화될 때 이전 State의 Action이 뒤늦게 실행되는 것을 막는다.
    /// </summary>
    public virtual void OnDisable()
    {
        TimingStopped?.Invoke();
        StopActionSequence();
        ClearPendingHierarchyOperation();

        if (CurrentState)
            StopTransitionTimers(CurrentState.transitions.Length);
    }

    public virtual void ChangeState(StateSO state)
    {
        StateChanging?.Invoke(CurrentState, state);
        CurrentState.ExitState(this);
        CurrentState = state;
        CurrentState.EnterState(this);
    }

    public virtual void ChangeState(string stateName)
    {
        StateSO targetState = StateMachine.allStates.Find(a => a.name == stateName);
        if (targetState) ChangeState(targetState);
    }

    public virtual void UpdateState()
    {
        CurrentState.UpdateState(this);
        CurrentState.CheckDecision(this);
    }

    public void InitializeStateMachine(StateMachineSO stateMachine)
    {
        // customVariables를 **먼저** 바꿔야 한다. ResetStateMachine은 그 자리에서 초기 State의
        // 진입 액션을 실행하는데, 예전 순서에서는 그 액션이 쓴 값이 전부 직전 상태 기계의 저장소로
        // 들어갔다가 바로 다음 줄의 대입으로 통째로 버려졌다. 그리고 읽는 쪽은 새 저장소를 보므로,
        // "진입할 때 쓰고 곧바로 읽는" 값(이벤트 단계 타임아웃의 시각 표식 등)이 항상 어긋났다.
        //
        // 게다가 이 대입은 SO의 객체를 그대로 참조로 물어간다(아래 경고). 그래서 읽는 쪽이 보는 값은
        // 0이 아니라 **지난 플레이에서 남은 값**이었다 — 이벤트 단계가 지난 판의 시각을 기준으로
        // 기다리는 바람에, 같은 이벤트인데도 진입한 시점에 따라 대기 시간이 제멋대로 달라졌다.
        customVariables = stateMachine.customVariables;
        ResetStateMachine(stateMachine);
        //////
        ///  HAVE TO FIX THIS NOT TO DEEP REFERENCE CUSTOMVARS!!!
        //////
    }

    public void ResetStateMachine(StateMachineSO stateMachine)
    {
        if (stateMachine == null)
        {
            Debug.LogError("ERROR: StateMachine Missing!!!");
            return;
        }
        if (_stateMachineFrames.Count > 0)
        {
            Debug.LogWarning("[StateController] 외부 StateMachine 교체로 실행 중인 서브 계층을 제거합니다.", this);
            _stateMachineFrames.Clear();
        }
        ClearPendingHierarchyOperation();

        if (CurrentState)
            StateChanging?.Invoke(CurrentState, stateMachine.initialState);

        stateMachine.UnbindCommands();
        _stateMachine = stateMachine;
        stateMachine.BindCommands(this);
        CurrentState = stateMachine.initialState;
        CurrentState.EnterState(this);
    }

    public void EliminateStateMachine()
    {
        if (_stateMachine) _stateMachine.UnbindCommands();
        _stateMachine = null;
        if (CurrentState)
        {
            StateChanging?.Invoke(CurrentState, null);
            CurrentState.ExitState(this);
        }
        CurrentState = null;
        _stateMachineFrames.Clear();
        ClearPendingHierarchyOperation();
        StopAllCoroutines();
    }
    #endregion

    #region TransitionTimer

    /// <summary>
    /// 전이 인덱스로 접근하는 두 리스트가 최소 count개는 되도록 보장한다.
    /// Timers는 timerID로 접근하는 별개의 축이라 여기서 늘리지 않는다.
    ///
    /// 전에는 Awake가 채우는 10개가 전부여서, 전이가 10개를 넘는 State에 진입하면
    /// StateSO.ReserveTransitions가 TransitionConditions[10]을 건드리며 터졌다
    /// (Delta_Lily_NormalAttack4~8State가 전이 11개다).
    /// </summary>
    public void EnsureTransitionCapacity(int count)
    {
        while (TransitionConditions.Count < count)
        {
            TransitionConditions.Add(false);
            _transitionElapsedTimes.Add(0f);
            _transitionAvailableTriggered.Add(false);
            _transitionDisableTriggered.Add(false);
        }
    }

    /// <summary>
    /// 새 State의 전이 가능·비활성 시간을 0부터 Update 기반으로 시작한다.
    /// availableTime이 0 이하인 전이는 진입 프레임부터 즉시 사용할 수 있다.
    /// </summary>
    public void StartTransitionTimers(StateTransition[] transitions)
    {
        int count = transitions?.Length ?? 0;
        EnsureTransitionCapacity(count);

        for (int i = 0; i < count; i++)
        {
            TransitionConditions[i] = false;
            _transitionElapsedTimes[i] = 0f;
            _transitionAvailableTriggered[i] = false;
            _transitionDisableTriggered[i] = false;
            ApplyTransitionTimerMilestones(i, transitions[i]);
        }
    }

    /// <summary>
    /// 현재 State에 속한 전이 시간 진행을 중단하고 조건을 초기화한다.
    /// 다음 State의 StartTransitionTimers 호출 전까지 이전 예약은 진행되지 않는다.
    /// </summary>
    public void StopTransitionTimers(int transitionCount)
    {
        int safeCount = Mathf.Min(transitionCount, TransitionConditions.Count);
        for (int i = 0; i < safeCount; i++)
        {
            TransitionConditions[i] = false;
            _transitionElapsedTimes[i] = 0f;
            _transitionAvailableTriggered[i] = true;
            _transitionDisableTriggered[i] = true;
        }
    }

    /// <summary>
    /// 현재 State의 전이별 경과시간을 진행시키고 도달한 조건을 적용한다.
    /// availableTime과 disableTime은 State 진입 시점을 기준으로 각각 한 번 처리된다.
    /// </summary>
    private void UpdateTransitionTimers(StateTransition[] transitions, float deltaTime)
    {
        if (transitions == null) return;

        int count = Mathf.Min(transitions.Length, _transitionElapsedTimes.Count);
        for (int i = 0; i < count; i++)
        {
            if (_transitionAvailableTriggered[i] && _transitionDisableTriggered[i]) continue;

            _transitionElapsedTimes[i] += deltaTime;
            ApplyTransitionTimerMilestones(i, transitions[i]);
        }
    }

    /// <summary>
    /// 한 전이가 도달한 available·disable 시점을 기존 실행 순서대로 반영한다.
    /// 두 시간이 같은 프레임에 도달하면 available 적용 뒤 disable을 적용한다.
    /// </summary>
    private void ApplyTransitionTimerMilestones(int index, StateTransition transition)
    {
        float elapsedTime = _transitionElapsedTimes[index];

        if (!_transitionAvailableTriggered[index] && elapsedTime >= transition.availableTime)
        {
            _transitionAvailableTriggered[index] = true;
            TransitionConditions[index] = true;
        }

        if (!_transitionDisableTriggered[index] && transition.disableTime > 0f &&
            elapsedTime >= transition.disableTime)
        {
            _transitionDisableTriggered[index] = true;
            TransitionConditions[index] = false;
        }

        if (transition.disableTime <= 0f)
            _transitionDisableTriggered[index] = true;
    }
    #endregion

    #region ActionSequence
    /// <summary>
    /// StateSO의 ActionSequence를 Update 기반으로 새로 시작한다.
    /// 같은 컨트롤러에서 실행 중인 이전 Sequence는 콜백 없이 교체한다.
    /// </summary>
    public void StartActionSequence(ActionSequence[] actionSequence, bool loop)
    {
        StopActionSequence();
        if (actionSequence == null || actionSequence.Length == 0) return;

        _currentActionSequence = actionSequence;
        _sequenceInt = 0;
        _actionSequenceElapsedTime = 0f;
        _loopActionSequence = loop;
        _actionSequenceHasPositiveDelay = false;
        for (int i = 0; i < actionSequence.Length; i++)
        {
            if (actionSequence[i].time > 0f)
            {
                _actionSequenceHasPositiveDelay = true;
                break;
            }
        }
        _isActionSequenceRunning = true;
        _actionSequenceVersion++;
    }

    /// <summary>
    /// 현재 ActionSequence를 중단하고 모든 런타임 진행 상태를 초기화한다.
    /// 아직 실행되지 않은 Action은 호출하지 않는다.
    /// </summary>
    public void StopActionSequence()
    {
        _isActionSequenceRunning = false;
        _currentActionSequence = null;
        _sequenceInt = 0;
        _actionSequenceElapsedTime = 0f;
        _loopActionSequence = false;
        _actionSequenceHasPositiveDelay = false;
        _actionSequenceVersion++;
    }

    /// <summary>
    /// 현재 ActionSequence의 다음 항목까지 스케일 시간을 누적하고 Action을 실행한다.
    /// 프레임 초과 시간이 있으면 다음 항목으로 넘기되 무시간 무한 루프는 프레임당 한 번만 돈다.
    /// </summary>
    private void UpdateActionSequence(float deltaTime)
    {
        if (!_isActionSequenceRunning || _currentActionSequence == null ||
            _currentActionSequence.Length == 0)
            return;

        _actionSequenceElapsedTime += deltaTime;
        int sequenceVersion = _actionSequenceVersion;
        int processedCount = 0;
        int itemCount = _currentActionSequence.Length;

        while (_isActionSequenceRunning && processedCount < 1024)
        {
            ActionSequence item = _currentActionSequence[_sequenceInt];
            float delay = Mathf.Max(0f, item.time);
            if (_actionSequenceElapsedTime < delay) return;

            _actionSequenceElapsedTime -= delay;
            _sequenceInt++;
            processedCount++;
            item.Action?.Act(this);

            if (!_isActionSequenceRunning || sequenceVersion != _actionSequenceVersion) return;

            if (_sequenceInt < itemCount) continue;
            if (!_loopActionSequence)
            {
                StopActionSequence();
                return;
            }

            _sequenceInt = 0;

            // 모든 항목의 시간이 0인 무한 Sequence는 한 프레임에 무한히 실행될 수 있다.
            // 한 바퀴만 허용하면 Update 기반 반복이라는 의미도 명확하게 유지된다.
            if (processedCount >= itemCount && !_actionSequenceHasPositiveDelay)
                return;
        }

        if (processedCount >= 1024)
        {
            Debug.LogWarning(
                "[StateController] 한 프레임에 ActionSequence 항목을 1024개 이상 처리하지 않도록 중단했습니다.",
                this);
        }
    }
    #endregion

    #region SubStateMachine
    /// <summary>
    /// 현재 State 처리가 끝난 뒤 지정한 서브 StateMachine을 시작하도록 예약한다.
    /// 부모 State는 Exit되고, 서브 완료 시 returnState에 새로 진입한다.
    /// </summary>
    public bool RequestStartSubStateMachine(
        StateMachineSO subStateMachine,
        StateSO startState,
        StateSO returnState)
    {
        if (subStateMachine == null || subStateMachine.initialState == null)
        {
            Debug.LogError("[StateController] 시작할 서브 StateMachine 또는 Initial State가 비어 있습니다.", this);
            return false;
        }

        if (returnState == null)
        {
            Debug.LogError("[StateController] 서브 StateMachine 완료 뒤 돌아갈 Return State가 비어 있습니다.", this);
            return false;
        }

        if (_stateMachine == null || CurrentState == null)
        {
            Debug.LogError("[StateController] 부모 StateMachine이 실행 중일 때만 서브 StateMachine을 시작할 수 있습니다.", this);
            return false;
        }

        if (startState != null && !ContainsState(subStateMachine, startState))
        {
            Debug.LogError("[StateController] Start State가 지정한 서브 StateMachine에 속하지 않습니다.", this);
            return false;
        }

        if (!ContainsState(_stateMachine, returnState))
        {
            Debug.LogError("[StateController] Return State가 현재 부모 StateMachine에 속하지 않습니다.", this);
            return false;
        }

        if (!CanReserveHierarchyOperation()) return false;

        _pendingHierarchyOperation = StateMachineHierarchyOperation.Start;
        _pendingSubStateMachine = subStateMachine;
        _pendingSubStartState = startState;
        _pendingReturnState = returnState;
        return true;
    }

    /// <summary>
    /// 현재 서브 StateMachine을 종료하고 부모 Return State로 돌아가도록 예약한다.
    /// 정상 종료와 취소 결과는 부모의 Decision에서 확인할 수 있다.
    /// </summary>
    public bool RequestFinishSubStateMachine(SubStateMachineResult result)
    {
        if (!IsSubStateMachineActive)
        {
            Debug.LogError("[StateController] 종료할 서브 StateMachine이 없습니다.", this);
            return false;
        }

        if (result == SubStateMachineResult.None)
        {
            Debug.LogError("[StateController] 서브 StateMachine 종료 결과로 None을 사용할 수 없습니다.", this);
            return false;
        }

        if (!CanReserveHierarchyOperation()) return false;

        _pendingHierarchyOperation = StateMachineHierarchyOperation.Finish;
        _pendingSubStateMachineResult = result;
        return true;
    }

    /// <summary>
    /// 현재 컨트롤러가 지정한 StateMachine을 서브 계층으로 실행 중인지 확인한다.
    /// 대상을 비우면 종류와 관계없이 서브 계층 여부만 반환한다.
    /// </summary>
    public bool IsRunningSubStateMachine(StateMachineSO target)
    {
        return IsSubStateMachineActive && (target == null || StateMachine == target);
    }

    /// <summary>
    /// 지정한 서브 StateMachine이 기대한 결과로 가장 최근 종료됐는지 확인한다.
    /// 대상을 비우면 StateMachine 종류와 관계없이 결과만 비교한다.
    /// </summary>
    public bool HasFinishedSubStateMachine(StateMachineSO target, SubStateMachineResult result)
    {
        return result != SubStateMachineResult.None &&
               _lastSubStateMachineResult == result &&
               (target == null || _lastFinishedSubStateMachine == target);
    }

    /// <summary>
    /// StateAction이 예약한 계층 전환을 Update의 안전한 시작 지점에서 처리한다.
    /// 전환 중 발생한 추가 예약은 거부하여 부모·서브 스택이 엇갈리지 않게 한다.
    /// </summary>
    private void ProcessPendingHierarchyOperation()
    {
        if (_pendingHierarchyOperation == StateMachineHierarchyOperation.None) return;

        StateMachineHierarchyOperation operation = _pendingHierarchyOperation;
        StateMachineSO subStateMachine = _pendingSubStateMachine;
        StateSO startState = _pendingSubStartState;
        StateSO returnState = _pendingReturnState;
        SubStateMachineResult result = _pendingSubStateMachineResult;
        ClearPendingHierarchyOperation();

        _isProcessingHierarchyOperation = true;
        try
        {
            if (operation == StateMachineHierarchyOperation.Start)
                StartSubStateMachineNow(subStateMachine, startState, returnState);
            else if (operation == StateMachineHierarchyOperation.Finish)
                FinishSubStateMachineNow(result);
        }
        finally
        {
            _isProcessingHierarchyOperation = false;
        }
    }

    /// <summary>
    /// 부모 State를 정상 Exit한 뒤 서브 StateMachine의 시작 State로 교체한다.
    /// 부모의 시간 예약은 Exit에서 정리되므로 서브 실행 중 진행되지 않는다.
    /// </summary>
    private void StartSubStateMachineNow(
        StateMachineSO subStateMachine,
        StateSO startState,
        StateSO returnState)
    {
        StateSO resolvedStartState = startState != null ? startState : subStateMachine.initialState;
        StateChanging?.Invoke(CurrentState, resolvedStartState);
        CurrentState.ExitState(this);
        _stateMachine.UnbindCommands();

        _stateMachineFrames.Push(new StateMachineFrame(
            _stateMachine,
            returnState,
            customVariables));

        _lastFinishedSubStateMachine = null;
        _lastSubStateMachineResult = SubStateMachineResult.None;
        _stateMachine = subStateMachine;
        customVariables = subStateMachine.customVariables;
        subStateMachine.UnbindCommands();
        subStateMachine.BindCommands(this);
        CurrentState = resolvedStartState;
        CurrentState.EnterState(this);
    }

    /// <summary>
    /// 현재 서브 State를 Exit하고 스택의 부모 StateMachine과 변수를 복원한다.
    /// 부모의 이전 State는 재개하지 않고 미리 지정한 Return State에 진입한다.
    /// </summary>
    private void FinishSubStateMachineNow(SubStateMachineResult result)
    {
        if (_stateMachineFrames.Count == 0 || CurrentState == null) return;

        StateMachineSO finishedStateMachine = _stateMachine;
        StateMachineFrame frame = _stateMachineFrames.Pop();
        StateChanging?.Invoke(CurrentState, frame.ReturnState);
        CurrentState.ExitState(this);
        finishedStateMachine.UnbindCommands();

        _stateMachine = frame.ParentStateMachine;
        customVariables = frame.ParentVariables;
        _stateMachine.BindCommands(this);
        _lastFinishedSubStateMachine = finishedStateMachine;
        _lastSubStateMachineResult = result;
        CurrentState = frame.ReturnState;
        CurrentState.EnterState(this);
    }

    /// <summary>
    /// 한 프레임에 하나의 StateMachine 계층 작업만 예약되도록 검사한다.
    /// 이미 예약됐거나 전환 처리 중이면 새 요청을 오류와 함께 거부한다.
    /// </summary>
    private bool CanReserveHierarchyOperation()
    {
        if (_isProcessingHierarchyOperation ||
            _pendingHierarchyOperation != StateMachineHierarchyOperation.None)
        {
            Debug.LogError("[StateController] 한 프레임에 StateMachine 계층 작업을 두 번 예약할 수 없습니다.", this);
            return false;
        }

        return true;
    }

    /// <summary>
    /// State가 지정한 StateMachine의 Initial State 또는 All States 목록에 속하는지 확인한다.
    /// 잘못 연결된 서브 시작·반환 State가 런타임에 다른 머신 문맥으로 들어가는 것을 막는다.
    /// </summary>
    private static bool ContainsState(StateMachineSO stateMachine, StateSO state)
    {
        if (stateMachine == null || state == null) return false;
        if (stateMachine.initialState == state) return true;
        return stateMachine.allStates != null && stateMachine.allStates.Contains(state);
    }

    /// <summary>
    /// 아직 처리하지 않은 StateMachine 계층 예약의 참조와 결과를 초기화한다.
    /// 컨트롤러 비활성화 뒤 이전 예약이 실행되는 것을 방지할 때도 사용한다.
    /// </summary>
    private void ClearPendingHierarchyOperation()
    {
        _pendingHierarchyOperation = StateMachineHierarchyOperation.None;
        _pendingSubStateMachine = null;
        _pendingSubStartState = null;
        _pendingReturnState = null;
        _pendingSubStateMachineResult = SubStateMachineResult.None;
    }

    #endregion

    #region CustomParameter
    public void SetBoolParameterTrue(string name)
    {
        if (!customVariables.boolVariables.ContainsKey(name))
        {
            Debug.LogWarning("Warning: Generated missing variable: " + name);
            customVariables.boolVariables[name] = new() { Value = true };
        }
        else
            customVariables.boolVariables[name].Value = true;
    }

    public void SetBoolParameterFalse(string name)
    {
        if (!customVariables.boolVariables.ContainsKey(name))
        {
            Debug.LogWarning("Warning: Generated missing variable: " + name);
            customVariables.boolVariables[name] = new() { Value = false };
        }
        else
            customVariables.boolVariables[name].Value = false;
    }

    public void SetIntParameter(string name, int value)
    {
        if (!customVariables.intVariables.ContainsKey(name))
        {
            Debug.LogWarning("Warning: Generated missing variable: " + name);
            customVariables.intVariables[name] = new() { Value = value };
        }
        else
            customVariables.intVariables[name].Value = value;
    }

    public void IncrementIntParameter(string name, int value)
    {
        if (!customVariables.intVariables.ContainsKey(name))
        {
            Debug.LogWarning("Warning: Generated missing variable: " + name);
            customVariables.intVariables[name] = new() { Value = value };
        }
        else
            customVariables.intVariables[name].Value += value;
    }

    public bool GetBoolParameter(string name)
    {
        if (!customVariables.boolVariables.ContainsKey(name))
        {
            Debug.LogWarning("Warning: Generated missing variable: " + name);
            customVariables.boolVariables[name] = new() { Value = false };
        }

        return customVariables.boolVariables[name].Value;
    }

    public int GetIntParameter(string name)
    {
        if (!customVariables.intVariables.ContainsKey(name))
        {
            Debug.LogWarning("Warning: Generated missing variable: " + name);
            customVariables.intVariables[name] = new() { Value = 0 };
        }

        return customVariables.intVariables[name].Value;
    }
    #endregion
}
