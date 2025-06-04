using System.Collections;
using UnityEngine;

public class DodgeController : MonoBehaviour
{
    private Cooltime _dodgeCooltime;
    private Cooltime _dodgeCountReset;
    private Cooltime _keepDodgeMaxTime;
    private Cooltime _dodgeInvincibleTime;
    private Cooltime _canJustDodgeTime;
    private Cooltime _canCounterAttackTime;
    public int DodgeCount { get; set; }
    public bool CanDodge { get; private set; } = true;
    public bool CanCounterAttack { get; private set; } = false;
    public ParticlePlayer dtemp;
    private ParticlePlayer _currentKeepDodgeParticle;
    [SerializeField] private StatBuffSO _invincibleBuff;
    [SerializeField] private StatBuffSO _superArmourBuff;
    [SerializeField] private HealthController _healthController;
    [SerializeField] private DodgeHelper _dodgeHelper;
    [SerializeField] private StatBuffController buffController;

    public void Awake()
    {
        _dodgeHelper.transform.parent = null;
        _dodgeHelper.gameObject.SetActive(false);
        _dodgeHelper.HealthController.OnDamaged += OnDamaged;
        _healthController.OnDamaged += OnDamaged;
        ResetDodgeCount();
        _dodgeCooltime = new();
        _dodgeCountReset = new();
        _keepDodgeMaxTime = new();
        _dodgeInvincibleTime = new();
        _canJustDodgeTime = new();
        _canCounterAttackTime = new();
    }

    public void OnDestroy()
    {
        _healthController.OnDamaged -= OnDamaged;
        _dodgeHelper.HealthController.OnDamaged -= OnDamaged;
    }

    public void ResetDodgeCount() => DodgeCount = 0;

    public void StartDodge(float invincibleTime)
    {
        _dodgeHelper.transform.position = transform.position;
        _dodgeHelper.gameObject.SetActive(true);
        _invincibleBuff.ApplyBuff(buffController);
        _superArmourBuff.ApplyBuff(buffController);
        _dodgeInvincibleTime.StartCooltime(invincibleTime, DisableDodgeInvincible);
        _canJustDodgeTime.StartCooltime(0.2f); // temp!!!
        DodgeCount++;
    }

    public void DisableDodgeInvincible()
    {
        _dodgeHelper.gameObject.SetActive(false);
        _invincibleBuff.RemoveBuff(buffController);
    }

    public void StartKeepDodge(float keepDodgeMaxTime, int particlePlayerID)
    {
        _keepDodgeMaxTime.StartCooltime(keepDodgeMaxTime);
        _currentKeepDodgeParticle = GameManager.instance.particleManager.PlayParticle(particlePlayerID, this.transform, true);
    }

    public bool CheckKeepDodgeLimit()
    {
        return _keepDodgeMaxTime.IsCooltimeEnded;
    }

    public void EndDodge(float dodgeCooltime, int continuousDodgeLimit)
    {
        DisableDodgeInvincible();
        GameManager.instance.particleManager.StopPlaying(_currentKeepDodgeParticle.GetInstanceID());
        if (DodgeCount >= continuousDodgeLimit)
        {
            CanDodge = false;
            _dodgeCooltime.StartCooltime(dodgeCooltime, () => CanDodge = true);
        }
        else
        {
            _dodgeCountReset.StartCooltime(dodgeCooltime, ResetDodgeCount);
        }
        _dodgeInvincibleTime.CancelCooltime();
        _keepDodgeMaxTime.CancelCooltime();
        _superArmourBuff.RemoveBuff(buffController);
    }

    public void OnDamaged()
    {
        if (!_canJustDodgeTime.IsCooltimeEnded)
        {
            GameManager.instance.particleManager.PlayParticleOneShot(dtemp.GetInstanceID(), transform);
            _canJustDodgeTime.CancelCooltime();
            _canCounterAttackTime.CancelCooltime();
            _canCounterAttackTime.StartCooltime(1, EndCounterAttackTime);
            buffController.GetInterface<IDodgeable>().JustDodgeBuff.EnableAllBuffs(buffController);
            CanCounterAttack = true;
        }
    }

    public void EndCounterAttackTime() => CanCounterAttack = false;
}