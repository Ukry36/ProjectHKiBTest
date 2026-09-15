using System;
using System.Collections.Generic;

// 고정 단서 보드의 플레이어 진행(성립 연결) — C03-C 세션 유지 + C04 저장/복원의 공통 상태 객체.
// Unity 수명주기와 무관한 순수 C#이라(ClueNewState와 같은 위치) 세이브 왕복과 정의 검증을 한 경로로 검증한다.
//
// [정의와 진행의 분리] 좌표·관계 종류·초기 연결은 ClueBoardDefinition이 소유한다. 여기에는
// "플레이어가 이은 관계 ID"만 보드별로 쌓인다. 초기 연결(initialRelationIds)은 정의가 항상 다시 넣으므로
// 여기 저장하지 않으며, 저장돼 있어도 무해하다(정의에 있으면 그냥 연결).
//
// [검증 시점] Import는 정의를 보지 않고 그대로 담는다. 로드 시점에 보드 정의(ClueBoardCatalog)가 아직 없을 수
// 있는데, 그때 정의 기준으로 걸러 버리면 진행이 통째로 사라진다. 정의와의 대조는 화면이 보드를 열 때
// ResolveForBoard가 하며, 맞지 않는 항목은 런타임 연결로 만들지 않고 진단만 남긴다(저장소는 건드리지 않는다).
//
// [무엇을 담지 않는가] 드래그 중인 시작 노드·임시 선, 검색어/필터/하이라이트, 스크롤·선택은 이 객체와 세이브에
// 들어가지 않는다. 그것들은 화면(ClueBoardScreen/SearchPanel) 안의 세션 상태다.
public sealed class ClueBoardProgressState
{
    // boardId → relationId → 저장 당시 정규화 쌍(모르면 null)
    private readonly Dictionary<string, Dictionary<string, ClueBoardNodePair?>> _boards =
        new(StringComparer.Ordinal);

    public IReadOnlyCollection<string> BoardIds => _boards.Keys;

    public int ConnectionCount(string boardId) =>
        boardId != null && _boards.TryGetValue(boardId, out var set) ? set.Count : 0;

    public bool IsRecorded(string boardId, string relationId) =>
        boardId != null && relationId != null &&
        _boards.TryGetValue(boardId, out var set) && set.ContainsKey(relationId);

    /// <summary>플레이어가 새로 이은 연결을 기록한다. 이미 있으면 false(멱등).</summary>
    public bool MarkConnected(string boardId, string relationId, string firstNodeId = null, string secondNodeId = null)
    {
        if (string.IsNullOrEmpty(boardId) || string.IsNullOrEmpty(relationId)) return false;
        if (!_boards.TryGetValue(boardId, out var set))
            _boards[boardId] = set = new Dictionary<string, ClueBoardNodePair?>(StringComparer.Ordinal);
        if (set.ContainsKey(relationId)) return false;
        set[relationId] = PairOrNull(firstNodeId, secondNodeId);
        return true;
    }

    /// <summary>명시적 해제(C02 TryDisconnect가 허용한 경우에만 호출). 영구 연결 판정은 엔진이 한다.</summary>
    public bool MarkDisconnected(string boardId, string relationId) =>
        boardId != null && relationId != null &&
        _boards.TryGetValue(boardId, out var set) && set.Remove(relationId);

    public void Clear() => _boards.Clear();

    /// <summary>
    /// 보드 정의와 대조해 **지금 런타임 연결로 만들 수 있는** 관계 ID만 돌려준다(초기 연결은 포함하지 않는다 —
    /// 엔진 CreateState가 정의에서 넣는다). 거절 사유는 diagnostics에 남기고 저장소는 바꾸지 않는다.
    ///   - 정의에 없는 관계 ID(삭제·오타·다른 보드의 ID)
    ///   - 보조 검증 쌍이 정의의 쌍과 다른 관계(ID가 다른 연결로 재사용됨)
    ///   - Unrelated 관계(TryConnect가 성립시키지 않는 종류)
    /// </summary>
    public List<string> ResolveForBoard(ClueBoardDefinition definition, List<string> diagnostics = null)
    {
        var resolved = new List<string>();
        if (definition == null || string.IsNullOrEmpty(definition.boardId)) return resolved;
        if (!_boards.TryGetValue(definition.boardId, out var stored) || stored.Count == 0) return resolved;

        var relations = new Dictionary<string, ClueBoardRelation>(StringComparer.Ordinal);
        if (definition.relations != null)
            foreach (ClueBoardRelation relation in definition.relations)
                if (relation != null && !string.IsNullOrEmpty(relation.relationId))
                    relations[relation.relationId] = relation;

        foreach (KeyValuePair<string, ClueBoardNodePair?> entry in stored)
        {
            if (!relations.TryGetValue(entry.Key, out ClueBoardRelation relation))
            {
                diagnostics?.Add($"보드 '{definition.boardId}'에 관계 '{entry.Key}'가 없어 복원하지 않습니다.");
                continue;
            }
            if (relation.kind == ClueBoardRelationKind.Unrelated)
            {
                diagnostics?.Add($"보드 '{definition.boardId}'의 관계 '{entry.Key}'는 Unrelated라 복원하지 않습니다.");
                continue;
            }
            if (entry.Value.HasValue &&
                !entry.Value.Value.Equals(new ClueBoardNodePair(relation.firstNodeId, relation.secondNodeId)))
            {
                diagnostics?.Add($"보드 '{definition.boardId}'의 관계 '{entry.Key}'가 저장 당시와 다른 노드 쌍을 가리켜 복원하지 않습니다.");
                continue;
            }
            resolved.Add(entry.Key);
        }
        return resolved;
    }

