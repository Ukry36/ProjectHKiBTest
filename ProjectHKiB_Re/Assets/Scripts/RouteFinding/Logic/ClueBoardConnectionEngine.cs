using System;
using System.Collections.Generic;

public readonly struct ClueBoardNodePair : IEquatable<ClueBoardNodePair>
{
    public readonly string firstNodeId;
    public readonly string secondNodeId;

    public ClueBoardNodePair(string firstNodeId, string secondNodeId)
    {
        if (string.CompareOrdinal(firstNodeId, secondNodeId) <= 0)
        {
            this.firstNodeId = firstNodeId;
            this.secondNodeId = secondNodeId;
        }
        else
        {
            this.firstNodeId = secondNodeId;
            this.secondNodeId = firstNodeId;
        }
    }

    public bool Equals(ClueBoardNodePair other) =>
        string.Equals(firstNodeId, other.firstNodeId, StringComparison.Ordinal) &&
        string.Equals(secondNodeId, other.secondNodeId, StringComparison.Ordinal);

    public override bool Equals(object obj) => obj is ClueBoardNodePair other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            return ((firstNodeId != null ? StringComparer.Ordinal.GetHashCode(firstNodeId) : 0) * 397) ^
                   (secondNodeId != null ? StringComparer.Ordinal.GetHashCode(secondNodeId) : 0);
        }
    }
}

public enum ClueBoardConnectStatus
{
    Connected,
    AlreadyConnected,
    BoardMismatch,
    NodeNotOnBoard,
    NodeLocked,
    SilhouetteCannotConnect,
    HallucinationCannotConnect,
    SelfConnection,
    Unrelated,
}

public enum ClueBoardDisconnectStatus
{
    Disconnected,
    BoardMismatch,
    RelationshipNotFound,
    NotConnected,
    PermanentConnection,
}

public readonly struct ClueBoardConnectResult
{
    public readonly ClueBoardConnectStatus status;
    public readonly string relationId;
    public readonly ClueBoardRelationKind relationKind;

    // 시도한 노드 쌍(드래그 출발 → 드롭 순서 그대로). 거절 결과에는 relationId가 없으므로 거절 코멘트("이 [A]와 [B]는
    // 연관이 없어 보인다")가 어느 두 단서였는지 알려면 이 값이 필요하다. 판정은 순서 무관이지만 문구는 순서를 지킨다.
    public readonly string firstNodeId;
    public readonly string secondNodeId;

    public bool succeeded => status == ClueBoardConnectStatus.Connected ||
                             status == ClueBoardConnectStatus.AlreadyConnected;
    public bool createdConnection => status == ClueBoardConnectStatus.Connected;

    public ClueBoardConnectResult(
        ClueBoardConnectStatus status,
        string relationId = null,
        ClueBoardRelationKind relationKind = ClueBoardRelationKind.Unrelated,
        string firstNodeId = null,
        string secondNodeId = null)
    {
        this.status = status;
        this.relationId = relationId;
        this.relationKind = relationKind;
        this.firstNodeId = firstNodeId;
        this.secondNodeId = secondNodeId;
    }
}

public sealed class ClueBoardRuntimeState
{
    private readonly HashSet<string> _unlockedNodeIds;
    private readonly HashSet<string> _silhouetteNodeIds;
    private readonly HashSet<string> _hallucinationNodeIds;
    private readonly HashSet<string> _connectedRelationIds;

    public string boardId { get; }
    public IReadOnlyCollection<string> connectedRelationIds => _connectedRelationIds;

    public ClueBoardRuntimeState(
        string boardId,
        IEnumerable<string> unlockedNodeIds = null,
        IEnumerable<string> silhouetteNodeIds = null,
        IEnumerable<string> hallucinationNodeIds = null,
        IEnumerable<string> connectedRelationIds = null)
    {
        if (string.IsNullOrWhiteSpace(boardId)) throw new ArgumentException("Board ID is empty.", nameof(boardId));
        this.boardId = boardId;
        _unlockedNodeIds = ToSet(unlockedNodeIds);
        _silhouetteNodeIds = ToSet(silhouetteNodeIds);
        _hallucinationNodeIds = ToSet(hallucinationNodeIds);
        _connectedRelationIds = ToSet(connectedRelationIds);
    }

    public bool IsUnlocked(string nodeId) => _unlockedNodeIds.Contains(nodeId);
    public bool IsSilhouette(string nodeId) => _silhouetteNodeIds.Contains(nodeId);
    public bool IsHallucination(string nodeId) => _hallucinationNodeIds.Contains(nodeId);
    public bool IsConnected(string relationId) => _connectedRelationIds.Contains(relationId);

