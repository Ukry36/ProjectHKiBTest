using UnityEngine;

[CreateAssetMenu(fileName = "DamageManager", menuName = "Scriptable Objects/Manager/DamageManager", order = 3)]
public class DamageManagerSO : ScriptableObject
{
    public void Damage(DamageDataSO damageData, IAttackable hitter, IDamagable getHit, Transform hitTransform)
    {
        bool isCritical = Random.value < hitter.CriticalChanceRate.Value;
        int value = (int)
        (
            (
                damageData.damageCoefficient
                * hitter.ATK.Value
                * (1f + (isCritical ? hitter.CriticalDamageRate.Value : 0))
                * (1f - getHit.Resistance.Value)
            )
            - getHit.DEF.Value
        );
        if (value <= 0) value = 1;
        getHit.HP.Value -= value;
        GameManager.instance.damageParticleManager.PlayHitParticle
        (
            hitter.DamageParticle,
            value,
            value > getHit.MaxHP.Value * 0.5 || damageData.knockBack > getHit.Mass,
            isCritical,
            hitTransform,
            hitter.DamageIndicatorRandomPosInfo
        );
    }

}