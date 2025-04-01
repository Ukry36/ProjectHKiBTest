
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerBaseData", menuName = "Scriptable Objects/Data/PlayerBaseData", order = 1)]
public class PlayerBaseDataSO : ScriptableObject, IID, IMovable, IAttackable, IDodgeable, IDamagable, IGraffitiable, ISkinable, IStateControllable
{
    [field: SerializeField] public int ID { get; set; }
    [field: SerializeField] public string Name { get; set; }

    public MovePoint MovePoint { get; set; }
    [field: SerializeField] public StatContainer Speed { get; set; }
    [field: SerializeField] public StatContainer SprintCoeff { get; set; }
    [field: SerializeField] public LayerMask WallLayer { get; set; }
    public bool IsSprinting { get; set; }
    [field: SerializeField] public AudioDataSO FootStepAudio { get; set; }

    [field: SerializeField] public StatContainer ATK { get; set; }
    [field: SerializeField] public StatContainer CriticalChanceRate { get; set; }
    [field: SerializeField] public StatContainer CriticalDamageRate { get; set; }
    [field: SerializeField] public DamageDataSO[] AttackDatas { get; set; }

    [field: SerializeField] public CustomVariable<float> DodgeCooltime { get; set; }
    [field: SerializeField] public CustomVariable<float> ContinuousDodgeLimit { get; set; }
    [field: SerializeField] public CustomVariable<float> KeepDodgeMaxTime { get; set; }
    [field: SerializeField] public CustomVariable<float> KeepDodgeMaxDistance { get; set; }

    [field: SerializeField] public StatContainer MaxHP { get; set; }
    public StatContainer HP { get; set; }
    [field: SerializeField] public StatContainer DEF { get; set; }
    [field: SerializeField] public List<CustomVariable<Resistance>> Resistances { get; set; }
    [field: SerializeField] public float Mass { get; set; }
    [field: SerializeField] public AudioDataSO HitSound { get; set; }
    [field: SerializeField] public ParticleDataSO HitParticle { get; set; }

    [field: SerializeField] public StatContainer MaxGP { get; set; }
    public StatContainer GP { get; set; }

    [field: SerializeField] public SkinDataSO SkinData { get; set; }

    [field: SerializeField] public StateMachineSO StateMachine { get; set; }
    [field: SerializeField] public AnimatorController AnimatorController { get; set; }

    public EntityTypeSO type;


    public void Damage(DamageDataSO damageData, IAttackable hitter, IDamagable getHit)
    {
        throw new System.NotSupportedException();
    }

    public Vector3 GetAttackOrigin()
    {
        throw new System.NotSupportedException();
    }
}