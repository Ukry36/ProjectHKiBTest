using UnityEngine;

[CreateAssetMenu(fileName = "DamagableObjectDataSO", menuName = "Scriptable Objects/Data/DamagableObjectData", order = 0)]
public class DamagableObjectDataSO : ScriptableObject, IDamagable, IMovable, IAttackable
{
    [field: SerializeField] public StatContainer MaxHP { get; set; }
    [field: SerializeField] public StatContainer HP { get; set; }
    [field: SerializeField] public StatContainer DEF { get; set; }
    [field: SerializeField] public StatContainer Resistance { get; set; }
    [field: SerializeField] public float Mass { get; set; }
    [field: SerializeField] public AudioDataSO HitSound { get; set; }
    [field: SerializeField] public ParticlePlayer HitParticle { get; set; }
    [field: SerializeField] public StatContainer Speed { get; set; }
    [field: SerializeField] public StatContainer SprintCoeff { get; set; }
    [field: SerializeField] public LayerMask WallLayer { get; set; }
    [field: SerializeField] public bool IsSprinting { get; set; }
    [field: SerializeField] public AudioDataSO FootStepAudio { get; set; }
    public MovePoint MovePoint { get; set; }
    public FootstepController FootstepController { get; set; }
    [field: SerializeField] public IMovable.ExternalForce ExForce { get; set; } = new(true);
    [field: SerializeField] public bool DieWhenKnockBack { get; set; }
    [field: SerializeField] public LayerMask CanPushLayer { get; set; }

    public float DamageIndicatorRandomPosInfo { get; set; } = 0;

    public StatContainer ATK { get; set; }
    public StatContainer CriticalChanceRate { get; set; }
    public StatContainer CriticalDamageRate { get; set; }
    public AttackDataSO[] AttackDatas { get; set; }
    public AttackController AttackController { get; set; }
    public LayerMask[] TargetLayers { get; set; }
    public Transform CurrentTarget { get; set; }
    [field: SerializeField] public DamageParticleDataSO DamageParticle { get; set; }
    public bool IsKnockbackMove { get; set; }

    public void Damage(DamageDataSO damageData, IAttackable hitter, Vector3 origin) { }

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
