using System;
using System.Collections.Generic;

// 보드 노드 하나가 지금 화면에서 어떤 상태인지.
//
// 도감(Codex)의 "미발견 ??? 슬롯"과는 다른 축이다 — 도감은 맵별 미획득 개수를 세어 빈칸을 만들지만,
// 보드는 고정 슬롯이 이미 있고 그 자리를 "안 보임/실루엣/보임" 중 무엇으로 그릴지를 정한다.
public enum ClueBoardNodeVisibility
{
    Locked = 0,      // 화면에 아예 표시하지 않는다
    Silhouette = 1,  // 고정 자리에 실루엣만. 연결 시작·드롭 대상이 아니다
    Unlocked = 2,    // 아이콘·이름 표시. 연결 가능
}

// 보드 정의 + 플레이어 진행에서 노드별 표시 상태를 계산한 결과. UI가 없어도 계산·검증할 수 있게
// 순수 C# 클래스로 둔다(MapPathFinder/DifficultyCalculator와 같은 위치).
//
// [환각(C07) 경계] 환각 노드는 아직 구현 대상이 아니라 기본값이 빈 집합이다. 다만 계산 경로에는
// 자리를 만들어 둔다 — 나중에 C07이 노드 ID 집합만 주입하면 표시(보이되 연결 불가)와
// ClueBoardConnectionEngine의 거절이 함께 맞물린다. 지금 자리를 안 만들면 그때 표시 계층까지
// 다시 손대야 한다.
public sealed class ClueBoardNodeStates
{
    private readonly Dictionary<string, ClueBoardNodeVisibility> _visibility;
    private readonly HashSet<string> _hallucinationNodeIds;

    public string boardId { get; }
    public IReadOnlyCollection<string> hallucinationNodeIds => _hallucinationNodeIds;

    private ClueBoardNodeStates(
        string boardId,
        Dictionary<string, ClueBoardNodeVisibility> visibility,
        HashSet<string> hallucinationNodeIds)
    {
        this.boardId = boardId;
        _visibility = visibility;
        _hallucinationNodeIds = hallucinationNodeIds;
    }

    public ClueBoardNodeVisibility Get(string nodeId) =>
        nodeId != null && _visibility.TryGetValue(nodeId, out var value) ? value : ClueBoardNodeVisibility.Locked;

    public bool IsHallucination(string nodeId) => nodeId != null && _hallucinationNodeIds.Contains(nodeId);

    public IEnumerable<string> NodeIdsWith(ClueBoardNodeVisibility visibility)
    {
        foreach (var pair in _visibility)
            if (pair.Value == visibility) yield return pair.Key;
    }

    /// <summary>두 계산 결과의 노드별 표시가 같은지(체인 공개로 화면을 다시 그려야 하는지 판단용).</summary>
    public bool SameVisibilityAs(ClueBoardNodeStates other)
    {
        if (other == null || other._visibility.Count != _visibility.Count) return false;
        foreach (var pair in _visibility)
            if (!other._visibility.TryGetValue(pair.Key, out var value) || value != pair.Value) return false;
        return true;
    }

