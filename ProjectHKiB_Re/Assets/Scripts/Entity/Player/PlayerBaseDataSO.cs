using UnityEditor.Animations;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerBaseData", menuName = "Scriptable Objects/Data/PlayerBaseData", order = 1)]
public class PlayerBaseDataSO : ScriptableObject, IMovable, IAttackable, IDodgeable, IDamagable, IGraffitiable, ISkinable, IEntityStateControllable
{
    public MovePoint MovePoint { get; set; }
    [field: NaughtyAttributes.Foldout("Move")][field: SerializeField] public float Speed { get; set; }
    [field: NaughtyAttributes.Foldout("Move")][field: SerializeField] public float SprintCoeff { get; set; }
    [field: NaughtyAttributes.Foldout("Move")][field: SerializeField] public LayerMask WallLayer { get; set; }
    [field: NaughtyAttributes.Foldout("Move")][field: SerializeField] public LayerMask CanPushLayer { get; set; }
    [field: NaughtyAttributes.Foldout("Move")][field: SerializeField] public AudioDataSO FootStepAudio { get; set; }
    public FootstepController FootstepController { get; set; }
    public MovementController MovementController { get; set; }

    [field: NaughtyAttributes.Foldout("Attack")][field: SerializeField] public int ATK { get; set; }
    [field: NaughtyAttributes.Foldout("Attack")][field: SerializeField] public float CriticalChanceRate { get; set; }
    [field: NaughtyAttributes.Foldout("Attack")][field: SerializeField] public float CriticalDamageRate { get; set; }
    [field: NaughtyAttributes.Foldout("Attack")][field: SerializeField] public AttackDataSO[] AttackDatas { get; set; }
    public AttackController AttackController { get; set; }
    [field: NaughtyAttributes.Foldout("Attack")][field: SerializeField] public LayerMask[] TargetLayers { get; set; }
    public Transform CurrentTarget { get; set; }
    [field: NaughtyAttributes.Foldout("Attack")][field: SerializeField] public DamageParticleDataSO DamageParticle { get; set; }
    public float DamageIndicatorRandomPosInfo { get; set; } = 0;

    [field: NaughtyAttributes.Foldout("Dodge")][field: SerializeField] public float DodgeCooltime { get; set; }
    [field: NaughtyAttributes.Foldout("Dodge")][field: SerializeField] public float InitialDodgeMaxDistance { get; set; }
    [field: NaughtyAttributes.Foldout("Dodge")][field: SerializeField] public float DodgeSpeed { get; set; }
    [field: NaughtyAttributes.Foldout("Dodge")][field: SerializeField] public int ContinuousDodgeLimit { get; set; }
    [field: NaughtyAttributes.Foldout("Dodge")][field: SerializeField] public LayerMask KeepDodgeWallLayer { get; set; }
    [field: NaughtyAttributes.Foldout("Dodge")][field: SerializeField] public float KeepDodgeMaxTime { get; set; }
    [field: NaughtyAttributes.Foldout("Dodge")][field: SerializeField] public float DodgeInvincibleTime { get; set; }
    public DodgeController DodgeController { get; set; }
    [field: NaughtyAttributes.Foldout("Dodge")][field: SerializeField] public ParticlePlayer KeepDodgeParticle { get; set; }

    [field: NaughtyAttributes.Foldout("Health")][field: SerializeField] public int BaseMaxHP { get; set; }
    [field: NaughtyAttributes.Foldout("Health")][field: SerializeField] public int BaseDEF { get; set; }
    [field: NaughtyAttributes.Foldout("Health")][field: SerializeField] public float Mass { get; set; }
    [field: NaughtyAttributes.Foldout("Health")][field: SerializeField] public AudioDataSO HitSound { get; set; }
    [field: NaughtyAttributes.Foldout("Health")][field: SerializeField] public ParticlePlayer HitParticle { get; set; }
    public HealthController HealthController { get; set; }

    [field: NaughtyAttributes.Foldout("Graffiti")][field: SerializeField] public int MaxGP { get; set; }
    [field: NaughtyAttributes.Foldout("Graffiti")][field: SerializeField] public int GP { get; set; }

    [field: NaughtyAttributes.Foldout("Skin")][field: SerializeField] public SkinDataSO SkinData { get; set; }

    [field: NaughtyAttributes.Foldout("Control")][field: SerializeField] public StateMachineSO StateMachine { get; set; }
    [field: NaughtyAttributes.Foldout("Control")][field: SerializeField] public AnimatorController AnimatorController { get; set; }
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