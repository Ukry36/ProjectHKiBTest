using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Security.Cryptography;
using UnityEngine;

public class DamagableObject : Entity
{
    public MovementManagerSO movementManager;
    public bool dieWhenKnockBack;
    private bool isKnockbacking;

    public Coroutine knockBackCoroutine;

    public void Start()
    {
        MovePoint.Initialize();
    }

    public override void Damage(DamageDataSO damageData, IAttackable hitter)
    {
        // temporary
        // you need to make damageManager or smth
        if (damageData.knockBack > Mass)
        {
            if (dieWhenKnockBack)
            {
                Die();
                return;
            }

            if (isKnockbacking)
            {
                movementManager.EndKnockbackEarly(transform, this, null);
                StopCoroutine(knockBackCoroutine);
            }
            isKnockbacking = true;
            knockBackCoroutine = StartCoroutine(movementManager.KnockBackCoroutine
                        (
                            transform,
                            this,
                            transform.position - hitter.GetAttackOrigin(),
                            damageData.knockBack - Mass,
                            () => isKnockbacking = false
                        ));
        }
        HP.Value--;

        if (HP.Value <= 0)
            Die();
    }

    public void Die()
    {
        MovePoint.Die();
        gameObject.SetActive(false);
    }

    public void Update()
    {
        movementManager.FollowMovePointIdle(transform, this);
    }
}