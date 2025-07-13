using System;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Tilemaps;

[Serializable]
public class TilePosInfo
{
    [field: SerializeField] public Vector3Int Pos { get; private set; }
    [field: SerializeField] public Vector3Int FrontWallPos { get; private set; }
    [field: SerializeField] public Vector3Int RearWallPos { get; private set; }
    [field: SerializeField] public bool IsWall { get; private set; }
    [field: SerializeField] public float PlaceOrder { get; set; }
    [field: SerializeField] public TileBase QuantumTile { get; private set; }
    [field: SerializeField] public TileBase OutlineTile { get; private set; }
    [field: SerializeField] public TileBase FrontWallTile { get; private set; }
    [field: SerializeField] public TileBase RearWallTile { get; private set; }
    [field: SerializeField] public Transform FrontDecoration { get; private set; }
    [field: SerializeField] public Transform QuantumDecoration { get; private set; }
    [field: SerializeField] public Transform RearDecoration { get; private set; }
    [field: SerializeField] public Matrix4x4 FlipInfo { get; private set; }

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

    public void SetTile(Tilemap quantumTilemap, Tilemap outlineTilemap, EntityWallGrid frontWall, EntityWallGrid rearWall, bool frontOrRear, bool isDirectionForward)
    {
        quantumTilemap.SetTile(Pos, frontOrRear && QuantumTile ? QuantumTile : null);
        quantumTilemap.SetTransformMatrix(Pos, FlipInfo);
        outlineTilemap.SetTile(Pos, !(frontOrRear && QuantumTile) ? OutlineTile : null);
        if (QuantumDecoration != null)
            QuantumDecoration.gameObject.SetActive(frontOrRear);

        if (frontOrRear)
        {
            if (isDirectionForward)
            {
                frontWall.SetTile(FrontWallPos, null);
                if (FrontDecoration != null) FrontDecoration.gameObject.SetActive(false);
            }
            else
            {
                rearWall.SetTile(RearWallPos, null);
                if (RearDecoration != null) RearDecoration.gameObject.SetActive(true);
            }
        }
        else
        {
            if (isDirectionForward)
            {
                rearWall.SetTile(RearWallPos, RearWallTile);
                if (RearDecoration != null) RearDecoration.gameObject.SetActive(false);
            }
            else
            {
                frontWall.SetTile(FrontWallPos, FrontWallTile);
                if (FrontDecoration != null) FrontDecoration.gameObject.SetActive(true);
            }
        }
    }

    public void Init(Tilemap quantumTilemap, Tilemap outlineTilemap, EntityWallGrid frontWall, EntityWallGrid rearWall, bool isDirectionForward)
    {
        quantumTilemap.SetTile(Pos, null);
        quantumTilemap.SetTransformMatrix(Pos, FlipInfo);
        outlineTilemap.SetTile(Pos, OutlineTile);
        if (QuantumDecoration != null)
            QuantumDecoration.gameObject.SetActive(false);
        if (isDirectionForward)
        {
            frontWall.SetTile(FrontWallPos, FrontWallTile);
            rearWall.SetTile(RearWallPos, null);

            if (FrontDecoration != null) FrontDecoration.gameObject.SetActive(true);
            if (RearDecoration != null) RearDecoration.gameObject.SetActive(false);
        }
        else
        {
            frontWall.SetTile(FrontWallPos, null);
            rearWall.SetTile(RearWallPos, RearWallTile);

            if (FrontDecoration != null) FrontDecoration.gameObject.SetActive(false);
            if (RearDecoration != null) RearDecoration.gameObject.SetActive(true);
        }
    }
}

public class WaveTileManager : MonoBehaviour
{
    [SerializeField] private Tilemap _quantumTilemap;
    [SerializeField] private Tilemap _outlineTilemap;
    [SerializeField] private TilemapCollider2D _wallColider;

    [SerializeField] private Tilemap _frontFloor;
    [SerializeField] private Tilemap _rearFloor;

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
    [field: SerializeField] public List<TilePosInfo> TileInfoList { get; private set; }
    public TileBase outlineTile;
    [SerializeField] private ParticlePlayer _placeParticle;

    [SerializeField] private AnimationCurve _XCurve;
    [SerializeField] private AnimationCurve _YCurve;

    [SerializeField][MaxValue(1), MinValue(0)] private float _shuffle;
    [SerializeField][MinValue(1)] private int _stepPerWave;
    [SerializeField][MinValue(0)] private float _delayBetweenStep;
    [SerializeField][MaxValue(1), MinValue(0)] private float _stepSpeed;

