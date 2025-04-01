using System;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;
public class MergedPlayerBaseData : IID, IMovable, IAttackable, IDodgeable, IDamagable, IGraffitiable, ISkinable, IStateControllable
{
    public int ID { get; set; }
    public string Name { get; set; }

    public MovePoint MovePoint { get; set; }
    public StatContainer Speed { get; set; }
    public StatContainer SprintCoeff { get; set; }
    public LayerMask WallLayer { get; set; }
    public bool IsSprinting { get; set; }
    public AudioDataSO FootStepAudio { get; set; }

    public StatContainer ATK { get; set; }
    public StatContainer CriticalChanceRate { get; set; }
    public StatContainer CriticalDamageRate { get; set; }
    public DamageDataSO[] AttackDatas { get; set; }

    public CustomVariable<float> DodgeCooltime { get; set; }
    public CustomVariable<float> ContinuousDodgeLimit { get; set; }
    public CustomVariable<float> KeepDodgeMaxTime { get; set; }
    public CustomVariable<float> KeepDodgeMaxDistance { get; set; }

    public StatContainer MaxHP { get; set; }
    public StatContainer HP { get; set; }
    public StatContainer DEF { get; set; }
    public List<CustomVariable<Resistance>> Resistances { get; set; }
    public float Mass { get; set; }
    public AudioDataSO HitSound { get; set; }
    public ParticleDataSO HitParticle { get; set; }

    public StatContainer MaxGP { get; set; }
    public StatContainer GP { get; set; }

    public SkinDataSO SkinData { get; set; }

    public StateMachineSO StateMachine { get; set; }
    public AnimatorController AnimatorController { get; set; }

    public EntityTypeSO entityType;
    public GearTypeSO gearType;

    public void Damage(DamageDataSO damageData, IAttackable hitter, IDamagable getHit)
    {
        throw new NotSupportedException();
    }

    public Vector3 GetAttackOrigin()
    {
        throw new NotSupportedException();
    }
}