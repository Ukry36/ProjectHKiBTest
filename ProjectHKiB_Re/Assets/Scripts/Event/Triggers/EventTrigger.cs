using DG.Tweening;
using UnityEngine;

public abstract class GameEventTrigger : MonoBehaviour
{
    public GameEvent Event { get; set; }
    [SerializeField] private float _cooltime;
    protected bool _canTrigger = true;
    [SerializeField] protected LayerMask _layerMask;
    protected ContactFilter2D _contactFilter = new();
    [SerializeField] protected ZCollider2D _collider2D;
    protected readonly Collider2D[] colliders = new Collider2D[10];
    public ChunkData chunk;

    protected void Awake()
    {
        //chunk = GetComponentInParent<ChunkData>();

        if (!_collider2D)
        {
            if (TryGetComponent(out ZCollider2D col))
                _collider2D = col;
            else
                Debug.LogError("ERROR: No collider is attatched to the trigger!!!");
        }
        _contactFilter.useLayerMask = true;
        _contactFilter.layerMask = _layerMask;
    }

    public void Initialize(GameEvent @event)
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
        else _canTrigger = true;
    }

    public void FixedUpdate()
    {
        if (gameObject.activeSelf)
        {
            if (chunk)
            {
                if (chunk.Active)
                    UpdateTrigger();
            }
            else UpdateTrigger();
        }
    }

    public abstract void UpdateTrigger();

}