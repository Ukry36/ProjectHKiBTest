using UnityEngine;

[CreateAssetMenu(fileName = "Database Manager", menuName = "Scriptable Objects/Manager/Database Manager", order = 0)]
public class DatabaseManagerSO : ScriptableObject
{
    public void SetIDamagable(IDamagable damagable, IDamagable data)
    {
        damagable.MaxHP = data.MaxHP;
        damagable.HP = data.HP;
        damagable.DEF = data.DEF;
        damagable.Mass = data.Mass;
        damagable.Resistances = data.Resistances;
        damagable.HitParticle = data.HitParticle;
        damagable.HitSound = data.HitSound;
    }

    public void SetIAttackable(IAttackable attackable, IAttackable data)
    {
        attackable.ATK = data.ATK;
        attackable.AttackDatas = data.AttackDatas;
        attackable.CriticalChanceRate = data.CriticalChanceRate;
        attackable.CriticalDamageRate = data.CriticalDamageRate;
    }

    public void SetIDodgeable(IDodgeable dodgeable, IDodgeable data)
    {
        dodgeable.DodgeCooltime = data.DodgeCooltime;
        dodgeable.ContinuousDodgeLimit = data.ContinuousDodgeLimit;
        dodgeable.KeepDodgeMaxTime = data.KeepDodgeMaxTime;
        dodgeable.KeepDodgeMaxDistance = data.KeepDodgeMaxDistance;
    }

    public void SetGraffiriable(IGraffitiable graffiriable, IGraffitiable data)
    {
        graffiriable.GP = data.GP;
        graffiriable.MaxGP = data.MaxGP;
    }

    public void SetIInteractable(IInteractable interactable, IInteractable data)
    {
        interactable.Event = data.Event;
        interactable.TriggerCoolTime = data.TriggerCoolTime;
    }

    public void SetIMovable(IMovable movable, IMovable data)
    {
        movable.Speed = data.Speed;
        movable.SprintCoeff = data.SprintCoeff;
        movable.WallLayer = data.WallLayer;
        movable.IsSprinting = false;
        movable.FootStepAudio = data.FootStepAudio;
    }

    public void SetISkinable(ISkinable skinable, ISkinable data)
    {
        skinable.SkinData = data.SkinData;
    }

    public void SetIStateControllable(IStateControllable stateControllable, IStateControllable data)
    {
        stateControllable.AnimatorController = data.AnimatorController;
        stateControllable.StateMachine = data.StateMachine;
    }
}