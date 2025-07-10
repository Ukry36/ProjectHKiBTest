using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using UnityEngine;

public class EntityWallGrid : MonoBehaviour
{
    [field: SerializeField] public SerializedDictionary<int, EntityWallRow> Rows { get; private set; } = new();
    [SerializeField] private GameObject _wallRowPrefab;
    [field: SerializeField] public int Top { get; private set; }
    [field: SerializeField] public int Bottom { get; private set; }

    [NaughtyAttributes.Button("generate grid")]
    private void GenerateGrid() => GenerateGrid(Top, Bottom);

    public void GenerateGrid(int top, int bottom)
    {
        Top = top;
        Bottom = bottom;

        for (int y = Bottom; y < Top; y++)
        {
            GameObject instance = Instantiate(_wallRowPrefab, transform);
            instance.transform.position = new Vector3(0, y, 0);
            Rows[y] = new EntityWallRow(instance);
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
    public void DestroyGrid()
    {
        for (int y = Bottom; y < Top; y++)
        {
            if (Rows.ContainsKey(y))
                Destroy(Rows[y].Transform.gameObject);
        }
        Rows.Clear();
        Bottom = 0;
        Top = 0;
    }
}