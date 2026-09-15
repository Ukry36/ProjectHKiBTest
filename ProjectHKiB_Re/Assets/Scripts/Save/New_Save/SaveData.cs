using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ItemSaveInfo
{
    public string itemGuid;
    public int count;
}

[Serializable]
public class GearSaveInfo
{
    public string gearGuid;
    public int slot;
    public List<int> equippedCards;
}

[Serializable]
public class CardSaveInfo
{
    public string cardName;
    public List<string> gearGuids; // 슬롯별
}

[Serializable]
public class BuffSaveInfo
{
    public string buffId;    // StatBuffSO.SaveId (에셋 GUID)
    public string gearGuid;  // BuffInfo.SourceGear의 GearDataSO.GUID — 없으면 빈 문자열
    public int buffStack;
    public float remainTime; // 남은 지속시간. 무한 지속(IsBuffTimeInfinite)이면 -1
}

[Serializable]
public class EventFlagSaveInfo
{
    public string id;
    public int value;
}

[Serializable]
public class PassageSaveInfo
{
    public string id;
    public bool opened;
}

// IEventSaveProvider 구현체(RouteModule, EventManager, ...) 한 개 분량의 스냅샷.
// provider별로 리스트를 나눈 이유: RouteModule은 "mapGuid:eventKey" 형식 ID를, EventManager는
// EventFlagSO.Id(에셋 GUID) 형식 ID를 쓴다 — 서로 다른 ID 체계를 한 리스트에 섞으면 충돌 위험은
// 낮아도(형식이 달라 실제 매치는 안 되지만) 다른 provider의 정체불명 항목을 서로 떠안게 되어
// 지저분해진다. providerId(IEventSaveProvider.ProviderId)로 로드 시 자기 몫만 정확히 되찾는다.
[Serializable]
public class ProviderFlagsSaveInfo
{
    public string providerId;
    public List<EventFlagSaveInfo> eventFlags = new();
    public List<PassageSaveInfo> passages = new();
}

[Serializable]
public class SaveSlotData
{
    public string savedAt;

    public float hp;

    public List<ItemSaveInfo> items = new();
    public List<GearSaveInfo> ownedGears = new();
    public List<CardSaveInfo> cards = new();

    // [2026-07-28 개편] RouteModule/EventManager 등 모든 IEventSaveProvider 구현체가 이 리스트
    // 하나에 provider별로 스코프를 나눠 들어간다(ProviderFlagsSaveInfo.providerId로 구분).
    // 예전엔 RouteModule 전용 eventFlags/passages 필드와, SaveModule이 GameManager.eventManager를
    // 직접 붙잡던 별도의 worldEventFlags 필드가 나뉘어 있었다 — SaveModule.eventProvider가 단일
    // 슬롯이라 provider를 늘릴 때마다 필드를 하나씩 추가해야 했던 구조를, provider 목록을 합성하는
    // 방식으로 바꾸며 통합했다(SaveModule.SaveEvents/LoadEvents 참고).
    public List<ProviderFlagsSaveInfo> providerFlags = new();

    // [신설, 2026-07-28] 플레이어의 활성 버프(BuffableModule.CurrentBuffs).
    // 감정 스택도 여기 같이 담긴다 — EmotionModule은 스택을 따로 들고 있지 않고
    // BuffInfo.BuffStack을 그대로 읽기 때문에(EmotionModule.GetStacks), 버프를 복원하면
    // 감정 상태가 함께 복원된다. EmotionVectorModule의 축/엔트로피도 스택에서 재계산되는
    // 파생값이라 별도 저장 대상이 아니다(게다가 그쪽은 적 전용이라 세이브 범위 밖).
    public List<BuffSaveInfo> buffs = new();

