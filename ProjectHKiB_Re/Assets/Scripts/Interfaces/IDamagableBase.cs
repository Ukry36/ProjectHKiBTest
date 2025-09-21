using System;
using UnityEngine;
using UnityEngine.Events;

public interface IDamagableBase
{
    public float BaseMaxHP { get; set; }
    public float BaseDEF { get; set; }

    public AudioDataSO HitSound { get; set; }
    public ParticlePlayer HitParticle { get; set; }
}