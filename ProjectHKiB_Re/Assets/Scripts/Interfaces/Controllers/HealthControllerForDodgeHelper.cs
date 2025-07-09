using System;
using System.Collections.Generic;
using UnityEngine;

public class HealthControllerForDodgeHelper : HealthController
{
    public override void Damage(DamageDataSO damageData, IAttackable hitter, Vector3 origin, IDamagable self)
    {
        OnDamaged?.Invoke();
    }

    public override void Die()
    { }
}