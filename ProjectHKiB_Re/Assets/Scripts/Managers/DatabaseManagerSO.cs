using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Database Manager", menuName = "Scriptable Objects/Manager/Database Manager", order = 0)]
public class DatabaseManagerSO : ScriptableObject
{
    public void SetIDamagable(IDamagable damagable, IDamagable data)
    {
        damagable.BaseMaxHP = data.BaseMaxHP;
        damagable.BaseDEF = data.BaseDEF;
        damagable.HitParticle = data.HitParticle;
        damagable.HitSound = data.HitSound;
    }

    public void SetIAttackable(IAttackable attackable, IAttackable data)
    {
        attackable.BaseATK = data.BaseATK;
        if (data.AttackDatas != null)
        {
            attackable.AttackDatas = new AttackDataSO[data.AttackDatas.Length];
            Array.Copy(data.AttackDatas, attackable.AttackDatas, data.AttackDatas.Length);
        }
        attackable.CriticalChanceRate = data.CriticalChanceRate;
        attackable.CriticalDamageRate = data.CriticalDamageRate;
        attackable.DamageParticle = data.DamageParticle;
    }

    public void SetITargetable(ITargetable targetable, ITargetable data)
    {
        targetable.TargetLayers = data.TargetLayers;
    }

    public void SetIDodgeable(IDodgeable dodgeable, IDodgeable data)
    {
        dodgeable.BaseDodgeCooltime = data.BaseDodgeCooltime;
        dodgeable.InitialDodgeMaxDistance = data.InitialDodgeMaxDistance;
        dodgeable.BaseDodgeSpeed = data.BaseDodgeSpeed;
        dodgeable.BaseContinuousDodgeLimit = data.BaseContinuousDodgeLimit;
        dodgeable.KeepDodgeWallLayer = data.KeepDodgeWallLayer;
        dodgeable.BaseKeepDodgeMaxTime = data.BaseKeepDodgeMaxTime;
        dodgeable.BaseDodgeInvincibleTime = data.BaseDodgeInvincibleTime;
        dodgeable.KeepDodgeParticle = data.KeepDodgeParticle;
        dodgeable.JustDodgeBuff = data.JustDodgeBuff;
    }

    public void SetGraffiriable(IGraffitiable graffiriable, IGraffitiable data)
    {
        graffiriable.GP = data.GP;
        graffiriable.MaxGP = data.MaxGP;
    }

    public void SetIMovable(IMovable movable, IMovable data)
    {
        movable.Speed = data.Speed;
        movable.SprintCoeff = data.SprintCoeff;
        movable.WallLayer = data.WallLayer;
        movable.CanPushLayer = data.CanPushLayer;
        movable.FootStepAudio = data.FootStepAudio;
        movable.Mass = data.Mass;
    }

    public void SetISkinable(ISkinable skinable, ISkinable data)
    {
        skinable.SkinData = data.SkinData;
    }

    public void SetIStateControllable(IEntityStateControllable stateControllable, IEntityStateControllable data)
    {
        stateControllable.AnimatorController = data.AnimatorController;
        stateControllable.StateMachine = data.StateMachine;
    }
}