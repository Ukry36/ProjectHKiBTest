using System;
using UnityEngine;

public interface IAttackable : IAttackableBase
{
    public FloatBuffContainer ATKBuffer { get; set; }
    public int ATK { get => (int)ATKBuffer.BuffedStat; }
    public Action<float> OnATKChanged { get => ATKBuffer.OnBuffed; }
    public bool IsAttackCooltime { get; set; }
    public float DamageIndicatorRandomPosInfo { get; set; }
    public int AttackNumber { get; set; }

    public void Initialize();
    public void SetAttacker();
    public void StartAttackCooltime();
    public void SetAttackData(int attackNumber);

    public void Attack(int damageNumber);
}