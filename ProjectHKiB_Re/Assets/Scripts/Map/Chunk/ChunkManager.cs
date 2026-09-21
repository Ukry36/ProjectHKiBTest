using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    [SerializeField] private List<ChunkData> _currentMapChunkList;

    public Collider2D chunkActivator;

    public void RegisterChunkData(ChunkData chunkData)
    {
        _currentMapChunkList.Add(chunkData);
        chunkData.Initialize();
    }

    public void RegisterChunkDataAll()
    {
        _currentMapChunkList = FindObjectsOfType<ChunkData>().ToList();
        InitializeChunkDataAll();
    }

    public void UnregisterChunkDataAll()
    {
        _currentMapChunkList.Clear();
    }

    public void InitializeChunkDataAll()
    {
        for (int i = 0; i < _currentMapChunkList.Count; i++)
        {
            _currentMapChunkList[i].Initialize();
        }
    }

    public void UpdateChunkActivation()
    {
        for (int i = 0; i < _currentMapChunkList.Count; i++)
        {
            if (chunkActivator.Distance(_currentMapChunkList[i].boundary).distance <= 0)
            {
                if (!_currentMapChunkList[i].Active)
                    _currentMapChunkList[i].ActivateChunk();
            }
            else
            {
                if (_currentMapChunkList[i].Active)
                    _currentMapChunkList[i].DeactivateChunk();
            }
        }
    }

    public void Update()
    {
        UpdateChunkActivation();
    }
    /*
        public void Start()
        {
            RegisterChunkDataAll();
        }
        */
}