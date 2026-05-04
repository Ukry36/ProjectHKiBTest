using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Tilemaps;
[RequireComponent(typeof(Tilemap))]

[RequireComponent(typeof(TilemapRenderer))]
public class DynamicHeightMap: MonoBehaviour
{
    private TilemapRenderer tilemapRenderer;
    public float z;

    public void Initialize(float baseZLevel, IMovable player, int sortingOrder)
    {
        tilemapRenderer = GetComponent<TilemapRenderer>();
        tilemapRenderer.sortingOrder = sortingOrder;
        this.z = baseZLevel;
        tilemapRenderer.material.SetFloat("_BaseID", baseZLevel/16);
    }
}