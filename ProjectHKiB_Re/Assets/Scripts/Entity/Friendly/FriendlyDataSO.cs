
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.U2D.Animation;

[CreateAssetMenu(fileName = "Friendly Data", menuName = "Scriptable Objects/Data/Friendly Data", order = 1)]
public class FriendlyDataSO : ScriptableObject, IMovable, IAttackable, IDamagable, IPoolable, IInteractable
{
    [field: SerializeField] public StatContainer MaxHP { get; set; }
    [field: SerializeField] public StatContainer HP { get; set; }
    [field: SerializeField] public StatContainer DEF { get; set; }
    [field: SerializeField] public StatContainer Resistance { get; set; }
    [field: SerializeField] public StatContainer ATK { get; set; }
    [field: SerializeField] public StatContainer CriticalChanceRate { get; set; }
    [field: SerializeField] public StatContainer CriticalDamageRate { get; set; }
    public AttackController AttackController { get; set; }
    public int LastAttackNum { get; set; }
    [field: SerializeField] public DamageParticleDataSO DamageParticle { get; set; }
    public float DamageIndicatorRandomPosInfo { get; set; } = 0;
    [field: SerializeField] public float Mass { get; set; }
    [field: SerializeField] public StatContainer Speed { get; set; }
    [field: SerializeField] public StatContainer SprintCoeff { get; set; }
    public MovePoint MovePoint { get; set; }
    [field: SerializeField] public AttackDataSO[] AttackDatas { get; set; }
    [field: SerializeField] public int PoolSize { get; set; }
    public Collider2D Trigger { get; set; }
    [field: SerializeField] public UnityEvent Event { get; set; }
    [field: SerializeField] public float TriggerCoolTime { get; set; }
    [field: SerializeField] public AudioDataSO HitSound { get; set; }
    [field: SerializeField] public ParticlePlayer HitParticle { get; set; }
    [field: SerializeField] public LayerMask WallLayer { get; set; }
    [field: SerializeField] public LayerMask CanPushLayer { get; set; }
    public bool IsSprinting { get; set; } = false;
    [field: SerializeField] public AudioDataSO FootStepAudio { get; set; }
    public FootstepController FootstepController { get; set; }
    public IMovable.ExternalForce ExForce { get; set; } = new();
    public LayerMask[] TargetLayers { get; set; }
    public Transform CurrentTarget { get; set; }

    public EntityTypeSO type;
    public StateMachineSO stateMachine;
    public SpriteLibraryAsset defaultSkin;
    public AnimatorController animationController;

    public Vector3 GetAttackOrigin()
    {
        throw new System.NotImplementedException();
    }

    public void Damage(DamageDataSO damageData, IAttackable hitter)
    {
        throw new System.NotImplementedException();
    }

    public void OnDisable()
    {
        throw new System.NotImplementedException();
    }

    public void KnockBack(Vector3 dir, float strength)
    {
        throw new System.NotImplementedException();
    }

    public void Die()
    {
        throw new System.NotImplementedException();
    }
}