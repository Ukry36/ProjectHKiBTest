using System.Collections.Generic;
using UnityEngine;

public abstract class Entity : MonoBehaviour, IDamagable, IMovable
{
    public StatContainer MaxHP { get; set; }
    public StatContainer HP { get; set; }
    public StatContainer DEF { get; set; }
    public StatContainer Resistance { get; set; }
    public float Mass { get; set; }
    public AudioDataSO HitSound { get; set; }
    public ParticlePlayer HitParticle { get; set; }
    public StatContainer Speed { get; set; }
    public StatContainer SprintCoeff { get; set; }
    public LayerMask WallLayer { get; set; }
    public LayerMask CanPushLayer { get; set; }
    public bool IsSprinting { get; set; }
    public AudioDataSO FootStepAudio { get; set; }
    [field: SerializeField] public MovePoint MovePoint { get; set; }
    [field: SerializeField] public FootstepController FootstepController { get; set; }
    public IMovable.ExternalForce ExForce { get; set; } = new(true);

    public virtual void Damage(DamageDataSO damageData, IAttackable hitter, Vector3 origin)
    {
        if (damageData.knockBack > Mass)
        {
            KnockBack(transform.position - origin, damageData.knockBack);
        }
        damageManager.Damage(damageData, hitter, this, transform);

        if (HP.Value <= 0)
            Die();
    }
    public virtual void Die()
    {
        MovePoint.Die();
        gameObject.SetActive(false);
    }

    [SerializeField] protected DamageManagerSO damageManager;
    [SerializeField] protected MovementManagerSO movementManager;
    public bool IsKnockbackMove { get; set; } = false;
    private Coroutine knockBackCoroutine;
    private MovementManagerSO.KnockBackEnded OnKnockBackEnded;
    protected virtual void KnockBackEndCallback() => IsKnockbackMove = false;
    protected virtual void Awake()
    {
        OnKnockBackEnded += KnockBackEndCallback;
    }
    protected virtual void OnDestroy()
    {
        OnKnockBackEnded -= KnockBackEndCallback;
    }

    public virtual void EndKnockbackEarly()
    {
        if (IsKnockbackMove)
        {
            movementManager.EndKnockbackEarly(transform, this);
            StopCoroutine(knockBackCoroutine);
        }
        KnockBackEndCallback();
    }

    public virtual void KnockBack(Vector3 dir, float strength)
    {
        if (strength < Mass) return;
        if (IsKnockbackMove)
        {
            EndKnockbackEarly();
        }
        IsKnockbackMove = true;
        knockBackCoroutine =
        StartCoroutine(movementManager.KnockBackCoroutine(transform, this, dir, strength, Mass, OnKnockBackEnded));
    }
}