using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageParticlePlayer : MonoBehaviour
{
    [field: SerializeField] public int PoolSize { get; set; }
    [SerializeField] private ParticlePlayer _damageParticle;

    private const int MAXDAMAGEDIGITS = 10;
    public readonly ParticlePlayer[] digits = new ParticlePlayer[MAXDAMAGEDIGITS];

    private void Awake()
    {
        for (int i = 0; i < MAXDAMAGEDIGITS; i++)
        {
            digits[i] = Instantiate(_damageParticle.gameObject, this.transform).GetComponent<ParticlePlayer>();
        }
    }

    public void InitializeFromPool()
    {
        this.gameObject.SetActive(true);
    }
}