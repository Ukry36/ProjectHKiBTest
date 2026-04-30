using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Tilemaps;
[RequireComponent(typeof(Tilemap))]

[RequireComponent(typeof(TilemapRenderer))]
public class DynamicHeightMap: MonoBehaviour
{
    private TilemapRenderer tilemapRenderer;
    [SerializeField] [ReadOnly] private float baseZLevel;
    [SerializeField] [ReadOnly] private float dynamicZlevel;
    private IMovable player;

    public void Initialize(float baseZLevel, IMovable player)
    {
        tilemapRenderer = GetComponent<TilemapRenderer>();
        this.baseZLevel = baseZLevel;
        this.player = player;
    }

    public void SetRelativeZlevel()
    {
        dynamicZlevel = baseZLevel - player.ZLevel;
        tilemapRenderer.material.SetFloat("_BaseID", (baseZLevel - player.ZLevel)/256);
    }
}