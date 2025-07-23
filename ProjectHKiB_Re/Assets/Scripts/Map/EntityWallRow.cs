using System;
using UnityEngine;
using UnityEngine.Tilemaps;

[Serializable]
public class EntityWallRow
{
    public Transform Transform { get; private set; }
    [SerializeField] private Tilemap _tilemap;
    public EntityWallRow(GameObject grid)
    {
        Transform = grid.transform;
        _tilemap = grid.GetComponentInChildren<Tilemap>();
    }

    public void ClearAllTiles() => _tilemap.ClearAllTiles();

    public void SetTile(Vector3Int position, TileBase tile) => _tilemap.SetTile(position, tile);

    public TileBase GetTile(Vector3Int position) => _tilemap.GetTile(position);

    public void SetTransformMatrix(Vector3Int position, Matrix4x4 flipInfo) => _tilemap.SetTransformMatrix(position, flipInfo);
}