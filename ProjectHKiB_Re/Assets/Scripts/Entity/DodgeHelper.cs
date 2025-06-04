using UnityEngine;

public class DodgeHelper : MonoBehaviour, IDamagable
{
    public int BaseMaxHP { get; set; }
    public int BaseDEF { get; set; }
    public AudioDataSO HitSound { get; set; }
    public ParticlePlayer HitParticle { get; set; }
    [field: SerializeField] public HealthController HealthController { get; set; }
}