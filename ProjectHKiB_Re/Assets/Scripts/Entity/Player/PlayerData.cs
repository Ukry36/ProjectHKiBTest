using System;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.U2D.Animation;
[Serializable]
public class PlayerData : IMovable, IAttackable, IDodgeable, IDamagable, IGraffitiable, ISkinable, IStateControllable
{
    [field: SerializeField]
    public MovePoint MovePoint { get; set; }
    /*[field: SerializeField] */
    public StatContainer Speed { get; set; }
    /*[field: SerializeField] */
    public StatContainer SprintCoeff { get; set; }
    /*[field: SerializeField] */
    public LayerMask WallLayer { get; set; }
    public bool IsSprinting { get; set; }
    /*[field: SerializeField] */
    public AudioDataSO FootStepAudio { get; set; }

    /*[field: SerializeField] */
    public StatContainer ATK { get; set; }
    /*[field: SerializeField] */
    public StatContainer CriticalChanceRate { get; set; }
    /*[field: SerializeField] */
    public StatContainer CriticalDamageRate { get; set; }
    /*[field: SerializeField] */
    public DamageDataSO[] AttackDatas { get; set; }

    /*[field: SerializeField] */
    public CustomVariable<float> DodgeCooltime { get; set; }
    /*[field: SerializeField] */
    public CustomVariable<float> ContinuousDodgeLimit { get; set; }
    /*[field: SerializeField] */
    public CustomVariable<float> KeepDodgeMaxTime { get; set; }
    /*[field: SerializeField] */
    public CustomVariable<float> KeepDodgeMaxDistance { get; set; }

    /*[field: SerializeField] */
    public StatContainer MaxHP { get; set; }
    /*[field: SerializeField] */
    public StatContainer HP { get; set; }
    /*[field: SerializeField] */
    public StatContainer DEF { get; set; }
    /*[field: SerializeField] */
    public List<CustomVariable<Resistance>> Resistances { get; set; }
    /*[field: SerializeField] */
    public float Mass { get; set; }
    /*[field: SerializeField] */
    public AudioDataSO HitSound { get; set; }
    /*[field: SerializeField] */
    public ParticleDataSO HitParticle { get; set; }

    /*[field: SerializeField] */
    public StatContainer MaxGP { get; set; }
    /*[field: SerializeField] */
    public StatContainer GP { get; set; }

    /*[field: SerializeField] */
    public SkinDataSO SkinData { get; set; }

    /*[field: SerializeField] */
    public StateMachineSO StateMachine { get; set; }
    /*[field: SerializeField] */
    public AnimatorController AnimatorController { get; set; }

    public MergedPlayerBaseData PlayerBaseData
    {
        get => _playerBaseData;
        set
        {
            _playerBaseData = value;
            UpdateDatas();
        }
    }
    private MergedPlayerBaseData _playerBaseData;


    [SerializeField] private DatabaseManagerSO databaseManager;


    public void SetGear(MergedPlayerBaseData mergedPlayerBaseData)
    {
        PlayerBaseData = mergedPlayerBaseData;
    }


    public void Initialize()
    {
        UpdateDatas();
    }

    public void UpdateDatas()
    {
        MovePoint.Initialize();
        databaseManager.SetIMovable(this, PlayerBaseData);
        databaseManager.SetIAttackable(this, PlayerBaseData);
        databaseManager.SetIDodgeable(this, PlayerBaseData);
        databaseManager.SetIDamagable(this, PlayerBaseData);
        databaseManager.SetIDodgeable(this, PlayerBaseData);
        databaseManager.SetGraffiriable(this, PlayerBaseData);
        databaseManager.SetISkinable(this, PlayerBaseData);
        databaseManager.SetIStateControllable(this, PlayerBaseData);
    }

    public void Damage(DamageDataSO damageData, IAttackable hitter, IDamagable getHit)
    {
        throw new System.NotImplementedException();
    }

    public Vector3 GetAttackOrigin()
    {
        throw new System.NotImplementedException();
    }
}
