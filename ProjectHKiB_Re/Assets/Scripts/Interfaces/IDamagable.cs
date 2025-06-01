using UnityEngine;

public interface IDamagable
{
    public int BaseMaxHP { get; set; }
    public int CurrentMaxHP { get => (int)HealthController.MaxHPBuffer.GetBuffedStat(BaseMaxHP); }
    public int HP { get => HealthController.HP; set => HealthController.HP = value; }
    public int BaseDEF { get; set; }
    public int CurrentDef { get => (int)HealthController.DEFBuffer.GetBuffedStat(BaseDEF); }
    public float Resistance { get => HealthController.ResistanceBuffer.GetBuffedStat(0); }
    public bool Invincible { get => HealthController.Invincible.GetBuffedStat(); }
    public bool SuperArmour { get => HealthController.SuperArmour.GetBuffedStat(); }
    public void Damage(DamageDataSO damageData, IAttackable hitter, Vector3 origin)
    => HealthController.Damage(damageData, hitter, origin, this);
    public void Die()
    => HealthController.Die();
    public AudioDataSO HitSound { get; set; }
    public ParticlePlayer HitParticle { get; set; }
    public HealthController HealthController { get; set; }
}