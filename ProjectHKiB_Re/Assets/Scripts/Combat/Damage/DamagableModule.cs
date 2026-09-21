using System;
using Combat;
using Gameplay;
using NaughtyAttributes;
using UnityEngine;

public interface IDamagableBase
{
    public float BaseMaxHP { get; set; }
    public float BaseDEF { get; set; }

    public AudioDataSO HitSound { get; set; }
    public ParticlePlayer HitParticle { get; set; }
}

public interface IDamagable : IDamagableBase, IInitializable
{
    public FloatBuffContainer MaxHPBuffer { get; set; }
    public float MaxHP { get => MaxHPBuffer.GetBuffedStat(BaseMaxHP, 0); }
    public float HP { get; set; }
    public Action<float> OnHPChanged { get; set; }

    public FloatBuffContainer DEFBuffer { get; set; }
    public float DEF { get => DEFBuffer.GetBuffedStat(BaseDEF, 0); }
    public Action<float> OnDEFChanged { get; set; }

    public FloatBuffContainer ResistanceBuffer { get; set; }
    public float Resistance { get => ResistanceBuffer.GetBuffedStat(0); }
    public Action<float> OnResistanceChanged { get; set; }

    public BoolBuffContainer InvincibleBuffer { get; set; }
    public bool Invincible { get => InvincibleBuffer.GetBuffedStat(0, isNegative: false); }
    public Action<bool> OnInvincibleChanged { get; set; }

    public BoolBuffContainer SuperArmourBuffer { get; set; }
    public bool SuperArmour { get => SuperArmourBuffer.GetBuffedStat(0, isNegative: false); }
    public Action<bool> OnSuperArmourChanged { get; set; }

    public Action OnDamaged { get; set; }
    public Action OnDie { get; set; }
    public Action OnHealed { get; set; }

    public void Damage(DamageDataSO damageData, IAttackable hitter, Vector3 origin);
    public void Die();
    public void Heal(int amount);

}

namespace Assets.Scripts.Interfaces.Modules
{
    [RequireComponent(typeof(PhysicsModule))]
    public class DamagableModule : InterfaceModule, IDamagable, ICombatHitReceiver
    {
        public float BaseMaxHP { get; set; }
        public FloatBuffContainer MaxHPBuffer { get; set; }
        public float MaxHP { get => MaxHPBuffer.GetBuffedStat(BaseMaxHP, 0); }
        private float _prevMaxHP;
        public float HP { get; set; }
        public float BaseDEF { get; set; }
        public float DEF { get => DEFBuffer.GetBuffedStat(BaseDEF, 0); }
        public FloatBuffContainer DEFBuffer { get; set; }
        public FloatBuffContainer ResistanceBuffer { get; set; }
        public BoolBuffContainer InvincibleBuffer { get; set; }
        public BoolBuffContainer SuperArmourBuffer { get; set; }
        public AudioDataSO HitSound { get; set; }
        public ParticlePlayer HitParticle { get; set; }

        [SerializeField] protected DamageManagerSO damageManager;
        [SerializeField] protected PhysicsModule _physics;

        [Tooltip("패링 판정과 피격 사건의 Target State를 조회할 소유 StateController.")]
        [NaughtyAttributes.ReadOnly, SerializeField]
        private StateController _owner;

        public Action OnDie { get; set; }
        public Action OnDamaged { get; set; }
        public Action OnHealed { get; set; }
        public Action<float> OnHPChanged { get; set; }
        public Action<float> OnDEFChanged { get; set; }
        public Action<float> OnResistanceChanged { get; set; }
        public Action<bool> OnInvincibleChanged { get; set; }
        public Action<bool> OnSuperArmourChanged { get; set; }

        public override void Register(IInterfaceRegistable interfaceRegistable)
        {
            interfaceRegistable.RegisterInterface<IDamagable>(this);
            _owner = interfaceRegistable as StateController;
        }

