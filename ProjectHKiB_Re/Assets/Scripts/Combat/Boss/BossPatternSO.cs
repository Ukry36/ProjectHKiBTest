using System;
using StateMachine;
using UnityEngine;

/// <summary>
/// 한 패턴이 페이즈 안에서 어떤 계기로 시작되는지 나타낸다.
/// 실제 조건과 연결 대상은 StateTransition이 담당하고 PatternSO에는 제작 의도를 보관한다.
/// </summary>
public enum BossPatternEntryMode
{
    PhaseStart,
    AfterPattern,
    Conditional
}

/// <summary>
/// 보스전 전체에 유지되는 특수행동의 미결정·성공·실패 결과를 나타낸다.
/// 패턴 클리어와 별개의 결과이며 다른 패턴과 페이즈의 진입 조건으로 사용할 수 있다.
/// </summary>
public enum BossSpecialActionResult
{
    Unresolved,
    Success,
    Failure
}

/// <summary>
/// 패턴 클리어에 타임라인 종료와 필수 특수행동 중 무엇이 필요한지 정의한다.
/// 기존 직렬화 값 호환을 위해 각 열거형의 숫자는 유지한다.
/// </summary>
public enum BossPatternCompletionRule
{
    TimelineOnly = 0,
    RequiredSpecialActions = 1,
    TimelineAndRequiredSpecialActions = 2
}

/// <summary>
/// 패턴 안에서 요구하거나 관찰할 하나의 특수행동을 정의한다.
/// 성공·실패 결과는 Runner에 보스전 전체 범위로 기록된다.
/// </summary>
[Serializable]
public sealed class BossSpecialActionDefinition
{
    [Tooltip("Action과 Decision에서 사용할 논리 ID. 같은 행동을 여러 패턴에서 누적할 때 같은 ID와 Required Count를 사용한다.")]
    [SerializeField]
    private string _id = "SpecialAction";

    [Tooltip("이 특수행동을 성공으로 확정하기 위해 누적해야 하는 횟수.")]
    [SerializeField, Min(1)]
    private int _requiredCount = 1;

    [Tooltip("이 특수행동 성공이 현재 패턴 클리어에 반드시 필요한지 설정한다.")]
    [SerializeField]
    private bool _requiredForPatternClear;

    [Tooltip("패턴 한 사이클이 끝날 때 미결정이면 기록할 실패 이유 ID.")]
    [SerializeField]
    private string _unansweredFailureReasonId = "Ignored";

    public string Id => NormalizeId(_id);
    public int RequiredCount => Mathf.Max(1, _requiredCount);
    public bool RequiredForPatternClear => _requiredForPatternClear;
    public string UnansweredFailureReasonId =>
        BossPatternSO.NormalizeFailureReasonId(_unansweredFailureReasonId);

    /// <summary>
    /// 에디터 자동 생성에서 특수행동의 핵심 설정을 한 번에 지정한다.
    /// 일반 인스펙터 생성은 매개변수 없는 생성자를 계속 사용할 수 있다.
    /// </summary>
    public BossSpecialActionDefinition(
        string id,
        string displayName,
        int requiredCount,
        bool requiredForPatternClear)
    {
        _id = NormalizeId(id);
        _requiredCount = Mathf.Max(1, requiredCount);
        _requiredForPatternClear = requiredForPatternClear;
        _unansweredFailureReasonId = "Ignored";
    }

    /// <summary>
    /// Unity 직렬화와 배열 추가가 빈 특수행동 항목을 만들 때 사용하는 기본 생성자다.
    /// 실제 값은 Inspector 또는 Validate 과정에서 안전하게 정리된다.
    /// </summary>
    public BossSpecialActionDefinition() { }

    /// <summary>
    /// 비어 있는 ID와 잘못된 횟수를 실행 가능한 값으로 보정한다.
    /// 에디터 저장 전에만 원본 직렬화 값을 변경한다.
    /// </summary>
    public void Validate(int index)
    {
        _id = string.IsNullOrWhiteSpace(_id) ? $"SpecialAction{index + 1}" : _id.Trim();
        _requiredCount = Mathf.Max(1, _requiredCount);
        _unansweredFailureReasonId = BossPatternSO.NormalizeFailureReasonId(
            _unansweredFailureReasonId);
    }

    /// <summary>
    /// 특수행동 ID 비교에서 공백과 빈 문자열 때문에 기록이 갈라지지 않게 한다.
    /// 비어 있는 값은 공통 기본 ID인 SpecialAction으로 처리한다.
    /// </summary>
    public static string NormalizeId(string id)
    {
        return string.IsNullOrWhiteSpace(id) ? "SpecialAction" : id.Trim();
    }
}

