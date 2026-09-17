using System;
using System.Collections.Generic;
using UnityEngine;

// 보드 정의 파일의 공통 읽기/검증 계약. ClueDatabaseCodec과 같은 역할이며 실제 파일 쓰기는
// 에디터에서만 수행한다(Editor/ClueBoardDatabaseFile).
//
// [진단 방침] 판정 계약(ClueBoardConnectionEngine.TryValidateDefinition)은 보드 한 장을 검사해
// 첫 번째 문제에서 멈춘다. 콘텐츠 작업자는 "어느 보드의 무엇이 잘못됐는지"를 한 번에 보고 싶으므로,
// 이 코덱은 보드마다 그 결과를 받아 보드 ID를 붙여 모아서 돌려준다. 파일 전체가 한 줄짜리 오류로
// 끝나지 않게 하는 것이 목적이다.
public static class ClueBoardDatabaseCodec
{
    public static bool TryRead(string json, out ClueBoardDatabase database, out string error)
    {
        database = null;
        error = null;
        if (string.IsNullOrWhiteSpace(json)) { error = "보드 정의 JSON이 비어 있습니다."; return false; }

        ClueBoardDatabase parsed;
        try
        {
            parsed = JsonUtility.FromJson<ClueBoardDatabase>(json);
        }
        catch (Exception ex)
        {
            error = "보드 정의 JSON 읽기 실패: " + ex.Message;
            return false;
        }

        if (parsed == null) { error = "보드 정의 데이터베이스가 없습니다."; return false; }
        if (parsed.schemaVersion == 0)
        {
            // 버전 필드가 없거나 0이다. 이 형식에는 승격시킬 구 데이터가 없으므로 추측하지 않는다.
            error = $"보드 정의에 schemaVersion이 없습니다(구형/버전 누락). " +
                    $"현재 형식은 {ClueBoardDatabase.CurrentSchemaVersion}입니다.";
            return false;
        }
        if (parsed.schemaVersion != ClueBoardDatabase.CurrentSchemaVersion)
        {
            error = $"지원하지 않는 보드 정의 스키마: {parsed.schemaVersion} " +
                    $"(지원 버전 {ClueBoardDatabase.CurrentSchemaVersion})";
            return false;
        }

        if (!Validate(parsed, out error)) return false;
        database = parsed;
        return true;
    }

    // 파일 하나 안에서 닫히는 검사 — 보드 ID 중복과 보드별 구조 검증. 단서/맵 참조는 그 두 파일이
    // 있어야 판단할 수 있으므로 ValidateReferences로 분리했다(런타임 로드는 이쪽만 통과하면 된다).
    public static bool Validate(ClueBoardDatabase database, out string error)
    {
        if (database == null) { error = "보드 정의 데이터베이스가 없습니다."; return false; }
        if (database.boards == null) { error = "boards 배열이 필요합니다."; return false; }

        var errors = new List<string>();
        var boardIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < database.boards.Length; i++)
        {
            ClueBoardDefinition board = database.boards[i];
            if (board == null) { errors.Add($"boards[{i}]: 보드 정의가 null입니다."); continue; }

            // 판정 계약과 같은 규칙을 그대로 쓴다 — 저장은 통과했는데 런타임 엔진 생성자가
            // 예외를 던지는 상태를 만들지 않기 위해서다.
            if (!ClueBoardConnectionEngine.TryValidateDefinition(board, out string definitionError))
                errors.Add(Describe(i, board) + ": " + definitionError);

            if (string.IsNullOrWhiteSpace(board.boardId)) continue; // 위에서 이미 보고됨
            if (!boardIds.Add(board.boardId))
                errors.Add(Describe(i, board) + ": 중복 보드 ID입니다.");
        }

