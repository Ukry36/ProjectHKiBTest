using System.Collections.Generic;
using UnityEngine;

public interface IWaveEventable : IInitializable
{
    public bool IsDirectionForward { get; set; }
    public int CurrentWaveIndex { get; set; }
    public int AllWavesCount { get => Waves.AllWavesCount; }
    public WaveDataSO CurrentWaveData { get => Waves.GetWaveData(CurrentWaveIndex); }
    public bool IsFrontWave { get => Waves.CheckFrontWave(CurrentWaveIndex); }
    public bool IsQuantumWave { get => Waves.CheckQuantumWave(CurrentWaveIndex); }
    public bool IsRearWave { get => Waves.CheckRearWave(CurrentWaveIndex); }
    public WaveEventDataSO Waves { get; set; }
    public Cooltime WaveCooltime { get; set; }
    public List<ObjectDeathCounter> ObjectDeathCounterList { get; set; }
    public bool PrevWaveEnemyDead
    {
        get
        {
            for (int i = 0; i < ObjectDeathCounterList.Count; i++)
            {
                if (!ObjectDeathCounterList[i].IsDead) return false;
            }
            return true;
        }
    }
    public bool CurrentWaveEnemyDead
    {
        get => ObjectDeathCounterList[CurrentWaveIndex].IsDead;
    }
    public bool ClearTrigger { get; set; }
    public void WaveStarted();
    public void WaveCleared();
    public void WaveEventStarted();
    public void WaveEventEnded();
    public void StartWaveCooltime(float time);
    public void SpawnCurrentWaveEnemies();
}