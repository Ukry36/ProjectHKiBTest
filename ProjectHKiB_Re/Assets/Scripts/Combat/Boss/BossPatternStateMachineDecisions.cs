using System;
using StateMachine;
using UnityEngine;

/// <summary>
/// 선택한 BossPatternSO 또는 임의의 보스 패턴이 현재 실행 중인지 검사한다.
/// 패턴 State의 중복 시작 방지나 실행 중 분기에 사용할 수 있다.
/// </summary>
[AddTypeMenu("Boss/Is Boss Pattern Running")]
[Serializable]
public sealed class IsBossPatternRunningDecision : StateDecision
{
    [Tooltip("검사할 BossPatternSO. 비워두면 종류와 관계없이 실행 중인 패턴을 허용한다.")]
    [SerializeField, NaughtyAttributes.Expandable]
    private BossPatternSO _pattern;

    /// <summary>
    /// Runner가 Running 결과와 선택한 패턴을 가지는지 확인한다.
    /// Runner가 등록되지 않았다면 false를 반환한다.
    /// </summary>
    public override bool Decide(StateController stateController)
    {
        return stateController != null &&
               stateController.TryGetInterface(out IBossPatternRunner runner) &&
               runner.Matches(_pattern, BossPatternRunResult.Running);
    }
}

/// <summary>
/// 선택한 패턴의 현재 실행이 자연 완료됐는지 검사한다.
/// 패턴 State에서 다음 State로 나가는 기본 완료 조건이다.
/// </summary>
[AddTypeMenu("Boss/Is Current Boss Pattern Completed")]
[Serializable]
public sealed class IsBossPatternCompletedDecision : StateDecision
{
    [Tooltip("검사할 BossPatternSO. 비워두면 가장 최근 완료된 패턴을 허용한다.")]
    [SerializeField, NaughtyAttributes.Expandable]
    private BossPatternSO _pattern;

    public BossPatternSO Pattern => _pattern;

    /// <summary>
    /// SerializeReference 선택기에서 생성할 때 패턴을 Inspector에서 지정하도록 비워 둔다.
    /// 자동 생성 도구는 매개변수 생성자를 사용한다.
    /// </summary>
    public IsBossPatternCompletedDecision() { }

    /// <summary>
    /// 자동 완료 전이가 검사할 BossPatternSO를 직접 연결한다.
    /// 다른 패턴의 최근 실행 결과에 반응하지 않게 한다.
    /// </summary>
    public IsBossPatternCompletedDecision(BossPatternSO pattern)
    {
        _pattern = pattern;
    }

    /// <summary>
    /// Runner의 현재 실행 결과가 선택 패턴의 Completed인지 검사한다.
    /// 과거 클리어 이력은 BossPatternClearedDecision을 사용한다.
    /// </summary>
    public override bool Decide(StateController stateController)
    {
        return stateController != null &&
               stateController.TryGetInterface(out IBossPatternRunner runner) &&
               runner.Matches(_pattern, BossPatternRunResult.Completed);
    }
}

/// <summary>
/// 보스전 시작 이후 선택한 패턴이 한 번이라도 클리어됐는지 검사한다.
/// 특수행동 결과와 조합해 다른 페이즈의 진입 조건을 만들 때 사용한다.
/// </summary>
[AddTypeMenu("Boss/Pattern Was Cleared")]
[Serializable]
public sealed class BossPatternClearedDecision : StateDecision
{
    [Tooltip("보스전 전체 클리어 이력을 검사할 BossPatternSO.")]
    [SerializeField, NaughtyAttributes.Expandable]
    private BossPatternSO _pattern;

    /// <summary>
    /// Runner의 전역 패턴 클리어 이력에서 선택한 패턴을 찾는다.
    /// 페이즈가 바뀌거나 재시작돼도 이미 클리어한 기록은 유지된다.
    /// </summary>
    public override bool Decide(StateController stateController)
    {
        return stateController != null &&
               stateController.TryGetInterface(out IBossPatternRunner runner) &&
               runner.WasPatternCleared(_pattern);
    }
}

/// <summary>
/// 선택한 패턴의 현재 실행이 State 이탈로 취소됐는지 검사한다.
/// 중단 전용 후처리 State가 필요할 때 사용한다.
/// </summary>
[AddTypeMenu("Boss/Is Boss Pattern Cancelled")]
[Serializable]
public sealed class IsBossPatternCancelledDecision : StateDecision
{
    [Tooltip("검사할 BossPatternSO. 비워두면 가장 최근 취소된 패턴을 허용한다.")]
    [SerializeField, NaughtyAttributes.Expandable]
    private BossPatternSO _pattern;

