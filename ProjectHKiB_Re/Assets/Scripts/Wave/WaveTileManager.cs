using System;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Tilemaps;

[Serializable]
public class TilePosInfo
{
    public Vector3Int Pos { get; private set; }
    public Vector3Int FrontWallPos { get; private set; }
    public Vector3Int RearWallPos { get; private set; }
    public float PlaceOrder { get; set; }
    public TileBase QuantumTile { get; private set; }
    public TileBase OutlineTile { get; private set; }
    public TileBase FrontWallTile { get; private set; }
    public TileBase RearWallTile { get; private set; }
    public Transform FrontDecoration { get; private set; }
    public Transform QuantumDecoration { get; private set; }
    public Transform RearDecoration { get; private set; }
    public Matrix4x4 FlipInfo { get; private set; }

    public TilePosInfo(Vector3Int pos, float placeOrder, TileBase quantumTile, TileBase outlineTile, Matrix4x4 flipInfo, GameObject decoration)
    {
        Pos = pos;
        PlaceOrder = placeOrder;
        QuantumTile = quantumTile;
        OutlineTile = outlineTile;
        FlipInfo = flipInfo;
    }

    public TilePosInfo(Vector3Int pos, Vector3Int frontWallPos, Vector3Int rearWallPos, float placeOrder, TileBase quantumTile, TileBase outlineTile, TileBase frontVoidTile, TileBase rearVoidTile, Matrix4x4 flipInfo, Transform quantumDecoration, Transform frontDecoration, Transform rearDecoration)
    {
        Pos = pos;
        FrontWallPos = frontWallPos;
        RearWallPos = rearWallPos;
        PlaceOrder = placeOrder;
        QuantumTile = quantumTile;
        OutlineTile = outlineTile;
        FrontWallTile = frontVoidTile;
        RearWallTile = rearVoidTile;
        FlipInfo = flipInfo;
        QuantumDecoration = quantumDecoration;
        FrontDecoration = frontDecoration;
        RearDecoration = rearDecoration;
    }

    public void SetTile(Tilemap quantumTilemap, Tilemap outlineTilemap, bool placeOrRemove)
    {
        quantumTilemap.SetTile(Pos, placeOrRemove ? QuantumTile : null);
        quantumTilemap.SetTransformMatrix(Pos, FlipInfo);
        outlineTilemap.SetTile(Pos, !placeOrRemove ? OutlineTile : null);
        if (QuantumDecoration != null)
            QuantumDecoration.gameObject.SetActive(placeOrRemove);
    }
}

public class WaveTileManager : MonoBehaviour
{
    [SerializeField] private Tilemap _quantumTilemap;
    [SerializeField] private Tilemap _outlineTilemap;
    [SerializeField] private TilemapCollider2D _wallColider;

    [SerializeField] private Transform _quantumDecoParent;
    [SerializeField] private Transform _frontDecoParent;
    [SerializeField] private Transform _rearDecoParent;

    [SerializeField] private EntityWallGrid _frontEntityWallGrid;
    [SerializeField] private EntityWallGrid _rearEntityWallGrid;
    [SerializeField] private Tilemap _frontWallRefTilemap;
    [SerializeField] private Tilemap _rearWallRefTilemap;
    [SerializeField] private int _frontWallHeight;
    [SerializeField] private int _rearWallHeight;
    [SerializeField] private TileBase _wallReferenceTile;
    [SerializeField] private TileBase[] _wallTile;


    [HideInInspector] public WaveSequence waveSequence;
    public delegate void TileSetCompleted();
    public event TileSetCompleted OnTileSetCompleted;
    public LayerMask layerMask;
    [SerializeField] private List<TilePosInfo> _TileInfoList;
    public TileBase outlineTile;
    [SerializeField] private ParticlePlayer _placeParticle;

    [SerializeField] private AnimationCurve _XCurve;
    [SerializeField] private AnimationCurve _YCurve;