    internal bool AddConnection(string relationId) => _connectedRelationIds.Add(relationId);
    internal bool RemoveConnection(string relationId) => _connectedRelationIds.Remove(relationId);

    private static HashSet<string> ToSet(IEnumerable<string> values) =>
        values == null
            ? new HashSet<string>(StringComparer.Ordinal)
            : new HashSet<string>(values, StringComparer.Ordinal);
}

public sealed class ClueBoardConnectionEngine
{
    private readonly ClueBoardDefinition _definition;
    private readonly Dictionary<string, ClueBoardSlot> _slotsById;
    private readonly Dictionary<string, ClueBoardRelation> _relationsById;
    private readonly Dictionary<ClueBoardNodePair, ClueBoardRelation> _relationsByPair;
    private readonly HashSet<string> _initialRelationIds;

    public ClueBoardConnectionEngine(ClueBoardDefinition definition)
    {
        if (!TryValidateDefinition(definition, out string error))
            throw new ArgumentException(error, nameof(definition));

        _definition = definition;
        _slotsById = new Dictionary<string, ClueBoardSlot>(StringComparer.Ordinal);
        _relationsById = new Dictionary<string, ClueBoardRelation>(StringComparer.Ordinal);
        _relationsByPair = new Dictionary<ClueBoardNodePair, ClueBoardRelation>();
        _initialRelationIds = new HashSet<string>(definition.initialRelationIds, StringComparer.Ordinal);

        foreach (ClueBoardSlot slot in definition.slots) _slotsById.Add(slot.nodeId, slot);
        foreach (ClueBoardRelation relation in definition.relations)
        {
            _relationsById.Add(relation.relationId, relation);
            _relationsByPair.Add(new ClueBoardNodePair(relation.firstNodeId, relation.secondNodeId), relation);
        }
    }

    public ClueBoardRuntimeState CreateState(
        IEnumerable<string> unlockedNodeIds = null,
        IEnumerable<string> silhouetteNodeIds = null,
        IEnumerable<string> hallucinationNodeIds = null,
        IEnumerable<string> restoredConnectionIds = null)
    {
        var connections = new HashSet<string>(_initialRelationIds, StringComparer.Ordinal);
        if (restoredConnectionIds != null)
            foreach (string relationId in restoredConnectionIds)
            {
                if (!_relationsById.ContainsKey(relationId))
                    throw new ArgumentException("Unknown restored relationship ID: " + relationId,
                        nameof(restoredConnectionIds));
                connections.Add(relationId);
            }

        return new ClueBoardRuntimeState(
            _definition.boardId, unlockedNodeIds, silhouetteNodeIds, hallucinationNodeIds, connections);
    }

    public bool TryConnect(
        ClueBoardRuntimeState state,
        string firstNodeId,
        string secondNodeId,
        out ClueBoardConnectResult result)
    {
        if (state == null) throw new ArgumentNullException(nameof(state));

        if (!string.Equals(state.boardId, _definition.boardId, StringComparison.Ordinal))
            return Fail(ClueBoardConnectStatus.BoardMismatch, firstNodeId, secondNodeId, out result);
        if (!_slotsById.ContainsKey(firstNodeId) || !_slotsById.ContainsKey(secondNodeId))
            return Fail(ClueBoardConnectStatus.NodeNotOnBoard, firstNodeId, secondNodeId, out result);
        if (string.Equals(firstNodeId, secondNodeId, StringComparison.Ordinal))
            return Fail(ClueBoardConnectStatus.SelfConnection, firstNodeId, secondNodeId, out result);
        if (state.IsSilhouette(firstNodeId) || state.IsSilhouette(secondNodeId))
            return Fail(ClueBoardConnectStatus.SilhouetteCannotConnect, firstNodeId, secondNodeId, out result);
        if (state.IsHallucination(firstNodeId) || state.IsHallucination(secondNodeId))
            return Fail(ClueBoardConnectStatus.HallucinationCannotConnect, firstNodeId, secondNodeId, out result);
        if (!state.IsUnlocked(firstNodeId) || !state.IsUnlocked(secondNodeId))
            return Fail(ClueBoardConnectStatus.NodeLocked, firstNodeId, secondNodeId, out result);

        if (!_relationsByPair.TryGetValue(new ClueBoardNodePair(firstNodeId, secondNodeId), out ClueBoardRelation relation) ||
            relation.kind == ClueBoardRelationKind.Unrelated)
            return Fail(ClueBoardConnectStatus.Unrelated, firstNodeId, secondNodeId, out result);

        if (state.IsConnected(relation.relationId))
        {
            result = new ClueBoardConnectResult(
                ClueBoardConnectStatus.AlreadyConnected, relation.relationId, relation.kind, firstNodeId, secondNodeId);
            return true;
        }

        state.AddConnection(relation.relationId);
        result = new ClueBoardConnectResult(
            ClueBoardConnectStatus.Connected, relation.relationId, relation.kind, firstNodeId, secondNodeId);
        return true;
    }

