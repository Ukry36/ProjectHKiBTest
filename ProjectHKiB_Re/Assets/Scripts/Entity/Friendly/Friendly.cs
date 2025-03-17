using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Friendly : StateController, IGridMovable, IAttackable, IDamagable, IPoolable, IInteractable
{
    public Collider2D Trigger { get; set; }
    [field: SerializeField] public UnityEvent Event { get; set; }
    [field: SerializeField] public float TriggerCoolTime { get; set; }
    public CustomVariable<float> ATK { get; set; }
    public CustomVariable<float> CriticalChanceRate { get; set; }
    public CustomVariable<float> CriticalDamageRate { get; set; }
    public DamageDataSO[] AttackDatas { get; set; }
    public MovePoint MovePoint { get; set; }
    public CustomVariable<float> Speed { get; set; }
    public CustomVariable<float> SprintCoeff { get; set; }
    public CustomVariable<float> MaxHP { get; set; }
    public CustomVariable<float> HP { get; set; }
    public CustomVariable<float> DEF { get; set; }
    public List<CustomVariable<Resistance>> Resistances { get; set; }
    public float Mass { get; set; }
    public int PoolSize { get; set; }
    public AudioDataSO HitSound { get; set; }
    public ParticleDataSO HitParticle { get; set; }
    public LayerMask WallLayer { get; set; }

    public delegate void GameObjectDisabled(int ID, int hash);
    public event GameObjectDisabled OnGameObjectDisabled;

    public FriendlyDataSO friendlyBaseData;

    public void Initialize()
    {
        MovePoint.Initialize();
        InitializeState(friendlyBaseData.stateMachine);
        if (friendlyBaseData)
            InitializeFromPool(friendlyBaseData);
    }

    public void InitializeFromPool(FriendlyDataSO friendlyData)
    {
        friendlyBaseData = friendlyData;
    }

    public void OnDisable()
    {
        OnGameObjectDisabled?.Invoke(friendlyBaseData.ID, this.gameObject.GetHashCode());
    }

    public System.Numerics.Vector3 GetAttackOrigin()
    {
        throw new System.NotImplementedException();
    }

    public void Damage(DamageDataSO damageData, IAttackable hitter, IDamagable getHit)
    {
        throw new System.NotImplementedException();
    }
}