/// <summary>
/// 보스 패턴 타임라인에서 한 번의 대기와 그 뒤 실행할 Action 묶음을 표현한다.
/// Delay는 이전 항목이 실행된 시점부터 흐르는 StateController 스케일 시간이다.
/// </summary>
[Serializable]
public sealed class BossPatternTimelineEntry
{
    [Tooltip("인스펙터에서 이 타임라인 항목의 역할을 알아보기 위한 이름.")]
    [SerializeField]
    private string _label;

    [Tooltip("이전 항목 실행 뒤 이 Action 묶음까지 기다릴 스케일 시간(초).")]
    [SerializeField, Min(0f)]
    private float _delay;

    [Tooltip("Delay가 지난 같은 프레임에 위에서부터 순서대로 실행할 StateAction들.")]
    [SerializeReference, SubclassSelector]
    private StateAction[] _actions = Array.Empty<StateAction>();

    public string Label => _label;
    public float Delay => Mathf.Max(0f, _delay);
    public StateAction[] Actions => _actions;

    /// <summary>
    /// 인스펙터에서 입력된 지연시간과 Action 배열을 안전한 값으로 정리한다.
    /// 런타임에서는 원본 SO를 수정하지 않고 읽기만 한다.
    /// </summary>
    public void Validate()
    {
        _delay = Mathf.Max(0f, _delay);
        _actions ??= Array.Empty<StateAction>();
    }
}

/// <summary>
/// 하나의 보스 패턴의 진입 방식, 특수행동들과 상대 지연 타임라인을 정의한다.
/// 런타임 진행 상태는 보관하지 않으며 BossPatternRunner가 컨트롤러별로 실행한다.
/// </summary>
[CreateAssetMenu(fileName = "BossPattern", menuName = "State Machine/Boss/Boss Pattern")]
public sealed class BossPatternSO : ScriptableObject
{
    [Tooltip("기획서와 에셋을 연결하는 짧은 ID. 예: P1.")]
    [SerializeField]
    private string _label;

    [Tooltip("이 패턴의 진입 조건, 동작, 특수행동과 대응 결과를 기록한다.")]
    [SerializeField, TextArea(1, 3)]
    private string _summary;

    [Tooltip("페이즈 시작, 특정 패턴 이후 또는 임의 조건 충족 중 이 패턴의 진입 방식.")]
    [SerializeField]
    private BossPatternEntryMode _entryMode;

    [Tooltip("한 페이즈 실행 안에서 진입 조건이 다시 만족되면 이 패턴을 재실행할 수 있다.")]
    [SerializeField]
    private bool _allowRepeatWithinPhase;

    [Tooltip("패턴 클리어에 타임라인 종료와 필수 특수행동 중 무엇이 필요한지 선택한다.")]
    [SerializeField]
    private BossPatternCompletionRule _completionRule;

    [Tooltip("이 패턴 중 성공 또는 실패할 수 있는 특수행동들. 패턴당 0개 이상 설정할 수 있다.")]
    [SerializeField]
    private BossSpecialActionDefinition[] _specialActions =
        Array.Empty<BossSpecialActionDefinition>();

    [Tooltip("필수 특수행동이 남아 있으면 설정한 Loop Start Beat부터 타임라인을 반복 실행한다.")]
    [SerializeField, NaughtyAttributes.ShowIf(nameof(UsesRequiredSpecialActions))]
    private bool _loopTimelineUntilCleared;

    [Tooltip("첫 타임라인은 0번부터 실행하고, 재시도부터 다시 시작할 Beat 인덱스. 최초 연출을 반복하지 않을 때 사용한다.")]
    [SerializeField, Min(0), NaughtyAttributes.ShowIf(nameof(_loopTimelineUntilCleared))]
    private int _timelineLoopStartIndex;

    [Tooltip("필수 특수행동 미달로 타임라인을 반복할 때 다음 사이클까지 기다릴 스케일 시간(초).")]
    [SerializeField, Min(0f), NaughtyAttributes.ShowIf(nameof(_loopTimelineUntilCleared))]
    private float _timelineLoopDelay;

    [Tooltip("패턴 시작 뒤 순서대로 실행할 상대 지연시간 기반 타임라인.")]
    [SerializeField]
    private BossPatternTimelineEntry[] _timeline = Array.Empty<BossPatternTimelineEntry>();

    [Tooltip("패턴 클리어 조건 충족 뒤 다음 State 전이까지 기다릴 스케일 시간(초).")]
    [SerializeField, Min(0f)]
    private float _recoveryDuration;

    public string Label => _label;
    public string Summary => _summary;
    public BossPatternEntryMode EntryMode => _entryMode;
    public bool AllowRepeatWithinPhase => _allowRepeatWithinPhase;
    public BossPatternCompletionRule CompletionRule => _completionRule;
    public BossSpecialActionDefinition[] SpecialActions => _specialActions;
    public bool LoopTimelineUntilCleared =>
        _loopTimelineUntilCleared && UsesRequiredSpecialActions;
    public int TimelineLoopStartIndex => _timeline == null || _timeline.Length == 0
        ? 0
        : Mathf.Clamp(_timelineLoopStartIndex, 0, _timeline.Length - 1);
    public float TimelineLoopDelay => Mathf.Max(0f, _timelineLoopDelay);
    public BossPatternTimelineEntry[] Timeline => _timeline;
    public float RecoveryDuration => Mathf.Max(0f, _recoveryDuration);
    public bool HasSpecialActions => _specialActions != null && _specialActions.Length > 0;
    public bool UsesRequiredSpecialActions =>
        _completionRule != BossPatternCompletionRule.TimelineOnly;

