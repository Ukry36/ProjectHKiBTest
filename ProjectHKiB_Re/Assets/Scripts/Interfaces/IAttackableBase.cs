using UnityEngine.U2D.Animation;

public interface IAttackableBase
{
    public int BaseATK { get; set; }

    public float CriticalChanceRate { get; set; }
    public float CriticalDamageRate { get; set; }
    public AttackDataSO[] AttackDatas { get; set; }
    public DamageParticleDataSO DamageParticle { get; set; }
    public SimpleAnimationDataSO EffectAnimationData { get; set; }
    public SpriteLibraryAsset EffectSpriteLibrary { get; set; }
}