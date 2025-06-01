using System;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;
public class MergedPlayerBaseData : IMovable, IAttackable, IDodgeable, IDamagable, IGraffitiable, ISkinable, IEntityStateControllable
{
    public MovePoint MovePoint { get; set; }
    public float Speed { get; set; }
    public float SprintCoeff { get; set; }
    public LayerMask WallLayer { get; set; }
    public LayerMask CanPushLayer { get; set; }
    public AudioDataSO FootStepAudio { get; set; }
    public MovementController MovementController { get; set; }

    public int ATK { get; set; }
    public float CriticalChanceRate { get; set; }
    public float CriticalDamageRate { get; set; }
    public AttackDataSO[] AttackDatas { get; set; }
    public int LastAttackNum { get; set; }
    public AttackController AttackController { get; set; }
    public LayerMask[] TargetLayers { get; set; }
    public Transform CurrentTarget { get; set; }
    public DamageParticleDataSO DamageParticle { get; set; }
    public float DamageIndicatorRandomPosInfo { get; set; } = 0;

    public float DodgeCooltime { get; set; }
    public float DodgeSpeed { get; set; }
    public float InitialDodgeMaxDistance { get; set; }
    public int ContinuousDodgeLimit { get; set; }
    public LayerMask KeepDodgeWallLayer { get; set; }
    public float KeepDodgeMaxTime { get; set; }
    public float DodgeInvincibleTime { get; set; }
    public DodgeController DodgeController { get; set; }
    public ParticlePlayer KeepDodgeParticle { get; set; }

    public int BaseMaxHP { get; set; }
    public int BaseDEF { get; set; }
    public float Mass { get; set; }
    public AudioDataSO HitSound { get; set; }
    public ParticlePlayer HitParticle { get; set; }
    public HealthController HealthController { get; set; }

    public int MaxGP { get; set; }
    public int GP { get; set; }

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