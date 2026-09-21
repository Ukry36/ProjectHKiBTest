using System;
using StateMachine;
using UnityEngine;

/// <summary>
/// 새 보스전 시작 시 이전 실행에서 남은 모든 보스 진행 기록을 초기화한다.
/// 메인 StateMachine의 최초 진입 State에서 한 번만 실행한다.
/// </summary>
[AddTypeMenu("Boss/Reset Boss Battle Progress")]
[Serializable]
public sealed class ResetBossBattleProgressAction : StateAction
{
    /// <summary>
    /// 등록된 BossPatternRunner의 특수행동, 패턴 클리어와 페이즈 지역 이력을 모두 지운다.
    /// Runner가 없으면 설정 오류만 기록한다.
    /// </summary>
    public override void Act(StateController stateController)
    {
        if (TryGetRunner(stateController, out IBossPatternRunner runner))
            runner.ResetBattleProgress();
    }

    /// <summary>
    /// 공통 Runner 조회와 누락 오류 메시지를 제공한다.
    /// 다른 보스 Action은 각자의 문맥에 맞는 처리만 수행한다.
    /// </summary>
    private static bool TryGetRunner(
        StateController stateController,
        out IBossPatternRunner runner)
    {
        if (stateController != null && stateController.TryGetInterface(out runner)) return true;
        Debug.LogError("[ResetBossBattleProgressAction] BossPatternRunner가 없습니다.", stateController);
        runner = null;
        return false;
    }
}

/// <summary>
/// Boss Phase 진입 또는 재시작 시 페이즈 지역 패턴 실행 이력만 초기화한다.
/// 보스전 전체 특수행동과 패턴 클리어 기록은 다음 페이즈 조건을 위해 유지한다.
/// </summary>
[AddTypeMenu("Boss/Begin Boss Phase")]
[Serializable]
public sealed class BeginBossPhaseAction : StateAction
{
    [Tooltip("시작하거나 재시작할 Boss Phase StateMachineSO.")]
    [SerializeField, NaughtyAttributes.Expandable]
    private StateMachineSO _phase;

    public StateMachineSO Phase => _phase;

    /// <summary>
    /// SerializeReference 선택기로 생성할 때 Phase를 Inspector에서 지정하도록 비워 둔다.
    /// 자동 생성 도구는 매개변수 생성자를 사용한다.
    /// </summary>
    public BeginBossPhaseAction() { }

    /// <summary>
    /// 자동 생성한 Launch 또는 재시작 전이에 대상 Phase를 연결한다.
    /// Begin과 StartSubStateMachine Action의 참조를 일치시키는 데 사용한다.
    /// </summary>
    public BeginBossPhaseAction(StateMachineSO phase)
    {
        _phase = phase;
    }

    /// <summary>
    /// Runner에 페이즈 시작을 알려 지역 패턴 실행 이력만 초기화한다.
    /// 특수행동 결과와 전역 패턴 클리어 이력은 지우지 않는다.
    /// </summary>
    public override void Act(StateController stateController)
    {
        if (stateController != null && stateController.TryGetInterface(out IBossPatternRunner runner))
            runner.BeginPhase(_phase);
        else
            Debug.LogError("[BeginBossPhaseAction] BossPatternRunner가 없습니다.", stateController);
    }
}

/// <summary>
/// State에서 BossPatternSO 타임라인을 실행하도록 Runner에 요청한다.
/// 패턴 전용 State의 EnterActions에 배치하는 것을 기본 사용법으로 한다.
/// </summary>
[AddTypeMenu("Boss/Play Boss Pattern")]
[Serializable]
public sealed class PlayBossPatternAction : StateAction
{
    [Tooltip("실행할 보스 패턴 타임라인.")]
    [SerializeField, NaughtyAttributes.Expandable]
    private BossPatternSO _pattern;

    public BossPatternSO Pattern => _pattern;

