using System;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Tilemaps;

[Serializable]
public class TilePosInfo
{
    public Vector3Int pos;
    public float placeOrder;
    public TileBase placeTile;
    public TileBase outlineTile;
    public GameObject Object;

    public TilePosInfo(Vector3Int pos, float placeOrder, TileBase placeTile, TileBase outlineTile, GameObject Object)
    {
        this.pos = pos;
        this.placeOrder = placeOrder;
        this.placeTile = placeTile;
        this.outlineTile = outlineTile;
        this.Object = Object;
    }

    public void SetTile(Tilemap quantumTilemap, Tilemap outlineTilemap, bool placeOrRemove)
    {
        quantumTilemap.SetTile(pos, placeOrRemove ? placeTile : null);
        outlineTilemap.SetTile(pos, !placeOrRemove ? outlineTile : null);
        if (Object != null)
            Object.SetActive(placeOrRemove);
    }
}

public class WaveTileManager : MonoBehaviour
{
    public Tilemap quantumTilemap;
    public Tilemap outlineTilemap;
    [HideInInspector] public WaveSequence waveSequence;
    public delegate void TileSetCompleted();
    public event TileSetCompleted OnTileSetCompleted;
    public LayerMask layerMask;
    private List<TilePosInfo> _TileInfoList;
    public TileBase outlineTile;

    [SerializeField] private AnimationCurve _XCurve;
    [SerializeField] private AnimationCurve _YCurve;

    [SerializeField][MaxValue(1), MinValue(0)] private float _shuffle;
    [SerializeField][MinValue(1)] private int _stepPerWave;
    [SerializeField][MinValue(0)] private float _delayBetweenStep;
    [SerializeField][MaxValue(1), MinValue(0)] private float _stepSpeed;

    private const bool PLACE = true;
    private const bool REMOVE = false;

    [Button("front")]
    public void Front02()
    {
        FrontWaveCompleted(currentFrontWave, tempWaveCount);
        currentFrontWave++;
        currentFrontWave %= tempWaveCount;
    }

    [Button("rear")]
    public void Rear02()
    {
        RearWaveCompleted(currentRearWave, tempWaveCount);
        currentRearWave++;
        currentRearWave %= tempWaveCount;
    }

    public int tempWaveCount;

    [ReadOnly] public int currentFrontWave;
    [ReadOnly] public int currentRearWave;

    [Button("erase")]
    public void Erase()
    {
        currentFrontWave = 0;
        currentRearWave = 0;
        InitTileMap(_TileInfoList, REMOVE);
    }

    [Button("init")]
    public void Init()
    {
        currentFrontWave = 0;
        currentRearWave = 0;
        InitTileMap(_TileInfoList, PLACE);
        ScanTiles();

    }


    void Start()
    {
        ScanTiles();
    }

    private void ScanTiles()
    {
        List<TilePosInfo> TileInfoList = new();
        BoundsInt bounds = quantumTilemap.cellBounds;
        HashSet<Collider2D> processColliders = new();
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int pos = new(x, y);
                TileBase tile = quantumTilemap.GetTile(pos);

                GameObject Object = null;

                Collider2D collider = Physics2D.OverlapPoint(quantumTilemap.GetCellCenterWorld(pos), layerMask);

                if (collider != null && collider is BoxCollider2D)
                {
                    processColliders.Add(collider);
                    Object = collider.gameObject;
                    Object.SetActive(false);
                }

                if (tile != null)
                {
                    TileInfoList.Add(new TilePosInfo
                    (
                        pos,
                        _XCurve.Evaluate((float)(pos.x - bounds.xMin) / (bounds.xMax - bounds.xMin))
                        + _YCurve.Evaluate((float)(pos.y - bounds.yMin) / (bounds.yMax - bounds.yMin)),
                        tile,
                        outlineTile,
                        Object
                    ));
                }
            }
        }

        TileInfoList.Sort((a, b) => b.placeOrder.CompareTo(a.placeOrder));

        float max = TileInfoList[0].placeOrder;
        foreach (var comp in TileInfoList)
        {
            comp.placeOrder /= max + 0.0001f;
        }

        _TileInfoList = PlaceOrderShuffle(TileInfoList, (int)(TileInfoList.Count * _shuffle));

        InitTileMap(_TileInfoList, REMOVE);
    }

    public void InitTileMap(List<TilePosInfo> tilePosInfoList, bool placeOrRemove)
    {
        for (int i = 0; i < tilePosInfoList.Count; i++)
        {
            tilePosInfoList[i].SetTile(quantumTilemap, outlineTilemap, placeOrRemove);
        }
    }

    private List<TilePosInfo> PlaceOrderShuffle(List<TilePosInfo> tilePosInfoList, int strength)
    {
        for (int i = 0; i < tilePosInfoList.Count; i++)
        {
            int pos = i + UnityEngine.Random.Range(-strength, strength);
            pos = pos < 0 ? 0 : pos > tilePosInfoList.Count - 1 ? tilePosInfoList.Count - 1 : pos;

            (tilePosInfoList[i].placeOrder, tilePosInfoList[pos].placeOrder) = (tilePosInfoList[pos].placeOrder, tilePosInfoList[i].placeOrder);
        }
        tilePosInfoList.Sort((a, b) => b.placeOrder.CompareTo(a.placeOrder));
        return tilePosInfoList;
    }

    private IEnumerator PlaceOneWaveTiles(int currentWaveIndex, int waveCount, bool placeOrRemove)
    {
        List<List<TilePosInfo>> WaveSeparatedTileInfo = new();
        for (int i = 0; i < waveCount; i++)
        {
            WaveSeparatedTileInfo.Insert(0, _TileInfoList.FindAll(a => Mathf.FloorToInt(a.placeOrder * waveCount) == i));
        }

        int tilePerStep = WaveSeparatedTileInfo[currentWaveIndex].Count / _stepPerWave;
        OnSequenceStart();
        for (int i = 0; i < _stepPerWave; i++)
        {
            OnStepStart();
            int div = (int)(tilePerStep * _stepSpeed);
            div = div < 1 ? 1 : div;
            for (int j = 0; j < tilePerStep; j++)
            {
                TilePosInfo tile = WaveSeparatedTileInfo[currentWaveIndex][0];
                tile.SetTile(quantumTilemap, outlineTilemap, placeOrRemove);
                WaveSeparatedTileInfo[currentWaveIndex].Remove(tile);

                if (j % div == 0)
                {
                    if (placeOrRemove) OnPlaceTile();
                    else OnRemoveTile();
                    yield return null;
                }
            }
            if (i == _stepPerWave - 1)
                foreach (var tile in WaveSeparatedTileInfo[currentWaveIndex])
                {
                    tile.SetTile(quantumTilemap, outlineTilemap, placeOrRemove);
                }
            yield return new WaitForSeconds(_delayBetweenStep);
        }

        OnTileSetCompleted?.Invoke();
    }

    private void OnSequenceStart()
    {

    }
    private void OnStepStart()
    {

    }
    private void OnPlaceTile()
    {

    }
    private void OnRemoveTile()
    {

    }

    public void FrontWaveCompleted(int waveIndex, int totalFrontWaveCount)
    {
        StartCoroutine(PlaceOneWaveTiles(waveIndex, totalFrontWaveCount, PLACE));
    }

    public void RearWaveCompleted(int waveIndex, int totalRearWaveCount)
    {
        StartCoroutine(PlaceOneWaveTiles(waveIndex, totalRearWaveCount, REMOVE));
    }
}