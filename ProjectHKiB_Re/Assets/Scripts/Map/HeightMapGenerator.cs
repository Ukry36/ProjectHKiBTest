
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;
[RequireComponent(typeof(Tilemap))]

[RequireComponent(typeof(TilemapRenderer))]
public class HeightMapGenerator: MonoBehaviour
{
    public Tilemap referenceTilemap;

    public void Initialize(GameObject dynamicHeightPrefab, Dictionary<TileBase, float> zValueMap)
    {
        Tilemap heightTilemap = GetComponent<Tilemap>();
        TilemapRenderer renderer = GetComponent<TilemapRenderer>();
        Dictionary<float, DynamicHeightMap> childs = new();
        BoundsInt bounds = heightTilemap.cellBounds;
        IMovable player = GameManager.instance.player.GetInterface<IMovable>();

        for (int x = bounds.min.x; x < bounds.max.x; x++)
        {
            for (int y = bounds.min.y; y < bounds.max.y; y++)
            {
                Vector3Int pos = Vector3Int.right * x + Vector3Int.up * y;
                TileBase tile = heightTilemap.GetTile(pos);
                TileBase refTile = referenceTilemap.GetTile(pos);
                
                if (tile != null && zValueMap.TryGetValue(tile, out float z))
                {
                    if (!childs.ContainsKey(z))
                    {
                        childs[z] = Instantiate(dynamicHeightPrefab, transform.position, Quaternion.identity, transform.parent).GetComponent<DynamicHeightMap>();
                        childs[z].Initialize(z, player, renderer.sortingOrder);
                    }
                    Tilemap tilemap = childs[z].GetComponent<Tilemap>();
                    tilemap.SetTile(pos, refTile);
                    tilemap.SetTransformMatrix(pos, referenceTilemap.GetTransformMatrix(pos));
                }
            }
        }
        Destroy(gameObject);
    }
}