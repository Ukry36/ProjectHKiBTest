using System;
using System.Linq;
using UnityEngine;
namespace Assets.Scripts.Interfaces.Modules
{
    public class WaveEventModule : InterfaceModule, IWaveEventable
    {
        [field: SerializeField] public bool IsDirectionForward { get; set; }
        [field: NaughtyAttributes.ReadOnly][field: SerializeField] public int CurrentWaveIndex { get; set; }

        [field: SerializeField] public WaveDataSO[] FrontWaves { get; set; }
        [field: SerializeField] public WaveDataSO[] QuantumWaves { get; set; }
        [field: SerializeField] public WaveDataSO[] RearWaves { get; set; }
        private WaveDataSO[] _allWaves;
        [SerializeField] private WaveTileManager _waveTileManager;
        [SerializeField] private GameObject _frontObjects;
        [SerializeField] private GameObject _rearObjects;
        private void Awake()
        {
            _allWaves = FrontWaves.Concat(QuantumWaves).ToArray();
            _allWaves = _allWaves.Concat(RearWaves).ToArray();
        }
        public override void Register(IInterfaceRegistable interfaceRegistable)
        {
            interfaceRegistable.RegisterInterface<IWaveEventable>(this);
        }

        public WaveDataSO GetCurrentWaveData()
        {
            if (CurrentWaveIndex < _allWaves.Length)
                return _allWaves[CurrentWaveIndex];
            else
                return null;
        }

        public void WaveCleared()
        {
            bool isFront = CurrentWaveIndex < FrontWaves.Length;
            bool isRear = CurrentWaveIndex > _allWaves.Length - RearWaves.Length;
            if (isFront || isRear)
                _waveTileManager.WaveCleared(CurrentWaveIndex, FrontWaves.Length, RearWaves.Length, isFront, IsDirectionForward);
            CurrentWaveIndex++;
        }

        public void WaveEventStarted()
        {
            _frontObjects.SetActive(false);
            _rearObjects.SetActive(false);
        }

        public void WaveEventEnded()
        {
            _frontObjects.SetActive(!IsDirectionForward);
            _rearObjects.SetActive(IsDirectionForward);
        }
    }
}