    public bool TryDisconnect(
        ClueBoardRuntimeState state,
        string relationId,
        out ClueBoardDisconnectStatus status)
    {
        if (state == null) throw new ArgumentNullException(nameof(state));
        if (!string.Equals(state.boardId, _definition.boardId, StringComparison.Ordinal))
        {
            status = ClueBoardDisconnectStatus.BoardMismatch;
            return false;
        }
        if (!_relationsById.TryGetValue(relationId, out ClueBoardRelation relation))
        {
            status = ClueBoardDisconnectStatus.RelationshipNotFound;
            return false;
        }

        if (!state.IsConnected(relationId))
        {
            status = ClueBoardDisconnectStatus.NotConnected;
            return false;
        }

        if (IsPermanent(relation))
        {
            status = ClueBoardDisconnectStatus.PermanentConnection;
            return false;
        }

        state.RemoveConnection(relationId);
        status = ClueBoardDisconnectStatus.Disconnected;
        return true;
    }

    public bool IsPermanent(ClueBoardRelation relation) =>
        relation != null &&
        (relation.kind == ClueBoardRelationKind.Required || relation.permanent ||
         _initialRelationIds.Contains(relation.relationId));

    public static bool TryValidateDefinition(ClueBoardDefinition definition, out string error)
    {
        if (definition == null) return Invalid("Board definition is missing.", out error);
        if (string.IsNullOrWhiteSpace(definition.boardId)) return Invalid("Board ID is empty.", out error);
        if (!Enum.IsDefined(typeof(ClueBoardKind), definition.kind))
            return Invalid("Unsupported board kind.", out error);
        if (definition.slots == null || definition.relations == null ||
            definition.silhouetteNeighbors == null || definition.initialRelationIds == null)
            return Invalid("Board arrays must not be null.", out error);

        var nodeIds = new HashSet<string>(StringComparer.Ordinal);
        var clueIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (ClueBoardSlot slot in definition.slots)
        {
            if (slot == null || string.IsNullOrWhiteSpace(slot.nodeId) || string.IsNullOrWhiteSpace(slot.clueId))
                return Invalid("Every slot needs a node ID and clue ID.", out error);
            if (!nodeIds.Add(slot.nodeId)) return Invalid("Duplicate node ID: " + slot.nodeId, out error);
            if (!clueIds.Add(slot.clueId)) return Invalid("Duplicate clue ID on board: " + slot.clueId, out error);
            if (!IsFinite(slot.anchoredPosition.x) || !IsFinite(slot.anchoredPosition.y))
                return Invalid("Slot position must be finite: " + slot.nodeId, out error);
            if (!IsFinite(slot.sizePercent) || (slot.sizePercent != 0f &&
                (slot.sizePercent < 25f || slot.sizePercent > 300f)))
                return Invalid("Slot size percent must be 0 or between 25 and 300: " + slot.nodeId, out error);
            if (!IsFinite(slot.fontSize) || (slot.fontSize != 0f &&
                (slot.fontSize < 4f || slot.fontSize > 24f)))
                return Invalid("Slot font size must be 0 or between 4 and 24: " + slot.nodeId, out error);
        }

        var relationIds = new HashSet<string>(StringComparer.Ordinal);
        var relationKinds = new Dictionary<string, ClueBoardRelationKind>(StringComparer.Ordinal);
        var pairs = new HashSet<ClueBoardNodePair>();
        foreach (ClueBoardRelation relation in definition.relations)
        {
            if (relation == null || string.IsNullOrWhiteSpace(relation.relationId))
                return Invalid("Every relationship needs an ID.", out error);
            if (!relationIds.Add(relation.relationId))
                return Invalid("Duplicate relationship ID: " + relation.relationId, out error);
            if (!Enum.IsDefined(typeof(ClueBoardRelationKind), relation.kind))
                return Invalid("Unsupported relationship kind: " + relation.relationId, out error);
            if (!nodeIds.Contains(relation.firstNodeId) || !nodeIds.Contains(relation.secondNodeId))
                return Invalid("Relationship references a missing node: " + relation.relationId, out error);
            if (string.Equals(relation.firstNodeId, relation.secondNodeId, StringComparison.Ordinal))
                return Invalid("Self relationship is not allowed: " + relation.relationId, out error);
            if (!pairs.Add(new ClueBoardNodePair(relation.firstNodeId, relation.secondNodeId)))
                return Invalid("Duplicate unordered node pair: " + relation.relationId, out error);
            relationKinds.Add(relation.relationId, relation.kind);
        }

        // 실루엣 공개는 revealedBy -> silhouette 방향을 가진다. A가 B를 공개하는 것과
        // B가 A를 공개하는 것은 서로 다른 정의이므로 관계 pair처럼 정규화하지 않는다.
        var silhouettePairs = new HashSet<string>(StringComparer.Ordinal);
        foreach (ClueBoardSilhouetteNeighbor neighbor in definition.silhouetteNeighbors)
        {
            if (neighbor == null || !nodeIds.Contains(neighbor.revealedByNodeId) ||
                !nodeIds.Contains(neighbor.silhouetteNodeId))
                return Invalid("Silhouette neighbor references a missing node.", out error);
            if (string.Equals(neighbor.revealedByNodeId, neighbor.silhouetteNodeId, StringComparison.Ordinal))
                return Invalid("A node cannot reveal itself as a silhouette.", out error);
            string directedKey = neighbor.revealedByNodeId + "\u001f" + neighbor.silhouetteNodeId;
            if (!silhouettePairs.Add(directedKey))
                return Invalid("Duplicate silhouette neighbor pair.", out error);
        }

        var initialIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (string relationId in definition.initialRelationIds)
        {
            if (string.IsNullOrWhiteSpace(relationId) || !relationKinds.TryGetValue(relationId, out var kind))
                return Invalid("Initial connection references a missing relationship: " + relationId, out error);
            if (kind == ClueBoardRelationKind.Unrelated)
                return Invalid("An unrelated pair cannot be an initial connection: " + relationId, out error);
            if (!initialIds.Add(relationId))
                return Invalid("Duplicate initial relationship ID: " + relationId, out error);
        }

        error = null;
        return true;
    }