    // 노트(핀 단서)/도감(유저 메모) — IEventSaveProvider(Dictionary<string,bool> 전용)로는
    // 표현 안 되는 구조화 데이터라 별도 필드로 둔다. NoteEntry/CodexUserEntry는
    // RouteFinding/Data/*.cs에 정의된 [Serializable] 클래스(네임스페이스 없음, 이 파일과 동일)라
    // using 없이 바로 참조 가능하다 — RouteFinding 폴더 밖인 이 파일이 RouteFinding 타입을 직접
    // 아는 것은 SaveModule.cs와 마찬가지로 이번 세이브 연동 작업에서 의도적으로 감수한 결합이다.
    public List<NoteEntry> noteEntries = new();
    public List<CodexUserEntry> codexUserEntries = new();

    // 해몽 결과 재발행 방지 + 신규 단서 판정 상태. 구 세이브는 JsonUtility 기본값(false/빈 목록)으로
    // 읽혀 해몽 저장 기준점이 생기기 전 획득 단서를 NEW로 소급하지 않는다.
    public List<string> resolvedDreamReadingIds = new();
    public bool hasSavedDreamReadingCheckpoint = false;
    public List<string> newClueIds = new();

    // 노트 상단 툴바 "저장한 루트" 창에서 이름 붙여 저장해둔 스냅샷들 — 위 noteEntries(현재 화면
    // 상태)와 별개로, 세이브 슬롯 하나 안에 이름 붙은 보드가 여러 개 같이 저장된다.
    public List<NoteSavedBoard> noteSavedBoards = new();

    // 노트 "단서 연동 모드"로 사용자가 마우스로 직접 이어둔 단서 관계(NoteModule._clueLinks) —
    // ValueTuple은 JsonUtility가 직렬화 못 해서 NoteClueLink(평범한 클래스)로 변환해 저장한다.
    public List<NoteClueLink> noteClueLinks = new();

    // [신설, 2026-07-21] 노트 그래프에서 사용자가 옮겨둔 단서 노드의 위치와, 카드가 아니라 노드로
    // "펼쳐져" 있던 경로연동 단서 목록 — "저장한 루트" 보드의 cluePositions/expandedClueIds와 같은
    // 개념을 F5/F9 일반 세이브에도 그대로 적용한 것(단서 연동 간선이 화면에 그려지려면 두 끝이 전부
    // 노드로 떠 있어야 하는데, 이게 없으면 로드 후 경로연동 단서가 카드로 되돌아가 있어 noteClueLinks가
    // 정상 복원돼도 간선이 안 그려져 "연동이 저장 안 된 것처럼" 보였다). CluePositionEntry는
    // Data/NoteSavedBoard.cs에 정의된 것을 그대로 재사용한다. 저장/복원은 NoteRouteGraphView.Instance를
    // 통해 이뤄진다(NoteModule과 달리 씬 오브젝트라 자동 생성 싱글턴이 아님).
    public List<CluePositionEntry> notePositions = new();
    public List<string> noteExpandedClueIds = new();

    // [C04, 2026-09-12] 고정 단서 보드(ClueBoard*)의 플레이어 진행 — 보드별로 플레이어가 이은 관계 ID.
    // 위 notePositions/noteClueLinks/noteSavedBoards(구 노트: 자유 배치·임의 링크)와는 별개이며 서로 변환하지
    // 않는다. 좌표·관계·초기 연결은 Resources/clue_boards.json(정의)이 소유하고 여기엔 진행만 담는다.
    // 구 세이브는 version 0/빈 목록으로 읽혀 진행 없음이 된다. DTO는 RouteFinding/Data/ClueBoardProgressSaveInfo.cs.
    public ClueBoardProgressSaveData clueBoardProgress = new();

