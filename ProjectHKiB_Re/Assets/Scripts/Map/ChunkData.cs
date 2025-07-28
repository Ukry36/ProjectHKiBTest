using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(BoxCollider2D))]
public class ChunkData : MonoBehaviour
{
    public BoxCollider2D boundary;
    public bool Active { get; private set; }
    [SerializeField] private Transform[] _transforms;

    private bool _toggle;
    [NaughtyAttributes.Button("toggle")]
    public void Toggle()
    {
        _toggle = !_toggle;

        if (_toggle) DeactivateChunk();
        else ActivateChunk();
    }

    [NaughtyAttributes.Button("autogenerate boundary")]
    public void GenerateBoundary()
    {
        boundary = GetComponent<BoxCollider2D>();
        TilemapCollider2D wall = GetComponentInChildren<TilemapCollider2D>();
        if (wall)
        {
            boundary.size = wall.bounds.size;
            boundary.offset = wall.bounds.center - this.transform.position;//+ wall.transform.position;
        }
        else
        {
            Debug.Log("Failed to generate boundary: TilemapCollider2D is supported only");
        }
    }

    [NaughtyAttributes.Button("assign triggers")]
    public void AssignTriggers()
    {
        if (!boundary) Debug.LogError("ERROR: Boundary not attatched!!!");

        Collider2D[] cols = new Collider2D[10000];
        List<GameEventTrigger> trigs = new();
        int length = boundary.OverlapCollider(new(), cols);
        for (int i = 0; i < length; i++)
        {
            if (cols[i].TryGetComponent(out GameEventTrigger trigger))
            {
                trigs.Add(trigger);
                trigger.chunk = this;
            }
        }
        triggers = trigs.ToArray();
    }
    [NaughtyAttributes.ReadOnly][SerializeField] private GameEventTrigger[] triggers;

    public void Awake()
    {
        GameManager.instance.chunkManager.RegisterChunkData(this);
    }

    public virtual void Initialize()
    {
        DeactivateChunk();
    }

    public virtual void DeactivateChunk()
    {
        Active = false;
        for (int i = 0; i < _transforms.Length; i++)
            _transforms[i].gameObject.SetActive(false);
    }

    public virtual void ActivateChunk()
    {
        Active = true;
        for (int i = 0; i < _transforms.Length; i++)
            _transforms[i].gameObject.SetActive(true);
    }
}