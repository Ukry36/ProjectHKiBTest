using System;
using System.Collections.Generic;

// 고정 단서 보드의 **플레이어 진행**(성립 연결) 세이브 형태 — C04.
//
// 정의(ClueBoardDefinition: 슬롯·좌표·관계·초기 연결)는 Resources/clue_boards.json이 소유하고,
// 여기에는 "플레이어가 이은 관계 ID"만 보드별로 담는다. 좌표·관계 종류·초기 연결은 저장하지 않는다 —
// 저장하면 정의를 고칠 때 세이브 쪽 사본이 화면의 근거처럼 남는다.
//
// 구 노트(SaveSlotData.notePositions/noteClueLinks/noteSavedBoards)와는 별개 필드이며 서로 변환하지 않는다.
// 그쪽은 자유 배치 + 맵 GUID가 섞인 임의 링크라 고정 슬롯 보드의 관계 ID로 옮길 근거가 없다.

[Serializable]
public class ClueBoardConnectionSaveInfo
{
    // 안정 식별자. 복원은 이 ID로 정의의 관계를 찾는다.
    public string relationId;

    // 보조 검증값 — 저장 당시 관계의 양 끝 노드(정규화: 사전순). 정의에서 같은 relationId가 다른 쌍으로
    // 바뀌었으면 복원하지 않는다. 정의를 복제하려는 것이 아니라 "이 ID가 아직 그 연결인가"를 확인하는 용도다.
    public string firstNodeId;
    public string secondNodeId;
}

[Serializable]
public class ClueBoardProgressSaveInfo
{
    public string boardId;
    public List<ClueBoardConnectionSaveInfo> connections = new();
}

[Serializable]
public class ClueBoardProgressSaveData
{
    public const int CurrentVersion = 1;

    // 0 = 이 필드가 없던 구 세이브(JsonUtility 기본값). 진행 없음으로 읽는다.
    // CurrentVersion보다 크면 미래 형식 — 읽지 않고 진단만 남긴다.
    public int version;
    public List<ClueBoardProgressSaveInfo> boards = new();
}
