using System;
using UnityEngine;
using UnityEngine.U2D.Animation;

[CreateAssetMenu(fileName = "DamagableObjectDataSO", menuName = "Scriptable Objects/Data/DamagableObjectData", order = 0)]
public class DamagableObjectDataSO : ScriptableObject, IDamagableBase, IPhysicsBase, IAttackableBase
{
    [field: SerializeField] public float BaseMaxHP { get; set; }
    [field: SerializeField] public float BaseDEF { get; set; }
    [field: SerializeField] public AudioDataSO HitSound { get; set; }
    [field: SerializeField] public ParticlePlayer HitParticle { get; set; }


    [field: Header("Physics")]
    [field: SerializeField] public Vector2Int Size { get; set; } = Vector2Int.one;
    [field: SerializeField] public float Mass { get; set; } = 1f;

    [field: SerializeField] public float WalkAcceleration { get; set; } = 300f;
    [field: SerializeField] public float MaxWalkSpeed { get; set; } = 5f;
    [field: SerializeField] public float SprintCoeff { get; set; } = 1.5f;
    [field: SerializeField] public float FrictionWalkInfluence { get; set; } = 1f;
    [field: SerializeField] public AudioDataSO DefaultFootstepAudio { get; set; }

    [field: SerializeField] public float GroundFriction { get; set; } = 0.6f;
    [field: SerializeField] public float AirFriction { get; set; } = 0.98f;
    [field: SerializeField] public float BounceCoeff { get; set; } = 0.3f;

    [field: SerializeField] public float GridEndureSpeed { get; set; } = 10f;
    [field: SerializeField] public float GridEndureForce { get; set; } = 50f;
    [field: SerializeField] public float StaticEndureForce { get; set; } = 100f;

    [field: SerializeField] public float StepUpTolerance { get; set; } = 0.5f;
    [field: SerializeField] public float StepDownTolerance { get; set; } = 0.2f;

    [field: SerializeField] public LayerMask WallLayer { get; set; }
    [field: SerializeField] public LayerMask FloorLayer { get; set; }
    [field: SerializeField] public LayerMask CanPushLayer { get; set; }

    public int BaseATK { get; set; }
    public float CriticalChanceRate { get; set; }
    public float CriticalDamageRate { get; set; }
    public AttackDataSO[] AttackDatas { get; set; }
    [field: SerializeField] public DamageParticleDataSO DamageParticle { get; set; }
    
    [field: SerializeField] public SimpleAnimationDataSO EffectAnimationData { get; set; }
    [field: SerializeField] public SpriteLibraryAsset EffectSpriteLibrary { get; set; }
}
