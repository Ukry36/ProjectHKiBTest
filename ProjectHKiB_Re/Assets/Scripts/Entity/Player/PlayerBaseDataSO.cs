using UnityEditor;
using UnityEngine;
using UnityEngine.U2D.Animation;

[CreateAssetMenu(fileName = "PlayerBaseData", menuName = "Scriptable Objects/Data/PlayerBaseData", order = 1)]
public class PlayerBaseDataSO : ScriptableObject, IMovableBase, IAttackableBase, ITargetableBase,
IDodgeableBase, IDamagableBase, ISkinableBase, IAnimatableBase, IFootstepBase, IGraffitiableBase
{
    [NaughtyAttributes.Button]
    public void Save()
    {
        AssetDatabase.SaveAssets();
    }
    
    [field: Header("Move")]
    [field: SerializeField] public float Speed { get; set; }
    [field: SerializeField] public float SprintCoeff { get; set; }
    [field: SerializeField] public LayerMask WallLayer { get; set; }
    [field: SerializeField] public LayerMask CanPushLayer { get; set; }
    [field: SerializeField] public AudioDataSO DefaultFootstepAudio { get; set; }

    [field: Header("Attack")]
    [field: SerializeField] public int BaseATK { get; set; }
    [field: SerializeField] public float CriticalChanceRate { get; set; }
    [field: SerializeField] public float CriticalDamageRate { get; set; }
    [field: SerializeField] public AttackDataSO[] AttackDatas { get; set; }
    [field: SerializeField] public LayerMask[] TargetLayers { get; set; }
    [field: SerializeField] public DamageParticleDataSO DamageParticle { get; set; }

    [field: Header("Dodge")]
    [field: SerializeField] public float BaseDodgeCooltime { get; set; }
    [field: SerializeField] public float InitialDodgeMaxDistance { get; set; }
    [field: SerializeField] public float BaseDodgeSpeed { get; set; }
    [field: SerializeField] public int BaseContinuousDodgeLimit { get; set; }
    [field: SerializeField] public LayerMask KeepDodgeWallLayer { get; set; }
    [field: SerializeField] public float BaseKeepDodgeMaxTime { get; set; }
    [field: SerializeField] public float BaseDodgeInvincibleTime { get; set; }
    [field: SerializeField] public ParticlePlayer KeepDodgeParticle { get; set; }
    [field: SerializeField] public StatBuffCompilation JustDodgeBuff { get; set; }

    [field: Header("Health")]
    [field: SerializeField] public float BaseMaxHP { get; set; }
    [field: SerializeField] public float BaseDEF { get; set; }
    [field: SerializeField] public float Mass { get; set; }
    [field: SerializeField] public AudioDataSO HitSound { get; set; }
    [field: SerializeField] public ParticlePlayer HitParticle { get; set; }

    [field: Header("Graffiti")]
    [field: SerializeField] public StateSO GraffitiAttackState { get; set; }
    [field: SerializeField] public StateSO GraffitiSkillState { get; set; }
    [field: SerializeField] public Vector2 GraffitiTinkerOffset { get; set; }

    [field: Header("Control")]
    [field: SerializeField] public StateMachineSO StateMachine { get; set; }
    [field: Header("Visual")]
    [field: SerializeField] public SkinDataSO SkinData { get; set; }
    [field: SerializeField] public SimpleAnimationDataSO AnimationData { get; set; }
    [field: SerializeField] public SimpleAnimationDataSO EffectAnimationData { get; set; }
    [field: SerializeField] public SpriteLibraryAsset EffectSpriteLibrary { get; set; }
}