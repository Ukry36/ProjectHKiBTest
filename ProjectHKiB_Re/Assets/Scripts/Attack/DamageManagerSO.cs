using UnityEngine;

[CreateAssetMenu(fileName = "DamageManager", menuName = "Scriptable Objects/Manager/DamageManager", order = 3)]
public class DamageManagerSO : ScriptableObject
{
    public void Damage(DamageDataSO damageData, IAttackable hitter, IDamagable getHit, Vector3 hitPos, bool IsKnockback)
    {
        int value = 0;
        bool isCritical = Random.value < hitter.CriticalChanceRate;
        if (!getHit.Invincible)
        {
            value = (int)
            (
                (
                    damageData.damageCoefficient
                    * hitter.ATK
                    * (1f + (isCritical ? hitter.CriticalDamageRate : 0))
                    * (1f - getHit.Resistance)
                )
                - getHit.DEF
            );
            if (value <= 0) value = 1;
            getHit.HP -= value;
        }
        if (!getHit.Invincible || IsKnockback)
        {
            GameManager.instance.damageParticleManager.PlayHitParticle
            (
                hitter.DamageParticle,
                value,
                value > getHit.MaxHP * 0.5 || IsKnockback,
                isCritical,
                hitPos,
                hitter.DamageIndicatorRandomPosInfo
            );
        }

    }

}