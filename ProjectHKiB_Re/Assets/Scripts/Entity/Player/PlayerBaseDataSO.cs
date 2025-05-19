using UnityEditor.Animations;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerBaseData", menuName = "Scriptable Objects/Data/PlayerBaseData", order = 1)]
public class PlayerBaseDataSO : ScriptableObject, IMovable, IAttackable, IDodgeable, IDamagable, IGraffitiable, ISkinable, IEntityStateControllable
{
    public MovePoint MovePoint { get; set; }
    [field: SerializeField] public StatContainer Speed { get; set; }
    [field: SerializeField] public StatContainer SprintCoeff { get; set; }
    [field: SerializeField] public LayerMask WallLayer { get; set; }
    [field: SerializeField] public LayerMask CanPushLayer { get; set; }
    public bool IsSprinting { get; set; }
    [field: SerializeField] public AudioDataSO FootStepAudio { get; set; }
    public FootstepController FootstepController { get; set; }
    public IMovable.ExternalForce ExForce { get; set; } = new();
    public bool IsKnockbackMove { get; set; }

    [field: SerializeField] public StatContainer ATK { get; set; }
    [field: SerializeField] public StatContainer CriticalChanceRate { get; set; }
    [field: SerializeField] public StatContainer CriticalDamageRate { get; set; }
    [field: SerializeField] public AttackDataSO[] AttackDatas { get; set; }
    public AttackController AttackController { get; set; }
    [field: SerializeField] public LayerMask[] TargetLayers { get; set; }
    public Transform CurrentTarget { get; set; }
    [field: SerializeField] public DamageParticleDataSO DamageParticle { get; set; }
    public float DamageIndicatorRandomPosInfo { get; set; } = 0;

    [field: SerializeField] public StatContainer DodgeCooltime { get; set; }
    [field: SerializeField] public StatContainer ContinuousDodgeLimit { get; set; }
    [field: SerializeField] public StatContainer KeepDodgeMaxTime { get; set; }
    [field: SerializeField] public StatContainer KeepDodgeMaxDistance { get; set; }

    [field: SerializeField] public StatContainer MaxHP { get; set; }
    [field: SerializeField] public StatContainer HP { get; set; }
    [field: SerializeField] public StatContainer DEF { get; set; }
    [field: SerializeField] public StatContainer Resistance { get; set; }
    [field: SerializeField] public float Mass { get; set; }
    [field: SerializeField] public AudioDataSO HitSound { get; set; }
    [field: SerializeField] public ParticlePlayer HitParticle { get; set; }

    [field: SerializeField] public StatContainer MaxGP { get; set; }
    [field: SerializeField] public StatContainer GP { get; set; }

    [field: SerializeField] public SkinDataSO SkinData { get; set; }

    [field: SerializeField] public StateMachineSO StateMachine { get; set; }
    [field: SerializeField] public AnimatorController AnimatorController { get; set; }
    public DirAnimationController AnimationController { get; set; }
    public StateController StateController { get; set; }

    public void Damage(DamageDataSO damageData, IAttackable hitter, Vector3 origin)
    {
        throw new System.NotImplementedException();
    }

    public void Die()
    {
        throw new System.NotImplementedException();
    }

    public Vector3 GetAttackOrigin()
    {
        throw new System.NotImplementedException();
    }

    public void KnockBack(Vector3 dir, float strength) { }
    public void EndKnockbackEarly() { }
}