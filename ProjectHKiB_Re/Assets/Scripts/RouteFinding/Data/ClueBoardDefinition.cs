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
}