    // 맵과 로컬 보드의 연결은 명시적 ID만 사용한다. 단서의 codexMapGuid/targetMapGuid나
    // 주변 맵 관계로 보드를 추론하는 fallback은 이 계약에 포함하지 않는다.
    public static bool TryValidateMapBindings(
        MapDatabase maps,
        IEnumerable<ClueBoardDefinition> definitions,
        out string error)
    {
        if (maps?.maps == null) return Invalid("Map database is missing.", out error);
        if (definitions == null) return Invalid("Board definitions are missing.", out error);

        var boardsById = new Dictionary<string, ClueBoardDefinition>(StringComparer.Ordinal);
        foreach (ClueBoardDefinition definition in definitions)
        {
            if (!TryValidateDefinition(definition, out error)) return false;
            if (boardsById.ContainsKey(definition.boardId))
                return Invalid("Duplicate board ID: " + definition.boardId, out error);
            boardsById.Add(definition.boardId, definition);
        }

        var mapIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (MapNodeData map in maps.maps)
        {
            if (map == null || string.IsNullOrWhiteSpace(map.guid))
                return Invalid("Every map needs a GUID.", out error);
            if (!mapIds.Add(map.guid)) return Invalid("Duplicate map GUID: " + map.guid, out error);
            if (string.IsNullOrWhiteSpace(map.localBoardId))
                return Invalid("Map has no local board ID: " + map.guid, out error);
            if (!boardsById.TryGetValue(map.localBoardId, out ClueBoardDefinition board))
                return Invalid("Map references a missing local board: " + map.guid + " -> " + map.localBoardId, out error);
            if (board.kind != ClueBoardKind.Local)
                return Invalid("Map references a non-local board: " + map.guid + " -> " + map.localBoardId, out error);
        }

        error = null;
        return true;
    }

    private static bool Fail(
        ClueBoardConnectStatus status, string firstNodeId, string secondNodeId, out ClueBoardConnectResult result)
    {
        result = new ClueBoardConnectResult(status, firstNodeId: firstNodeId, secondNodeId: secondNodeId);
        return false;
    }

    private static bool Invalid(string message, out string error)
    {
        error = message;
        return false;
    }

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