    /// <summary>
    /// Unity SerializeReference 선택기가 빈 Action을 생성할 때 사용한다.
    /// 실행할 패턴은 Inspector에서 지정한다.
    /// </summary>
    public PlayBossPatternAction() { }

    /// <summary>
    /// 에디터 자동 생성에서 실행할 BossPatternSO를 지정한다.
    /// 생성된 Pattern State의 EnterActions에 바로 사용할 수 있다.
    /// </summary>
    public PlayBossPatternAction(BossPatternSO pattern)
    {
        _pattern = pattern;
    }

    /// <summary>
    /// Runner가 현재 StateMachine을 페이즈로 알고 있는지 보장한 뒤 패턴을 시작한다.
    /// Runner 누락은 설정 오류로 기록한다.
    /// </summary>
    public override void Act(StateController stateController)
    {
        if (stateController == null || !stateController.TryGetInterface(out IBossPatternRunner runner))
        {
            Debug.LogError("[PlayBossPatternAction] BossPatternRunner가 없습니다.", stateController);
            return;
        }

        if (runner.CurrentPhase != stateController.StateMachine)
            runner.BeginPhase(stateController.StateMachine);
        runner.Play(_pattern);
    }
}

/// <summary>
/// 현재 실행 중인 패턴의 남은 타임라인을 취소한다.
/// 그로기·페이즈 종료·사망처럼 예약 Action을 즉시 버려야 할 때 사용한다.
/// </summary>
[AddTypeMenu("Boss/Cancel Boss Pattern")]
[Serializable]
public sealed class CancelBossPatternAction : StateAction
{
    /// <summary>
    /// Runner가 등록돼 있다면 현재 패턴을 취소한다.
    /// 실행 중인 패턴이 없으면 아무 작업도 하지 않는다.
    /// </summary>
    public override void Act(StateController stateController)
    {
        if (stateController != null && stateController.TryGetInterface(out IBossPatternRunner runner))
            runner.Cancel();
    }
}

/// <summary>
/// 현재 패턴에 정의된 특수행동 횟수를 증가시킨다.
/// 패링 성공·유도 성공처럼 올바른 행동이 확정된 State에서 호출한다.
/// </summary>
[AddTypeMenu("Boss/Report Special Action Progress")]
[Serializable]
public sealed class ReportBossSpecialActionProgressAction : StateAction
{
    [Tooltip("특수행동을 보고할 패턴. 비워두면 현재 실행 중인 패턴을 사용한다.")]
    [SerializeField, NaughtyAttributes.Expandable]
    private BossPatternSO _pattern;

    [Tooltip("BossPatternSO Special Actions에 정의한 논리 ID. 여러 패턴에서 누적할 때 같은 ID를 사용한다.")]
    [SerializeField]
    private string _specialActionId = "SpecialAction";

    [Tooltip("한 번 실행할 때 더할 특수행동 진행 횟수.")]
    [SerializeField, Min(1)]
    private int _amount = 1;

    /// <summary>
    /// Runner에 특수행동 ID와 증가량을 전달한다.
    /// 필요 횟수에 도달하면 Runner가 해당 특수행동을 성공으로 확정한다.
    /// </summary>
    public override void Act(StateController stateController)
    {
        if (stateController != null && stateController.TryGetInterface(out IBossPatternRunner runner))
            runner.ReportSpecialActionProgress(_pattern, _specialActionId, Mathf.Max(1, _amount));
    }
}

/// <summary>
/// StateDecision 결과를 관찰하고 false에서 true가 된 순간에만 특수행동 횟수를 증가시킨다.
/// 여러 프레임 지속되는 충돌·그리드 위치 조건을 UpdateActions에서 안전하게 누적한다.
/// </summary>
[AddTypeMenu("Boss/Observe Special Action Condition")]
[Serializable]
public sealed class ObserveBossSpecialActionConditionAction : StateAction
{
    [Tooltip("특수행동을 보고할 패턴. 비워두면 현재 실행 중인 패턴을 사용한다.")]
    [SerializeField, NaughtyAttributes.Expandable]
    private BossPatternSO _pattern;

