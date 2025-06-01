using System;
using System.Collections.Generic;
using UnityEngine;

public class HealthController : MonoBehaviour
{
    [SerializeField] protected DamageManagerSO damageManager;
    protected IMovable _movable;
    public StatBufferFloat MaxHPBuffer { get; set; } = new();
    public int HP { get; set; }
    public StatBufferFloat DEFBuffer { get; set; } = new();
    public StatBufferFloat ResistanceBuffer { get; set; } = new();
    public StatBufferBool Invincible { get; set; } = new();
    public StatBufferBool SuperArmour { get; set; } = new();
    public Action OnDie;

    public void Initialize(IMovable movable)
    {
        _movable = movable;
    }

    public virtual void Damage(DamageDataSO damageData, IAttackable hitter, Vector3 origin, IDamagable self)
    {
        bool IsKnockback = false;
        if (damageData.knockBack > _movable.Mass && !SuperArmour.GetBuffedStat())
        {
            _movable.KnockBack(transform.position - origin, damageData.knockBack);
            IsKnockback = true;
        }
        damageManager.Damage(damageData, hitter, self, transform, IsKnockback);

        if (HP <= 0)
            Die();
    }

    public virtual void Die()
    {
        _movable.MovePoint.Die();
        gameObject.SetActive(false);
        OnDie?.Invoke();
    }
}