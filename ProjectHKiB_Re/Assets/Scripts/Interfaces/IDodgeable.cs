using UnityEngine;

public interface IDodgeableBase
{
    public float BaseDodgeCooltime { get; set; }
    public float InitialDodgeMaxDistance { get; set; }
    public float BaseDodgeSpeed { get; set; }
    public int BaseContinuousDodgeLimit { get; set; }
    public LayerMask KeepDodgeWallLayer { get; set; }
    public float BaseKeepDodgeMaxTime { get; set; }
    public float BaseDodgeInvincibleTime { get; set; }
    public ParticlePlayer KeepDodgeParticle { get; set; }
    public StatBuffCompilation JustDodgeBuff { get; set; }
}

public interface IDodgeable : IDodgeableBase, IInitializable
{
    public bool CanDodge { get; set; }
    public int DodgeCount { get; set; }
    public bool CanCounterAttack { get; set; }
    public void StartDodge();
    public void StartKeepDodge();
    public bool CheckKeepDodgeEnded();
    public void EndDodge();
}