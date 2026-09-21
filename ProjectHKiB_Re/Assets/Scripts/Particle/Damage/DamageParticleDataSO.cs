using System;
using System.Collections.Generic;
using UnityEngine;
[Serializable]
public class Damageparticle
{
    public Sprite[] anim;
}

[CreateAssetMenu(fileName = "DamageParticleData", menuName = "Scriptable Objects/DamageParticleData", order = 0)]
public class DamageParticleDataSO : ScriptableObject
{
    [field: SerializeField] public ParticlePlayer SmallHitParticle { get; set; }
    [field: SerializeField] public ParticlePlayer BigHitParticle { get; set; }
    [field: SerializeField] public ParticlePlayer SmallCriticalHitParticle { get; set; }
    [field: SerializeField] public ParticlePlayer BigCriticalHitParticle { get; set; }
    [field: SerializeField] public DamageParticlePlayer DamageParticlePlayer { get; set; }
    [field: SerializeField] public List<Damageparticle> DamageParticleFont { get; set; }
    [field: SerializeField] public List<Damageparticle> CriticalDamageParticleFont { get; set; }
}