
using System.Collections.Generic;

public interface IDamagable
{
    public CustomVariable<float> MaxHP { get; set; }
    public CustomVariable<float> HP { get; set; }
    public CustomVariable<float> DEF { get; set; }
    public List<CustomVariable<Resistance>> Resistances { get; set; }
    public float Mass { get; set; }
    public void Damage(DamageDataSO damageData, IAttackable hitter, IDamagable getHit);
    public AudioDataSO HitSound { get; set; }
    public ParticleDataSO HitParticle { get; set; }
}