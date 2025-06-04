using UnityEngine;

public interface IDodgeable
{
    public float BaseDodgeCooltime { get; set; }
    public float InitialDodgeMaxDistance { get; set; }
    public float BaseDodgeSpeed { get; set; }
    public int BaseContinuousDodgeLimit { get; set; }
    public LayerMask KeepDodgeWallLayer { get; set; }
    public float BaseKeepDodgeMaxTime { get; set; }
    //public StatContainer KeepDodgeMaxDistance { get; set; }
    public float BaseDodgeInvincibleTime { get; set; }
    public DodgeController DodgeController { get; set; }
    public ParticlePlayer KeepDodgeParticle { get; set; }
    public bool CanCounterAttack { get => DodgeController.CanCounterAttack; }
    public StatBuffCompilation JustDodgeBuff { get; set; }

    public void StartKeepDodge() => DodgeController.StartKeepDodge(BaseKeepDodgeMaxTime, KeepDodgeParticle.GetInstanceID());

    public bool IsKeepDodgeEnded() => DodgeController.CheckKeepDodgeLimit();

    public bool CanDodge() => DodgeController.CanDodge;

    public void StartDodge() => DodgeController.StartDodge(BaseDodgeInvincibleTime);

    public void EndDodge() => DodgeController.EndDodge(BaseDodgeCooltime, BaseContinuousDodgeLimit);
}