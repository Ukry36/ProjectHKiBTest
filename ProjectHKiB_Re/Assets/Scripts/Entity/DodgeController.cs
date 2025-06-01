using UnityEngine;

public class DodgeController : MonoBehaviour
{
    private Cooltime DodgeCooltime { get; set; }
    private Cooltime DodgeCountReset { get; set; }
    private Cooltime KeepDodgeMaxTime { get; set; }
    private Cooltime DodgeInvincibleTime { get; set; }
    public int DodgeCount { get; set; } = 0;
    public bool CanDodge { get; private set; } = true;
    private ParticlePlayer _currentKeepDodgeParticle;

    public void Awake()
    {
        DodgeCooltime = new();
        DodgeCountReset = new();
        KeepDodgeMaxTime = new();
        DodgeInvincibleTime = new();
    }

    public void ResetDodgeCount() => DodgeCount = 0;

    public void StartDodge(float invincibleTime)
    {
        DodgeInvincibleTime.StartCooltime(invincibleTime);
        DodgeCount++;
    }

    public void StartKeepDodge(float keepDodgeMaxTime, int particlePlayerID)
    {
        KeepDodgeMaxTime.StartCooltime(keepDodgeMaxTime);
        _currentKeepDodgeParticle = GameManager.instance.particleManager.PlayParticle(particlePlayerID, this.transform, true);
    }

    public bool CheckKeepDodgeLimit()
    {
        return KeepDodgeMaxTime.IsCooltimeEnded;
    }

    public void EndDodge(float dodgeCooltime, int continuousDodgeLimit)
    {
        GameManager.instance.particleManager.StopPlaying(_currentKeepDodgeParticle.GetInstanceID());
        if (DodgeCount >= continuousDodgeLimit)
        {
            CanDodge = false;
            DodgeCooltime.StartCooltime(dodgeCooltime, () => CanDodge = true);
        }
        else
        {
            DodgeCountReset.StartCooltime(dodgeCooltime, ResetDodgeCount);
        }
        DodgeInvincibleTime.CancelCooltime();
        KeepDodgeMaxTime.CancelCooltime();
    }
}