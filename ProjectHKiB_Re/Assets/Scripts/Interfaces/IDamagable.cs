
using System.Collections.Generic;

public interface IDamagable
{
    public StatContainer MaxHP { get; set; }
    public StatContainer HP { get; set; }
    public StatContainer DEF { get; set; }
    public List<CustomVariable<Resistance>> Resistances { get; set; }
    public float Mass { get; set; }
    public void Damage(DamageDataSO damageData, IAttackable hitter, IDamagable getHit);
    public AudioDataSO HitSound { get; set; }
    public ParticleDataSO HitParticle { get; set; }
}