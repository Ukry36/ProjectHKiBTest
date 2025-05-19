using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Database Manager", menuName = "Scriptable Objects/Manager/Database Manager", order = 0)]
public class DatabaseManagerSO : ScriptableObject
{
    public void SetIDamagable(IDamagable damagable, IDamagable data)
    {
        damagable.MaxHP = new(data.MaxHP);
        damagable.HP = new(data.HP);
        damagable.DEF = new(data.DEF);
        damagable.Mass = data.Mass;
        damagable.Resistance = new(data.Resistance);
        damagable.HitParticle = data.HitParticle;
        damagable.HitSound = data.HitSound;
    }

    public void SetIAttackable(IAttackable attackable, IAttackable data)
    {
        attackable.ATK = new(data.ATK);
        if (data.AttackDatas != null)
        {
            attackable.AttackDatas = new AttackDataSO[data.AttackDatas.Length];
            Array.Copy(data.AttackDatas, attackable.AttackDatas, data.AttackDatas.Length);
        }
        attackable.CriticalChanceRate = new(data.CriticalChanceRate);
        attackable.CriticalDamageRate = new(data.CriticalDamageRate);
        attackable.TargetLayers = data.TargetLayers;
        attackable.DamageParticle = data.DamageParticle;
    }

    public void SetIDodgeable(IDodgeable dodgeable, IDodgeable data)
    {
        dodgeable.DodgeCooltime = new(data.DodgeCooltime);
        dodgeable.ContinuousDodgeLimit = new(data.ContinuousDodgeLimit);
        dodgeable.KeepDodgeMaxTime = new(data.KeepDodgeMaxTime);
        dodgeable.KeepDodgeMaxDistance = new(data.KeepDodgeMaxDistance);
    }

    public void SetGraffiriable(IGraffitiable graffiriable, IGraffitiable data)
    {
        graffiriable.GP = new(data.GP);
        graffiriable.MaxGP = new(data.MaxGP);
    }

    public void SetIMovable(IMovable movable, IMovable data)
    {
        movable.Speed = new(data.Speed);
        movable.SprintCoeff = new(data.SprintCoeff);
        movable.WallLayer = data.WallLayer;
        movable.CanPushLayer = data.CanPushLayer;
        movable.IsSprinting = false;
        movable.FootStepAudio = data.FootStepAudio;
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