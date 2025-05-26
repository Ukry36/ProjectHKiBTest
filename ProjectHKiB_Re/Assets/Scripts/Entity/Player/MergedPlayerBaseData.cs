using System;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;
public class MergedPlayerBaseData : IMovable, IAttackable, IDodgeable, IDamagable, IGraffitiable, ISkinable, IEntityStateControllable
{
    public MovePoint MovePoint { get; set; }
    public StatContainer Speed { get; set; }
    public StatContainer SprintCoeff { get; set; }
    public LayerMask WallLayer { get; set; }
    public LayerMask CanPushLayer { get; set; }
    public bool IsSprinting { get; set; }
    public AudioDataSO FootStepAudio { get; set; }
    public IMovable.ExternalForce ExForce { get; set; } = new();
    public bool IsKnockbackMove { get; set; }
    public Vector3 LastSetDir { get; set; }

    public StatContainer ATK { get; set; }
    public StatContainer CriticalChanceRate { get; set; }
    public StatContainer CriticalDamageRate { get; set; }
    public AttackDataSO[] AttackDatas { get; set; }
    public int LastAttackNum { get; set; }
    public AttackController AttackController { get; set; }
    public LayerMask[] TargetLayers { get; set; }
    public Transform CurrentTarget { get; set; }
    public DamageParticleDataSO DamageParticle { get; set; }
    public float DamageIndicatorRandomPosInfo { get; set; } = 0;

    public Cooltime DodgeCooltime { get; set; }
    public StatContainer DodgeSpeed { get; set; }
    public StatContainer InitialDodgeMaxDistance { get; set; }
    public StatContainer ContinuousDodgeLimit { get; set; }
    public LayerMask KeepDodgeWallLayer { get; set; }
    public Cooltime KeepDodgeMaxTime { get; set; }
    public StatContainer KeepDodgeMaxDistance { get; set; }
    public Cooltime DodgeInvincibleTime { get; set; }
    public int TotalDodgeCount { get; set; }

    public StatContainer MaxHP { get; set; }
    public StatContainer HP { get; set; }
    public StatContainer DEF { get; set; }
    public StatContainer Resistance { get; set; }
    public float Mass { get; set; }
    public AudioDataSO HitSound { get; set; }
    public ParticlePlayer HitParticle { get; set; }

    public StatContainer MaxGP { get; set; }
    public StatContainer GP { get; set; }

    public SkinDataSO SkinData { get; set; }

    public StateMachineSO StateMachine { get; set; }
    public AnimatorController AnimatorController { get; set; }
    public FootstepController FootstepController { get; set; }
    public DirAnimationController AnimationController { get; set; }
    public StateController StateController { get; set; }

    public EntityTypeSO entityType;
    public GearTypeSO gearType;

    public void Damage(DamageDataSO damageData, IAttackable hitter, Vector3 origin)
    {
        throw new NotImplementedException();
    }

    public Vector3 GetAttackOrigin()
    {
        throw new NotImplementedException();
    }

    public void KnockBack(Vector3 dir, float strength) { }
    public void EndKnockbackEarly() { }

    public void Die()
    {
        throw new NotImplementedException();
    }
}