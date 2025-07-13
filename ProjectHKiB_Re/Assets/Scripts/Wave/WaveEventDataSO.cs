using System;
using UnityEngine;


[CreateAssetMenu(fileName = "WaveEventData", menuName = "Scriptable Objects/Wave/WaveEventData")]
public class WaveEventDataSO : ScriptableObject
{
    [field: SerializeField] public WaveDataSO[] FrontWaves { get; private set; }
    [field: SerializeField] public WaveDataSO[] QuantumWaves { get; private set; }
    [field: SerializeField] public WaveDataSO[] RearWaves { get; private set; }
    public int AllWavesCount { get => FrontWaves.Length + QuantumWaves.Length + RearWaves.Length; }

    public WaveDataSO GetWaveData(int waveIndex)
    {
        int tempWaveIndex = waveIndex;
        if (tempWaveIndex < FrontWaves.Length) return FrontWaves[tempWaveIndex];
        else tempWaveIndex -= FrontWaves.Length;
        if (tempWaveIndex < QuantumWaves.Length) return QuantumWaves[tempWaveIndex];
        else tempWaveIndex -= QuantumWaves.Length;
        if (tempWaveIndex < RearWaves.Length) return RearWaves[tempWaveIndex];
        return null;
    }

    public bool CheckFrontWave(int waveIndex)
    => waveIndex < FrontWaves.Length;
    public bool CheckQuantumWave(int waveIndex)
    => !CheckFrontWave(waveIndex) && !CheckRearWave(waveIndex);
    public bool CheckRearWave(int waveIndex)
    => waveIndex >= RearWaves.Length;
}