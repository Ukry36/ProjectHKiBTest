using System;
using UnityEngine;
using UnityEngine.Tilemaps;

[Serializable]
public class EntityWallRow
{
    public Transform Transform { get; private set; }
    private readonly Tilemap _tilemap;
    public EntityWallRow(GameObject grid)
    {
        Transform = grid.transform;
        _tilemap = grid.GetComponentInChildren<Tilemap>();
    }

    public void ClearAllTiles() => _tilemap.ClearAllTiles();
    public void SetTile(Vector3 position, TileBase tile)
    {
        _tilemap.SetTile(new Vector3Int(
        (int)position.x,
        (int)(position.y - Transform.localPosition.y)), tile);
        if (tile)
            Debug.Log(position.x + ", " + tile.name);
        else
            Debug.Log(position.x + ", void");
    }
    public void SetTransformMatrix(Vector3Int position, Matrix4x4 flipInfo) => _tilemap.SetTransformMatrix(position, flipInfo);
}