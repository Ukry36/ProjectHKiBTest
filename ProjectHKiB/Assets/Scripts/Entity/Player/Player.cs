using System.Collections.Generic;
using UnityEngine;

public class Player : StateController, IGridMovable, IAttackable, IDamagable, IGraffitiable
{
    //public GearManager gearManager;
    public MovePoint MovePoint { get; set; }
    public CustomVariable<float> Speed { get; set; }
    public CustomVariable<float> SprintCoeff { get; set; }
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
    public LayerMask WallLayer { get; set; }

    public PlayerDataSO playerData;

    public void Initialize()
    {
        MovePoint.Initialize();
        InitializeState(playerData.stateMachine);
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
