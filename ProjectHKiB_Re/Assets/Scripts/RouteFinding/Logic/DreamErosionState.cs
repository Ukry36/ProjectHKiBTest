using System;
using System.Collections.Generic;

/// <summary>꿈 잠식의 저장 가능한 순수 상태입니다. UI/씬 참조를 갖지 않아 회귀 검증과 세이브 이식에 사용합니다.</summary>
[Serializable]
public sealed class DreamErosionSaveData
{
    public const int CurrentVersion = 1;
    public int version = CurrentVersion;
    public int level;
    public int consecutiveFailureCount;
    public List<string> hallucinationClueIds = new();
}

public sealed class DreamErosionState
{
    public const int MaxLevel = 3;

    private readonly HashSet<string> _hallucinationClueIds = new();
    private int _failuresPerLevel;

    public int Level { get; private set; }
    public int ConsecutiveFailureCount { get; private set; }
    public int FailuresPerLevel => _failuresPerLevel;
    public IReadOnlyCollection<string> HallucinationClueIds => _hallucinationClueIds;

    public DreamErosionState(int failuresPerLevel = 1)
    {
        _failuresPerLevel = Math.Max(1, failuresPerLevel);
    }

    public bool RegisterConnectionFailure()
    {
        int before = Level;
        ConsecutiveFailureCount++;
        Level = Math.Min(MaxLevel, ConsecutiveFailureCount / _failuresPerLevel);
        return before != Level;
    }

    public bool RegisterConnectionSuccess() => ClearErosion();
    public bool RegisterClueAcquired() => ClearErosion();

    public bool ForceLevel(int level)
    {
        int clamped = Math.Max(0, Math.Min(MaxLevel, level));
        bool changed = Level != clamped;
        Level = clamped;
        // 강제 난이도는 다음 실패에서 낮아지지 않도록 해당 단계의 최소 연속 실패값도 맞춘다.
        ConsecutiveFailureCount = clamped * _failuresPerLevel;
        return changed;
    }

    public bool AddHallucinationClue(string clueId)
        => !string.IsNullOrEmpty(clueId) && _hallucinationClueIds.Add(clueId);

    public bool HasHallucinationClue(string clueId)
        => !string.IsNullOrEmpty(clueId) && _hallucinationClueIds.Contains(clueId);

    public DreamErosionSaveData Export()
    {
        return new DreamErosionSaveData
        {
            version = DreamErosionSaveData.CurrentVersion,
            level = Level,
            consecutiveFailureCount = ConsecutiveFailureCount,
            hallucinationClueIds = new List<string>(_hallucinationClueIds),
        };
    }

    public void Import(DreamErosionSaveData saved)
    {
        // JsonUtility에서 구 필드가 없으면 null이 된다. 이 경우 새 게임과 같은 안전한 기본값이다.
        _hallucinationClueIds.Clear();
        Level = 0;
        ConsecutiveFailureCount = 0;
        if (saved == null || saved.version <= 0) return;

        Level = Math.Max(0, Math.Min(MaxLevel, saved.level));
        ConsecutiveFailureCount = Math.Max(0, saved.consecutiveFailureCount);
        // 손상/구 저장이 단계보다 작은 실패 수를 갖더라도 다음 실패에서 단계가 역행하지 않게 보정한다.
        ConsecutiveFailureCount = Math.Max(ConsecutiveFailureCount, Level * _failuresPerLevel);
        if (saved.hallucinationClueIds == null) return;
        foreach (string clueId in saved.hallucinationClueIds)
            AddHallucinationClue(clueId);
    }

    private bool ClearErosion()
    {
        bool changed = Level != 0;
        Level = 0;
        ConsecutiveFailureCount = 0;
        return changed;
    }
}
