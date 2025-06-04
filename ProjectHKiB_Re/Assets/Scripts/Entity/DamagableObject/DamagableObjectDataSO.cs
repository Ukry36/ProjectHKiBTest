using UnityEngine;

[CreateAssetMenu(fileName = "DamagableObjectDataSO", menuName = "Scriptable Objects/Data/DamagableObjectData", order = 0)]
public class DamagableObjectDataSO : ScriptableObject, IDamagable, IMovable, IAttackable
{
    [field: SerializeField] public int BaseMaxHP { get; set; }
    [field: SerializeField] public int BaseDEF { get; set; }
    [field: SerializeField] public float Mass { get; set; }
    [field: SerializeField] public AudioDataSO HitSound { get; set; }
    [field: SerializeField] public ParticlePlayer HitParticle { get; set; }
    [field: SerializeField] public float Speed { get; set; }
    [field: SerializeField] public float SprintCoeff { get; set; }
    [field: SerializeField] public LayerMask WallLayer { get; set; }
    [field: SerializeField] public AudioDataSO FootStepAudio { get; set; }
    public MovePoint MovePoint { get; set; }
    public FootstepController FootstepController { get; set; }
    [field: SerializeField] public bool DieWhenKnockBack { get; set; }
    [field: SerializeField] public LayerMask CanPushLayer { get; set; }

    public float DamageIndicatorRandomPosInfo { get; set; } = 0;

    public int BaseATK { get; set; }
    public float CriticalChanceRate { get; set; }
    public float CriticalDamageRate { get; set; }
    public AttackDataSO[] AttackDatas { get; set; }
    public AttackController AttackController { get; set; }
    public LayerMask[] TargetLayers { get; set; }
    public Transform CurrentTarget { get; set; }
    [field: SerializeField] public DamageParticleDataSO DamageParticle { get; set; }

    public MovementController MovementController { get; set; }

    public HealthController HealthController { get; set; }

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