        public void Initialize()
        {
            MaxHPBuffer = new();
            DEFBuffer = new();
            ResistanceBuffer = new();
            InvincibleBuffer = new();
            SuperArmourBuffer = new();
            HP = MaxHP;
            OnHPChanged?.Invoke(HP);
            _prevMaxHP = MaxHP;
            MaxHPBuffer.OnBuffed += OnMaxHpChanged;
            if (!_physics) _physics = GetComponent<PhysicsModule>();
            if (!_owner) _owner = GetComponent<StateController>();
        }

        private void OnMaxHpChanged()
        {
            HP *= MaxHP / _prevMaxHP;
            _prevMaxHP = MaxHP;
            OnHPChanged?.Invoke(HP);
        }
        [Button]
        public void Damage10()
        {

            HP -= 10;
            Debug.Log($"[Damage10] {name} HP now = {HP}");
            OnDamaged?.Invoke();
        }

        public virtual void Damage(DamageDataSO damageData, IAttackable hitter, Vector3 origin)
        {
            CombatHitRequest request = CombatHitRequest.CreateLegacy(
                damageData,
                hitter,
                (hitter as Component)?.gameObject,
                transform.position,
                origin);
            ReceiveHit(request);
        }

        /// <summary>
        /// 실제 피해와 넉백을 적용하기 전에 현재 패링 창을 먼저 검사한다.
        /// 호출자에게 명확한 결과를 반환하여 패링과 일반 피격 연출을 분리한다.
        /// </summary>
        public virtual CombatHitResult ReceiveHit(in CombatHitRequest request)
        {
            if (request.DamageData == null || request.Attacker == null || damageManager == null)
                return new CombatHitResult(CombatHitOutcome.Invalid);

            if (_owner != null && _owner.TryGetInterface(out IParryable parryable) &&
                parryable.TryParry(request, out GameplayEvent parryEvent))
                return new CombatHitResult(CombatHitOutcome.Parried, gameplayEvent: parryEvent);

            OnDamaged?.Invoke();
            bool isKnockback = false;
            if (_physics != null && request.DamageData.knockBack > _physics.Mass &&
                !SuperArmourBuffer.GetBuffedStat(0, isNegative: false))
            {
                _physics.KnockBack(
                    transform.position - request.KnockbackOrigin,
                    request.DamageData.knockBack);
                isKnockback = true;
            }

            float previousHP = HP;
            bool wasInvincible = InvincibleBuffer.GetBuffedStat(0, isNegative: false);
            damageManager.Damage(
                request.DamageData,
                request.Attacker,
                this,
                request.HitPosition,
                isKnockback);
            OnHPChanged?.Invoke(HP);
            if (HP <= 0)
                Die();

            int appliedDamage = Mathf.Max(0, Mathf.RoundToInt(previousHP - HP));
            GameplayEvent hitEvent = CombatEventDispatcher.PublishHit(request, _owner, appliedDamage);
            if (isKnockback)
                CombatEventDispatcher.PublishKnockback(request, _owner);

            return new CombatHitResult(
                wasInvincible ? CombatHitOutcome.Invulnerable : CombatHitOutcome.Damaged,
                appliedDamage,
                hitEvent);
        }

        public virtual void Die()
        {
            Debug.Log("Dead: " + gameObject.name);
            gameObject.SetActive(false);
            OnDie?.Invoke();
        }

        public virtual void Heal(int amount)
        {
            OnHealed?.Invoke();
            if (amount <= 0) return;
            HP += amount;
            if (HP > MaxHP) HP = MaxHP;
            OnHPChanged?.Invoke(HP);
        }
        //Save 시스템에서 Hp 저장
        public void ApplySavedHP(float savedHp)
        {
            // 현재 MaxHP 기준으로 clamp
            HP = Mathf.Clamp(savedHp, 0f, MaxHP);

            // "현재 MaxHP"를 기준으로 prev 동기화해서
            // 이후 MaxHP 변경 이벤트에서 HP가 또 비율 보정되는 걸 줄임
            _prevMaxHP = MaxHP;

            OnHPChanged?.Invoke(HP);
        }
    }
}