        error = errors.Count == 0 ? null : string.Join("\n", errors);
        return errors.Count == 0;
    }

    /// <summary>
    /// 콘텐츠 완성 검사 — 아직 로컬 보드를 배선하지 않은 맵도 오류로 본다.
    /// </summary>
    public static bool ValidateReferences(
        ClueBoardDatabase database, ClueDatabase clues, MapDatabase maps, out string error) =>
        ValidateReferences(database, clues, maps, requireEveryMapBound: true, out error, out _);

    // 교차 참조 검사. 슬롯이 가리키는 단서가 실제로 존재하는지, 맵이 가리키는 로컬 보드가
    // 존재하고 실제로 Local인지 함께 본다.
    //
    // [왜 누락과 오참조의 심각도를 나누나] "맵이 없는 보드를 가리킨다"와 "글로벌 보드를 로컬로
    // 참조한다"는 이 보드 파일을 저장하는 순간 런타임에서 깨지는 참조라 저장을 막아야 한다.
    // 반면 "아직 localBoardId를 안 넣은 맵이 있다"는 map_database.json 쪽의 미완성이고, 이것까지
    // 저장을 막으면 첫 보드를 만들 수 없다 — 맵이 보드를 가리키려면 보드가 먼저 저장돼 있어야
    // 하는데, 보드를 저장하려면 모든 맵이 이미 배선돼 있어야 하는 순환이 된다.
    // 그래서 저장 경로는 requireEveryMapBound=false로 부르고 경고로만 남긴다.
    //
    // maps를 넘기지 않으면(null) 맵 바인딩 검사는 건너뛴다 — 보드 파일만 단독으로 검사하고 싶은
    // 호출부(런타임 진단 등)를 위해서다. clues는 필수다.
    public static bool ValidateReferences(
        ClueBoardDatabase database, ClueDatabase clues, MapDatabase maps,
        bool requireEveryMapBound, out string error, out string warning)
    {
        warning = null;
        if (!Validate(database, out error)) return false;
        if (clues?.clues == null) { error = "단서 데이터가 없어 보드 참조를 검증할 수 없습니다."; return false; }

        var knownClueIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (ClueData clue in clues.clues)
            if (clue != null && !string.IsNullOrWhiteSpace(clue.id)) knownClueIds.Add(clue.id);

        var errors = new List<string>();
        for (int i = 0; i < database.boards.Length; i++)
        {
            ClueBoardDefinition board = database.boards[i];
            if (board?.slots == null) continue;
            foreach (ClueBoardSlot slot in board.slots)
            {
                if (slot == null || string.IsNullOrWhiteSpace(slot.clueId)) continue; // Validate가 이미 보고
                if (!knownClueIds.Contains(slot.clueId))
                    errors.Add($"{Describe(i, board)}: 슬롯 '{slot.nodeId}'가 없는 단서 ID '{slot.clueId}'를 가리킵니다.");
            }
        }

        AppendWarning(ref warning, GetConnectivityWarning(database));
        // [C06] 결과 배선(relation.readingId / chains)의 오타는 보드 자체를 막지 않고 경고로만 남긴다 —
        // 런타임은 같은 진단을 내고 그 결과만 무시한다(ClueBoardOutcomeCatalog).
        AppendWarning(ref warning, ClueBoardOutcomeCatalog.DescribeDiagnostics(database));
        // 실루엣 힌트 누락도 경고다 — 기본 문구가 대신 나가므로 보드는 뜬다.
        AppendWarning(ref warning, ClueBoardSilhouetteHint.DescribeMissingHints(database, clues));
        AppendWarning(ref warning, GetChainRevealWarning(database));

        if (maps != null) ValidateMapBindings(database, maps, requireEveryMapBound, errors, ref warning);

        error = errors.Count == 0 ? null : string.Join("\n", errors);
        return errors.Count == 0;
    }

    /// <summary>
    /// 슬롯을 추가하고 관계 정의를 빠뜨린 경우를 찾는다. Unrelated 관계도 콘텐츠 작성자가
    /// '의도적으로 연결 불가'를 명시한 것으로 인정한다. 한 슬롯짜리 보드는 연결 대상 자체가 없으므로 제외한다.
    /// </summary>
    public static string GetConnectivityWarning(ClueBoardDatabase database)
    {
        if (database?.boards == null) return null;
        var messages = new List<string>();
        for (int i = 0; i < database.boards.Length; i++)
        {
            ClueBoardDefinition board = database.boards[i];
            if (board?.slots == null || board.slots.Length <= 1) continue;

            var declaredNodes = new HashSet<string>(StringComparer.Ordinal);
            if (board.relations != null)
                foreach (ClueBoardRelation relation in board.relations)
                {
                    if (relation == null) continue;
                    if (!string.IsNullOrWhiteSpace(relation.firstNodeId)) declaredNodes.Add(relation.firstNodeId);
                    if (!string.IsNullOrWhiteSpace(relation.secondNodeId)) declaredNodes.Add(relation.secondNodeId);
                }

            var isolated = new List<string>();
            foreach (ClueBoardSlot slot in board.slots)
                if (slot != null && !string.IsNullOrWhiteSpace(slot.nodeId) && !declaredNodes.Contains(slot.nodeId))
                    isolated.Add($"{slot.nodeId}({slot.clueId})");

            if (isolated.Count > 0)
                messages.Add($"{Describe(i, board)}: 관계가 한 건도 선언되지 않은 슬롯: {string.Join(", ", isolated)}. " +
                             "연결 대상이면 relation을, 의도적 연결 불가면 Unrelated relation을 명시하세요.");
        }
        return messages.Count == 0 ? null : string.Join("\n", messages);
    }

    /// <summary>체인의 실루엣 공개 노드가 보드에 없거나 비어 있으면 경고. 런타임은 그 노드만 무시한다.</summary>
    public static string GetChainRevealWarning(ClueBoardDatabase database)
    {
        if (database?.boards == null) return null;
        var messages = new List<string>();
        for (int i = 0; i < database.boards.Length; i++)
        {
            ClueBoardDefinition board = database.boards[i];
            if (board?.chains == null) continue;
            var nodeIds = new HashSet<string>(StringComparer.Ordinal);
            if (board.slots != null)
                foreach (ClueBoardSlot slot in board.slots)
                    if (slot != null && !string.IsNullOrWhiteSpace(slot.nodeId)) nodeIds.Add(slot.nodeId);
            foreach (ClueBoardRelationChain chain in board.chains)
            {
                if (chain?.revealSilhouetteNodeIds == null) continue;
                foreach (string nodeId in chain.revealSilhouetteNodeIds)
                {
                    if (string.IsNullOrWhiteSpace(nodeId))
                        messages.Add($"{Describe(i, board)}: 체인 '{chain.chainId}'의 실루엣 공개 노드가 비어 있습니다.");
                    else if (!nodeIds.Contains(nodeId))
                        messages.Add($"{Describe(i, board)}: 체인 '{chain.chainId}'가 없는 노드 '{nodeId}'를 실루엣으로 공개하려 합니다.");
                }
            }
        }
        return messages.Count == 0 ? null : string.Join("\n", messages);
    }

    private static void AppendWarning(ref string warning, string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        warning = string.IsNullOrWhiteSpace(warning) ? message : warning + "\n" + message;
    }

    // 배선된 맵만 골라 판정 계약(TryValidateMapBindings)에 넘긴다. 규칙을 여기서 다시 구현하지
    // 않으려는 것 — 없는 보드/글로벌 오참조/중복 보드 ID의 정의는 엔진 한 곳에만 둔다.
    // 이 함수가 직접 판단하는 것은 "아직 배선되지 않았다"와 "GUID가 비었다" 둘뿐이다.
    private static void ValidateMapBindings(
        ClueBoardDatabase database, MapDatabase maps, bool requireEveryMapBound,
        List<string> errors, ref string warning)
    {
        if (maps.maps == null) { errors.Add("맵 데이터의 maps 배열이 없습니다."); return; }

        var bound = new List<MapNodeData>();
        var unbound = new List<string>();
        foreach (MapNodeData map in maps.maps)
        {
            if (map == null) { errors.Add("맵 항목이 null입니다."); continue; }
            if (string.IsNullOrWhiteSpace(map.guid)) { errors.Add("맵에 GUID가 없습니다."); continue; }
            if (string.IsNullOrWhiteSpace(map.localBoardId)) unbound.Add(map.guid);
            else bound.Add(map);
        }

        if (unbound.Count > 0)
        {
            string message = "로컬 보드 ID가 없는 맵: " + string.Join(", ", unbound) +
                " — 단서 출처·codexMapGuid·주변 맵으로 추론하지 않으므로 맵 데이터에 직접 지정해야 합니다.";
            if (requireEveryMapBound) errors.Add(message);
            else AppendWarning(ref warning, message);
        }

        var filtered = new MapDatabase
        {
            maps = bound.ToArray(),
            connections = maps.connections ?? Array.Empty<MapConnectionData>(),
        };
        if (!ClueBoardConnectionEngine.TryValidateMapBindings(filtered, database.boards, out string bindingError))
            errors.Add(bindingError);
    }

    public static bool TryWrite(ClueBoardDatabase database, out string json, out string error)
    {
        json = null;
        if (!Validate(database, out error)) return false;
        // 원본 객체의 버전을 바꾸지 않는다 — 저장에 성공한 뒤 호출부가 갱신한다(ClueDatabaseCodec과 동일).
        var output = new ClueBoardDatabase
        {
            schemaVersion = ClueBoardDatabase.CurrentSchemaVersion,
            boards = database.boards,
        };
        json = JsonUtility.ToJson(output, true);
        return true;
    }

    private static string Describe(int index, ClueBoardDefinition board) =>
        string.IsNullOrWhiteSpace(board?.boardId) ? $"boards[{index}]" : $"보드 '{board.boardId}'";
}