    /// <summary>
    /// 획득 단서 집합에서 노드 상태를 계산한다.
    ///
    /// 규칙:
    ///   해금  = 그 슬롯의 단서를 획득했다.
    ///   실루엣 = 아직 획득하지 않았지만, 이 노드를 실루엣으로 공개하는 이웃(ClueBoardSilhouetteNeighbor의
    ///           revealedByNodeId)이 해금돼 있다 — 또는 이 노드를 revealSilhouetteNodeIds로 가리키는 멀티 체인의
    ///           관계가 **전부** 성립해 있다(connectedRelationIds).
    ///   잠김  = 그 밖의 전부.
    ///
    /// 실루엣은 **오직 silhouetteNeighbors 데이터로만** 판단한다. 관계(relations)에서 "이 단서와
    /// 이어질 수 있는 상대"를 역산해 실루엣으로 띄우면, 연결 정답이 화면에 미리 새어 나간다
    /// (격차 분석의 실루엣 확정 사항: 별도 목록으로 공개, 클릭 힌트만, 연결 불가).
    ///
    /// 실루엣의 실루엣은 만들지 않는다 — 공개는 해금된 노드에서 한 단계만 뻗는다.
    ///
    /// connectedRelationIds는 지금 성립한 관계(초기 연결 + 복원 + 이번 세션)다. 넘기지 않으면 체인 공개는 계산하지
    /// 않는다 — 판정 엔진의 런타임 상태와 같은 집합을 넘겨야 "체인이 완성됐는데 실루엣이 안 뜬다"가 생기지 않는다.
    /// </summary>
    public static ClueBoardNodeStates Build(
        ClueBoardDefinition definition,
        IEnumerable<string> acquiredClueIds,
        IEnumerable<string> hallucinationNodeIds = null,
        IEnumerable<string> connectedRelationIds = null)
    {
        if (definition == null) throw new ArgumentNullException(nameof(definition));

        var acquired = acquiredClueIds == null
            ? new HashSet<string>(StringComparer.Ordinal)
            : new HashSet<string>(acquiredClueIds, StringComparer.Ordinal);

        var visibility = new Dictionary<string, ClueBoardNodeVisibility>(StringComparer.Ordinal);
        var unlocked = new HashSet<string>(StringComparer.Ordinal);
        if (definition.slots != null)
            foreach (ClueBoardSlot slot in definition.slots)
            {
                if (slot == null || string.IsNullOrEmpty(slot.nodeId)) continue;
                bool isUnlocked = !string.IsNullOrEmpty(slot.clueId) && acquired.Contains(slot.clueId);
                visibility[slot.nodeId] = isUnlocked ? ClueBoardNodeVisibility.Unlocked : ClueBoardNodeVisibility.Locked;
                if (isUnlocked) unlocked.Add(slot.nodeId);
            }

        // 보드 옵션: 미획득은 전부 실루엣. 이웃/체인 규칙을 계산할 필요가 없다.
        if (definition.revealAllAsSilhouette)
            foreach (string nodeId in new List<string>(visibility.Keys))
                if (visibility[nodeId] == ClueBoardNodeVisibility.Locked)
                    visibility[nodeId] = ClueBoardNodeVisibility.Silhouette;

        if (definition.silhouetteNeighbors != null)
            foreach (ClueBoardSilhouetteNeighbor neighbor in definition.silhouetteNeighbors)
            {
                if (neighbor == null) continue;
                if (!unlocked.Contains(neighbor.revealedByNodeId)) continue;
                if (!visibility.TryGetValue(neighbor.silhouetteNodeId, out var current)) continue;
                // 이미 해금된 노드를 실루엣으로 되돌리지 않는다.
                if (current == ClueBoardNodeVisibility.Locked)
                    visibility[neighbor.silhouetteNodeId] = ClueBoardNodeVisibility.Silhouette;
            }

        // 체인 공개: 관계가 전부 성립한 체인만. 관계 하나짜리·빈 체인은 결과 배선 정책(ClueBoardOutcomeCatalog)과
        // 같이 무시한다 — 실루엣 공개까지 열어 주면 "체인은 관계 2개 이상" 규칙이 화면에서만 느슨해진다.
        if (definition.chains != null && connectedRelationIds != null)
        {
            var connected = new HashSet<string>(connectedRelationIds, StringComparer.Ordinal);
            foreach (ClueBoardRelationChain chain in definition.chains)
            {
                if (chain?.relationIds == null || chain.relationIds.Length < 2 || chain.revealSilhouetteNodeIds == null) continue;
                bool satisfied = true;
                foreach (string relationId in chain.relationIds)
                    if (string.IsNullOrEmpty(relationId) || !connected.Contains(relationId)) { satisfied = false; break; }
                if (!satisfied) continue;
                foreach (string nodeId in chain.revealSilhouetteNodeIds)
                {
                    if (string.IsNullOrEmpty(nodeId) || !visibility.TryGetValue(nodeId, out var current)) continue;
                    if (current == ClueBoardNodeVisibility.Locked)
                        visibility[nodeId] = ClueBoardNodeVisibility.Silhouette;
                }
            }
        }

        var hallucinations = new HashSet<string>(StringComparer.Ordinal);
        if (hallucinationNodeIds != null)
            foreach (string nodeId in hallucinationNodeIds)
                if (!string.IsNullOrEmpty(nodeId) && visibility.ContainsKey(nodeId)) hallucinations.Add(nodeId);

        return new ClueBoardNodeStates(definition.boardId, visibility, hallucinations);
    }

    /// <summary>
    /// 이 상태로 판정 엔진의 런타임 상태를 만든다. 표시 계산과 연결 판정이 같은 집합을 쓰게 해서
    /// "화면에는 연결 가능해 보이는데 엔진이 거절한다" 같은 어긋남을 막는다.
    /// </summary>
    public ClueBoardRuntimeState CreateRuntimeState(
        ClueBoardConnectionEngine engine, IEnumerable<string> restoredRelationIds = null)
    {
        if (engine == null) throw new ArgumentNullException(nameof(engine));
        return engine.CreateState(
            NodeIdsWith(ClueBoardNodeVisibility.Unlocked),
            NodeIdsWith(ClueBoardNodeVisibility.Silhouette),
            _hallucinationNodeIds,
            restoredRelationIds);
    }
}
