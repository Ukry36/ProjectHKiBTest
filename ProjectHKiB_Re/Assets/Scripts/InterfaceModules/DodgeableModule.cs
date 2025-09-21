using System;
using UnityEngine;
namespace Assets.Scripts.Interfaces.Modules
{
    public class DodgeableModule : InterfaceModule, IDodgeable
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
        public int DodgeCount { get; set; }
        public bool CanDodge { get; set; }
        public bool CanCounterAttack { get; set; } = false;
        private Cooltime _dodgeCooltime;
        private Cooltime _dodgeCountReset;
        private Cooltime _keepDodgeMaxTime;
        private Cooltime _dodgeInvincibleTime;
        private Cooltime _canJustDodgeTime;
        private Cooltime _canCounterAttackTime;
        public ParticlePlayer dtemp;
        private ParticlePlayer _currentKeepDodgeParticle;
        [SerializeField] private StatBuffSO _invincibleBuff;
        [SerializeField] private StatBuffSO _superArmourBuff;
        [SerializeField] private DamagableModule _damagableModule;
        [SerializeField] private DodgeHelper _dodgeHelper;
        [SerializeField] private BuffableModule _buffableModule;
        public override void Register(IInterfaceRegistable interfaceRegistable)
        {
            interfaceRegistable.RegisterInterface<IDodgeable>(this);
        }

        public void Initialize()
        {
            _dodgeHelper.transform.parent = null;
            _dodgeHelper.gameObject.SetActive(false);
            ResetDodgeCount();
            _dodgeCooltime = new();
            _dodgeCountReset = new();
            _keepDodgeMaxTime = new();
            _dodgeInvincibleTime = new();
            _canJustDodgeTime = new();
            _canCounterAttackTime = new();
            CanCounterAttack = false;
            CanDodge = true;
        }

        public void Awake()
        {
            _dodgeHelper.damagableModule.OnDamaged += OnDamaged;
            _damagableModule.OnDamaged += OnDamaged;
        }
        public void OnDestroy()
        {
            _damagableModule.OnDamaged -= OnDamaged;
            _dodgeHelper.damagableModule.OnDamaged -= OnDamaged;
        }

        public void ResetDodgeCount() => DodgeCount = 0;

        public void StartDodge()
        {
            _dodgeHelper.transform.position = transform.position;
            _dodgeHelper.gameObject.SetActive(true);
            _buffableModule.Buff(_invincibleBuff);
            _buffableModule.Buff(_superArmourBuff);
            _dodgeInvincibleTime.StartCooltime(_invincibleBuff.BuffTime, DisableDodgeInvincible);
            _canJustDodgeTime.StartCooltime(0.2f); // temp!!!
            DodgeCount++;
        }

        public void DisableDodgeInvincible()
        {
            _dodgeHelper.gameObject.SetActive(false);
        }

        public void StartKeepDodge()
        {
            _keepDodgeMaxTime.StartCooltime(BaseKeepDodgeMaxTime);
            _currentKeepDodgeParticle = GameManager.instance.particleManager.
            PlayParticle(KeepDodgeParticle.GetInstanceID(), this.transform, true);
        }

        public bool CheckKeepDodgeEnded() => _keepDodgeMaxTime.IsCooltimeEnded;

        public void EndDodge()
        {
            DisableDodgeInvincible();
            GameManager.instance.particleManager.StopPlaying(_currentKeepDodgeParticle.GetInstanceID());
            if (DodgeCount >= BaseContinuousDodgeLimit)
            {
                CanDodge = false;
                _dodgeCooltime.StartCooltime(BaseDodgeCooltime, () => CanDodge = true);
            }
            else
            {
                _dodgeCountReset.StartCooltime(BaseDodgeCooltime, ResetDodgeCount);
            }
            _dodgeInvincibleTime.CancelCooltime();
            _keepDodgeMaxTime.CancelCooltime();
            _buffableModule.UnBuff(_superArmourBuff);
            _buffableModule.UnBuff(_invincibleBuff);
        }

        public void OnDamaged()
        {
            if (!_canJustDodgeTime.IsCooltimeEnded)
            {
                GameManager.instance.particleManager.PlayParticleOneShot(dtemp.GetInstanceID(), transform.position);
                _canJustDodgeTime.CancelCooltime();
                _canCounterAttackTime.CancelCooltime();
                _canCounterAttackTime.StartCooltime(1, () => CanCounterAttack = false);
                JustDodgeBuff.EnableAllBuffs(_buffableModule);
                CanCounterAttack = true;
            }
        }

    }
}