using System;
using System.Collections.Generic;
using UnityEngine;

public class HealthController : MonoBehaviour
{
    [SerializeField] protected DamageManagerSO damageManager;
    protected IMovable _movable;
    public FloatBuffCalculator MaxHPBuffer { get; set; } = new();
    public int HP
    {
        get => _HP;
        set
        {
            _HP = value;
        }
    }
    [SerializeField][NaughtyAttributes.ReadOnly()] private int _HP;
    public FloatBuffCalculator DEFBuffer { get; set; } = new();
    public FloatBuffCalculator ResistanceBuffer { get; set; } = new();
    public BoolBuffCalculator InvincibleBuffer { get; set; } = new();
    public BoolBuffCalculator SuperArmourBuffer { get; set; } = new();
    public Action OnDie;
    public Action OnDamaged;
    public Action OnHealed;

    public void Initialize(IMovable movable, int maxHP)
    {
        HP = maxHP;
        _movable = movable;
    }

    public virtual void Damage(DamageDataSO damageData, IAttackable hitter, Vector3 origin, IDamagable self)
    {
        OnDamaged?.Invoke();
        bool IsKnockback = false;
        if (damageData.knockBack > _movable.Mass && !SuperArmourBuffer.GetBuffedStat())
        {
            _movable.KnockBack(transform.position - origin, damageData.knockBack);
            IsKnockback = true;
        }
        damageManager.Damage(damageData, hitter, self, transform.position, IsKnockback);

        if (HP <= 0)
            Die();
    }

    public virtual void Die()
    {
        _movable.MovePoint.Die();
        gameObject.SetActive(false);
        OnDie?.Invoke();
    }

    public virtual void Heal(int amount, int maxHP)
    {
        OnHealed?.Invoke();
        if (amount <= 0) return;
        HP += amount;
        if (HP > maxHP) HP = maxHP;
    }
}