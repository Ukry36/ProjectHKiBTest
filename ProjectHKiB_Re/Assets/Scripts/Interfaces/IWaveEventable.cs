using UnityEngine;

public interface IWaveEventable
{
    public bool IsDirectionForward { get; set; }
    public int CurrentWaveIndex { get; set; }

    public WaveDataSO[] FrontWaves { get; set; }
    public WaveDataSO[] QuantumWaves { get; set; }
    public WaveDataSO[] RearWaves { get; set; }

    public WaveDataSO GetCurrentWaveData();
    public void WaveEventStarted();
    public void WaveEventEnded();
}