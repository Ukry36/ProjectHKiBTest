using System;
using System.Collections.Generic;

// [C06] 고정 단서 보드의 **관계 결과 발행 상태** 세이브 형태.
//
// clueBoardProgress(C04: 플레이어가 이은 관계 ID)와 별개 필드다. 진행은 "선이 이어져 있다"이고 결과는
// "그 선(들)의 해몽을 이미 발행했다"라, 같은 필드에 섞으면 복원 시 어느 쪽 누락이 재발행을 뜻하는지
// 구분할 수 없다. 해몽 본문·관계 목록은 정의(clue_boards.json + DreamReadings.asset)가 소유하고
// 여기엔 식별자와 발행/확인 여부만 담는다.

[Serializable]
public class ClueBoardOutcomeSaveInfo
{
    public string boardId;
    public ClueBoardOutcomeKind kind;

    // Relation이면 relationId, Chain이면 chainId. (boardId, kind, outcomeId)가 안정 식별자다.
    public string outcomeId;

    // 보조 검증값 — 발행 당시의 해몽 ID. 정의가 다른 해몽으로 바뀌었으면 경고만 남기고 현재 정의를 따른다
    // (정의를 복제하려는 것이 아니라 "이 결과가 아직 그 해몽인가"를 진단하는 용도).
    public string readingId;

    // 결과 화면을 플레이어에게 실제로 보였는지. false면 발행은 됐지만 보여 줄 UI가 없었던 것이라
    // 다음에 보드를 열 때 보류분으로 한 번 보여 준다(재발행이 아니라 보류 표시).
    public bool viewed;
}

[Serializable]
public class ClueBoardOutcomeSaveData
{
    public const int CurrentVersion = 1;

    // 0 = 이 필드가 없던 구 세이브(JsonUtility 기본값). 발행 없음으로 읽는다.
    // CurrentVersion보다 크면 미래 형식 — 읽지 않고 진단만 남긴다.
    public int version;
    public List<ClueBoardOutcomeSaveInfo> entries = new();
}
