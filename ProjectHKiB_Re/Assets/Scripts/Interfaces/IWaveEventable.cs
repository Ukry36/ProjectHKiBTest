using UnityEngine;

public interface IWaveEventable
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

    public void WaveCleared();
    public void WaveEventStarted();
    public void WaveEventEnded();
    public void StartWaveCooltime(float time);
    public void SpawnCurrentWaveEnemies();
}