using UnityEngine;

public interface IDamagable
{
    public StatContainer MaxHP { get; set; }
    public StatContainer HP { get; set; }
    public StatContainer DEF { get; set; }
    public StatContainer Resistance { get; set; }
    public float Mass { get; set; }
    public void Damage(DamageDataSO damageData, IAttackable hitter);
    public void KnockBack(Vector3 dir, float strength);
    public void Die();
    public AudioDataSO HitSound { get; set; }
    public ParticlePlayer HitParticle { get; set; }
}