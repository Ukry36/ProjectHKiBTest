
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.U2D.Animation;

[CreateAssetMenu(fileName = "Enemy Data", menuName = "Scriptable Objects/Data/Enemy Data", order = 1)]
public class EnemyDataSO : ScriptableObject, IID, IMovable, IAttackable, IDamagable, IPoolable
{
    [field: SerializeField] public int ID { get; set; }
    [field: SerializeField] public string Name { get; set; }
    [field: SerializeField] public StatContainer MaxHP { get; set; }
    [field: SerializeField] public StatContainer HP { get; set; }
    [field: SerializeField] public StatContainer DEF { get; set; }
    [field: SerializeField] public List<CustomVariable<Resistance>> Resistances { get; set; }
    [field: SerializeField] public StatContainer ATK { get; set; }
    [field: SerializeField] public StatContainer CriticalChanceRate { get; set; }
    [field: SerializeField] public StatContainer CriticalDamageRate { get; set; }
    [field: SerializeField] public float Mass { get; set; }
    [field: SerializeField] public StatContainer Speed { get; set; }
    [field: SerializeField] public StatContainer SprintCoeff { get; set; }
    public MovePoint MovePoint { get; set; }
    [field: SerializeField] public DamageDataSO[] AttackDatas { get; set; }
    [field: SerializeField] public int PoolSize { get; set; }
    [field: SerializeField] public AudioDataSO HitSound { get; set; }
    [field: SerializeField] public ParticleDataSO HitParticle { get; set; }
    [field: SerializeField] public LayerMask WallLayer { get; set; }
    public bool IsSprinting { get; set; } = false;
    [field: SerializeField] public AudioDataSO FootStepAudio { get; set; }

    public EntityTypeSO type;
    public StateMachineSO stateMachine;
    public SpriteLibraryAsset defaultSkin;
    public AnimatorController animationController;

    public Vector3 GetAttackOrigin()
    {
        throw new System.NotSupportedException();
    }

    public void Damage(DamageDataSO damageData, IAttackable hitter, IDamagable getHit)
    {
        throw new System.NotSupportedException();
    }

    public void OnDisable()
    {
        throw new System.NotSupportedException();
    }
}