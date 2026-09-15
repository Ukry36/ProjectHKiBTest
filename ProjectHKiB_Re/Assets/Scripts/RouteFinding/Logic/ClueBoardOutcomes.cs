using System;
using UnityEngine;

// [C06] 세션 전체가 공유하는 관계 결과 발행 상태의 소유자 — ClueBoardProgress와 같은 정적 계층.
//
// 발행 기록은 패널을 닫았다 열어도·글로벌/로컬을 오가도·씬을 옮겨도 남아야 재발행이 막힌다. SaveModule이
// Export/Import로 세이브와 잇는다. Import는 판정을 거치지 않으므로 결과 화면·보상·코멘트를 내지 않는다.
public static class ClueBoardOutcomes
{
    public static ClueBoardOutcomeState Current { get; } = new();

    /// <summary>세이브 복원 등으로 내용이 교체됐을 때. 화면은 큐를 비우기만 하고 아무것도 표시하지 않는다.</summary>
    public static event Action OnReplaced;

    /// <summary>새 게임·검증 시작처럼 발행 기록을 비워야 할 때.</summary>
    public static void Reset()
    {
        Current.Clear();
        OnReplaced?.Invoke();
    }

    public static ClueBoardOutcomeSaveData Export() => Current.Export();

    /// <summary>SaveModule.LoadEvents가 호출한다. 진단은 여기서 한 번 로그로 남긴다.</summary>
    public static ClueBoardOutcomeState.ImportReport Import(ClueBoardOutcomeSaveData data)
    {
        ClueBoardOutcomeState.ImportReport report = Current.Import(data);
        foreach (string message in report.messages)
        {
            if (report.rejectedFutureVersion) Debug.LogError("[ClueBoardOutcomes] " + message);
            else Debug.LogWarning("[ClueBoardOutcomes] " + message);
        }
        if (report.invalidSkipped > 0 || report.duplicatesSkipped > 0)
            Debug.LogWarning($"[ClueBoardOutcomes] 관계 결과 복원: 손상 항목 {report.invalidSkipped}건, 중복 {report.duplicatesSkipped}건을 건너뛰었습니다.");
        OnReplaced?.Invoke();
        return report;
    }
}