    [SerializeField][MaxValue(1), MinValue(0)] private float _shuffle;
    [SerializeField][MinValue(1)] private int _stepPerWave;
    [SerializeField][MinValue(0)] private float _delayBetweenStep;
    [SerializeField][MaxValue(1), MinValue(0)] private float _stepSpeed;

    private const bool PLACE = true;
    private const bool REMOVE = false;

    [Button("generate TilePosInfo")]
    public void GenerateTilePosInfo()
    {
        Vector2 max = _wallColider.bounds.max;
        Vector2 min = _wallColider.bounds.min;

        for (int x = (int)min.x; x < (int)max.x; x++)
        {
            for (int y = (int)min.y; y < (int)max.y; y++)
            {
                Vector3Int pos = new(x, y);
                Vector3Int frontWallPos = pos + Vector3Int.up * _frontWallHeight;
                Vector3Int rearWallPos = pos + Vector3Int.up * _rearWallHeight;
                TileBase quantumTile = _quantumTilemap.GetTile(pos);
                TileBase frontVoidTile = _frontWallRefTilemap.GetTile(frontWallPos);
                TileBase rearVoidTile = _rearWallRefTilemap.GetTile(rearWallPos);
                if (frontVoidTile == _wallReferenceTile)
                {
                    int tileType = _wallTile.Length / 3;
                    int rand = UnityEngine.Random.Range(0, tileType);
                    rand = rand * 3 + ((x - (int)min.x) % 3);
                    frontVoidTile = _wallTile[rand];
                }
                if (rearVoidTile == _wallReferenceTile)
                {
                    int tileType = _wallTile.Length / 3;
                    int rand = UnityEngine.Random.Range(0, tileType);
                    rand = rand * 3 + ((x - (int)min.x) % 3);
                    rearVoidTile = _wallTile[rand];
                }
                Transform quantumDeco = GetChildOfPos(_quantumDecoParent, pos);
                Transform frontDeco = GetChildOfPos(_frontDecoParent, pos);
                Transform rearDeco = GetChildOfPos(_rearDecoParent, pos);

                _TileInfoList.Add(new TilePosInfo
                    (
                        pos,
                        frontWallPos,
                        rearWallPos,
                        _XCurve.Evaluate((float)(pos.x - min.x) / (max.x - min.x))
                        + _YCurve.Evaluate((float)(pos.y - min.y) / (max.y - min.y)),
                        quantumTile,
                        outlineTile,
                        frontVoidTile,
                        rearVoidTile,
                        _quantumTilemap.GetTransformMatrix(pos),
                        quantumDeco,
                        frontDeco,
                        rearDeco
                    ));
            }
        }
    }