    /// <summary>
    /// Runner가 Cancelled 결과와 선택한 패턴을 가지는지 확인한다.
    /// Runner가 등록되지 않았다면 false를 반환한다.
    /// </summary>
    public override bool Decide(StateController stateController)
    {
        return stateController != null &&
               stateController.TryGetInterface(out IBossPatternRunner runner) &&
               runner.Matches(_pattern, BossPatternRunResult.Cancelled);
    }
}

/// <summary>
/// 선택한 패턴의 실행 자체가 Failed로 종료됐는지 검사한다.
/// 특수행동 실패는 이 Decision이 아니라 BossSpecialActionResultDecision으로 검사한다.
/// </summary>
[AddTypeMenu("Boss/Is Boss Pattern Failed")]
[Serializable]
public sealed class IsBossPatternFailedDecision : StateDecision
{
    [Tooltip("검사할 BossPatternSO. 비워두면 가장 최근 실패한 패턴을 허용한다.")]
    [SerializeField, NaughtyAttributes.Expandable]
    private BossPatternSO _pattern;

    public BossPatternSO Pattern => _pattern;

    /// <summary>
    /// Runner가 Failed 결과와 선택한 패턴을 가지는지 확인한다.
    /// 특수행동의 오답·무시는 패턴 Failed로 취급하지 않는다.
    /// </summary>
    public override bool Decide(StateController stateController)
    {
        return stateController != null &&
               stateController.TryGetInterface(out IBossPatternRunner runner) &&
               runner.Matches(_pattern, BossPatternRunResult.Failed);
    }
}

/// <summary>
/// 이름 있는 특수행동의 보스전 전체 누적 횟수를 기준값과 비교한다.
/// 여러 번 성공해야 하는 패링·오브젝트 유도 같은 조건에 사용한다.
/// </summary>
[AddTypeMenu("Boss/Special Action Progress")]
[Serializable]
public sealed class BossSpecialActionProgressDecision : StateDecision
{
    [Tooltip("진행 횟수를 검사할 특수행동 논리 ID. 여러 패턴의 같은 ID는 진행도를 공유한다.")]
    [SerializeField]
    private string _specialActionId = "SpecialAction";

    [Tooltip("현재 누적 횟수와 비교할 기준값.")]
    [SerializeField, Min(0)]
    private int _value = 1;

    [Tooltip("현재 누적 횟수와 기준값을 비교할 방법.")]
    [SerializeField]
    private EnumManager.CompareType _compareType = EnumManager.CompareType.BiggerOrSameAs;

    /// <summary>
    /// Runner의 특수행동 누적 횟수를 공통 CompareType 규칙으로 비교한다.
    /// Runner가 없으면 false를 반환한다.
    /// </summary>
    public override bool Decide(StateController stateController)
    {
        if (stateController == null || !stateController.TryGetInterface(out IBossPatternRunner runner))
            return false;

        return Compare(runner.GetSpecialActionProgress(_specialActionId), Mathf.Max(0, _value));
    }

    /// <summary>
    /// 두 정수를 프로젝트 공통 CompareType 설정에 맞춰 비교한다.
    /// Inspector에서 같은 Decision으로 모든 횟수 관계를 표현하게 한다.
    /// </summary>
    private bool Compare(int origin, int target)
    {
        return _compareType switch
        {
            EnumManager.CompareType.SameAs => origin == target,
            EnumManager.CompareType.BiggerThan => origin > target,
            EnumManager.CompareType.BiggerOrSameAs => origin >= target,
            EnumManager.CompareType.SmallerThan => origin < target,
            EnumManager.CompareType.SmallerOrSameAs => origin <= target,
            EnumManager.CompareType.NotSame => origin != target,
            _ => false
        };
    }
}

/// <summary>
/// 현재 패턴의 타임라인 반복을 포함한 시도 횟수를 비교한다.
/// 반복할수록 난이도를 높이거나 일정 횟수 뒤 힌트를 표시하는 분기에 사용한다.
/// </summary>
[AddTypeMenu("Boss/Pattern Attempt Count")]
[Serializable]
public sealed class BossPatternAttemptCountDecision : StateDecision
{
    [Tooltip("현재 패턴 시도 횟수와 비교할 기준값.")]
    [SerializeField, Min(1)]
    private int _value = 1;

    [Tooltip("현재 패턴 시도 횟수와 기준값을 비교할 방법.")]
    [SerializeField]
    private EnumManager.CompareType _compareType = EnumManager.CompareType.BiggerOrSameAs;

