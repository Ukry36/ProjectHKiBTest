using System;
using System.Collections.Generic;
using UnityEngine;

// 세션 전체가 공유하는 보드 진행의 소유자(ClueBoardCatalog와 같은 정적 계층).
//
// [왜 정적인가] 보드 화면(ClueBoardScreen)은 패널마다 만들어지고 씬과 함께 사라지지만, "플레이어가 이은 선"은
// 패널을 닫았다 열어도·글로벌/로컬을 오가도·씬을 옮겨도 남아야 한다. 씬 오브젝트에 두면 배선(프리팹/씬 수정)이
// 필요한데 이 단계는 씬을 건드리지 않는다. SaveModule이 Export/Import로 세이브와 잇는다.
//
// [이벤트] OnReplaced는 세이브 복원처럼 내용이 통째로 바뀌었을 때만 발행한다. 화면이 열려 있으면 같은 보드를
// 다시 그리되, 그 다시 그리기는 ClueBoardView.ShowBoard 경로라 OnConnectionEstablished/OnConnectionResult를
// 발행하지 않는다 — 복원이 보상·코멘트를 재발행하지 않는 근거는 "복원은 판정(TryConnect)을 거치지 않는다"이다.
public static class ClueBoardProgress
{
    public static ClueBoardProgressState Current { get; } = new();

    /// <summary>세이브 복원 등으로 내용이 교체됐을 때.</summary>
    public static event Action OnReplaced;

    /// <summary>새 게임·검증 시작처럼 진행을 비워야 할 때. 이벤트를 발행해 열린 화면도 비운 상태로 다시 그린다.</summary>
    public static void Reset()
    {
        Current.Clear();
        OnReplaced?.Invoke();
    }

    public static ClueBoardProgressSaveData Export() => Current.Export();

    /// <summary>SaveModule.LoadEvents가 호출한다. 진단은 여기서 한 번 로그로 남긴다.</summary>
    public static ClueBoardProgressState.ImportReport Import(ClueBoardProgressSaveData data)
    {
        ClueBoardProgressState.ImportReport report = Current.Import(data);
        foreach (string message in report.messages)
        {
            if (report.rejectedFutureVersion) Debug.LogError("[ClueBoardProgress] " + message);
            else Debug.LogWarning("[ClueBoardProgress] " + message);
        }
        if (report.invalidSkipped > 0 || report.duplicatesSkipped > 0)
            Debug.LogWarning($"[ClueBoardProgress] 보드 진행 복원: 손상 항목 {report.invalidSkipped}건, 중복 {report.duplicatesSkipped}건을 건너뛰었습니다.");
        OnReplaced?.Invoke();
        return report;
    }

    /// <summary>보드를 열 때 쓰는 복원 목록. 정의와 맞지 않는 항목은 경고로만 남기고 연결하지 않는다.</summary>
    public static List<string> ResolveForBoard(ClueBoardDefinition definition)
    {
        var diagnostics = new List<string>();
        List<string> resolved = Current.ResolveForBoard(definition, diagnostics);
        foreach (string message in diagnostics) Debug.LogWarning("[ClueBoardProgress] " + message);
        return resolved;
    }
}