    private Transform GetChildOfPos(Transform parent, Vector3 pos)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            if (parent.GetChild(i).position == pos)
            {
                return parent.GetChild(i);
            }
        }
        return null;
    }

    [Button("generate wall")]
    public void GenerateWall()
    {
        Vector2 max = _wallColider.bounds.max;
        Vector2 min = _wallColider.bounds.min;
        _frontEntityWallGrid.GenerateGrid((int)max.y, (int)min.y);
        _rearEntityWallGrid.GenerateGrid((int)max.y, (int)min.y);
        for (int i = 0; i < _TileInfoList.Count; i++)
        {
            Vector3Int frontWallPos = _TileInfoList[i].FrontWallPos;
            Vector3Int rearWallPos = _TileInfoList[i].RearWallPos;
            int frontY = _TileInfoList[i].Pos.y;
            int rearY = _TileInfoList[i].Pos.y;
            TileBase frontWallTile = _TileInfoList[i].FrontWallTile;
            TileBase rearWallTile = _TileInfoList[i].RearWallTile;

            _frontEntityWallGrid.Rows[frontY].SetTile(frontWallPos, frontWallTile);
            _rearEntityWallGrid.Rows[rearY].SetTile(rearWallPos, rearWallTile);
        }
    }

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
        BoundsInt bounds = _quantumTilemap.cellBounds;
        HashSet<Collider2D> processColliders = new();
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int pos = new(x, y);
                TileBase tile = _quantumTilemap.GetTile(pos);

                GameObject Object = null;

                Collider2D collider = Physics2D.OverlapPoint(_quantumTilemap.GetCellCenterWorld(pos), layerMask);

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
                        _quantumTilemap.GetTransformMatrix(pos),
                        Object
                    ));
                }
            }
        }

        TileInfoList.Sort((a, b) => b.PlaceOrder.CompareTo(a.PlaceOrder));

        float max = TileInfoList[0].PlaceOrder;
        foreach (var comp in TileInfoList)
        {
            comp.PlaceOrder /= max + 0.0001f;
        }

        _TileInfoList = PlaceOrderShuffle(TileInfoList, (int)(TileInfoList.Count * _shuffle));

        InitTileMap(_TileInfoList, REMOVE);
    }

    public void InitTileMap(List<TilePosInfo> tilePosInfoList, bool placeOrRemove)
    {
        for (int i = 0; i < tilePosInfoList.Count; i++)
        {
            tilePosInfoList[i].SetTile(_quantumTilemap, _outlineTilemap, placeOrRemove);
        }
    }

    private List<TilePosInfo> PlaceOrderShuffle(List<TilePosInfo> tilePosInfoList, int strength)
    {
        for (int i = 0; i < tilePosInfoList.Count; i++)
        {
            int pos = i + UnityEngine.Random.Range(-strength, strength);
            pos = pos < 0 ? 0 : pos > tilePosInfoList.Count - 1 ? tilePosInfoList.Count - 1 : pos;

            (tilePosInfoList[i].PlaceOrder, tilePosInfoList[pos].PlaceOrder) = (tilePosInfoList[pos].PlaceOrder, tilePosInfoList[i].PlaceOrder);
        }
        tilePosInfoList.Sort((a, b) => b.PlaceOrder.CompareTo(a.PlaceOrder));
        return tilePosInfoList;
    }

    private IEnumerator PlaceOneWaveTiles(int currentWaveIndex, int waveCount, bool placeOrRemove)
    {
        List<List<TilePosInfo>> WaveSeparatedTileInfo = new();
        for (int i = 0; i < waveCount; i++)
        {
            WaveSeparatedTileInfo.Insert(0, _TileInfoList.FindAll(a => Mathf.FloorToInt(a.PlaceOrder * waveCount) == i));
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
                tile.SetTile(_quantumTilemap, _outlineTilemap, placeOrRemove);
                OnPlaceTile(tile.Pos, placeOrRemove);
                WaveSeparatedTileInfo[currentWaveIndex].Remove(tile);

                if (j % div == 0)
                {
                    OnPlaceTileReduced(placeOrRemove);
                    yield return null;
                }
            }
            if (i == _stepPerWave - 1)
            {
                for (int k = 0; k < WaveSeparatedTileInfo[currentWaveIndex].Count; k++)
                {
                    WaveSeparatedTileInfo[currentWaveIndex][k].SetTile(_quantumTilemap, _outlineTilemap, placeOrRemove);
                    OnPlaceTile(WaveSeparatedTileInfo[currentWaveIndex][k].Pos, placeOrRemove);
                    if (k % div == 0)
                    {
                        OnPlaceTileReduced(placeOrRemove);
                        yield return null;
                    }
                }
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

    // if there is too many tiles placed in one step (at the same time)
    // use this
    private void OnPlaceTileReduced(bool placeOrRemove)
    {

    }

    private void OnPlaceTile(Vector3 pos, bool placeOrRemove)
    {
        //if (placeOrRemove)
        if (Application.isPlaying)
            GameManager.instance.particleManager.PlayParticleOneShot(_placeParticle.GetInstanceID(), pos + Vector3.one);

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