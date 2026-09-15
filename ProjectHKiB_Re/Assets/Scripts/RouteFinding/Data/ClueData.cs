using System;

// 도감 카드 분류용 타입. 지도 공개 트리거(targetMapGuid 등)와는 무관하게
// 도감(Codex)에서 종류별로 묶어 보여주기 위한 것 — CodexFilterService.GroupByKeyword가
// 이 타입의 표시 이름도 키워드 그룹에 포함시킨다.
public enum ClueType
{
    Symbol = 1,
    Picture = 2,
    Prints = 3,
    Object = 4
}

public enum ClueSubtype
{
    Symbol = 1, ObscureText = 2, TextFragment = 3, DamagedText = 4,
    Drawing = 5, Photograph = 6,
    PromotionalMaterial = 7, Flyer = 8, Newspaper = 9,
    PhysicalObject = 10
}

// 0은 유효한 분류가 아니다. 객체가 없으면 미분류이며 필드 누락도 검증에서 거절한다.
[Serializable]
public class ClueClassification
{
    public ClueType type;
    public ClueSubtype subtype;
}

// 전투 EmotionColor와 독립. 네 번째 감정은 콘텐츠 확정 전 사용할 수 없다.
public enum ClueEmotionTag
{
    Unset = 0, Joy = 1, Sadness = 2, Anger = 3, ReservedFourth = 4
}

// 단서 하나의 데이터. clues.json에서 별도 관리된다.
// 출발 맵(이 단서가 MapNodeData.clueIds에 등록된 맵)을 방문한 뒤,
// requiredEventKey가 비어있으면 즉시, 아니면 해당 이벤트가 발생해야 획득된다.
//
// targetMapGuid/targetConnectionGuid(지도 공개 대상)와 codexMapGuid(도감 분류 기준)는
// 의미가 다르므로 별도 필드다 — 예: 출처가 되는 인물이 있는 맵과, 이 단서가 지도에 공개하는
// 목적지 맵은 서로 다를 수 있다.
[Serializable]
public class ClueData
{
    public string id;
    public string name;
    public string description;
    public string targetMapGuid;         // 이 단서가 공개하는 맵 GUID (없으면 빈 문자열)
    public string targetConnectionGuid;  // 이 단서가 공개하는 연결 GUID (없으면 빈 문자열)
    public string requiredEventKey;      // 획득에 필요한 이벤트 키 (비어있으면 방문만으로 획득)

    // ─── 도감(Codex) 전용 필드 ──────────────────────────────────
    public ClueClassification classification;
    public ClueEmotionTag emotionTag;
    public string iconAddress;

    // 단서 보드 카드 공통 외형. 0/빈 값은 런타임 기본값(100%, 6.5pt, 기본 폰트)을 뜻한다.
    // 위치와 관계는 보드 문맥이므로 ClueBoardSlot에 남고, 특정 보드에서만 달라야 할 때는
    // 슬롯의 overrideAppearance를 켜서 이 값을 덮어쓴다.
    public float boardSizePercent;
    public float boardFontSize;
    public string boardFontAddress;
    public bool boardHideLabel;
    // 구 JSON type은 보고용으로만 보존한다. 새 분류로 캐스팅하거나 재저장하지 않는다.
    [NonSerialized] public int legacyType = -1;
    public string timestamp;    // 표시용 텍스트(예: "00:00"). 빈 문자열이면 카드에 표시 안 함
    public string content;      // 도감 카드 본문 — description(지도 툴팁용 짧은 텍스트)과 별개

    // 본문 매체 블록(C01, 2026-09-08) — 글/사진/영상/소리를 작성 순서대로 이어 붙인 본문.
    // 위 content(단일 문자열)를 대체하지 않고 그 아래에 덧붙는다. 비어 있으면 지금까지와 동일하게
    // content만 표시되므로 기존 16건은 손대지 않아도 그대로 열린다.
    public ClueMediaBlock[] mediaBlocks = Array.Empty<ClueMediaBlock>();

    public string source;       // 출처(사람/물건/위치)
    public string codexMapGuid; // 이 단서가 "소속"되는 맵(도감 분류 기준). 없으면 "기타" 카테고리
    public string[] keywords;   // 검색/자동분류용 태그

    // 4단계(2026-07-14) — NPC/시스템 코멘트. 플레이어가 입력하는 게 아니라 콘텐츠 작업자가
    // Editor/MapDatabaseEditorWindow.cs에서 직접 채워 넣는다(1-4장 확정 사항).
    public CodexComment[] comments = Array.Empty<CodexComment>();

    // 첨부물(2026-08-11) — 사진/소리/맵 참조를 여러 개 붙일 수 있다. 실제 에셋은 JSON에 담을 수
    // 없어 Addressable 주소(또는 맵 GUID)로 참조하며, 로딩은 ClueAttachmentService가 맡는다.
    // 도감 카드(CodexCardView)의 "첨부" 영역에 표시된다.
    public ClueAttachment[] attachments = Array.Empty<ClueAttachment>();
}

[Serializable]
public class ClueDatabase
{
    public const int CurrentSchemaVersion = 1;
    public int schemaVersion;
    public ClueData[] clues;
}
