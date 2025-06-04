using UnityEngine;

public class MovementController : MonoBehaviour
{
    [SerializeField] protected MovementManagerSO movementManager;
    private Coroutine knockBackCoroutine;
    private MovementManagerSO.KnockBackEnded OnKnockBackEnded;
    public bool IsKnockbackMove { get; set; }
    public Vector3 LastSetDir { get; set; }
    public IMovable.ExternalForce ExForce { get; set; }
    public bool IsSprinting { get; set; }
    [field: SerializeField] public MovePoint MovePoint { get; set; }

    public void Initialize()
    {
        MovePoint.Initialize();
        ExForce = new(true);
    }
    public virtual void KnockBack(Vector3 dir, float strength, IMovable movable)
    {
        if (strength < movable.Mass) return;
        if (IsKnockbackMove)
        {
            EndKnockbackEarly(movable);
        }
        IsKnockbackMove = true;
        knockBackCoroutine =
        StartCoroutine(movementManager.KnockBackCoroutine(transform, movable, dir, strength, movable.Mass, OnKnockBackEnded));
    }

    public virtual void EndKnockbackEarly(IMovable movable)
    {
        if (IsKnockbackMove)
        {
            movementManager.EndKnockbackEarly(transform, movable);
            StopCoroutine(knockBackCoroutine);
        }
        KnockBackEndCallback();
    }

    protected virtual void KnockBackEndCallback() => IsKnockbackMove = false;
    protected virtual void Awake()
    {
        OnKnockBackEnded += KnockBackEndCallback;
    }
    protected virtual void OnDestroy()
    {
        OnKnockBackEnded -= KnockBackEndCallback;
    }
}