    /// <summary>
    /// Runner의 현재 AttemptCount를 기준값과 비교한다.
    /// Runner가 없으면 false를 반환한다.
    /// </summary>
    public override bool Decide(StateController stateController)
    {
        if (stateController == null || !stateController.TryGetInterface(out IBossPatternRunner runner))
            return false;

        int origin = runner.AttemptCount;
        int target = Mathf.Max(1, _value);
        return _compareType switch
        {
            EnumManager.CompareType.SameAs => origin == target,
            EnumManager.CompareType.BiggerThan => origin > target,
            EnumManager.CompareType.BiggerOrSameAs => origin >= target,
            EnumManager.CompareType.SmallerThan => origin < target,
            EnumManager.CompareType.SmallerOrSameAs => origin <= target,
            EnumManager.CompareType.NotSame => origin != target,
            _ => false
        };
    }
}

/// <summary>
/// 보스전 전체에서 특정 패턴이 시작되거나 정상 클리어된 횟수를 비교한다.
/// 페이즈 재시작 뒤에도 유지되어 최대 실행 횟수와 반복 순환 분기에 사용할 수 있다.
/// </summary>
[AddTypeMenu("Boss/Pattern Battle Count")]
[Serializable]
public sealed class BossPatternCountDecision : StateDecision
{
    [Tooltip("보스전 전체 횟수를 검사할 BossPatternSO.")]
    [SerializeField, NaughtyAttributes.Expandable]
    private BossPatternSO _pattern;

    [Tooltip("State 진입 횟수와 정상 클리어 횟수 중 검사할 값.")]
    [SerializeField]
    private BossPatternCountType _countType = BossPatternCountType.Cleared;

    [Tooltip("현재 패턴 횟수와 비교할 기준값.")]
    [SerializeField, Min(0)]
    private int _value = 1;

    [Tooltip("현재 패턴 횟수와 기준값을 비교할 방법.")]
    [SerializeField]
    private EnumManager.CompareType _compareType = EnumManager.CompareType.BiggerOrSameAs;

    /// <summary>
    /// Runner의 보스전 전체 패턴 횟수를 읽어 공통 CompareType 규칙으로 비교한다.
    /// Runner가 등록되지 않았거나 패턴이 비어 있으면 false를 반환한다.
    /// </summary>
    public override bool Decide(StateController stateController)
    {
        if (stateController == null || _pattern == null ||
            !stateController.TryGetInterface(out IBossPatternRunner runner))
            return false;

        int origin = runner.GetPatternCount(_pattern, _countType);
        int target = Mathf.Max(0, _value);
        return _compareType switch
        {
            EnumManager.CompareType.SameAs => origin == target,
            EnumManager.CompareType.BiggerThan => origin > target,
            EnumManager.CompareType.BiggerOrSameAs => origin >= target,
            EnumManager.CompareType.SmallerThan => origin < target,
            EnumManager.CompareType.SmallerOrSameAs => origin <= target,
            EnumManager.CompareType.NotSame => origin != target,
            _ => false
        };
    }
}

/// <summary>
/// 보스전 전체에 저장된 특정 특수행동의 미결정·성공·실패 결과를 검사한다.
/// 여러 Decision을 한 전이에 조합해 페이즈 진입 조건을 표현할 수 있다.
/// </summary>
[AddTypeMenu("Boss/Special Action Result")]
[Serializable]
public sealed class BossSpecialActionResultDecision : StateDecision
{
    [Tooltip("검사할 특수행동 논리 ID. 여러 패턴의 같은 ID는 결과를 공유한다.")]
    [SerializeField]
    private string _specialActionId = "SpecialAction";

    [Tooltip("전이가 요구하는 특수행동 결과.")]
    [SerializeField]
    private BossSpecialActionResult _expectedResult = BossSpecialActionResult.Success;

    /// <summary>
    /// SerializeReference 선택기에서 값을 Inspector로 지정하도록 비워 둔다.
    /// 조건부 패턴 자동 배선에서도 그대로 복사해 사용할 수 있다.
    /// </summary>
    public BossSpecialActionResultDecision() { }

    /// <summary>
    /// 자동 생성 조건에 특수행동 ID와 기대 결과를 직접 연결한다.
    /// 페이즈 참조 없이 보스전 전체 기록을 검사한다.
    /// </summary>
    public BossSpecialActionResultDecision(
        string specialActionId,
        BossSpecialActionResult expectedResult)
    {
        _specialActionId = specialActionId;
        _expectedResult = expectedResult;
    }

