
using System.Collections.Generic;
using UnityEngine;
namespace Assets.Scripts.Interfaces.Modules
{
    public class WaveEventModule : InterfaceModule, IWaveEventable
    {
        [field: SerializeField] public bool IsDirectionForward { get; set; }
        [field: NaughtyAttributes.ReadOnly][field: SerializeField] public int CurrentWaveIndex { get; set; }

        [field: SerializeField] public WaveEventDataSO Waves { get; set; }
        [SerializeField] private WaveTileManager _waveTileManager;
        [SerializeField] private GameObject _frontObjects;
        [SerializeField] private GameObject _rearObjects;
        public Cooltime WaveCooltime { get; set; }
        public List<int> activeEnemyList = new();
        private void Start()
        {
            GameManager.instance.enemyManager.OnObjectUseEndedAction += CheckEnemyDeath;
        }
        private void OnDestroy()
        {
            GameManager.instance.enemyManager.OnObjectUseEndedAction -= CheckEnemyDeath;
        }
        public override void Register(IInterfaceRegistable interfaceRegistable)
        {
            interfaceRegistable.RegisterInterface<IWaveEventable>(this);
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
                    Vector3 position = FindValidPosition();
                    int instanceID = GameManager.instance.objectSpawnManager.ReuseObjectFast
                    (spawnDatas[i].Prefab.GetInstanceID(), position).GetInstanceID();
                    activeEnemyList.Add(instanceID);
                }
            }
        }

        public Vector3 FindValidPosition()
        {
            return new();
        }

        public void CheckEnemyDeath(int ID, int instanceID)
        {
            if (activeEnemyList.Contains(instanceID))
            {
                activeEnemyList.Remove(instanceID);
            }
        }
    }
}