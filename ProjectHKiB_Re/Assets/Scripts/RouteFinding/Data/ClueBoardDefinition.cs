using System;
using UnityEngine;

public enum ClueBoardKind
{
    Global = 0,
    Local = 1,
}

public enum ClueBoardRelationKind
{
    Required = 0,
    Foreshadowing = 1,
    Unrelated = 2,
}

[Serializable]
public class ClueBoardSlot
{
    public string nodeId;
    public string clueId;
    public Vector2 anchoredPosition;

    // false면 ClueData의 공통 보드 외형을 사용한다. true일 때만 아래 값들이 보드별 예외다.
    // 0 is kept as the legacy/default value so existing board JSON remains compatible.
    public bool overrideAppearance;
    public float sizePercent;
    public float fontSize;
    public string fontAddress;
    public bool hideLabel;

    // 실루엣 상태에서 마우스를 올리면 보이는 해금 힌트의 **보드별 예외**. 비어 있으면 ClueData.silhouetteHint
    // (단서 공통)를 쓴다. 외형 덮어쓰기(overrideAppearance)와 무관하게 값이 있으면 그것이 우선이다 — 같은 단서라도
    // 글로벌 보드와 로컬 보드에서 암시할 문맥이 다를 수 있어서다. 구 JSON에 없으면 null → 공통 힌트.
    public string silhouetteHint;
}

[Serializable]
public class ClueBoardRelation
{
    public string relationId;
    public string firstNodeId;
    public string secondNodeId;
    public ClueBoardRelationKind kind;

    // Required and initial connections are always permanent. This flag only adds
    // permanence to a Foreshadowing relation.
    public bool permanent;

    // [C06] 이 관계 하나가 처음 성립했을 때 열리는 해몽 결과(DreamReadings.asset의 DreamReading.id).
    // 비어 있으면 "결과 없는 관계"다 — 선은 이어지지만 코멘트·보상·재열람이 없다. 결과 식별자는
    // (boardId, relationId)이며, 해몽 본문은 여기 복제하지 않고 ID로만 가리킨다(ClueBoardOutcomeCatalog).
    public string readingId;

    // 연결 성공 시 델타가 내는 코멘트(기획: "연결 가능한 단서끼리는 선이 이어지면서 델타의 코멘트가 출력된다").
    // 해몽 결과(readingId)가 있으면 그 카드가 대신 뜨고, 없으면 이 코멘트가 뜬다. 둘 다 비면 기본 문구
    // (ClueSystemSettings.connectionCommentTemplate)가 [A]/[B] 치환으로 나온다 — 성공에 아무 반응이 없는 경우는 없다.
    // 선을 다시 클릭하면 같은 코멘트를 다시 본다. 보상·기록과 무관한 표시 전용 문구다. 구 JSON은 null → 없음.
    public string comment;
}

// [C06] 멀티 체인 — 복수 관계가 **모두** 성립했을 때만 열리는 결과. 결과 식별자는 (boardId, chainId).
// 관계 하나짜리 결과는 ClueBoardRelation.readingId로 표현하므로 체인은 관계 2개 이상이어야 한다.
// 잘못된 참조(없는 관계·Unrelated·중복)는 판정 계약(TryValidateDefinition)이 아니라 ClueBoardOutcomeCatalog가
// 진단하고 그 체인만 무시한다 — 결과 배선 하나가 깨졌다고 보드 전체가 뜨지 않게 하지 않기 위해서다.
[Serializable]
public class ClueBoardRelationChain
{
    public string chainId;
    public string[] relationIds = Array.Empty<string>();
    public string readingId;

    // 기획 "멀티 체인: 한 번에 여러 단서를 연결해야만 해몽 가능 **및 새 노드 그림자 실루엣이 열리는** 조합".
    // 체인의 관계가 전부 성립하면 여기 노드들이 잠김 → 실루엣이 된다(해금된 노드는 그대로). 실루엣 이웃
    // (silhouetteNeighbors)과 별개의 두 번째 공개 축이며, 표시만 바꾸고 판정·결과 발행과는 무관하다. 없는 노드는
    // 경고만 남기고 무시한다(결과 배선과 같은 정책 — 보드가 안 뜨는 상황을 만들지 않는다). 구 JSON에 없으면 빈 배열.
    public string[] revealSilhouetteNodeIds = Array.Empty<string>();
}

[Serializable]
public class ClueBoardSilhouetteNeighbor
{
    public string revealedByNodeId;
    public string silhouetteNodeId;
}

[Serializable]
public class ClueBoardDefinition
{
    public string boardId;
    public ClueBoardKind kind;
    public ClueBoardSlot[] slots = Array.Empty<ClueBoardSlot>();
    public ClueBoardRelation[] relations = Array.Empty<ClueBoardRelation>();
    public ClueBoardSilhouetteNeighbor[] silhouetteNeighbors = Array.Empty<ClueBoardSilhouetteNeighbor>();

    // Relationship IDs, not node IDs. Initial connections are permanent even
    // when the referenced relation is Foreshadowing.
    public string[] initialRelationIds = Array.Empty<string>();

    // [C06] 멀티 체인 결과. 구 JSON에 없으면 빈 배열로 읽힌다(null도 결과 없음으로 취급).
    public ClueBoardRelationChain[] chains = Array.Empty<ClueBoardRelationChain>();

    // true면 미획득 슬롯을 전부 실루엣으로 보인다(잠김 없음). 실루엣 이웃/체인 공개 규칙보다 우선하며 개발용 보드나
    // "자리부터 다 보여 주는" 튜토리얼 보드용이다. 실루엣의 다른 규칙(연결 불가·호버 힌트)은 그대로다. 구 JSON은 false.
    public bool revealAllAsSilhouette;
}