    [Tooltip("BossPatternSO Special Actions에 정의한 논리 ID. 여러 패턴에서 누적할 때 같은 ID를 사용한다.")]
    [SerializeField]
    private string _specialActionId = "SpecialAction";

    [Tooltip("같은 특수행동을 관찰하는 여러 조건을 구분할 키. 비우면 특수행동 ID를 사용한다.")]
    [SerializeField]
    private string _signalKey;

    [Tooltip("false에서 true가 될 때 더할 특수행동 진행 횟수.")]
    [SerializeField, Min(1)]
    private int _amount = 1;

    [Tooltip("매 프레임 관찰할 조건. 공격 적중, 거리 또는 그리드 관계 Decision을 지정한다.")]
    [SerializeReference, SubclassSelector]
    private StateDecision _condition;

    /// <summary>
    /// 조건 현재값을 Runner에 전달하여 상승 에지에서만 진행도를 한 번 보고한다.
    /// Pattern State의 UpdateActions에 배치해야 false 복귀도 관찰할 수 있다.
    /// </summary>
    public override void Act(StateController stateController)
    {
        if (stateController == null || !stateController.TryGetInterface(out IBossPatternRunner runner))
            return;

        runner.ObserveSpecialActionSignal(
            _pattern,
            _specialActionId,
            _signalKey,
            _condition != null && _condition.Decide(stateController),
            Mathf.Max(1, _amount));
    }
}

/// <summary>
/// 한 특수행동을 성공 또는 구체적인 이유가 있는 실패로 명시적으로 기록한다.
/// 패턴 클리어는 기본적으로 건드리지 않아 두 개념을 독립적으로 구성할 수 있다.
/// </summary>
[AddTypeMenu("Boss/Resolve Special Action")]
[Serializable]
public sealed class ResolveBossSpecialActionAction : StateAction
{
    [Tooltip("결과를 기록할 특수행동을 소유한 패턴. 비워두면 현재 패턴을 사용한다.")]
    [SerializeField, NaughtyAttributes.Expandable]
    private BossPatternSO _pattern;

    [Tooltip("BossPatternSO Special Actions에 정의한 논리 ID. 여러 패턴에서 누적할 때 같은 ID를 사용한다.")]
    [SerializeField]
    private string _specialActionId = "SpecialAction";

    [Tooltip("기록할 특수행동의 성공 또는 실패 결과.")]
    [SerializeField]
    private BossSpecialActionResult _result = BossSpecialActionResult.Success;

    [Tooltip("Failure일 때 이유별 패턴·행동·이펙트 분기에 사용할 ID.")]
    [SerializeField, NaughtyAttributes.ShowIf(nameof(IsFailure))]
    private string _failureReasonId = "IncorrectResponse";

    [Tooltip("결과 기록 직후 현재 패턴도 강제로 클리어한다. 일반적으로는 끄고 별도 조건을 사용한다.")]
    [SerializeField]
    private bool _completePattern;

    private bool IsFailure => _result == BossSpecialActionResult.Failure;

    /// <summary>
    /// Runner에 지정 특수행동 결과를 기록하고 선택적으로 패턴 클리어를 요청한다.
    /// 실패도 특수행동 결과일 뿐 패턴 Failed 결과로 바꾸지 않는다.
    /// </summary>
    public override void Act(StateController stateController)
    {
        if (stateController == null || !stateController.TryGetInterface(out IBossPatternRunner runner))
            return;

        runner.ResolveSpecialAction(_pattern, _specialActionId, _result, _failureReasonId);
        if (_completePattern) runner.Complete(_pattern);
    }
}

/// <summary>
/// 현재 패턴 타임라인을 진행도 손실 없이 처음부터 다시 시작한다.
/// 수동 재시도 연출 뒤 같은 패턴을 반복할 때 사용한다.
/// </summary>
[AddTypeMenu("Boss/Restart Boss Pattern")]
[Serializable]
public sealed class RestartBossPatternAction : StateAction
{
    [Tooltip("재시작할 패턴. 비워두면 현재 실행 중인 패턴을 사용한다.")]
    [SerializeField, NaughtyAttributes.Expandable]
    private BossPatternSO _pattern;

