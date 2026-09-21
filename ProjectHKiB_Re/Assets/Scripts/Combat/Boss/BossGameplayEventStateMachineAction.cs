using System;
using Gameplay;
using StateMachine;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 게임플레이 사건에서 현재 보스가 Source인지 Target인지 선택한다.
/// 역할 판정에는 보조 오브젝트가 아닌 Root Owner를 사용한다.
/// </summary>
public enum BossGameplayEventRole
{
    Either = 0,
    Source = 1,
    Target = 2,
    Any = 3
}

/// <summary>
/// 선택적 State 조건이 사건의 어느 State 스냅샷을 검사할지 정한다.
/// 공격 시작 State와 사건 발생 State를 모두 지원한다.
/// </summary>
public enum GameplayEventStateSource
{
    None = 0,
    SourceOwnerAtStart = 1,
    SourceAtStart = 2,
    TargetOwnerAtEvent = 3,
    TargetAtEvent = 4,
    SourceOwnerAtEvent = 5,
    SourceAtEvent = 6
}

/// <summary>
/// 보스와 관련된 새 게임플레이 사건을 SpecialAction 진행도로 변환한다.
/// 사건 종류와 역할만으로 쓸 수 있고 필요할 때 ID·정의·State 하나를 추가로 거른다.
/// </summary>
[AddTypeMenu("Boss/Report Gameplay Event Special Action")]
[Serializable]
public class ReportBossGameplayEventSpecialAction : StateAction
{
    [Tooltip("특수행동을 보고할 패턴. 비워두면 현재 실행 중인 패턴을 사용한다.")]
    [SerializeField, NaughtyAttributes.Expandable]
    private BossPatternSO _pattern;

    [Tooltip("BossPatternSO Special Actions에 정의한 논리 ID. 여러 패턴에서 누적할 때 같은 ID를 사용한다.")]
    [SerializeField]
    private string _specialActionId = "SpecialAction";

    [Tooltip("같은 종류의 여러 관찰 Action이 소비 위치를 따로 가져야 할 때만 지정한다.")]
    [SerializeField]
    private string _signalKey;

    [Tooltip("사건 한 번이 더할 특수행동 진행 횟수.")]
    [SerializeField, Min(1)]
    private int _amount = 1;

    [Tooltip("집계할 게임플레이 사건 종류.")]
    [SerializeField, NaughtyAttributes.ShowIf(nameof(ShowsEventType))]
    private GameplayEventType _eventType = GameplayEventType.Parry;

    [Tooltip("보스가 사건에서 맡아야 하는 역할. 무관한 퍼즐 사건은 Any를 사용한다.")]
    [SerializeField]
    private BossGameplayEventRole _bossRole = BossGameplayEventRole.Source;

    [Tooltip("비워두면 같은 종류의 모든 사건을 허용하고, 지정하면 같은 Event ID만 집계한다.")]
    [SerializeField]
    private string _requiredEventId;

    [Tooltip("비워두면 모든 정의를 허용한다. 공격 정의, DamageData, GameEvent 등을 직접 지정할 수 있다.")]
    [SerializeField, FormerlySerializedAs("_requiredAttack")]
    private UnityEngine.Object _requiredDefinition;

    [Tooltip("선택적으로 검사할 사건 순간의 State 스냅샷 종류.")]
    [SerializeField]
    private GameplayEventStateSource _stateSource;

    [Tooltip("선택한 State Source가 이 StateSO와 같을 때만 집계한다.")]
    [SerializeField, NaughtyAttributes.ShowIf(nameof(UsesStateFilter)), NaughtyAttributes.Expandable]
    private StateSO _requiredState;

    protected virtual bool ShowsEventType => true;
    protected virtual GameplayEventType EventType => _eventType;
    private bool UsesStateFilter => _stateSource != GameplayEventStateSource.None;

    /// <summary>
    /// 범용 사건 Action을 기본 Inspector 값으로 생성한다.
    /// SerializeReference 선택 메뉴가 사용하는 공개 생성자다.
    /// </summary>
    public ReportBossGameplayEventSpecialAction()
    {
    }