    private const bool FRONT = true;
    private const bool REAR = false;

    [Button("generate TilePosInfo")]
    public void GenerateTilePosInfo()
    {
        TileInfoList = new();
        Vector2 max = _wallColider.bounds.max;
        Vector2 min = _wallColider.bounds.min;

        for (float x = min.x; x < max.x; x++)
        {
            for (float y = min.y; y < max.y; y++)
            {
                Vector3 pos = new(x, y);
                Vector3Int qPos = _quantumTilemap.WorldToCell(pos);
                Vector3Int fPos = _frontWallRefTilemap.WorldToCell(pos) + Vector3Int.up * _frontWallHeight;
                Vector3Int rPos = _rearWallRefTilemap.WorldToCell(pos) + Vector3Int.up * _rearWallHeight;
                TileBase quantumTile = _quantumTilemap.GetTile(qPos);
                TileBase frontWallTile = _frontWallRefTilemap.GetTile(fPos);
                TileBase rearWallTile = _rearWallRefTilemap.GetTile(rPos);

                if (frontWallTile == _wallReferenceTile)
                {
                    int tileType = _wallTile.Length / 3;
                    int rand = UnityEngine.Random.Range(0, tileType);
                    rand = rand * 3 + ((int)(x - min.x) % 3);
                    frontWallTile = _wallTile[rand];
                }
                if (rearWallTile == _wallReferenceTile)
                {
                    int tileType = _wallTile.Length / 3;
                    int rand = UnityEngine.Random.Range(0, tileType);
                    rand = rand * 3 + ((int)(x - min.x) % 3);
                    rearWallTile = _wallTile[rand];
                }
                Transform quantumDeco = GetChildOfPos(_quantumDecoParent, pos);
                Transform frontDeco = GetChildOfPos(_frontDecoParent, pos);
                Transform rearDeco = GetChildOfPos(_rearDecoParent, pos);

                Vector3Int frontWallPos = _frontEntityWallGrid.WolrdToCell(_frontWallRefTilemap.CellToWorld(fPos));
                Vector3Int rearWallPos = _rearEntityWallGrid.WolrdToCell(_rearWallRefTilemap.CellToWorld(rPos));
                frontWallPos.z = _frontWallHeight;
                rearWallPos.z = _rearWallHeight;
                frontWallPos.y -= _frontWallHeight;
                rearWallPos.y -= _rearWallHeight;
                TileInfoList.Add(new TilePosInfo
                    (
                        qPos,
                        frontWallPos,
                        rearWallPos,
                        _XCurve.Evaluate((float)(qPos.x - min.x) / (max.x - min.x))
                        + _YCurve.Evaluate((float)(qPos.y - min.y) / (max.y - min.y)),
                        quantumTile,
                        outlineTile,
                        frontWallTile,
                        rearWallTile,
                        _quantumTilemap.GetTransformMatrix(qPos),
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
            Vector3 dist = parent.GetChild(i).position - pos;
            if (dist.x <= 0.5f && dist.y <= 0.5f)
            {
                return parent.GetChild(i);
            }
        }
        return null;
    }

    [Button("generate wall")]
    public void GenerateWall()
    {
        Vector3Int max = _frontEntityWallGrid.WolrdToCell(_wallColider.bounds.max);
        Vector3Int min = _rearEntityWallGrid.WolrdToCell(_wallColider.bounds.min);
        _frontEntityWallGrid.GenerateGrid(max.y, min.y);
        _rearEntityWallGrid.GenerateGrid(max.y, min.y);
        for (int i = 0; i < TileInfoList.Count; i++)
        {
            Vector3Int frontWallPos = TileInfoList[i].FrontWallPos;
            Vector3Int rearWallPos = TileInfoList[i].RearWallPos;
            TileBase frontWallTile = TileInfoList[i].FrontWallTile;
            TileBase rearWallTile = TileInfoList[i].RearWallTile;

            _frontEntityWallGrid.SetTile(frontWallPos, frontWallTile);
            _rearEntityWallGrid.SetTile(rearWallPos, rearWallTile);
        }
    }

    [Button("front wave clear")]
    public void Front02()
    {
        FrontWaveCompleted(currentFrontWave, tempWaveCount, tempisDirectionForward);
        currentFrontWave++;
        currentFrontWave %= tempWaveCount;
    }

    [Button("rear wave clear")]
    public void Rear02()
    {
        RearWaveCompleted(currentRearWave, tempWaveCount, tempisDirectionForward);
        currentRearWave++;
        currentRearWave %= tempWaveCount;
    }
    [Button("change map")]
    public void ChangeMap()
    => ChangeMap(tempisDirectionForward);
    public int tempWaveCount;
    public bool tempisDirectionForward;

    [ReadOnly] public int currentFrontWave;
    [ReadOnly] public int currentRearWave;

    [Button("init")]
    public void Init()
    {
        currentFrontWave = 0;
        currentRearWave = 0;
        InitTileMap(TileInfoList, tempisDirectionForward);
        ShuffleTiles();
    }


    private void ShuffleTiles()
    {
        /*
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
        }*/

        TileInfoList.Sort((a, b) => b.PlaceOrder.CompareTo(a.PlaceOrder));

        float max = TileInfoList[0].PlaceOrder;
        foreach (var comp in TileInfoList)
        {
            comp.PlaceOrder /= max + 0.0001f;
        }

        TileInfoList = PlaceOrderShuffle(TileInfoList, (int)(TileInfoList.Count * _shuffle));
    }

    public void InitTileMap(List<TilePosInfo> tilePosInfoList, bool isDirectionForward)
    {
        ChangeMap(!isDirectionForward);

        for (int i = 0; i < tilePosInfoList.Count; i++)
        {
            tilePosInfoList[i].Init(_quantumTilemap, _outlineTilemap, _frontEntityWallGrid, _rearEntityWallGrid, isDirectionForward);
        }
    }

    public void ChangeMap(bool isDirectionForward)
    {
        if (isDirectionForward == FRONT)
        {
            _frontFloor.gameObject.SetActive(false);
            _rearFloor.gameObject.SetActive(true);
        }
        if (isDirectionForward == REAR)
        {
            _frontFloor.gameObject.SetActive(true);
            _rearFloor.gameObject.SetActive(false);
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

    private IEnumerator PlaceOneWaveTiles(int currentWaveIndex, int waveCount, bool frontOrRear, bool isDirectionForward)
    {
        List<List<TilePosInfo>> WaveSeparatedTileInfo = new();
        for (int i = 0; i < waveCount; i++)
        {
            WaveSeparatedTileInfo.Insert(0, TileInfoList.FindAll(a => Mathf.FloorToInt(a.PlaceOrder * waveCount) == i));
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
                tile.SetTile(_quantumTilemap, _outlineTilemap, _frontEntityWallGrid, _rearEntityWallGrid, frontOrRear, isDirectionForward);
                OnPlaceTile(_quantumTilemap.CellToWorld(tile.Pos), frontOrRear);
                WaveSeparatedTileInfo[currentWaveIndex].Remove(tile);

                if (j % div == 0)
                {
                    OnPlaceTileReduced(frontOrRear);
                    yield return null;
                }
            }
            if (i == _stepPerWave - 1)
            {
                for (int k = 0; k < WaveSeparatedTileInfo[currentWaveIndex].Count; k++)
                {
                    WaveSeparatedTileInfo[currentWaveIndex][k].SetTile(_quantumTilemap, _outlineTilemap, _frontEntityWallGrid, _rearEntityWallGrid, frontOrRear, isDirectionForward);
                    OnPlaceTile(_quantumTilemap.CellToWorld(WaveSeparatedTileInfo[currentWaveIndex][k].Pos), frontOrRear);
                    if (k % div == 0)
                    {
                        OnPlaceTileReduced(frontOrRear);
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
    private void OnPlaceTileReduced(bool frontOrRear)
    {

    }

    private void OnPlaceTile(Vector3 pos, bool frontOrRear)
    {
        //if (frontOrRear)
        if (Application.isPlaying)
            GameManager.instance.particleManager.PlayParticleOneShot(_placeParticle.GetInstanceID(), pos + Vector3.one);

    }

    private void FrontWaveCompleted(int waveIndex, int totalFrontWaveCount, bool isDirectionForward)
    {
        StartCoroutine(PlaceOneWaveTiles(waveIndex, totalFrontWaveCount, FRONT, isDirectionForward));
    }

    private void RearWaveCompleted(int waveIndex, int totalRearWaveCount, bool isDirectionForward)
    {
        StartCoroutine(PlaceOneWaveTiles(waveIndex, totalRearWaveCount, REAR, isDirectionForward));
    }

    public void WaveCleared(int waveIndex, int totalFrontWaveCount, int totalRearWaveCount, bool isFront, bool isDirectionForward)
    {
        StartCoroutine(PlaceOneWaveTiles
        (
            waveIndex,
            isFront ? totalFrontWaveCount : totalRearWaveCount,
            isFront,
            isDirectionForward
        ));
    }
}