    /// <summary>
    /// 현재 패턴의 타임라인과 조건 상승 에지 상태만 초기화한다.
    /// 이미 누적한 특수행동 진행도와 결과는 유지한다.
    /// </summary>
    public override void Act(StateController stateController)
    {
        if (stateController != null && stateController.TryGetInterface(out IBossPatternRunner runner))
            runner.Restart(_pattern);
    }
}

/// <summary>
/// 외부 퍼즐·컷신 시스템이 현재 패턴의 정상 클리어를 확정한다.
/// PatternSO의 RecoveryDuration은 유지하여 다음 패턴 전 준비 시간을 제공한다.
/// </summary>
[AddTypeMenu("Boss/Complete Boss Pattern")]
[Serializable]
public sealed class CompleteBossPatternAction : StateAction
{
    [Tooltip("클리어할 패턴. 비워두면 현재 실행 중인 패턴을 사용한다.")]
    [SerializeField, NaughtyAttributes.Expandable]
    private BossPatternSO _pattern;

    /// <summary>
    /// 실행 중인 패턴이 요청 대상과 일치하면 정상 완료 후딜레이를 시작한다.
    /// 특수행동 결과와 별개로 Pattern Cleared 기록을 남긴다.
    /// </summary>
    public override void Act(StateController stateController)
    {
        if (stateController != null && stateController.TryGetInterface(out IBossPatternRunner runner))
            runner.Complete(_pattern);
    }
}

/// <summary>
/// 특수행동 실패가 아닌 패턴 실행 자체를 Failed 결과로 종료한다.
/// 취소·스크립트 오류 같은 별도 복구 State가 필요할 때만 사용한다.
/// </summary>
[AddTypeMenu("Boss/Fail Boss Pattern")]
[Serializable]
public sealed class FailBossPatternAction : StateAction
{
    [Tooltip("실패로 종료할 패턴. 비워두면 현재 실행 중인 패턴을 사용한다.")]
    [SerializeField, NaughtyAttributes.Expandable]
    private BossPatternSO _pattern;

    /// <summary>
    /// Runner에 패턴 자체의 실패 종료를 요청한다.
    /// 특정 특수행동 실패는 ResolveBossSpecialActionAction을 사용해야 한다.
    /// </summary>
    public override void Act(StateController stateController)
    {
        if (stateController != null && stateController.TryGetInterface(out IBossPatternRunner runner))
            runner.Fail(_pattern);
    }
}

/// <summary>
/// BranchAction에 보스전 전체 패턴 시작·클리어 횟수를 정수로 제공한다.
/// 같은 패턴의 실행 차수에 따라 공격 방향이나 연출 Action을 바꿀 때 사용한다.
/// </summary>
[AddTypeMenu("Boss Pattern Battle Count")]
[Serializable]
public sealed class BossPatternCountBranchIntSource : BranchIntSource
{
    [Tooltip("BranchAction에 횟수를 제공할 BossPatternSO.")]
    [SerializeField, NaughtyAttributes.Expandable]
    private BossPatternSO _pattern;

    [Tooltip("State 진입 횟수와 정상 클리어 횟수 중 BranchAction에 제공할 값.")]
    [SerializeField]
    private BossPatternCountType _countType = BossPatternCountType.Started;

    /// <summary>
    /// 등록된 Runner에서 보스전 전체 패턴 횟수를 읽는다.
    /// Runner 또는 패턴이 없으면 0과 false를 반환해 분기 실행을 중단한다.
    /// </summary>
    public override bool TryGetValue(StateController stateController, out int value)
    {
        value = 0;
        if (stateController == null || _pattern == null ||
            !stateController.TryGetInterface(out IBossPatternRunner runner))
            return false;

        value = runner.GetPatternCount(_pattern, _countType);
        return true;
    }
}
