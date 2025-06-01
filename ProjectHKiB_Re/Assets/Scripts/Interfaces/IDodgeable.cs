using UnityEngine;

public interface IDodgeable
{
    public float DodgeCooltime { get; set; }
    public float InitialDodgeMaxDistance { get; set; }
    public float DodgeSpeed { get; set; }
    public int ContinuousDodgeLimit { get; set; }
    public LayerMask KeepDodgeWallLayer { get; set; }
    public float KeepDodgeMaxTime { get; set; }
    //public StatContainer KeepDodgeMaxDistance { get; set; }
    public float DodgeInvincibleTime { get; set; }
    public DodgeController DodgeController { get; set; }
    public ParticlePlayer KeepDodgeParticle { get; set; }

    public void StartKeepDodge() => DodgeController.StartKeepDodge(KeepDodgeMaxTime, KeepDodgeParticle.GetInstanceID());

    public bool IsKeepDodgeEnded() => DodgeController.CheckKeepDodgeLimit();

    public bool CanDodge() => DodgeController.CanDodge;

    public void StartDodge() => DodgeController.StartDodge(DodgeInvincibleTime);

    public void EndDodge() => DodgeController.EndDodge(DodgeCooltime, ContinuousDodgeLimit);
}