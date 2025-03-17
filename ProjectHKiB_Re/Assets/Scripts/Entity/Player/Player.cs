using System.Collections.Generic;
using UnityEngine;
using System;

public class Player : MonoBehaviour, IGridMovable, IAttackable, IDamagable, IGraffitiable
{
    //public GearManager gearManager;
    [field: SerializeField] public MovePoint MovePoint { get; set; }
    [field: SerializeField] public CustomVariable<float> Speed { get; set; }
    [field: SerializeField] public CustomVariable<float> SprintCoeff { get; set; }
    public CustomVariable<float> ATK { get; set; }
    public CustomVariable<float> CriticalChanceRate { get; set; }
    public CustomVariable<float> CriticalDamageRate { get; set; }
    public DamageDataSO[] AttackDatas { get; set; }
    public CustomVariable<float> MaxHP { get; set; }
    public CustomVariable<float> HP { get; set; }
    public CustomVariable<float> DEF { get; set; }
    public List<CustomVariable<Resistance>> Resistances { get; set; }
    public float Mass { get; set; }
    public AudioDataSO HitSound { get; set; }
    public ParticleDataSO HitParticle { get; set; }
    public CustomVariable<float> MaxGP { get; set; }
    public CustomVariable<float> GP { get; set; }
    [field: SerializeField] public LayerMask WallLayer { get; set; }

    public PlayerDataSO playerData;

    [SerializeField] private StateController stateController;

    public void Start()
    {
        Initialize();
    }
    public void Initialize()
    {
        MovePoint.Initialize();
        stateController.InitializeState(playerData.stateMachine);
        stateController.RegisterInterface<IGridMovable>(this);
        stateController.RegisterInterface(this);
    }

    public MovementManagerSO movementManager;
    public void Update()
    {
        movementManager.FollowMovePoint(transform, MovePoint.transform, Speed.Value);
    }

    public void Damage(DamageDataSO damageData, IAttackable hitter, IDamagable getHit)
    {
        throw new System.NotImplementedException();
    }

    public System.Numerics.Vector3 GetAttackOrigin()
    {
        throw new System.NotImplementedException();
    }
}
