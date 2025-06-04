using UnityEditor.Animations;
using UnityEngine;

[CreateAssetMenu(fileName = "Enemy Data", menuName = "Scriptable Objects/Data/Enemy Data", order = 1)]
public class EnemyDataSO : ScriptableObject, IMovable, IAttackable, ITargetable, IDamagable, IPoolable, IEntityStateControllable
{
    [field: SerializeField] public int PoolSize { get; set; }

    public MovePoint MovePoint { get; set; }
    [field: SerializeField] public float Speed { get; set; }
    [field: SerializeField] public float SprintCoeff { get; set; }
    [field: SerializeField] public LayerMask WallLayer { get; set; }
    [field: SerializeField] public LayerMask CanPushLayer { get; set; }
    [field: SerializeField] public AudioDataSO FootStepAudio { get; set; }
    public FootstepController FootstepController { get; set; }

    [field: SerializeField] public int BaseATK { get; set; }
    [field: SerializeField] public float CriticalChanceRate { get; set; }
    [field: SerializeField] public float CriticalDamageRate { get; set; }
    [field: SerializeField] public AttackDataSO[] AttackDatas { get; set; }
    public AttackController AttackController { get; set; }
    [field: SerializeField] public LayerMask[] TargetLayers { get; set; }
    public Transform CurrentTarget { get; set; }
    [field: SerializeField] public DamageParticleDataSO DamageParticle { get; set; }
    public float DamageIndicatorRandomPosInfo { get; set; } = 0;

    [field: SerializeField] public int BaseMaxHP { get; set; }
    [field: SerializeField] public int BaseDEF { get; set; }
    [field: SerializeField] public float Mass { get; set; }
    [field: SerializeField] public AudioDataSO HitSound { get; set; }
    [field: SerializeField] public ParticlePlayer HitParticle { get; set; }

    [field: SerializeField] public StateMachineSO StateMachine { get; set; }
    [field: SerializeField] public AnimatorController AnimatorController { get; set; }
    public DirAnimationController AnimationController { get; set; }
    public StateController StateController { get; set; }
    public HealthController HealthController { get; set; }

    public MovementController MovementController { get; set; }
    public TargetController TargetController { get; set; }

    public Vector3 GetAttackOrigin()
    {
        throw new System.NotImplementedException();
    }

    public void Damage(DamageDataSO damageData, IAttackable hitter, Vector3 origin) { }

    public void OnDisable() { }

    public void KnockBack(Vector3 dir, float strength) { }
    public void EndKnockbackEarly() { }

    public void Die()
    {
        throw new System.NotImplementedException();
    }
}