    /// <summary>
    /// Runner가 보스전 전체로 보관한 특수행동 결과를 기대값과 비교한다.
    /// 기록이 없으면 Unresolved로 비교된다.
    /// </summary>
    public override bool Decide(StateController stateController)
    {
        return stateController != null &&
               stateController.TryGetInterface(out IBossPatternRunner runner) &&
               runner.GetSpecialActionResult(_specialActionId) == _expectedResult;
    }
}

/// <summary>
/// 실패한 특수행동의 이유 ID를 검사해 이유별 패턴·연출 전이를 선택한다.
/// 결과가 Failure가 아니거나 아직 결정되지 않았다면 false를 반환한다.
/// </summary>
[AddTypeMenu("Boss/Special Action Failure Reason")]
[Serializable]
public sealed class BossSpecialActionFailureReasonDecision : StateDecision
{
    [Tooltip("실패 결과를 검사할 특수행동 논리 ID. 여러 패턴의 같은 ID는 결과를 공유한다.")]
    [SerializeField]
    private string _specialActionId = "SpecialAction";

    [Tooltip("Resolve Action 또는 자동 Ignored 처리가 기록한 실패 이유 ID.")]
    [SerializeField]
    private string _failureReasonId = "IncorrectResponse";

    /// <summary>
    /// Runner의 최신 실패 이유를 공백이 정리된 ID끼리 비교한다.
    /// 이후 성공으로 승격된 특수행동은 빈 이유를 반환하므로 일치하지 않는다.
    /// </summary>
    public override bool Decide(StateController stateController)
    {
        if (stateController == null || !stateController.TryGetInterface(out IBossPatternRunner runner))
            return false;

        return runner.GetSpecialActionFailureReason(_specialActionId) ==
               BossPatternSO.NormalizeFailureReasonId(_failureReasonId);
    }
}

/// <summary>
/// 보스전 전체에서 성공 또는 실패한 특수행동 개수를 기준값과 비교한다.
/// 개별 ID보다 결과 개수 자체가 분기를 정할 때 설정 수를 줄인다.
/// </summary>
[AddTypeMenu("Boss/Special Action Result Count")]
[Serializable]
public sealed class BossSpecialActionResultCountDecision : StateDecision
{
    [Tooltip("개수를 셀 성공 또는 실패 결과.")]
    [SerializeField]
    private BossSpecialActionResult _result = BossSpecialActionResult.Success;

    [Tooltip("현재 결과 개수와 비교할 기준값.")]
    [SerializeField, Min(0)]
    private int _value = 1;

    [Tooltip("현재 결과 개수와 기준값을 비교할 방법.")]
    [SerializeField]
    private EnumManager.CompareType _compareType = EnumManager.CompareType.BiggerOrSameAs;

    /// <summary>
    /// Runner의 전역 결과 개수를 읽고 프로젝트 공통 CompareType 규칙으로 비교한다.
    /// Runner가 없으면 false를 반환한다.
    /// </summary>
    public override bool Decide(StateController stateController)
    {
        if (stateController == null || !stateController.TryGetInterface(out IBossPatternRunner runner))
            return false;

        int origin = runner.CountSpecialActionResults(_result);
        int target = Mathf.Max(0, _value);
        return _compareType switch
        {
            EnumManager.CompareType.SameAs => origin == target,
            EnumManager.CompareType.BiggerThan => origin > target,
            EnumManager.CompareType.BiggerOrSameAs => origin >= target,
            EnumManager.CompareType.SmallerThan => origin < target,
            EnumManager.CompareType.SmallerOrSameAs => origin <= target,
            EnumManager.CompareType.NotSame => origin != target,
            _ => false
        };
    }
}

/// <summary>
/// 현재 페이즈 실행에서 특정 PatternSO가 아직 미실행인지 또는 어떤 결과인지 검사한다.
/// PhaseStart·Conditional 패턴의 1회 실행 보호와 지역 순차 흐름에 사용한다.
/// </summary>
[AddTypeMenu("Boss/Phase Pattern Result")]
[Serializable]
public sealed class BossPhasePatternResultDecision : StateDecision
{
    [Tooltip("검사할 Boss Phase. 비워두면 현재 페이즈를 사용한다.")]
    [SerializeField, NaughtyAttributes.Expandable]
    private StateMachineSO _phase;

    [Tooltip("현재 페이즈 실행 이력을 검사할 PatternSO.")]
    [SerializeField, NaughtyAttributes.Expandable]
    private BossPatternSO _pattern;

    [Tooltip("전이가 요구하는 패턴 실행 결과. None은 현재 페이즈에서 미실행임을 뜻한다.")]
    [SerializeField]
    private BossPatternRunResult _expectedResult = BossPatternRunResult.None;

