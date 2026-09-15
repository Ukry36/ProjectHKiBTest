using System;
using System.Collections.Generic;
using UnityEngine;

// 보드 정의의 런타임 조회 계층. MapGraph가 clues.json/map_database.json에 대해 하는 일을
// clue_boards.json에 대해 한다 — 읽기 전용이고, 플레이어 진행 상태는 담지 않는다.
//
// [왜 MapGraph에 넣지 않았나] MapGraph는 씬에 놓인 MonoBehaviour 싱글턴이라, 보드를 쓰려는 쪽이
// 항상 씬 구성에 묶인다. 보드 정의는 에디터 검증·회귀 테스트에서도 그대로 읽어야 해서
// (ClueAttachmentService와 같은) 씬 독립 정적 클래스로 뒀다. 대신 맵 바인딩을 확인할 때만
// MapGraph/MapDatabase를 인자로 받는다.
//
// [추론 금지] 맵이 어느 로컬 보드를 여는지는 MapNodeData.localBoardId **하나로만** 결정한다.
// 단서의 codexMapGuid/targetMapGuid, 그 맵에서 얻을 수 있는 단서 목록, 인접 맵 같은 것으로
// 보드를 되짚어 추론하는 경로는 의도적으로 만들지 않는다 — 그렇게 하면 데이터가 비어 있어도
// 화면이 "그럴듯하게" 떠서, 배선을 빠뜨린 맵을 영영 못 찾는다. 없으면 없다고 말한다.
public static class ClueBoardCatalog
{
    // Resources 키. 실제 파일은 Scripts/RouteFinding/Resources/clue_boards.json이다
    // (clues.json과 같은 폴더 = 같은 Resources 루트).
    public const string DefaultResourcePath = "clue_boards";

    private static ClueBoardDatabase _database;
    private static Dictionary<string, ClueBoardDefinition> _boardsById;
    private static string _loadError;
    private static bool _attemptedLoad;

    /// <summary>정의가 성공적으로 로드돼 조회 가능한 상태인지.</summary>
    public static bool IsLoaded => _boardsById != null;

    /// <summary>마지막 로드가 실패한 이유. 성공했거나 아직 시도하지 않았으면 null.</summary>
    public static string LoadError => _loadError;

    public static IReadOnlyList<ClueBoardDefinition> AllBoards =>
        _database?.boards ?? Array.Empty<ClueBoardDefinition>();

    /// <summary>
    /// 아직 읽지 않았으면 Resources에서 한 번 읽는다. 실패해도 매 프레임 다시 시도하지 않는다 —
    /// 실패 사유는 LoadError에 남고, 콘텐츠를 고친 뒤에는 Reload로 명시적으로 다시 읽는다.
    /// </summary>
    public static bool EnsureLoaded()
    {
        if (_attemptedLoad) return IsLoaded;
        Reload();
        return IsLoaded;
    }

    /// <summary>Resources에서 다시 읽는다. 편집기에서 파일을 고친 뒤 호출한다.</summary>
    public static bool Reload(string resourcePath = DefaultResourcePath)
    {
        _attemptedLoad = true;
        Clear();

        var asset = Resources.Load<TextAsset>(resourcePath);
        if (asset == null)
        {
            // 아직 보드 콘텐츠가 없는 단계에서도 게임은 떠야 한다. 조회는 전부 실패하지만
            // 이유가 분명하므로, 여기서 예외를 던지는 대신 사유만 남긴다.
            _loadError = $"보드 정의를 찾을 수 없습니다: Resources/{resourcePath}";
            Debug.LogWarning("[ClueBoardCatalog] " + _loadError);
            return false;
        }

        return LoadFromJson(asset.text);
    }

    /// <summary>
    /// Resources를 거치지 않고 JSON 문자열에서 직접 읽는다 — 편집기 검증과 회귀 테스트용.
    /// 실제 콘텐츠 파일을 건드리지 않고 카탈로그 동작을 확인할 수 있다.
    /// </summary>
    public static bool LoadFromJson(string json)
    {
        _attemptedLoad = true;
        Clear();

        if (!ClueBoardDatabaseCodec.TryRead(json, out ClueBoardDatabase database, out string error))
        {
            _loadError = error;
            Debug.LogError("[ClueBoardCatalog] " + error);
            return false;
        }

        var byId = new Dictionary<string, ClueBoardDefinition>(StringComparer.Ordinal);
        // 코덱이 이미 중복 보드 ID를 거절하므로 Add로 넣어도 안전하다. 그래도 여기서 터지면
        // 코덱과 카탈로그의 규칙이 어긋났다는 뜻이라 조용히 덮어쓰지 않는다.
        foreach (ClueBoardDefinition board in database.boards) byId.Add(board.boardId, board);

        _database = database;
        _boardsById = byId;
        _loadError = null;
        string connectivityWarning = ClueBoardDatabaseCodec.GetConnectivityWarning(database);
        if (!string.IsNullOrEmpty(connectivityWarning))
            Debug.LogWarning("[ClueBoardCatalog] " + connectivityWarning);
        return true;
    }

    public static void Clear()
    {
        _database = null;
        _boardsById = null;
        _loadError = null;
    }

    /// <summary>보드 ID로 정의를 찾는다. 없으면 false와 사람이 읽을 수 있는 사유.</summary>
    public static bool TryGetBoard(string boardId, out ClueBoardDefinition board, out string error)
    {
        board = null;
        if (!EnsureLoaded())
        {
            error = _loadError ?? "보드 정의가 로드되지 않았습니다.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(boardId))
        {
            error = "보드 ID가 비어 있습니다.";
            return false;
        }
        if (!_boardsById.TryGetValue(boardId, out board))
        {
            error = "없는 보드 ID: " + boardId;
            return false;
        }
        error = null;
        return true;
    }

    /// <summary>지정한 종류의 보드만 훑는다(글로벌 보드 목록 등).</summary>
    public static IEnumerable<ClueBoardDefinition> BoardsOfKind(ClueBoardKind kind)
    {
        if (!EnsureLoaded()) yield break;
        foreach (ClueBoardDefinition board in _database.boards)
            if (board.kind == kind) yield return board;
    }

    /// <summary>
    /// 맵이 현실 진입 시 기본으로 열 로컬 보드를 찾는다. localBoardId **만** 본다.
    ///
    /// 실패 사유를 네 가지로 구분해서 돌려준다 — 배선이 빠진 것(빈 ID)과 오타(없는 ID)와
    /// 보드 종류를 잘못 지정한 것(Global을 로컬로 참조)은 콘텐츠 쪽에서 고치는 방법이 서로 다르다.
    /// </summary>
    public static bool TryGetLocalBoardForMap(MapNodeData map, out ClueBoardDefinition board, out string error)
    {
        board = null;
        if (map == null)
        {
            error = "맵 데이터가 없습니다.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(map.localBoardId))
        {
            error = $"맵 '{map.guid}'에 localBoardId가 없습니다. " +
                    "단서 출처·codexMapGuid·주변 맵으로 보드를 추론하지 않으므로 맵 데이터에 직접 지정해야 합니다.";
            return false;
        }
        if (!TryGetBoard(map.localBoardId, out board, out string lookupError))
        {
            board = null;
            error = $"맵 '{map.guid}' → {lookupError}";
            return false;
        }
        if (board.kind != ClueBoardKind.Local)
        {
            error = $"맵 '{map.guid}'가 로컬이 아닌 보드를 참조합니다: " +
                    $"'{map.localBoardId}' (종류 {board.kind})";
            board = null;
            return false;
        }
        error = null;
        return true;
    }
}
