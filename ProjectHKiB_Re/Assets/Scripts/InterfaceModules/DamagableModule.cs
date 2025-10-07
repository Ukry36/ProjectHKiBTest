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

        }
        [Button]
        public void Damage10()
        {

            HP -= 10;
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
    }
}