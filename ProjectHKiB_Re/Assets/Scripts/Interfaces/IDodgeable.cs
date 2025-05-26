using UnityEngine;

public interface IDodgeable
{
    public Cooltime DodgeCooltime { get; set; }
    public StatContainer InitialDodgeMaxDistance { get; set; }
    public StatContainer DodgeSpeed { get; set; }
    public StatContainer ContinuousDodgeLimit { get; set; }
    public LayerMask KeepDodgeWallLayer { get; set; }
    public Cooltime KeepDodgeMaxTime { get; set; }
    public StatContainer KeepDodgeMaxDistance { get; set; }
    public Cooltime DodgeInvincibleTime { get; set; }
    public int TotalDodgeCount { get; set; }

    public void StartKeepDodge() => KeepDodgeMaxTime.StartCooltime();

    public bool IsKeepDodgeEnded() => KeepDodgeMaxTime.cooltimeEnded;

    public bool CanDodge() => DodgeCooltime.cooltimeEnded;

    public void StartDodge()
    {
        DodgeInvincibleTime.StartCooltime();
        TotalDodgeCount++;
    }

    public void EndDodge()
    {
        if (TotalDodgeCount % (int)ContinuousDodgeLimit.Value == 0)
            DodgeCooltime.StartCooltime();
        DodgeInvincibleTime.CancelCooltime();
        KeepDodgeMaxTime.CancelCooltime();
    }
}