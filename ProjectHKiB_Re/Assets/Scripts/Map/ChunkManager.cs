using System.Collections.Generic;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    [SerializeField] private List<ChunkData> _currentMapChunkList;

    public Collider2D[] chunkActivators;

    public void RegisterChunkData(ChunkData chunkData)
    {
        _currentMapChunkList.Add(chunkData);
        InitializeChunkList();
    }

    public void UnregisterChunkDataAll()
    {
        _currentMapChunkList.Clear();
    }

    public void InitializeChunkList()
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
            for (int j = 0; j < chunkActivators.Length; j++)
            {
                if (chunkActivators[j].Distance(_currentMapChunkList[i].boundary).distance <= 0)
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
    }

    public void Update()
    {
        UpdateChunkActivation();
    }
}