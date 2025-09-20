using System.Collections.Generic;
using UnityEngine;

public abstract class Entity : MonoBehaviour, IDamagable, IMovable
{
    public int BaseMaxHP { get; set; }
    public int BaseDEF { get; set; }
    public float Mass { get; set; }
    public AudioDataSO HitSound { get; set; }
    public ParticlePlayer HitParticle { get; set; }
    public float Speed { get; set; }
    public float SprintCoeff { get; set; }
    public LayerMask WallLayer { get; set; }
    public LayerMask CanPushLayer { get; set; }
    public AudioDataSO FootStepAudio { get; set; }
    [field: SerializeField] public FootstepController FootstepController { get; set; }
    [field: SerializeField] public MovementController MovementController { get; set; }
    [field: SerializeField] public HealthController HealthController { get; set; }

    public virtual void Initialize()
    {
        HealthController.Initialize(this, BaseMaxHP);
        MovementController.Initialize();
    }
}