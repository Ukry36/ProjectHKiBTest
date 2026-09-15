using System;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class EntityWallGrid : MonoBehaviour
{
    [SerializeField] private MathManagerSO mathManager;
    [SerializeField] private Tilemap _anchor;
    [field: SerializeField] public SerializedDictionary<int, EntityWallRow> Rows { get; private set; } = new();
    [SerializeField] private GameObject _wallRowPrefab;
    [field: SerializeField] public int Top { get; private set; }
    [field: SerializeField] public int Bottom { get; private set; }

    [NaughtyAttributes.Button("generate grid")]
    private void GenerateGrid() => GenerateGrid(Top, Bottom);

    public void GenerateGrid(float top, float bottom)
    {
        Top = mathManager.Round(top);
        Bottom = mathManager.Round(bottom);

        for (int y = Bottom; y < Top; y++)
        {
            if (Rows.ContainsKey(y))
            {
                Rows[y].ClearAllTiles();
            }
            else
            {
                GameObject instance = Instantiate(_wallRowPrefab, transform);
                instance.transform.localPosition = new(0, y, 0);
                Rows[y] = new EntityWallRow(instance);
            }

        }
    }
    [NaughtyAttributes.Button("clear grid")]
    public void ClearGrid()
    {
        for (int y = Bottom; y < Top; y++)
        {
            Rows[y].ClearAllTiles();
        }
    }
    [NaughtyAttributes.Button("destroy grid")]
    public void DestroyGrid()
    {
        for (int i = this.transform.childCount - 1; i > 0; --i)
        {
            DestroyImmediate(this.transform.GetChild(1).gameObject);
        }

        Rows.Clear();
        Bottom = 0;
        Top = 0;
    }

    public void SetTile(Vector3Int position, TileBase tile)
    {
        if (Rows.ContainsKey(position.y))
            Rows[position.y].SetTile(new(position.x, position.z), tile);
    }

    public void GetTile(Vector3Int position)
    {
        if (Rows.ContainsKey(position.y))
            Rows[position.y].GetTile(new(position.x, position.z));
    }

    public void SetTransformMatrix(Vector3Int position, Matrix4x4 flipInfo)
    {
        if (Rows.ContainsKey(position.y))
            Rows[position.y].SetTransformMatrix(new(position.x, position.z), flipInfo);
    }

    public Vector3Int WolrdToCell(Vector3 position) => _anchor.WorldToCell(position);
}