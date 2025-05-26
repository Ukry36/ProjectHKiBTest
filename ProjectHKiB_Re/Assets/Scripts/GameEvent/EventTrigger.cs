using DG.Tweening;
using UnityEngine;

public abstract class EventTrigger : MonoBehaviour
{
    public IEvent Event { get; set; }
    [SerializeField] private float _cooltime;
    protected bool _canTrigger = true;
    [SerializeField] protected ContactFilter2D _contactFilter;
    [SerializeField] protected Collider2D _collider2D;
    protected readonly Collider2D[] colliders = new Collider2D[10];
    private ChunkData chunk;

    protected void Awake()
    {
        chunk = GetComponentInParent<ChunkData>();

        if (!_collider2D)
        {
            if (TryGetComponent(out Collider2D col))
                _collider2D = col;
            else
                Debug.LogError("ERROR: No collider is attatched to the trigger!!!");
        }

    }

    public void Initialize(IEvent @event)
        => this.Event = @event;
    public void CoolTime()
    {
        if (_cooltime > 0)
        {
            _canTrigger = false;
            Sequence sequence = DOTween.Sequence();
            sequence.AppendInterval(_cooltime);
            sequence.OnComplete(() => _canTrigger = true);
            sequence.Play();
        }
    }

    public void Update()
    {
        if (chunk.Active)
            UpdateTrigger();
    }

    public abstract void UpdateTrigger();

}