    // [C06, 2026-09-15] 고정 단서 보드의 관계 결과(해몽) 발행/확인 기록 — (boardId, kind, outcomeId) 단위.
    // clueBoardProgress(선이 이어져 있다)와 별개 필드다: "이 선의 결과를 이미 보여 줬다"를 진행에 섞으면 복원 시
    // 어느 쪽 누락이 재발행을 뜻하는지 구분할 수 없다. 해몽 본문은 DreamReadings.asset, 관계 목록은 clue_boards.json이
    // 소유하고 여기엔 식별자·발행 당시 해몽 ID·viewed만 담는다. resolvedDreamReadingIds(해몽 성립 = 보상)와도 별개로,
    // 보드 결과가 성립시킨 해몽은 그쪽에도 함께 기록된다. 구 세이브는 version 0/빈 목록으로 읽혀 발행 없음이 된다.
    public ClueBoardOutcomeSaveData clueBoardOutcomes = new();

    // 지도/노트에서 마지막으로 커밋(RouteModule.SelectRoute)한 단일 경로 — 노트 좌측 그래프
    // (NoteRouteGraphView)가 표시하는 데 쓴다. PathResult 자체(MapNodeData 객체 참조를 들고 있어
    // JsonUtility로 그대로 직렬화하기 부적절)를 저장하지 않고, 노드 GUID 순서만 저장한다 —
    // 로드 시 RouteModule.ImportSelectedRoute가 복원.
    public List<string> selectedRouteNodeGuids = new();

    // 장착 장비(RouteModule._equippedGears) — 난이도 계산·통과 가능 판정 기준이라 SelectedRoute와
    // 마찬가지로 세이브 대상에서 빠져 있으면 로드 후 재계산되는 난이도가 저장 시점과 달라지는
    // 불일치가 생긴다.
    public List<EmotionColor> equippedGears = new();

    // 이동 중이 아닐 때도 유지되는 "현재 위치"(RouteModule.CurrentLocation) — 비어있으면 로드 시
    // 자동으로 집(시작 노드)으로 취급된다(RouteModule.CurrentLocation의 기존 기본값과 동일).
    // 이동 중(_isTraveling)이었던 상태 자체는 세이브 대상이 아니다 — 웨이브 전투 재개 자체가
    // 아직 구현되지 않았고(CLAUDE.md MVP 잔여 1번), 죽음/미세이브 진도 손실 설계상 이동 중 저장은
    // 애초에 지원 대상이 아니다.
    public string currentLocationGuid = "";

    // [신설] 플레이어의 실제 씬 좌표 + 그 좌표가 속한 씬(맵). 위 currentLocationGuid는 RouteFinding
    // 추상 그래프 노드 단위(지도 화면·난이도 계산용)라, 실제 게임플레이 씬 안에서 플레이어가
    // 정확히 어디 서 있었는지는 담지 못한다 — 이 필드가 그 간극을 메운다.
    // hasPlayerPosition: 이 필드가 생기기 전(구버전) 세이브는 currentMapSceneName/playerPosition이
    // JsonUtility 기본값(""/(0,0,0))으로 채워지는데, 그 기본값을 "정말 원점에 저장된 좌표"와 구분할
    // 방법이 없다. 원점 텔레포트 같은 오동작을 막기 위해, 새로 저장할 때만 명시적으로 true를 채운다.
    public bool hasPlayerPosition = false;
    public string currentMapSceneName = "";
    public Vector3 playerPosition = Vector3.zero;

    // 세이브 당시 바라보던 방향(IDirAnimatable.AnimationDirection) — hasPlayerPosition과 같은
    // 조건으로 유효성을 판단한다(위치·방향은 같은 순간의 한 상태라 플래그를 따로 두지 않는다).
    public EnumManager.AnimDir playerDirection = EnumManager.AnimDir.D;

    // [신설] TimeManager.GameTime — 일시정지를 제외한 누적 게임 내 경과 시간(초).
    // 구버전 세이브는 이 필드가 0으로 채워지는데, "정말 0초에 저장했다"와 구분할 수 없다.
    // 다만 0으로 복원돼도 시간이 처음부터 다시 흐를 뿐 오동작은 아니라서, 위치(hasPlayerPosition)와
    // 달리 별도 유효성 플래그를 두지 않았다.
    public float gameTime = 0f;
}
