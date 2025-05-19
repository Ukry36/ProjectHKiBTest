using DG.Tweening;
using UnityEngine;

public abstract class EventTrigger : MonoBehaviour
{
    public IEvent Event { get; set; }
    [SerializeField] private float _cooltime;
    protected bool _canTrigger = true;
    [SerializeField] protected ContactFilter2D _contactFilter;
    protected Collider2D _collider2D;
    protected readonly Collider2D[] colliders = new Collider2D[10];

    protected void Awake()
    {
        _collider2D = GetComponent<Collider2D>();
    }

    public void Initialize(IEvent @event)
        => this.Event = @event;
    public void CoolTime()
    {
        _canTrigger = false;
        Sequence sequence = DOTween.Sequence();
        sequence.AppendInterval(_cooltime);
        sequence.OnComplete(() => _canTrigger = true);
        sequence.Play();
    }
}