    /// <summary>
    /// SerializeReference 선택기에서 참조를 Inspector로 지정하도록 비워 둔다.
    /// 자동 진입 배선은 매개변수 생성자를 사용한다.
    /// </summary>
    public BossPhasePatternResultDecision() { }

    /// <summary>
    /// 자동 진입 조건에 페이즈, 패턴과 기대 실행 결과를 연결한다.
    /// 기본 진입 보호에서는 None을 사용한다.
    /// </summary>
    public BossPhasePatternResultDecision(
        StateMachineSO phase,
        BossPatternSO pattern,
        BossPatternRunResult expectedResult)
    {
        _phase = phase;
        _pattern = pattern;
        _expectedResult = expectedResult;
    }

    /// <summary>
    /// Runner의 페이즈 지역 패턴 실행 이력을 기대 결과와 비교한다.
    /// 다른 페이즈 또는 미실행 패턴은 None으로 처리한다.
    /// </summary>
    public override bool Decide(StateController stateController)
    {
        return stateController != null &&
               stateController.TryGetInterface(out IBossPatternRunner runner) &&
               runner.GetPhasePatternResult(_phase, _pattern) == _expectedResult;
    }
}

/// <summary>
/// PatternSO의 페이즈 내 반복 허용 설정과 현재 실행 이력을 함께 검사한다.
/// 자동 생성된 PhaseStart·Conditional 진입 전이가 Inspector 설정 변경을 즉시 따른다.
/// </summary>
[AddTypeMenu("Boss/Pattern Entry Available")]
[Serializable]
public sealed class BossPatternEntryAvailableDecision : StateDecision
{
    [Tooltip("패턴 실행 이력을 보관하는 Boss Phase.")]
    [SerializeField, NaughtyAttributes.Expandable]
    private StateMachineSO _phase;

    [Tooltip("반복 허용 설정과 실행 전 여부를 검사할 PatternSO.")]
    [SerializeField, NaughtyAttributes.Expandable]
    private BossPatternSO _pattern;

    [Tooltip("false면 자동 배선 식별용으로만 사용하고 Pattern 실행 이력을 검사하지 않는다.")]
    [SerializeField, HideInInspector]
    private bool _requireAvailability = true;

    public StateMachineSO Phase => _phase;
    public BossPatternSO Pattern => _pattern;

    /// <summary>
    /// SerializeReference 선택기로 생성할 때 참조를 Inspector에서 지정하도록 비워 둔다.
    /// 자동 진입 배선은 매개변수 생성자를 사용한다.
    /// </summary>
    public BossPatternEntryAvailableDecision() { }

    /// <summary>
    /// 자동 진입 전이에 검사할 Phase와 PatternSO를 직접 연결한다.
    /// PatternSO의 Allow Repeat 변경은 Decision 재생성 없이 반영된다.
    /// </summary>
    public BossPatternEntryAvailableDecision(
        StateMachineSO phase,
        BossPatternSO pattern,
        bool requireAvailability = true)
    {
        _phase = phase;
        _pattern = pattern;
        _requireAvailability = requireAvailability;
    }

    /// <summary>
    /// 반복 허용 패턴은 항상 true, 1회성 패턴은 현재 페이즈에서 미실행일 때만 true다.
    /// Runner 또는 Pattern 참조가 없으면 false를 반환한다.
    /// </summary>
    public override bool Decide(StateController stateController)
    {
        if (_pattern == null || stateController == null ||
            !stateController.TryGetInterface(out IBossPatternRunner runner))
            return false;

        return !_requireAvailability || _pattern.AllowRepeatWithinPhase ||
               runner.GetPhasePatternResult(_phase, _pattern) == BossPatternRunResult.None;
    }
}

/// <summary>
/// 조건부 패턴의 빈 Decision이 즉시 전이되는 것을 막는 안전한 자리표시자다.
/// 그래프나 EditorWindow에서 실제 특수행동·거리·게임 조건으로 교체한다.
/// </summary>
[AddTypeMenu("Boss/Unconfigured Pattern Entry (Always False)")]
[Serializable]
public sealed class UnconfiguredBossPatternEntryDecision : StateDecision
{
    [Tooltip("이 자리표시자를 어떤 실제 조건으로 교체해야 하는지 적는 제작 메모.")]
    [SerializeField, TextArea(1, 3)]
    private string _note = "실제 패턴 진입 조건으로 교체하세요.";

    public string Note => _note;

    /// <summary>
    /// 미설정 전이가 게임 시작 직후 실행되지 않도록 항상 false를 반환한다.
    /// 실제 Decision을 지정하면 이 객체는 제거한다.
    /// </summary>
    public override bool Decide(StateController stateController)
    {
        return false;
    }
}
