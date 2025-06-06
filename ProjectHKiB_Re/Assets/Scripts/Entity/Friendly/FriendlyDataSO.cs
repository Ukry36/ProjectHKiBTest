
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.U2D.Animation;

[CreateAssetMenu(fileName = "Friendly Data", menuName = "Scriptable Objects/Data/Friendly Data", order = 1)]
public class FriendlyDataSO : ScriptableObject, IMovable, IAttackable, IDamagable, IPoolable
{
    [field: SerializeField] public int BaseMaxHP { get; set; }
    [field: SerializeField] public int BaseDEF { get; set; }
    [field: SerializeField] public int BaseATK { get; set; }
    [field: SerializeField] public float CriticalChanceRate { get; set; }
    [field: SerializeField] public float CriticalDamageRate { get; set; }
    public AttackController AttackController { get; set; }
    [field: SerializeField] public DamageParticleDataSO DamageParticle { get; set; }
    public float DamageIndicatorRandomPosInfo { get; set; } = 0;
    [field: SerializeField] public float Mass { get; set; }
    [field: SerializeField] public float Speed { get; set; }
    [field: SerializeField] public float SprintCoeff { get; set; }
    public MovePoint MovePoint { get; set; }
    [field: SerializeField] public AttackDataSO[] AttackDatas { get; set; }
    [field: SerializeField] public int PoolSize { get; set; }
    [field: SerializeField] public AudioDataSO HitSound { get; set; }
    [field: SerializeField] public ParticlePlayer HitParticle { get; set; }
    [field: SerializeField] public LayerMask WallLayer { get; set; }
    [field: SerializeField] public LayerMask CanPushLayer { get; set; }
    [field: SerializeField] public AudioDataSO FootStepAudio { get; set; }
    public FootstepController FootstepController { get; set; }
    public LayerMask[] TargetLayers { get; set; }
    public Transform CurrentTarget { get; set; }
    public HealthController HealthController { get; set; }
    public MovementController MovementController { get; set; }

    public EntityTypeSO type;
    public StateMachineSO stateMachine;
    public SpriteLibraryAsset defaultSkin;
    public AnimatorController animationController;

    public Vector3 GetAttackOrigin()
    {
        throw new System.NotImplementedException();
    }

    public void Damage(DamageDataSO damageData, IAttackable hitter, Vector3 origin)
    {
        throw new System.NotImplementedException();
    }

    public void OnDisable()
    {
        throw new System.NotImplementedException();
    }

    public void KnockBack(Vector3 dir, float strength) { }
    public void EndKnockbackEarly() { }

    public void Die()
    {
        throw new System.NotImplementedException();
    }
}