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

    public BossPatternSO Pattern => _pattern;

    /// <summary>
    /// 실행 주체의 BossPatternRunner가 Running 결과와 선택한 패턴을 가지는지 확인한다.
    /// 실행기가 등록되지 않았다면 false를 반환한다.
    /// </summary>
    public override bool Decide(StateController stateController)
    {
        return stateController != null &&
               stateController.TryGetInterface(out IBossPatternRunner runner) &&
               runner.Matches(_pattern, BossPatternRunResult.Running);
    }
}

/// <summary>
/// 선택한 BossPatternSO 또는 임의의 보스 패턴이 자연 완료됐는지 검사한다.
/// 패턴 전용 State에서 다음 패턴 State로 전이하는 기본 완료 조건이다.
/// </summary>
[AddTypeMenu("Boss/Is Boss Pattern Completed")]
[Serializable]
public sealed class IsBossPatternCompletedDecision : StateDecision
{
    [Tooltip("검사할 BossPatternSO. 비워두면 가장 최근 자연 완료된 모든 패턴을 허용한다.")]
    [SerializeField, NaughtyAttributes.Expandable]
    private BossPatternSO _pattern;

    public BossPatternSO Pattern => _pattern;

    /// <summary>
    /// Unity의 SerializeReference 타입 선택기가 빈 Decision을 생성할 때 사용하는 생성자다.
    /// 검사할 패턴은 인스펙터에서 지정한다.
    /// </summary>
    public IsBossPatternCompletedDecision() { }

    /// <summary>
    /// 에디터 자동 생성에서 완료를 검사할 BossPatternSO를 지정한다.
    /// Unity 직렬화용 기본 생성은 매개변수 없는 생성자를 계속 사용할 수 있다.
    /// </summary>
    public IsBossPatternCompletedDecision(BossPatternSO pattern)
    {
        _pattern = pattern;
    }

    /// <summary>
    /// 실행 주체의 BossPatternRunner가 Completed 결과와 선택한 패턴을 가지는지 확인한다.
    /// State 이탈로 취소된 패턴은 완료로 판단하지 않는다.
    /// </summary>
    public override bool Decide(StateController stateController)
    {
        return stateController != null &&
               stateController.TryGetInterface(out IBossPatternRunner runner) &&
               runner.Matches(_pattern, BossPatternRunResult.Completed);
    }
}

/// <summary>
/// 선택한 BossPatternSO 또는 임의의 보스 패턴이 중단됐는지 검사한다.
/// 중단 전용 후처리 State가 필요할 때 완료 조건과 분리해 사용할 수 있다.
/// </summary>
[AddTypeMenu("Boss/Is Boss Pattern Cancelled")]
[Serializable]
public sealed class IsBossPatternCancelledDecision : StateDecision
{
    [Tooltip("검사할 BossPatternSO. 비워두면 가장 최근 취소된 모든 패턴을 허용한다.")]
    [SerializeField, NaughtyAttributes.Expandable]
    private BossPatternSO _pattern;

    public BossPatternSO Pattern => _pattern;

    /// <summary>
    /// 실행 주체의 BossPatternRunner가 Cancelled 결과와 선택한 패턴을 가지는지 확인한다.
    /// 실행기가 등록되지 않았다면 false를 반환한다.
    /// </summary>
    public override bool Decide(StateController stateController)
    {
        return stateController != null &&
               stateController.TryGetInterface(out IBossPatternRunner runner) &&
               runner.Matches(_pattern, BossPatternRunResult.Cancelled);
    }
}