    /// <summary>
    /// 모든 타임라인 Delay와 완료 후 대기시간을 더한 한 사이클 설정 길이를 반환한다.
    /// 반복 대기와 Action 내부의 별도 대기시간은 포함하지 않는다.
    /// </summary>
    public float TotalDuration
    {
        get
        {
            float totalDuration = RecoveryDuration;
            if (_timeline == null) return totalDuration;

            for (int i = 0; i < _timeline.Length; i++)
                if (_timeline[i] != null) totalDuration += _timeline[i].Delay;

            return totalDuration;
        }
    }

    /// <summary>
    /// 지정한 ID의 특수행동 정의와 필요 횟수를 찾는다.
    /// Action 오타가 조용히 새 기록을 만들지 않도록 정의되지 않은 ID는 false를 반환한다.
    /// </summary>
    public bool TryGetSpecialAction(
        string specialActionId,
        out BossSpecialActionDefinition specialAction)
    {
        string normalizedId = BossSpecialActionDefinition.NormalizeId(specialActionId);
        if (_specialActions != null)
        {
            for (int i = 0; i < _specialActions.Length; i++)
            {
                if (_specialActions[i] != null && _specialActions[i].Id == normalizedId)
                {
                    specialAction = _specialActions[i];
                    return true;
                }
            }
        }

        specialAction = null;
        return false;
    }

    /// <summary>
    /// 에디터 간편 생성 프리셋을 PatternSO 초기값으로 반영한다.
    /// 필요할 때 하나의 특수행동을 만들고 패턴 클리어 필수 여부를 함께 지정한다.
    /// </summary>
    public void ConfigureForAuthoring(
        string designId,
        BossPatternEntryMode entryMode,
        BossPatternCompletionRule completionRule,
        bool createSpecialAction,
        string specialActionId,
        int requiredCount,
        bool requiredForPatternClear,
        bool loopTimelineUntilCleared)
    {
        _label = string.IsNullOrWhiteSpace(designId) ? string.Empty : designId.Trim();
        _entryMode = entryMode;
        _allowRepeatWithinPhase = false;
        _completionRule = completionRule;
        _specialActions = createSpecialAction
            ? new[]
            {
                new BossSpecialActionDefinition(
                    specialActionId,
                    "특수행동",
                    requiredCount,
                    requiredForPatternClear)
            }
            : Array.Empty<BossSpecialActionDefinition>();
        _loopTimelineUntilCleared = loopTimelineUntilCleared && UsesRequiredSpecialActions;
        _timelineLoopStartIndex = 0;
        OnValidate();
    }

    /// <summary>
    /// 특수행동 ID를 Runner Dictionary에서 안전하게 사용할 문자열로 정리한다.
    /// 기존 호출부 호환을 위해 정의 타입의 정규화 함수를 전달한다.
    /// </summary>
    public static string NormalizeSpecialActionId(string specialActionId)
    {
        return BossSpecialActionDefinition.NormalizeId(specialActionId);
    }

    /// <summary>
    /// 실패 사유 ID의 공백과 빈 값을 정리해 Decision 비교가 안정적으로 동작하게 한다.
    /// 미입력 값은 이유가 지정되지 않았음을 뜻하는 Unspecified로 보정한다.
    /// </summary>
    public static string NormalizeFailureReasonId(string failureReasonId)
    {
        return string.IsNullOrWhiteSpace(failureReasonId)
            ? "Unspecified"
            : failureReasonId.Trim();
    }

    /// <summary>
    /// 시간, null 배열과 특수행동 정의를 에디터 저장 전에 안전한 값으로 정리한다.
    /// 각 타임라인 항목의 Action 배열도 함께 초기화한다.
    /// </summary>
    private void OnValidate()
    {
        _recoveryDuration = Mathf.Max(0f, _recoveryDuration);
        _timelineLoopDelay = Mathf.Max(0f, _timelineLoopDelay);
        _timeline ??= Array.Empty<BossPatternTimelineEntry>();
        _specialActions ??= Array.Empty<BossSpecialActionDefinition>();
        _timelineLoopStartIndex = _timeline.Length == 0
            ? 0
            : Mathf.Clamp(_timelineLoopStartIndex, 0, _timeline.Length - 1);

        for (int i = 0; i < _timeline.Length; i++)
            _timeline[i]?.Validate();
        for (int i = 0; i < _specialActions.Length; i++)
            _specialActions[i]?.Validate(i);
    }
}
