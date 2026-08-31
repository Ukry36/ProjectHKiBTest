using System;
using StateMachine;
using UnityEngine;

/// <summary>
/// State에서 BossPatternSO 타임라인을 실행하도록 BossPatternRunner에 요청한다.
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
    /// Unity의 SerializeReference 타입 선택기가 빈 Action을 생성할 때 사용하는 생성자다.
    /// 실행할 패턴은 인스펙터에서 지정한다.
    /// </summary>
    public PlayBossPatternAction() { }

    /// <summary>
    /// 에디터 자동 생성에서 실행할 BossPatternSO를 지정한다.
    /// Unity 직렬화용 기본 생성은 매개변수 없는 생성자를 계속 사용할 수 있다.
    /// </summary>
    public PlayBossPatternAction(BossPatternSO pattern)
    {
        _pattern = pattern;
    }

    /// <summary>
    /// 실행 주체의 IBossPatternRunner를 찾아 지정한 패턴을 시작한다.
    /// 모듈이 없으면 설정 오류를 기록하고 다른 Action 실행은 막지 않는다.
    /// </summary>
    public override void Act(StateController stateController)
    {
        if (stateController == null ||
            !stateController.TryGetInterface(out IBossPatternRunner runner))
        {
            Debug.LogError(
                "[PlayBossPatternAction] BossPatternRunner가 없습니다. StateController 오브젝트에 모듈을 추가하세요.",
                stateController);
            return;
        }

        runner.Play(_pattern);
    }
}

/// <summary>
/// 현재 실행 중인 BossPatternSO의 남은 타임라인을 취소한다.
/// 그로기·페이즈 종료·사망처럼 예약 Action을 즉시 버려야 할 때 사용한다.
/// </summary>
[AddTypeMenu("Boss/Cancel Boss Pattern")]
[Serializable]
public sealed class CancelBossPatternAction : StateAction
{
    /// <summary>
    /// 실행 주체에 BossPatternRunner가 등록돼 있다면 현재 패턴을 취소한다.
    /// 실행 중인 패턴이 없으면 아무 작업도 하지 않는다.
    /// </summary>
    public override void Act(StateController stateController)
    {
        if (stateController != null &&
            stateController.TryGetInterface(out IBossPatternRunner runner))
            runner.Cancel();
    }
}
