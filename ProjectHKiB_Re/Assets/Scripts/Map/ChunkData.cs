using System.Collections.Generic;
using UnityEngine;

public class ChunkData : MonoBehaviour
{
    public Collider2D boundary;
    public bool Active { get; private set; }
    public bool SimulateEvent { get; private set; }
    [SerializeField] private Transform[] _transforms;
    private EventTrigger[] _eventTriggers;

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