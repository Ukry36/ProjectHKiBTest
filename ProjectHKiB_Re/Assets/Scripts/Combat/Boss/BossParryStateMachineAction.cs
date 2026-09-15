using System;
using Gameplay;

/// <summary>
/// 기존 패링 전용 StateAction 직렬화 타입을 범용 사건 Action 위에 유지한다.
/// 새 구성에는 ReportBossGameplayEventSpecialAction 사용을 권장한다.
/// </summary>
[AddTypeMenu("Boss/Report Parry Special Action")]
[Serializable]
public sealed class ReportBossParrySpecialAction : ReportBossGameplayEventSpecialAction
{
    /// <summary>
    /// 새로 추가한 호환 Action도 기존 기본 ID인 Parry를 사용한다.
    /// 사건 종류는 Inspector에 노출하지 않고 항상 Parry로 고정한다.
    /// </summary>
    public ReportBossParrySpecialAction() : base(GameplayEventType.Parry, "Parry")
    {
    }

    protected override bool ShowsEventType => false;
    protected override GameplayEventType EventType => GameplayEventType.Parry;
}