    /// <summary>
    /// 호환용 특화 Action이 사건 종류와 기본 SpecialAction ID를 고정해 생성한다.
    /// 기존 직렬화 값이 있으면 Unity가 생성 뒤 해당 값을 복원한다.
    /// </summary>
    protected ReportBossGameplayEventSpecialAction(
        GameplayEventType eventType,
        string specialActionId)
    {
        _eventType = eventType;
        _specialActionId = specialActionId;
    }

    /// <summary>
    /// 아직 소비하지 않은 사건을 순서대로 검사하고 일치할 때마다 진행도를 보고한다.
    /// 소비 위치는 BossPatternRunner가 보관하여 공유 StateSO에 런타임 값이 남지 않는다.
    /// </summary>
    public override void Act(StateController stateController)
    {
        if (stateController == null ||
            !stateController.TryGetInterface(out IBossPatternRunner runner) ||
            runner is not IGameplayEventReader events)
            return;

        string consumerKey = ResolveConsumerKey();
        while (events.TryConsumeGameplayEvent(consumerKey, out GameplayEvent gameplayEvent))
        {
            if (!Matches(stateController, gameplayEvent)) continue;
            runner.ReportSpecialActionProgress(
                _pattern,
                _specialActionId,
                Mathf.Max(1, _amount));
        }
    }

    /// <summary>
    /// 명시한 Signal Key를 우선 사용하고 없으면 특수행동 ID와 사건 종류를 조합한다.
    /// 서로 다른 사건 종류가 기본 설정에서 같은 읽기 위치를 공유하지 않게 한다.
    /// </summary>
    private string ResolveConsumerKey()
    {
        if (!string.IsNullOrWhiteSpace(_signalKey)) return _signalKey.Trim();
        return $"{_specialActionId}:{EventType}";
    }

    /// <summary>
    /// 사건 종류, 보스 역할과 선택적인 ID·정의·State 조건을 함께 검사한다.
    /// 비어 있는 선택 조건은 무시하여 흔한 구성은 설정 두세 개로 끝난다.
    /// </summary>
    private bool Matches(StateController boss, GameplayEvent gameplayEvent)
    {
        if (gameplayEvent.Type != EventType) return false;

        if (_bossRole == BossGameplayEventRole.Any) return MatchesOptionalFilters(gameplayEvent);

        bool isSource = gameplayEvent.SourceOwnerController == boss;
        bool isTarget = gameplayEvent.TargetOwnerController == boss;
        if (_bossRole == BossGameplayEventRole.Source && !isSource) return false;
        if (_bossRole == BossGameplayEventRole.Target && !isTarget) return false;
        if (_bossRole == BossGameplayEventRole.Either && !isSource && !isTarget) return false;

        return MatchesOptionalFilters(gameplayEvent);
    }

    /// <summary>
    /// 역할 판정 뒤 선택적인 ID·정의·State 조건만 검사한다.
    /// Any 역할도 같은 필터를 사용해 전역 퍼즐 사건을 안전하게 좁힐 수 있다.
    /// </summary>
    private bool MatchesOptionalFilters(GameplayEvent gameplayEvent)
    {
        if (!string.IsNullOrWhiteSpace(_requiredEventId) &&
            !string.Equals(gameplayEvent.EventId, _requiredEventId.Trim(), StringComparison.Ordinal))
            return false;
        if (_requiredDefinition != null && gameplayEvent.Definition != _requiredDefinition)
            return false;
        if (!UsesStateFilter) return true;

        StateSO observedState = _stateSource switch
        {
            GameplayEventStateSource.SourceOwnerAtStart => gameplayEvent.SourceOwnerStateAtStart,
            GameplayEventStateSource.SourceAtStart => gameplayEvent.SourceStateAtStart,
            GameplayEventStateSource.SourceOwnerAtEvent => gameplayEvent.SourceOwnerStateAtEvent,
            GameplayEventStateSource.SourceAtEvent => gameplayEvent.SourceStateAtEvent,
            GameplayEventStateSource.TargetOwnerAtEvent => gameplayEvent.TargetOwnerStateAtEvent,
            GameplayEventStateSource.TargetAtEvent => gameplayEvent.TargetStateAtEvent,
            _ => null
        };
        return observedState == _requiredState;
    }
}
