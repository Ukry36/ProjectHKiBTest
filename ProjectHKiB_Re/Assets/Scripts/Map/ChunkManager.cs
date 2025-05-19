using System.Collections.Generic;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    private const int MAXCHUNKCOUNT = 100;
    private int _currentChunkCount;
    [SerializeField] private List<Chunk> _currentMapChunkList = new(MAXCHUNKCOUNT);
    [SerializeField] private Transform _chunkParent;
    private static ContactFilter2D _chunkContactFilter;
    public ContactFilter2D ChunkContactFilter;

    public Transform[] chunkActivators;
    public float chunkUpdateDistance;

    [System.Serializable]
    public class Chunk
    {
        public Collider2D boundary;
        [HideInInspector] public bool active;
        [HideInInspector] public int colliderCount;
        [HideInInspector] public Collider2D[] colliders = new Collider2D[1000];
        [HideInInspector] public List<SpriteRenderer> staticRenderers;
        [HideInInspector] public List<EventTrigger> staticTriggers;

        public void InitializeStatics()
        {
            UpdateColliders();
            staticRenderers = new();
            staticTriggers = new();

            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].TryGetComponent(out EventTrigger trigger))
                    staticTriggers.Add(trigger);
            }

            SpriteRenderer[] spriteRenderers = FindObjectsOfType<SpriteRenderer>(true);
            for (int i = 0; i < spriteRenderers.Length; i++)
                if (boundary.OverlapPoint(spriteRenderers[i].transform.position) && spriteRenderers[i].gameObject.isStatic)
                    staticRenderers.Add(spriteRenderers[i]);
        }

        public void UpdateColliders()
        {
            colliderCount = boundary.OverlapCollider(_chunkContactFilter, colliders);
        }

        public void DeactivateChunk()
        {
            active = false;
            int i;
            for (i = 0; i < staticRenderers.Count; i++)
                staticRenderers[i].enabled = false;
            for (i = 0; i < staticTriggers.Count; i++)
                staticTriggers[i].enabled = false;
        }

        public void ActivateChunk()
        {
            active = true;
            int i;
            for (i = 0; i < staticRenderers.Count; i++)
                staticRenderers[i].enabled = true;
            for (i = 0; i < staticTriggers.Count; i++)
                staticTriggers[i].enabled = true;
        }
    }

    public void Awake()
    {
        for (int i = 0; i < MAXCHUNKCOUNT; i++)
        {
            _currentMapChunkList.Add(new());
        }
        _currentChunkCount = 0;
        _chunkContactFilter = ChunkContactFilter;
    }

    public void InitializeChunkList()
    {
        _currentMapChunkList.Clear();
        Collider2D[] colliders = _chunkParent.GetComponentsInChildren<Collider2D>();
        _currentChunkCount = colliders.Length;
        SpriteRenderer[] spriteRenderers = FindObjectsOfType<SpriteRenderer>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            _currentMapChunkList[i].boundary = colliders[i];
            _currentMapChunkList[i].InitializeStatics();
            for (int j = 0; j < spriteRenderers.Length; j++)
                if (colliders[i].OverlapPoint(spriteRenderers[j].transform.position) && spriteRenderers[j].gameObject.isStatic)
                    _currentMapChunkList[i].staticRenderers.Add(spriteRenderers[j]);
            _currentMapChunkList[i].DeactivateChunk();
        }
    }

    public void Update()
    {
        for (int i = 0; i < _currentChunkCount; i++)
        {
            for (int j = 0; j < chunkActivators.Length; j++)
            {
                if (Vector3.Distance(_currentMapChunkList[i].boundary.transform.position, chunkActivators[j].position) < chunkUpdateDistance)
                {
                    if (!_currentMapChunkList[i].active)
                        _currentMapChunkList[i].ActivateChunk();
                }
                else
                {
                    if (_currentMapChunkList[i].active)
                        _currentMapChunkList[i].DeactivateChunk();
                }
            }
            _currentMapChunkList[i].UpdateColliders();
        }

    }
}