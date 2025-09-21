
using System.Collections.Generic;
using UnityEngine;
namespace Assets.Scripts.Interfaces.Modules
{
    public class WaveEventModule : InterfaceModule, IWaveEventable
    {
        [field: SerializeField] public bool IsDirectionForward { get; set; }
        [field: NaughtyAttributes.ReadOnly][field: SerializeField] public int CurrentWaveIndex { get; set; }
        public WaveDataSO CurrentWave { get => Waves.GetWaveData(CurrentWaveIndex); }
        [field: SerializeField] public WaveEventDataSO Waves { get; set; }
        [SerializeField] private WaveTileManager _waveTileManager;
        [SerializeField] private GameObject _frontObjects;
        [SerializeField] private GameObject _rearObjects;
        public Cooltime WaveCooltime { get; set; }
        [field: SerializeField] public List<ObjectDeathCounter> ObjectDeathCounterList { get; set; } = new();
        public List<Vector3> validPosList;
        public LayerMask wallLayer;
        public bool ClearTrigger { get; set; }
        [NaughtyAttributes.Button("bake valid pos")]
        private void BakeValidPos()
        {
            validPosList = new(_waveTileManager.TileInfoList.Count);
            foreach (var tile in _waveTileManager.TileInfoList)
            {
                if (tile.IsWall) continue;
                validPosList.Add(tile.Pos);
            }
        }
        public override void Register(IInterfaceRegistable interfaceRegistable)
        {
            interfaceRegistable.RegisterInterface<IWaveEventable>(this);
        }

        public void WaveStarted()
        {
            ObjectDeathCounterList.Add(GameManager.instance.objectDeathCountManager.AddCounter(CurrentWave.ObjectCount));
        }

        public void WaveCleared()
        {
            bool isFront = Waves.CheckFrontWave(CurrentWaveIndex);
            bool isRear = Waves.CheckRearWave(CurrentWaveIndex);
            Debug.Log("isFr: " + isFront + " isRr: " + isRear);
            if (isFront)
                _waveTileManager.WaveCleared(CurrentWaveIndex, Waves.FrontWaves.Length, Waves.RearWaves.Length, isFront, IsDirectionForward);
            if (isRear)
                _waveTileManager.WaveCleared(CurrentWaveIndex - Waves.FrontWaves.Length - Waves.QuantumWaves.Length, Waves.FrontWaves.Length, Waves.RearWaves.Length, isFront, IsDirectionForward);
            if (CurrentWaveIndex == Waves.FrontWaves.Length + Waves.QuantumWaves.Length - 1)
                _waveTileManager.ChangeMap(IsDirectionForward);
            CurrentWaveIndex++;
            ClearTrigger = false;
        }

        public void WaveEventStarted()
        {
            _waveTileManager.Init(IsDirectionForward);
            _frontObjects.SetActive(false);
            _rearObjects.SetActive(false);
        }

        public void WaveEventEnded()
        {
            _rearObjects.SetActive(true);
        }

        public void StartWaveCooltime(float time)
        {
            WaveCooltime = new(time);
            WaveCooltime.StartCooltime(time);
        }

        public void SpawnCurrentWaveEnemies()
        {
            ObjectSpawnData[] spawnDatas = Waves.GetWaveData(CurrentWaveIndex).ObjectSpawnDatas;
            for (int i = 0; i < spawnDatas.Length; i++)
            {
                for (int j = 0; j < spawnDatas[i].Count; j++)
                {
                    int posIndex = FindValidPosition();
                    if (posIndex < 0) continue;
                    int instanceID = GameManager.instance.objectSpawnManager.SpawnObjectSimple
                    (spawnDatas[i].Prefab.GetInstanceID(), validPosList[posIndex], spawnDatas[i].Record).GetInstanceID();

                }
            }
        }

        public int FindValidPosition()
        {
            for (int i = 0; i < validPosList.Count; i++)
            {
                int rand = Random.Range(0, validPosList.Count);
                if (!Physics2D.OverlapPoint(validPosList[rand], wallLayer))
                    return rand;
            }
            return -1;
        }
    }
}