    // ─── 세이브 왕복 ─────────────────────────────────────────

    public ClueBoardProgressSaveData Export()
    {
        var data = new ClueBoardProgressSaveData { version = ClueBoardProgressSaveData.CurrentVersion };
        foreach (KeyValuePair<string, Dictionary<string, ClueBoardNodePair?>> board in _boards)
        {
            if (board.Value.Count == 0) continue;
            var info = new ClueBoardProgressSaveInfo { boardId = board.Key };
            foreach (KeyValuePair<string, ClueBoardNodePair?> connection in board.Value)
                info.connections.Add(new ClueBoardConnectionSaveInfo
                {
                    relationId = connection.Key,
                    firstNodeId = connection.Value?.firstNodeId ?? "",
                    secondNodeId = connection.Value?.secondNodeId ?? "",
                });
            data.boards.Add(info);
        }
        return data;
    }

    public sealed class ImportReport
    {
        public bool rejectedFutureVersion;
        public int boardsImported;
        public int connectionsImported;
        public int duplicatesSkipped;
        public int invalidSkipped;
        public readonly List<string> messages = new();
    }

    /// <summary>
    /// 세이브 내용으로 **교체**한다(이전 세션 상태를 남기지 않는다). 정의와 대조하지 않는다(위 주석).
    /// version 0(구 세이브)은 진행 없음, CurrentVersion보다 크면 거절하고 비운다. 빈 ID·null 항목은 건너뛰고
    /// 같은 relationId 중복은 한 번만 적용한다.
    /// </summary>
    public ImportReport Import(ClueBoardProgressSaveData data)
    {
        var report = new ImportReport();
        _boards.Clear();
        if (data == null) return report;

        if (data.version > ClueBoardProgressSaveData.CurrentVersion)
        {
            report.rejectedFutureVersion = true;
            report.messages.Add($"보드 진행 저장 형식 v{data.version}은 이 빌드(v{ClueBoardProgressSaveData.CurrentVersion})가 읽을 수 없어 복원하지 않습니다.");
            return report;
        }
        if (data.boards == null || data.boards.Count == 0) return report;
        if (data.version <= 0)
        {
            // 이 필드를 쓰는 빌드는 항상 version을 넣는다. 0인데 내용이 있으면 손으로 고쳤거나 손상된 것이다.
            report.invalidSkipped += data.boards.Count;
            report.messages.Add("보드 진행 저장에 version이 없어 내용을 무시합니다.");
            return report;
        }

        foreach (ClueBoardProgressSaveInfo board in data.boards)
        {
            if (board == null || string.IsNullOrEmpty(board.boardId) || board.connections == null)
            {
                report.invalidSkipped++;
                continue;
            }
            bool any = false;
            foreach (ClueBoardConnectionSaveInfo connection in board.connections)
            {
                if (connection == null || string.IsNullOrEmpty(connection.relationId))
                {
                    report.invalidSkipped++;
                    continue;
                }
                if (MarkConnected(board.boardId, connection.relationId, connection.firstNodeId, connection.secondNodeId))
                {
                    report.connectionsImported++;
                    any = true;
                }
                else report.duplicatesSkipped++;
            }
            if (any) report.boardsImported++;
        }
        return report;
    }

    private static ClueBoardNodePair? PairOrNull(string firstNodeId, string secondNodeId) =>
        string.IsNullOrEmpty(firstNodeId) || string.IsNullOrEmpty(secondNodeId)
            ? (ClueBoardNodePair?)null
            : new ClueBoardNodePair(firstNodeId, secondNodeId);
}
