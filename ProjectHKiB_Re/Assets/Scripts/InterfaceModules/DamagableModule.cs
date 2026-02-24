using System;
using NaughtyAttributes;
using UnityEngine;
namespace Assets.Scripts.Interfaces.Modules
{
    public class DamagableModule : InterfaceModule, IDamagable
    {
        public float BaseMaxHP { get; set; }
        public FloatBuffContainer MaxHPBuffer { get; set; }
        public float MaxHP
        {
            get
            {
                MaxHPBuffer ??= new(BaseMaxHP);
                return MaxHPBuffer.BuffedStat;
            }
        }
        private float _prevMaxHP;
        public float HP { get; set; }
        public Action<float> OnHPChanged { get; set; }
        public float BaseDEF { get; set; }
        public float DEF { get => DEFBuffer.BuffedStat; }
        public FloatBuffContainer DEFBuffer { get; set; }
        public FloatBuffContainer ResistanceBuffer { get; set; }
        public BoolBuffContainer InvincibleBuffer { get; set; }
        public BoolBuffContainer SuperArmourBuffer { get; set; }
        public AudioDataSO HitSound { get; set; }
        public ParticlePlayer HitParticle { get; set; }

        [SerializeField] protected DamageManagerSO damageManager;
        [SerializeField] protected MovableModule _movable;

        public Action OnDie { get; set; }
        public Action OnDamaged { get; set; }
        public Action OnHealed { get; set; }

        public override void Register(IInterfaceRegistable interfaceRegistable)
        {
            interfaceRegistable.RegisterInterface<IDamagable>(this);
        }

        public void Initialize()
        {
            MaxHPBuffer = new(BaseMaxHP);
            DEFBuffer = new(BaseDEF);
            ResistanceBuffer = new(0);
            InvincibleBuffer = new();
            SuperArmourBuffer = new();
            HP = MaxHP;
            OnHPChanged?.Invoke(HP);
            _prevMaxHP = MaxHP;
            MaxHPBuffer.OnBuffed += OnMaxHpChanged;
        }

        private void OnMaxHpChanged(float buffedStat)
        {
            HP *= buffedStat / _prevMaxHP;
            _prevMaxHP = buffedStat;
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
            OnDamaged?.Invoke();
            bool IsKnockback = false;
            if (damageData.knockBack > _movable.Mass && !SuperArmourBuffer.BuffedStat)
            {
                _movable.KnockBack(transform.position - origin, damageData.knockBack);
                IsKnockback = true;
            }
            damageManager.Damage(damageData, hitter, this, transform.position, IsKnockback);
            OnHPChanged?.Invoke(HP);
            if (HP <= 0)
                Die();
        }

        public virtual void Die()
        {
            _movable.MovePoint.Die();
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