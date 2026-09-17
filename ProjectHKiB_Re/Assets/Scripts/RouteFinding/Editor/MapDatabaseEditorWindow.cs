using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEditor;

namespace RouteFinding.Editor
{
    // Unity 메뉴 RouteFinding > 맵 DB 편집기 로 열 수 있는 개발자 창.
    // map_database.json 과 clues.json 을 GUI로 열람·편집·저장한다.
    // Ctrl+S 로 즉시 저장. 미저장 상태는 상단에 표시됨.
    public class MapDatabaseEditorWindow : EditorWindow
    {
        private enum Tab { Maps, Connections, Clues, Boards, Internet, Settings }
        private Tab _tab = Tab.Maps;

        // ─── 데이터 ──────────────────────────────────────────────
        private MapDatabase      _db;
        private ClueDatabase     _clueDb;
        private InternetDatabase _netDb;
        private string _dbPath   = "";
        private string _cluePath = "";
        private string _netPath  = "";
        private bool   _dirty;
        private bool _clueDirty;
        private ClueDatabaseFile _clueFile;
        private string _clueError;

        // 단서 보드(C02) — clues/맵과 별개 파일이라 dirty·오류·저장 경로를 전부 따로 관리한다.
        private ClueBoardDatabase _boardDb;
        private ClueBoardDatabaseFile _boardFile;
        private string _boardPath = "";
        private string _boardError;
        private string _boardWarning;   // 저장을 막지는 않지만 남아 있는 미배선(맵 → 로컬 보드)
        private bool _boardDirty;

        // ─── UI 상태 ─────────────────────────────────────────────
        private int     _selMap  = -1;
        private int     _selConn = -1;
        private int     _selClue = -1;
        private int     _selBoard = -1;
        private int     _selSite = -1;
        private Vector2 _listScroll;
        private Vector2 _detailScroll;

        // 폴드아웃
        private bool _foldEvents       = true;
        // 슬롯·관계·체인 ID는 관계/실루엣/체인/세이브가 참조하는 안정 키다. 편집자는 단서 이름으로 고르고, ID는 자동 생성해
        // 고정한다. 직접 바꿔야 할 때만 이 토글을 켠다(바꾸면 참조도 같이 따라가지만 기존 세이브의 진행/발행 기록은 못 따라간다).
        private bool _editStableIds;

        // 보드 구조 탭 미리보기 캔버스. 보드(320×240)와 같은 비율로 그리고 카드를 끌면 슬롯 좌표가 바뀐다.
        private string _previewSelectedNodeId;
        private string _previewDragNodeId;
        private Vector2 _previewDragOffset;
        private bool _showPreview = true;
        private const float BoardCanvasWidth = 320f;
        private const float BoardCanvasHeight = 240f;
        private const float CardWidth = 36f;
        private const float CardHeight = 60f;
        private bool _foldClueIds      = true;
        private bool _foldWavePaths    = true;
        private bool _foldEnemies      = true;
        private bool _foldRequiredGears = true;
        private bool _foldKeywords = true;
        private bool _foldComments = true;
        private bool _foldAttachments = true;
        private bool _foldMedia = true;
        private bool _foldClueBoardPlacements = true;
        private bool _foldBoardSlots = true;
        private bool _foldBoardRelations = true;
        private bool _foldBoardSilhouettes = true;
        private bool _foldBoardInitial = true;
        private bool _foldBoardChains = true;

        // [C06] DreamReadings.asset의 해몽 ID 목록(읽기 전용 캐시). 불러오기 때 다시 읽는다. null이면 에셋을 못 읽은 상태.
        private List<string> _readingIds;

        // 인터넷 탭 — 사이트 하나에 게시글이 여러 개 들어가므로, 게시글은 각각 펼침 상태를 기억한다
        // (그 안의 하위 섹션 폴드아웃은 게시글별로 나누지 않고 전체 공용으로 둔다 — 상태를 게시글마다
        // 따로 들고 있을 만큼 얻는 게 없다).
        private bool _foldSiteUnlock = true;
        private bool _foldPostGrants = true;
        private bool _foldPostUnlock = true;
        private bool _foldPostAttachments;
        private bool _foldPostComments = true;
        private readonly System.Collections.Generic.HashSet<string> _expandedPostIds = new();

        // ─── 색상 ────────────────────────────────────────────────
        private static readonly Color ColDirty    = new(1.00f, 0.85f, 0.35f);
        private static readonly Color ColHeader   = new(0.18f, 0.22f, 0.30f);
        private static readonly Color ColSelected = new(0.25f, 0.43f, 0.78f, 0.55f);
        private static readonly Color ColSep      = new(0.45f, 0.45f, 0.45f, 0.35f);

        // ─── 진입점 ──────────────────────────────────────────────

        [MenuItem("Tools/RouteFinding/맵 DB 편집기")]
        public static void Open()
        {
            var w = GetWindow<MapDatabaseEditorWindow>("맵 DB 편집기");
            w.minSize = new Vector2(700f, 480f);
            w.Show();
        }

        private void OnEnable() => LoadAll();

        // ─── 파일 IO ─────────────────────────────────────────────

        private void LoadAll()
        {
            _db     = new MapDatabase  { maps = Array.Empty<MapNodeData>(), connections = Array.Empty<MapConnectionData>() };
            _clueDb = new ClueDatabase { clues = Array.Empty<ClueData>() };
            _netDb  = new InternetDatabase { sites = Array.Empty<InternetSite>() };

            // 씬 드롭다운 목록도 같이 새로 훑는다 — 데이터를 다시 불러오는 시점이면
            // 맵 에셋도 그 사이에 바뀌었을 가능성이 크다.
            MapSceneCatalog.Refresh();

            _dbPath   = FindAbsPath("map_database");
            _cluePath = Path.GetFullPath(Path.Combine(Application.dataPath,
                "Scripts/RouteFinding/Resources/clues.json"));
            _netPath  = FindAbsPath("internet");

            if (File.Exists(_dbPath))
            {
                _db = JsonUtility.FromJson<MapDatabase>(File.ReadAllText(_dbPath));
                NormalizeMaps();
            }
            // 보드 정의 — clues.json과 같은 Resources 폴더에 둔다. 아직 없을 수 있으므로 실패해도
            // 창 전체를 막지 않고 탭 안에서 만들 수 있게 안내한다.
            _boardPath = Path.GetFullPath(Path.Combine(Application.dataPath,
                "Scripts/RouteFinding/Resources/clue_boards.json"));
            _boardFile = null;
            _boardError = null;
            _boardWarning = null;
            _readingIds = null; // [C06] 해몽 목록도 불러오기 때마다 다시 읽는다
            _boardDb = new ClueBoardDatabase
            {
                schemaVersion = ClueBoardDatabase.CurrentSchemaVersion,
                boards = Array.Empty<ClueBoardDefinition>(),
            };
            if (File.Exists(_boardPath))
            {
                if (ClueBoardDatabaseFile.TryOpen(_boardPath, out _boardFile, out var loadedBoards, out _boardError))
                {
                    _boardDb = loadedBoards;
                    foreach (var board in _boardDb.boards) NormalizeBoard(board);
                }
            }
            else _boardError = "clue_boards.json이 아직 없습니다. 아래 [보드 정의 파일 만들기]로 생성하세요.";

            _clueFile = null;
            _clueError = null;
            if (ClueDatabaseFile.TryOpen(_cluePath, out _clueFile, out var loadedClues, out _clueError))
            {
                _clueDb = loadedClues;
                NormalizeClues();
            }

            if (File.Exists(_netPath))
            {
                _netDb = JsonUtility.FromJson<InternetDatabase>(File.ReadAllText(_netPath));
                NormalizeNet();
            }

            _dirty = false;
            _clueDirty = false;
            _boardDirty = false;
            ResetUndoHistory();
            Repaint();
        }

        private static void NormalizeUnlock(InternetUnlockCondition u)
        {
            u.requiredClueIds   = u.requiredClueIds   ?? Array.Empty<string>();
            u.requiredEventKeys = u.requiredEventKeys ?? Array.Empty<string>();
        }

        private void MarkDirty()
        {
            if (_tab == Tab.Clues) _clueDirty = true;
            else if (_tab == Tab.Boards) _boardDirty = true;
            else _dirty = true;
        }

        // JsonUtility는 없는 키를 null로 남길 수 있고, 판정 계약은 배열이 null이면 정의를 거절한다.
        // 편집기가 null 배열을 그대로 들고 있으면 아직 아무것도 안 건드린 새 보드조차 저장이 막히므로
        // 읽은 직후 한 번 채워둔다(맵/단서 탭이 LoadAll에서 하는 것과 같은 처리).
        private static void NormalizeBoard(ClueBoardDefinition board)
        {
            if (board == null) return;
            board.boardId ??= "";
            board.slots ??= Array.Empty<ClueBoardSlot>();
            board.relations ??= Array.Empty<ClueBoardRelation>();
            board.silhouetteNeighbors ??= Array.Empty<ClueBoardSilhouetteNeighbor>();
            board.initialRelationIds ??= Array.Empty<string>();
            foreach (var slot in board.slots)
            {
                if (slot == null) continue;
                slot.nodeId ??= "";
                slot.clueId ??= "";
                slot.fontAddress ??= "";
                slot.silhouetteHint ??= "";
                // overrideAppearance 도입 전 슬롯에 직접 저장된 외형은 명시적 덮어쓰기로 승격한다.
                if (!slot.overrideAppearance && (slot.sizePercent > 0f || slot.fontSize > 0f ||
                    !string.IsNullOrWhiteSpace(slot.fontAddress) || slot.hideLabel))
                    slot.overrideAppearance = true;
            }
            foreach (var relation in board.relations)
            {
                if (relation == null) continue;
                relation.relationId ??= "";
                relation.firstNodeId ??= "";
                relation.secondNodeId ??= "";
            }
            foreach (var neighbor in board.silhouetteNeighbors)
            {
                if (neighbor == null) continue;
                neighbor.revealedByNodeId ??= "";
                neighbor.silhouetteNodeId ??= "";
            }
            // [C06] 결과 배선(readingId/chains)도 같은 이유로 채운다.
            ClueBoardOutcomeEditing.NormalizeChains(board);
        }

        private void SaveAll()
        {
            // 보드 탭은 자기 파일을 저장하되, 보드 화면에서 맵 배선(localBoardId)을 고쳤으면 맵 DB를 먼저 쓴다 —
            // SaveBoards의 참조 검증이 디스크의 map_database.json을 읽기 때문이다.
            if (_tab == Tab.Boards)
            {
                if (_dirty && !string.IsNullOrEmpty(_dbPath))
                {
                    File.WriteAllText(_dbPath, JsonUtility.ToJson(_db, prettyPrint: true));
                    _dirty = false;
                }
                SaveBoards();
                return;
            }
            // 통합 단서 탭은 단서 공통값과 보드 배치 예외를 한 화면에서 고치므로 두 파일을 함께 저장한다.
            if (_tab == Tab.Clues)
            {
                SaveCluesAndBoards();
                return;
            }
            if (!string.IsNullOrEmpty(_dbPath))
                File.WriteAllText(_dbPath, JsonUtility.ToJson(_db, prettyPrint: true));
            if (!string.IsNullOrEmpty(_netPath))
                File.WriteAllText(_netPath, JsonUtility.ToJson(_netDb, prettyPrint: true));
            AssetDatabase.Refresh();
            _dirty = false;
        }

        private void SaveCluesAndBoards()
        {
            if (_clueFile == null)
            {
                _clueError = "단서 파일이 열려 있지 않습니다. 다시 불러오세요.";
                return;
            }
            if (_boardDirty && _boardFile == null)
            {
                _boardError = "보드 정의 파일이 열려 있지 않습니다. 다시 불러오세요.";
                return;
            }

            MapDatabase savedMaps;
            try
            {
                savedMaps = JsonUtility.FromJson<MapDatabase>(File.ReadAllText(_dbPath));
                if (!ClueDatabaseCodec.ValidateReferences(_clueDb, savedMaps, out _clueError)) return;
                if (!ClueBoardDatabaseCodec.ValidateReferences(
                        _boardDb, _clueDb, savedMaps, requireEveryMapBound: false,
                        out _boardError, out string warning)) return;
                _boardWarning = FilterBoardWarnings(AppendWarning(warning, ClueBoardOutcomeEditing.DescribeUnknownReadings(_boardDb, ReadingIds)));
            }
            catch (Exception ex)
            {
                _clueError = "통합 단서/보드 참조 검증 실패: " + ex.Message;
                return;
            }

            if (_clueDirty)
            {
                string clueBackupRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "../Backups/Clues"));
                if (!_clueFile.TrySave(_clueDb, clueBackupRoot, out _, out _clueError)) return;
                _clueDb.schemaVersion = ClueDatabase.CurrentSchemaVersion;
                _clueDirty = false;
            }

            if (_boardDirty)
            {
                string boardBackupRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "../Backups/ClueBoards"));
                if (!_boardFile.TrySave(_boardDb, boardBackupRoot, out _, out _boardError)) return;
                _boardDb.schemaVersion = ClueBoardDatabase.CurrentSchemaVersion;
                _boardDirty = false;
                _boardError = null;
            }
            AssetDatabase.Refresh();
        }

        // 보드 저장 — 정의 자체와 "맵 → 로컬 보드" 참조를 함께 통과해야만 파일을 바꾼다.
        // 단서/맵은 **디스크에 저장된 내용**으로 검사한다: 편집기 메모리에만 있는 새 단서나 새 맵을
        // 보드가 참조하면, 보드 파일만 먼저 저장됐을 때 런타임에서 깨진 참조가 되기 때문이다
        // (단서 탭이 맵을 디스크 기준으로 검사하는 것과 같은 이유).
        private void SaveBoards()
        {
            if (_boardFile == null)
            {
                _boardError = "보드 정의 파일이 열려 있지 않습니다. [보드 정의 파일 만들기] 또는 [불러오기]를 먼저 실행하세요.";
                return;
            }
            ClueDatabase savedClues;
            MapDatabase savedMaps;
            try
            {
                if (!ClueDatabaseCodec.TryRead(File.ReadAllText(_cluePath), out savedClues, out _boardError)) return;
                savedMaps = JsonUtility.FromJson<MapDatabase>(File.ReadAllText(_dbPath));
            }
            catch (Exception ex) { _boardError = "보드 참조 검증 실패: " + ex.Message; return; }

            // 아직 로컬 보드를 배선하지 않은 맵은 저장을 막지 않고 경고로만 남긴다 — 맵이 보드를
            // 가리키려면 보드 파일이 먼저 저장돼 있어야 하므로, 여기서 막으면 첫 보드를 만들 수 없다.
            if (!ClueBoardDatabaseCodec.ValidateReferences(
                    _boardDb, savedClues, savedMaps, requireEveryMapBound: false,
                    out _boardError, out string bindingWarning)) return;

            string backupRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "../Backups/ClueBoards"));
            if (!_boardFile.TrySave(_boardDb, backupRoot, out _, out _boardError)) return;
            _boardDb.schemaVersion = ClueBoardDatabase.CurrentSchemaVersion;
            _boardDirty = false;
            _boardError = null;
            // 저장은 됐지만 남은 배선(맵 미배선·결과 배선 오타·없는 해몽 ID)이 있으면 계속 눈에 보이게 둔다.
            _boardWarning = FilterBoardWarnings(AppendWarning(bindingWarning, ClueBoardOutcomeEditing.DescribeUnknownReadings(_boardDb, ReadingIds)));
            AssetDatabase.Refresh();
        }

        private static string FindAbsPath(string filename)
        {
            foreach (var g in AssetDatabase.FindAssets(filename + " t:TextAsset"))
            {
                var ap = AssetDatabase.GUIDToAssetPath(g);
                if (ap.EndsWith(filename + ".json", StringComparison.OrdinalIgnoreCase))
                    return Path.GetFullPath(
                        Path.Combine(Application.dataPath, "..", ap));
            }
            return "";
        }

        // ─── OnGUI 최상위 ────────────────────────────────────────

        private void OnGUI()
        {
            Event e = Event.current;
            // Ctrl+S 저장
            if (e.type == EventType.KeyDown && e.control && e.keyCode == KeyCode.S)
            {
                SaveAll();
                e.Use();
            }
            // Ctrl+Z 되돌리기 / Ctrl+Y·Ctrl+Shift+Z 다시 실행 — 순수 데이터라 Unity Undo 대신 스냅샷 이력을 쓴다.
            // 기타 설정 탭은 에셋(SerializedObject)이라 Unity Undo가 맡는다 — 거기서는 가로채지 않는다.
            if (e.type == EventType.KeyDown && e.control && !e.alt && _tab != Tab.Settings)
            {
                if (e.keyCode == KeyCode.Z && !e.shift) { UndoEdit(); e.Use(); }
                else if (e.keyCode == KeyCode.Y || (e.keyCode == KeyCode.Z && e.shift)) { RedoEdit(); e.Use(); }
            }

            try { DrawBody(); }
            finally { if (Event.current.type != EventType.Layout) RecordUndoIfChanged(); }
        }

        private void DrawBody()
        {
            DrawToolbar();

            // 기타 설정 탭은 목록이 없는 한 장짜리 화면이다.
            if (_tab == Tab.Settings)
            {
                _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll, GUILayout.ExpandWidth(true));
                DrawSettingsTab();
                EditorGUILayout.EndScrollView();
                return;
            }

            EditorGUILayout.BeginHorizontal();

            // 목록 패널 (좌 220px)
            GUILayout.BeginVertical(GUILayout.Width(220f));
            DrawList();
            GUILayout.EndVertical();

            // 구분선
            var sepR = GUILayoutUtility.GetRect(2f, 0f, GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(sepR, ColSep);

            // 상세 편집 패널
            GUILayout.BeginVertical();
            DrawDetail();
            GUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }


        // JsonUtility가 null로 남기는 문자열/배열을 채운다. 불러오기와 되돌리기(스냅샷 복원) 양쪽이 같은 규칙을 쓴다.
        private void NormalizeMaps()
        {
            _db.maps        = _db.maps        ?? Array.Empty<MapNodeData>();
            _db.connections = _db.connections ?? Array.Empty<MapConnectionData>();
            foreach (var m in _db.maps)
            {
                m.iconAddress = m.iconAddress ?? "";
                m.localBoardId = m.localBoardId ?? "";
                m.events  = m.events  ?? Array.Empty<MapEventFlag>();
                m.clueIds = m.clueIds ?? Array.Empty<string>();
                m.wavePaths     = m.wavePaths     ?? Array.Empty<string>();
                m.enemyGroups   = m.enemyGroups   ?? Array.Empty<EnemyGroupEntry>();
                m.requiredGears = m.requiredGears ?? Array.Empty<EmotionColor>();
            }
        }

        private void NormalizeClues()
        {
            _clueDb.clues = _clueDb.clues ?? Array.Empty<ClueData>();
            foreach (var cl in _clueDb.clues)
            {
                cl.requiredEventKey = cl.requiredEventKey ?? "";
                cl.timestamp     = cl.timestamp     ?? "";
                cl.content       = cl.content       ?? "";
                cl.source        = cl.source        ?? "";
                cl.codexMapGuid  = cl.codexMapGuid  ?? "";
                cl.keywords      = cl.keywords      ?? Array.Empty<string>();
                cl.comments      = cl.comments      ?? Array.Empty<CodexComment>();
                cl.attachments   = cl.attachments   ?? Array.Empty<ClueAttachment>();
                cl.iconAddress   = cl.iconAddress   ?? "";
                cl.boardFontAddress = cl.boardFontAddress ?? "";
                cl.silhouetteHint = cl.silhouetteHint ?? "";
                cl.mediaBlocks   = cl.mediaBlocks   ?? Array.Empty<ClueMediaBlock>();
                foreach (var at in cl.attachments)
                {
                    at.label        = at.label        ?? "";
                    at.address      = at.address      ?? "";
                    at.mapGuid      = at.mapGuid      ?? "";
                }
                foreach (var block in cl.mediaBlocks)
                {
                    block.text    = block.text    ?? "";
                    block.address = block.address ?? "";
                    block.caption = block.caption ?? "";
                }
            }
        }

        private void NormalizeNet()
        {
            _netDb.sites = _netDb.sites ?? Array.Empty<InternetSite>();
            foreach (var site in _netDb.sites)
            {
                site.iconAddress = site.iconAddress ?? "";
                site.unlock   = site.unlock ?? new InternetUnlockCondition();
                NormalizeUnlock(site.unlock);
                site.posts    = site.posts ?? Array.Empty<InternetPost>();
                foreach (var post in site.posts)
                {
                    post.title        = post.title ?? "";
                    post.author       = post.author ?? "";
                    post.postedAt     = post.postedAt ?? "";
                    post.body         = post.body ?? "";
                    post.grantClueIds = post.grantClueIds ?? Array.Empty<string>();
                    post.unlock       = post.unlock ?? new InternetUnlockCondition();
                    NormalizeUnlock(post.unlock);
                    post.attachments  = post.attachments ?? Array.Empty<ClueAttachment>();
                    foreach (var at in post.attachments)
                    {
                        at.label        = at.label        ?? "";
                        at.address      = at.address      ?? "";
                        at.mapGuid      = at.mapGuid      ?? "";
                    }
                    post.comments = post.comments ?? Array.Empty<CodexComment>();
                }
            }
        }

        // ─── 되돌리기 / 다시 실행 ─────────────────────────────────
        //
        // 이 창은 UnityEngine.Object가 아닌 순수 C# 데이터(JSON DTO)를 고치므로 Unity의 Undo가 잡지 못한다. 대신 값이
        // 바뀔 때마다 네 파일의 JSON 스냅샷을 쌓아 Ctrl+Z / Ctrl+Y(또는 Ctrl+Shift+Z)로 되돌린다. 스냅샷은 GUI가 바뀐
        // 프레임에만 만들고 직전과 같으면 쌓지 않는다. 불러오기는 이력을 비운다(파일이 기준이 바뀌므로).
        private sealed class Snapshot
        {
            public string maps, clues, boards, net;
            public bool dirty, clueDirty, boardDirty;
            public bool SameContent(Snapshot other) =>
                other != null && maps == other.maps && clues == other.clues && boards == other.boards && net == other.net;
        }

        private const int MaxUndoDepth = 100;
        private readonly List<Snapshot> _undoStack = new();
        private readonly List<Snapshot> _redoStack = new();
        private Snapshot _lastSnapshot;
        // 글자를 한 자씩 칠 때마다 한 단계가 되지 않게, 짧은 간격의 연속 변경은 한 단계로 묶는다.
        private double _lastUndoPushTime = -10.0;
        private const double UndoCoalesceSeconds = 0.6;

        private Snapshot TakeSnapshot() => new()
        {
            maps = JsonUtility.ToJson(_db), clues = JsonUtility.ToJson(_clueDb),
            boards = JsonUtility.ToJson(_boardDb), net = JsonUtility.ToJson(_netDb),
            dirty = _dirty, clueDirty = _clueDirty, boardDirty = _boardDirty,
        };

        private void ResetUndoHistory()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            _lastSnapshot = TakeSnapshot();
        }

        // OnGUI 끝에서 부른다. 값이 바뀐 프레임이면 직전 상태를 이력에 넣는다.
        private void RecordUndoIfChanged()
        {
            bool flagsChanged = _lastSnapshot != null &&
                (_lastSnapshot.dirty != _dirty || _lastSnapshot.clueDirty != _clueDirty || _lastSnapshot.boardDirty != _boardDirty);
            if (!GUI.changed && !flagsChanged) return;
            if (_lastSnapshot == null) { _lastSnapshot = TakeSnapshot(); return; }

            Snapshot now = TakeSnapshot();
            if (now.SameContent(_lastSnapshot))
            {
                _lastSnapshot = now; // 내용은 같고 저장 여부만 바뀐 경우(저장 직후)
                return;
            }
            double t = EditorApplication.timeSinceStartup;
            if (t - _lastUndoPushTime > UndoCoalesceSeconds)
            {
                _undoStack.Add(_lastSnapshot);
                if (_undoStack.Count > MaxUndoDepth) _undoStack.RemoveAt(0);
            }
            _lastUndoPushTime = t;
            _redoStack.Clear();
            _lastSnapshot = now;
        }

        private bool CanUndo => _undoStack.Count > 0;
        private bool CanRedo => _redoStack.Count > 0;

        private void UndoEdit()
        {
            if (!CanUndo) return;
            Snapshot target = _undoStack[_undoStack.Count - 1];
            _undoStack.RemoveAt(_undoStack.Count - 1);
            _redoStack.Add(TakeSnapshot());
            RestoreSnapshot(target);
        }

        private void RedoEdit()
        {
            if (!CanRedo) return;
            Snapshot target = _redoStack[_redoStack.Count - 1];
            _redoStack.RemoveAt(_redoStack.Count - 1);
            _undoStack.Add(TakeSnapshot());
            RestoreSnapshot(target);
        }

        private void RestoreSnapshot(Snapshot snapshot)
        {
            _db = JsonUtility.FromJson<MapDatabase>(snapshot.maps);
            _clueDb = JsonUtility.FromJson<ClueDatabase>(snapshot.clues);
            _boardDb = JsonUtility.FromJson<ClueBoardDatabase>(snapshot.boards);
            _netDb = JsonUtility.FromJson<InternetDatabase>(snapshot.net);
            NormalizeMaps();
            NormalizeClues();
            _boardDb.boards = _boardDb.boards ?? Array.Empty<ClueBoardDefinition>();
            foreach (var board in _boardDb.boards) NormalizeBoard(board);
            NormalizeNet();
            // 되돌린 뒤에는 파일과 다를 수 있으므로 미저장 표시를 스냅샷 시점 값으로 되돌린다.
            _dirty = snapshot.dirty;
            _clueDirty = snapshot.clueDirty;
            _boardDirty = snapshot.boardDirty;
            _lastSnapshot = snapshot;
            _lastUndoPushTime = -10.0; // 되돌린 직후의 변경은 새 단계로 시작한다
            // 포커스된 텍스트 필드는 자기 편집 버퍼를 다시 써 넣으므로 포커스를 뗀다.
            GUI.FocusControl(null);
            EditorGUIUtility.editingTextField = false;
            _previewDragNodeId = null;
            Repaint();
        }

        // ─── 툴바 ────────────────────────────────────────────────

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Toggle(_tab == Tab.Maps,        "맵 노드",  EditorStyles.toolbarButton, GUILayout.Width(72f))) _tab = Tab.Maps;
            if (GUILayout.Toggle(_tab == Tab.Connections, "연결",     EditorStyles.toolbarButton, GUILayout.Width(52f))) _tab = Tab.Connections;
            if (GUILayout.Toggle(_tab == Tab.Clues,       "단서 + 보드", EditorStyles.toolbarButton, GUILayout.Width(86f))) _tab = Tab.Clues;
            if (GUILayout.Toggle(_tab == Tab.Boards,      "보드 구조", EditorStyles.toolbarButton, GUILayout.Width(72f))) _tab = Tab.Boards;
            if (GUILayout.Toggle(_tab == Tab.Internet,    "인터넷",   EditorStyles.toolbarButton, GUILayout.Width(60f))) _tab = Tab.Internet;
            if (GUILayout.Toggle(_tab == Tab.Settings,    "기타 설정", EditorStyles.toolbarButton, GUILayout.Width(72f))) _tab = Tab.Settings;

            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(!CanUndo))
                if (GUILayout.Button(new GUIContent("↶", $"되돌리기 (Ctrl+Z) — {_undoStack.Count}단계"), EditorStyles.toolbarButton, GUILayout.Width(26f)))
                    UndoEdit();
            using (new EditorGUI.DisabledScope(!CanRedo))
                if (GUILayout.Button(new GUIContent("↷", $"다시 실행 (Ctrl+Y) — {_redoStack.Count}단계"), EditorStyles.toolbarButton, GUILayout.Width(26f)))
                    RedoEdit();

            if (_dirty || _clueDirty || _boardDirty)
            {
                var c = GUI.color; GUI.color = ColDirty;
                GUILayout.Label("● 미저장", EditorStyles.toolbarButton);
                GUI.color = c;
            }

            // 주소는 문자열이라 편집기 밖(JSON 직접 편집, 그룹 창에서 엔트리 삭제)에서 얼마든지
            // 어긋날 수 있다. 어긋나도 런타임엔 "(파일 없음)"으로만 보여서 발견이 늦으므로,
            // 세 파일의 주소를 한 번에 훑는 버튼을 둔다(MapDataRegistrySOEditor의 검증 버튼과 같은 역할).
            if (GUILayout.Button("첨부물 주소 검증", EditorStyles.toolbarButton, GUILayout.Width(102f)))
                ValidateAttachmentAddresses();

            if (GUILayout.Button("불러오기", EditorStyles.toolbarButton, GUILayout.Width(64f)))
            {
                if ((!_dirty && !_clueDirty && !_boardDirty) || EditorUtility.DisplayDialog(
                        "확인", "저장하지 않은 변경이 있습니다. 다시 불러오시겠습니까?", "불러오기", "취소"))
                    LoadAll();
            }

            var bg = GUI.backgroundColor;
            if (_tab == Tab.Settings)
            {
                // 설정 탭은 JSON이 아니라 에셋이다. 값은 바꾸는 즉시 에셋에 기록(SetDirty)되고, 여기서는 디스크로 내려쓴다.
                if (GUILayout.Button("설정 에셋 저장", EditorStyles.toolbarButton, GUILayout.Width(102f)))
                    AssetDatabase.SaveAssets();
                EditorGUILayout.EndHorizontal();
                return;
            }
            bool tabDirty = _tab == Tab.Clues ? (_clueDirty || _boardDirty) : _tab == Tab.Boards ? (_boardDirty || _dirty) : _dirty;
            string saveLabel = _tab == Tab.Clues ? "단서+보드 저장" : _tab == Tab.Boards ? (_dirty ? "보드+맵 저장" : "보드 저장") : "맵/인터넷 저장";
            GUI.backgroundColor = tabDirty ? ColDirty : bg;
            if (GUILayout.Button(saveLabel, EditorStyles.toolbarButton, GUILayout.Width(102f)))
                SaveAll();
            GUI.backgroundColor = bg;

            EditorGUILayout.EndHorizontal();

            // 파일 경로 한 줄
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label(
                "DB: "   + (string.IsNullOrEmpty(_dbPath)   ? "❌ map_database.json 없음" : ShortPath(_dbPath)),
                EditorStyles.miniLabel);
            GUILayout.Label(
                "단서: " + (string.IsNullOrEmpty(_cluePath) ? "❌ clues.json 없음"        : ShortPath(_cluePath)),
                EditorStyles.miniLabel);
            GUILayout.Label(
                "보드: " + (string.IsNullOrEmpty(_boardPath) ? "❌ clue_boards.json 없음" : ShortPath(_boardPath)),
                EditorStyles.miniLabel);
            GUILayout.Label(
                "인터넷: " + (string.IsNullOrEmpty(_netPath) ? "❌ internet.json 없음"    : ShortPath(_netPath)),
                EditorStyles.miniLabel);
            if (GUILayout.Button("탐색기", EditorStyles.miniButton, GUILayout.Width(50f)))
                EditorUtility.RevealInFinder(string.IsNullOrEmpty(_dbPath) ? Application.dataPath : _dbPath);
            EditorGUILayout.EndHorizontal();
            if (!string.IsNullOrEmpty(_clueError))
                EditorGUILayout.HelpBox(_clueError, MessageType.Error);
            if (_tab == Tab.Clues && !string.IsNullOrEmpty(_boardError))
                EditorGUILayout.HelpBox(_boardError, MessageType.Error);
            if (_tab == Tab.Clues && !string.IsNullOrEmpty(_boardWarning))
                EditorGUILayout.HelpBox(_boardWarning, MessageType.Warning);
            if (_tab == Tab.Boards && !string.IsNullOrEmpty(_boardError))
                EditorGUILayout.HelpBox(_boardError, MessageType.Error);
            if (_tab == Tab.Boards && !string.IsNullOrEmpty(_boardWarning))
                EditorGUILayout.HelpBox(_boardWarning, MessageType.Warning);
            if (_tab == Tab.Boards && _boardFile == null)
            {
                // 실제 콘텐츠는 만들지 않는다 — 빈 껍데기 파일만 명시적인 클릭으로 생성한다.
                if (GUILayout.Button("보드 정의 파일 만들기 (빈 clue_boards.json)", GUILayout.Height(20f)))
                {
                    if (ClueBoardDatabaseFile.TryCreate(_boardPath, out _boardFile, out var created, out _boardError))
                    {
                        _boardDb = created;
                        _boardDirty = false;
                        AssetDatabase.Refresh();
                    }
                }
            }
            if (_tab == Tab.Clues && _clueDb.schemaVersion == 0)
                EditorGUILayout.HelpBox("구 형식: 모든 단서의 분류를 명시적으로 선택해야 저장할 수 있습니다. 구 type 번호는 새 분류로 변환하지 않습니다.", MessageType.Warning);
        }

        // ─── 목록 패널 ───────────────────────────────────────────

        private void DrawList()
        {
            // 탭 헤더 + 추가 버튼
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            string hdr = _tab == Tab.Maps        ? $"맵 노드  ({_db.maps.Length})"        :
                         _tab == Tab.Connections  ? $"연결  ({_db.connections.Length})"    :
                         _tab == Tab.Clues        ? $"단서  ({_clueDb.clues.Length})"      :
                         _tab == Tab.Boards       ? $"보드  ({_boardDb.boards.Length})"    :
                                                    $"사이트  ({_netDb.sites.Length})";
            GUILayout.Label(hdr, EditorStyles.toolbarButton, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("+", EditorStyles.toolbarButton, GUILayout.Width(26f)))
            {
                AddItem();
                MarkDirty();
                Repaint();
            }
            EditorGUILayout.EndHorizontal();

            _listScroll = EditorGUILayout.BeginScrollView(_listScroll);

            switch (_tab)
            {
                case Tab.Maps:
                    for (int i = 0; i < _db.maps.Length; i++)
                        DrawListRow(i, _db.maps[i].nodeName, ref _selMap);
                    break;
                case Tab.Connections:
                    for (int i = 0; i < _db.connections.Length; i++)
                    {
                        var c = _db.connections[i];
                        DrawListRow(i, $"{NodeName(c.fromGuid)} → {NodeName(c.toGuid)}", ref _selConn);
                    }
                    break;
                case Tab.Clues:
                    for (int i = 0; i < _clueDb.clues.Length; i++)
                        DrawListRow(i, _clueDb.clues[i].name, ref _selClue);
                    break;
                case Tab.Boards:
                    for (int i = 0; i < _boardDb.boards.Length; i++)
                    {
                        var b = _boardDb.boards[i];
                        DrawListRow(i, $"[{(b.kind == ClueBoardKind.Global ? "글로벌" : "로컬")}] {b.boardId}  ({b.slots.Length})", ref _selBoard);
                    }
                    break;
                case Tab.Internet:
                    for (int i = 0; i < _netDb.sites.Length; i++)
                    {
                        var s = _netDb.sites[i];
                        int postCount = s.posts != null ? s.posts.Length : 0;
                        DrawListRow(i, $"{s.name}  ({postCount})", ref _selSite);
                    }
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawListRow(int idx, string label, ref int sel)
        {
            var row = GUILayoutUtility.GetRect(0f, 22f, GUILayout.ExpandWidth(true));
            if (idx == sel) EditorGUI.DrawRect(row, ColSelected);

            // 항목 선택
            if (GUI.Button(new Rect(row.x, row.y, row.width - 24f, row.height),
                    "  " + label,
                    idx == sel ? EditorStyles.whiteLabel : EditorStyles.label))
                sel = idx;

            // 삭제 버튼
            if (GUI.Button(new Rect(row.xMax - 22f, row.y + 1f, 20f, row.height - 2f), "×"))
            {
                RemoveItem(idx);
                if (sel >= idx) sel = Mathf.Max(-1, sel - 1);
                MarkDirty();
                GUIUtility.ExitGUI();
            }
        }

        // ─── 상세 편집 패널 ───────────────────────────────────────

        private void DrawDetail()
        {
            _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll, GUILayout.ExpandWidth(true));

            switch (_tab)
            {
                case Tab.Maps:
                    if (_selMap >= 0 && _selMap < _db.maps.Length)
                        DrawMapDetail(_db.maps[_selMap]);
                    else
                        EditorGUILayout.HelpBox("← 목록에서 맵을 선택하거나 [+] 로 추가하세요.", MessageType.Info);
                    break;
                case Tab.Connections:
                    if (_selConn >= 0 && _selConn < _db.connections.Length)
                        DrawConnDetail(_db.connections[_selConn]);
                    else
                        EditorGUILayout.HelpBox("← 목록에서 연결을 선택하거나 [+] 로 추가하세요.", MessageType.Info);
                    break;
                case Tab.Clues:
                    if (_selClue >= 0 && _selClue < _clueDb.clues.Length)
                        DrawClueDetail(_clueDb.clues[_selClue]);
                    else
                        EditorGUILayout.HelpBox("← 목록에서 단서를 선택하거나 [+] 로 추가하세요.", MessageType.Info);
                    break;
                case Tab.Boards:
                    if (_selBoard >= 0 && _selBoard < _boardDb.boards.Length)
                        DrawBoardDetail(_boardDb.boards[_selBoard]);
                    else
                        EditorGUILayout.HelpBox("← 목록에서 보드를 선택하거나 [+] 로 추가하세요.", MessageType.Info);
                    break;
                case Tab.Internet:
                    if (string.IsNullOrEmpty(_netPath))
                        EditorGUILayout.HelpBox(
                            "internet.json 을 찾을 수 없습니다. clues.json 과 같은 Resources 폴더에 만들어 주세요\n" +
                            "(내용은 {\"sites\":[]} 한 줄이면 충분합니다).", MessageType.Warning);
                    else if (_selSite >= 0 && _selSite < _netDb.sites.Length)
                        DrawSiteDetail(_netDb.sites[_selSite]);
                    else
                        EditorGUILayout.HelpBox("← 목록에서 사이트를 선택하거나 [+] 로 추가하세요.", MessageType.Info);
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        // ─── 맵 노드 편집 ─────────────────────────────────────────

        private void DrawMapDetail(MapNodeData n)
        {
            SectionHeader("맵 노드 편집");
            EditorGUI.BeginChangeCheck();

            // 기본 정보
            ReadonlyField("GUID", n.guid);
            n.nodeName    = TF("이름",    n.nodeName);
            n.sceneName   = SceneNamePopup("씬 (맵 데이터)", n.sceneName);
            n.iconAddress = AddressableField<Sprite>("아이콘 (선택)", n.iconAddress);
            n.description = TA("설명",    n.description);

            EditorGUILayout.Space(4f);
            n.localBoardId = TF("기본 로컬 단서 보드 ID", n.localBoardId);
            EditorGUILayout.HelpBox(
                "현재 맵에서 현실 관계도 보드를 열 때 사용할 명시적 ID입니다. " +
                "획득 단서의 출처·공개 대상이나 주변 맵에서 자동 추론하지 않습니다. " +
                "보드 정의 파일/편집 도구가 연결되기 전에는 빈 값과 오타를 저장할 수 있으므로 C02 통합 검증에서 차단합니다.",
                string.IsNullOrWhiteSpace(n.localBoardId) ? MessageType.Warning : MessageType.None);

            EditorGUILayout.Space(4f);
            n.graphPosition  = EditorGUILayout.Vector2Field("그래프 좌표", n.graphPosition);
            n.isStartNode    = EditorGUILayout.Toggle("시작 지점 (집)",  n.isStartNode);
            n.startsWithClue = EditorGUILayout.Toggle("초기 단서 보유", n.startsWithClue);

            // 이벤트 플래그 배열
            EditorGUILayout.Space(6f);
            _foldEvents = EditorGUILayout.Foldout(_foldEvents,
                $"이벤트 플래그  ({n.events.Length}개)", true, EditorStyles.foldoutHeader);
            if (_foldEvents)
            {
                EditorGUI.indentLevel++;
                int removeEvent = -1;
                for (int i = 0; i < n.events.Length; i++)
                {
                    var ev = n.events[i];
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label($"[{i}]", GUILayout.Width(28f));
                    ev.key   = EditorGUILayout.TextField(ev.key, GUILayout.ExpandWidth(true));
                    ev.value = EditorGUILayout.Toggle(ev.value, GUILayout.Width(20f));
                    if (GUILayout.Button("−", GUILayout.Width(22f))) removeEvent = i;
                    EditorGUILayout.EndHorizontal();
                }
                if (removeEvent >= 0) ArrayUtility.RemoveAt(ref n.events, removeEvent);
                if (GUILayout.Button("+ 이벤트 플래그 추가", GUILayout.ExpandWidth(false)))
                    ArrayUtility.Add(ref n.events, new MapEventFlag { key = "event_key", value = false });
                EditorGUI.indentLevel--;
            }

            // 단서 ID 배열
            EditorGUILayout.Space(4f);
            _foldClueIds = EditorGUILayout.Foldout(_foldClueIds,
                $"획득 가능 단서 ID  ({n.clueIds.Length}개)", true, EditorStyles.foldoutHeader);
            if (_foldClueIds)
            {
                EditorGUI.indentLevel++;
                int removeClue = -1;
                for (int i = 0; i < n.clueIds.Length; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label($"[{i}]", GUILayout.Width(28f));
                    n.clueIds[i] = EditorGUILayout.TextField(n.clueIds[i]);
                    if (GUILayout.Button("−", GUILayout.Width(22f))) removeClue = i;
                    EditorGUILayout.EndHorizontal();
                }
                if (removeClue >= 0) ArrayUtility.RemoveAt(ref n.clueIds, removeClue);
                if (GUILayout.Button("+ 단서 ID 추가", GUILayout.ExpandWidth(false)))
                    ArrayUtility.Add(ref n.clueIds, "");
                EditorGUI.indentLevel--;
            }

            // 2026-07-14 — 전투 데이터(웨이브 경로/적 구성/필수 장비)가 연결에서 맵으로 이동.
            // 웨이브 경로 배열
            EditorGUILayout.Space(6f);
            _foldWavePaths = EditorGUILayout.Foldout(_foldWavePaths,
                $"웨이브 경로  ({n.wavePaths.Length}개)", true, EditorStyles.foldoutHeader);
            if (_foldWavePaths)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox("Resources/ 이후 상대 경로  예) RouteFinding/Waves/wave_01", MessageType.None);
                int removeWave = -1;
                for (int i = 0; i < n.wavePaths.Length; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label($"[{i}]", GUILayout.Width(28f));
                    n.wavePaths[i] = EditorGUILayout.TextField(n.wavePaths[i]);
                    if (GUILayout.Button("−", GUILayout.Width(22f))) removeWave = i;
                    EditorGUILayout.EndHorizontal();
                }
                if (removeWave >= 0) ArrayUtility.RemoveAt(ref n.wavePaths, removeWave);
                if (GUILayout.Button("+ 웨이브 경로 추가", GUILayout.ExpandWidth(false)))
                    ArrayUtility.Add(ref n.wavePaths, "");
                EditorGUI.indentLevel--;
            }

            // 적 구성 배열
            EditorGUILayout.Space(4f);
            _foldEnemies = EditorGUILayout.Foldout(_foldEnemies,
                $"적 구성  ({n.enemyGroups.Length}그룹)", true, EditorStyles.foldoutHeader);
            if (_foldEnemies)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("감정 색상",    GUILayout.Width(140f));
                EditorGUILayout.LabelField("규모",          GUILayout.Width(80f));
                EditorGUILayout.LabelField("수량",          GUILayout.Width(50f));
                EditorGUILayout.EndHorizontal();
                int removeEnemy = -1;
                for (int i = 0; i < n.enemyGroups.Length; i++)
                {
                    var eg = n.enemyGroups[i];
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label($"[{i}]", GUILayout.Width(28f));
                    eg.emotionType = (EmotionColor)EditorGUILayout.EnumPopup(eg.emotionType, GUILayout.Width(140f));
                    eg.scale       = (EnemyScale)EditorGUILayout.EnumPopup(eg.scale,         GUILayout.Width(80f));
                    eg.count       = EditorGUILayout.IntField(eg.count,                        GUILayout.Width(50f));
                    if (GUILayout.Button("−", GUILayout.Width(22f))) removeEnemy = i;
                    EditorGUILayout.EndHorizontal();
                }
                if (removeEnemy >= 0) ArrayUtility.RemoveAt(ref n.enemyGroups, removeEnemy);
                if (GUILayout.Button("+ 적 그룹 추가", GUILayout.ExpandWidth(false)))
                    ArrayUtility.Add(ref n.enemyGroups,
                        new EnemyGroupEntry { emotionType = EmotionColor.SadnessBlue, scale = EnemyScale.Small, count = 1 });
                EditorGUI.indentLevel--;
            }

            // 필수 장비 배열 (비어있으면 진입 제한 없음)
            EditorGUILayout.Space(4f);
            _foldRequiredGears = EditorGUILayout.Foldout(_foldRequiredGears,
                $"필수 장비  ({n.requiredGears.Length}개)", true, EditorStyles.foldoutHeader);
            if (_foldRequiredGears)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox("비어있으면 제한 없음. 모두 충족해야 이 맵에 진입 가능 (그룹 단위 비교).", MessageType.None);
                int removeGear = -1;
                for (int i = 0; i < n.requiredGears.Length; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label($"[{i}]", GUILayout.Width(28f));
                    n.requiredGears[i] = (EmotionColor)EditorGUILayout.EnumPopup(n.requiredGears[i], GUILayout.Width(140f));
                    if (GUILayout.Button("−", GUILayout.Width(22f))) removeGear = i;
                    EditorGUILayout.EndHorizontal();
                }
                if (removeGear >= 0) ArrayUtility.RemoveAt(ref n.requiredGears, removeGear);
                if (GUILayout.Button("+ 필수 장비 추가", GUILayout.ExpandWidth(false)))
                    ArrayUtility.Add(ref n.requiredGears, EmotionColor.SadnessBlue);
                EditorGUI.indentLevel--;
            }

            if (EditorGUI.EndChangeCheck()) MarkDirty();
        }

        // ─── 연결 편집 ────────────────────────────────────────────

        private void DrawConnDetail(MapConnectionData c)
        {
            SectionHeader("연결 편집");
            EditorGUI.BeginChangeCheck();

            ReadonlyField("GUID", c.guid);
            c.fromGuid       = NodeGuidPopup("출발 맵", c.fromGuid);
            c.toGuid         = NodeGuidPopup("도착 맵",  c.toGuid);
            c.startsWithClue = EditorGUILayout.Toggle("초기 단서 보유", c.startsWithClue);
            EditorGUILayout.HelpBox(
                "전투 관련 데이터(웨이브 경로/적 구성/필수 장비)는 2026-07-14부로 맵 쪽으로 이동했습니다 — " +
                "도착 맵(위 드롭다운) 편집 화면에서 설정하세요.", MessageType.Info);

            if (EditorGUI.EndChangeCheck()) MarkDirty();
        }

        // ─── 단서 편집 ────────────────────────────────────────────

        private void DrawClueDetail(ClueData cl)
        {
            SectionHeader("단서 편집");
            string previousClueId = cl.id;
            EditorGUI.BeginChangeCheck();

            cl.id          = TF("ID",   cl.id);
            cl.name        = TF("이름", cl.name);
            cl.description = TA("설명", cl.description);

            EditorGUILayout.Space(4f);
            cl.targetMapGuid        = NodeGuidPopup("대상 맵",  cl.targetMapGuid,        allowEmpty: true);
            cl.targetConnectionGuid = ConnGuidPopup("대상 연결", cl.targetConnectionGuid, allowEmpty: true);

            EditorGUILayout.Space(4f);
            cl.requiredEventKey = TF("필요 이벤트 키", cl.requiredEventKey);
            EditorGUILayout.HelpBox(
                "출발 맵(이 단서가 '획득 가능 단서 ID'에 등록된 맵)을 방문해야 획득 가능.\n" +
                "비어있으면 방문만으로 획득. 값이 있으면 해당 맵에서 " +
                "RouteModule.Instance.Progress.SetEventFlag(맵GUID, 이 키)가 호출된 후에만 획득.",
                MessageType.None);

            // ─── 도감(Codex) 전용 필드 ──────────────────────────
            EditorGUILayout.Space(8f);
            SectionHeader("도감 카드 정보");

            DrawClueClassification(cl);
            cl.emotionTag = (ClueEmotionTag)EditorGUILayout.Popup("단서 감정", (int)cl.emotionTag,
                new[] { "미설정", "기쁨", "슬픔", "분노" });
            cl.iconAddress = TF("대표 아이콘 주소", cl.iconAddress);

            EditorGUILayout.Space(6f);
            SectionHeader("단서 보드 공통 외형");
            EditorGUILayout.HelpBox(
                "이 단서가 글로벌/로컬 보드에 여러 번 나와도 기본 외형은 한 번만 설정합니다. " +
                "특정 보드에서만 달라야 하면 아래 '보드 배치와 관계'에서 덮어쓰기를 켜세요.",
                MessageType.None);
            float commonSize = cl.boardSizePercent > 0f ? cl.boardSizePercent : 100f;
            cl.boardSizePercent = EditorGUILayout.Slider("카드 크기 (%)", commonSize, 25f, 300f);
            float commonFontSize = cl.boardFontSize > 0f ? cl.boardFontSize : 6.5f;
            cl.boardFontSize = EditorGUILayout.Slider("글자 크기", commonFontSize, 4f, 24f);
            cl.boardFontAddress = AddressableField<TMP_FontAsset>("폰트 (선택)", cl.boardFontAddress ?? "");
            cl.boardHideLabel = EditorGUILayout.Toggle("제목 숨김 (사진 전용)", cl.boardHideLabel);

            EditorGUILayout.Space(4f);
            cl.silhouetteHint = TA("실루엣 힌트 (미획득 실루엣에 마우스를 올리면 표시)", cl.silhouetteHint);
            EditorGUILayout.HelpBox(
                "형식적인 조건 문구가 아니라 \"이런 기억이 있던 것 같은데..\" 같은 암시로 씁니다. 실루엣은 이름·아이콘을 " +
                "숨기므로 단서 이름을 그대로 넣으면 정체가 새어 나갑니다. 비우면 기본 문구(" +
                ClueBoardSilhouetteHint.DefaultHint + ")가 표시되고, 보드별로 다른 암시가 필요하면 아래 배치에서 덮어씁니다.",
                MessageType.None);

            EditorGUILayout.Space(6f);
            SectionHeader("도감 본문 정보");
            cl.timestamp = TF("시간 (표시용, 비우면 숨김)", cl.timestamp);
            cl.content   = TA("도감 본문", cl.content);
            cl.source    = TF("출처", cl.source);
            cl.codexMapGuid = NodeGuidPopup("도감 분류 맵 (없으면 '기타')", cl.codexMapGuid, allowEmpty: true);

            EditorGUILayout.Space(4f);
            _foldKeywords = EditorGUILayout.Foldout(_foldKeywords,
                $"키워드  ({cl.keywords.Length}개)", true, EditorStyles.foldoutHeader);
            if (_foldKeywords)
            {
                EditorGUI.indentLevel++;
                int removeKw = -1;
                for (int i = 0; i < cl.keywords.Length; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label($"[{i}]", GUILayout.Width(28f));
                    cl.keywords[i] = EditorGUILayout.TextField(cl.keywords[i]);
                    if (GUILayout.Button("−", GUILayout.Width(22f))) removeKw = i;
                    EditorGUILayout.EndHorizontal();
                }
                if (removeKw >= 0) ArrayUtility.RemoveAt(ref cl.keywords, removeKw);
                if (GUILayout.Button("+ 키워드 추가", GUILayout.ExpandWidth(false)))
                    ArrayUtility.Add(ref cl.keywords, "");
                EditorGUI.indentLevel--;
            }

            // 본문 매체 블록(C01, 2026-09-08) — 위 "도감 본문" 문자열 아래에 순서대로 이어 붙는다.
            EditorGUILayout.Space(4f);
            DrawMediaList(ref cl.mediaBlocks, ref _foldMedia, "본문 매체 (글/사진/영상/소리)",
                "위 '도감 본문' 문자열 다음에 이 순서대로 카드에 이어 붙습니다. 매체 형식은 단서 유형(상징/그림/인쇄물/물체)과 무관합니다.\n" +
                "글 블록은 본문이, 나머지는 Addressable 주소가 비어 있으면 저장이 거절됩니다.");

            // 첨부물(2026-08-11) — 사진/소리/맵 참조. 도감 카드와 인터넷 게시글 본문에 표시된다.
            EditorGUILayout.Space(4f);
            DrawAttachmentList(ref cl.attachments, ref _foldAttachments, "첨부물 (사진/소리/맵)",
                "사진/소리는 Resources 폴더 안의 에셋만 쓸 수 있습니다 — 오브젝트 칸에 끌어다 놓으면 경로가 자동으로 채워집니다.\n" +
                "맵 첨부는 그 맵의 아이콘과 이름을 보여주고, 누르면 지도에서 해당 맵으로 이동합니다 (아이콘은 맵 노드 편집 화면에서 지정).");

            // 4단계(2026-07-14) — NPC/시스템 코멘트. 플레이어가 입력하는 게 아니라 콘텐츠 작업자가
            // 여기서 직접 채워 넣는 대사 데이터다(Clue_System.md 1-4장 확정 사항).
            EditorGUILayout.Space(4f);
            DrawCommentList(ref cl.comments, ref _foldComments, "코멘트 (NPC/시스템)",
                "플레이어 입력이 아니라 NPC/시스템이 다는 코멘트 — 카드에서 타이프라이터 연출로 출력됨.");

            if (EditorGUI.EndChangeCheck())
            {
                _clueDirty = true;
                if (!string.Equals(previousClueId, cl.id, StringComparison.Ordinal))
                    ReplaceClueIdInBoards(previousClueId, cl.id);
            }

            DrawClueBoardPlacements(cl);
        }

        // [한 곳 규칙] 단서 하나의 값(내용·공통 외형·실루엣 힌트)은 이 탭, 보드 안 값(슬롯 좌표·보드별 외형/힌트 예외·
        // 관계·실루엣·체인)은 "보드 구조" 탭에서만 편집한다. 같은 값을 두 화면에서 고치게 두면 어느 쪽이 최신인지 꼬이고,
        // 두 화면의 규칙(자동 정리·진단)이 조금씩 달라진다. 여기서는 "어느 보드에 올릴지"만 정하고 나머지는 읽기 전용 요약이다.
        private void DrawClueBoardPlacements(ClueData clue)
        {
            EditorGUILayout.Space(8f);
            _foldClueBoardPlacements = EditorGUILayout.Foldout(_foldClueBoardPlacements,
                "보드 배치", true, EditorStyles.foldoutHeader);
            if (!_foldClueBoardPlacements) return;

            if (_boardFile == null)
            {
                EditorGUILayout.HelpBox("clue_boards.json을 먼저 만들거나 다시 불러오세요.", MessageType.Warning);
                return;
            }

            EditorGUILayout.HelpBox(
                "체크하면 해당 보드에 이 단서를 배치합니다. 좌표·보드별 외형·관계·실루엣·체인은 보드 구조 탭에서만 편집합니다.",
                MessageType.None);

            for (int boardIndex = 0; boardIndex < _boardDb.boards.Length; boardIndex++)
            {
                ClueBoardDefinition board = _boardDb.boards[boardIndex];
                if (board == null) continue;
                NormalizeBoard(board);
                int slotIndex = Array.FindIndex(board.slots,
                    item => item != null && string.Equals(item.clueId, clue.id, StringComparison.Ordinal));
                bool placed = slotIndex >= 0;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                bool nextPlaced = EditorGUILayout.ToggleLeft(
                    $"[{(board.kind == ClueBoardKind.Global ? "글로벌" : "로컬")}] {board.boardId}", placed,
                    EditorStyles.boldLabel);
                if (placed && GUILayout.Button("보드 구조 탭에서 편집", GUILayout.Width(140f)))
                {
                    _tab = Tab.Boards;
                    _selBoard = boardIndex;
                    _foldBoardSlots = true;
                    _previewSelectedNodeId = board.slots[slotIndex].nodeId;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    GUIUtility.ExitGUI();
                    return;
                }
                EditorGUILayout.EndHorizontal();
                if (nextPlaced != placed)
                {
                    if (nextPlaced)
                    {
                        int index = board.slots.Length;
                        ArrayUtility.Add(ref board.slots, new ClueBoardSlot
                        {
                            nodeId = NextNodeId(board),
                            clueId = clue.id ?? "",
                            anchoredPosition = new Vector2(28f + (index % 5) * 60f, -34f - (index / 5) * 74f),
                            overrideAppearance = false,
                        });
                    }
                    else
                    {
                        string goneNodeId = board.slots[slotIndex].nodeId;
                        ArrayUtility.RemoveAt(ref board.slots, slotIndex);
                        RemoveBoardReferencesToNode(board, goneNodeId);
                    }
                    _boardDirty = true;
                    EditorGUILayout.EndVertical();
                    GUIUtility.ExitGUI();
                    return;
                }

                if (placed)
                {
                    ClueBoardSlot slot = board.slots[slotIndex];
                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField("슬롯", $"{slot.nodeId}  ·  좌표 ({slot.anchoredPosition.x:0}, {slot.anchoredPosition.y:0})" +
                        (UsesSlotAppearance(slot) ? "  ·  보드별 외형 덮어쓰기" : ""), EditorStyles.miniLabel);
                    var partners = new List<string>();
                    foreach (ClueBoardRelation relation in board.relations)
                    {
                        if (relation == null) continue;
                        if (relation.firstNodeId == slot.nodeId) partners.Add($"{DescribeNode(board, relation.secondNodeId)} [{relation.kind}]");
                        else if (relation.secondNodeId == slot.nodeId) partners.Add($"{DescribeNode(board, relation.firstNodeId)} [{relation.kind}]");
                    }
                    EditorGUILayout.LabelField("관계", partners.Count == 0 ? "(없음)" : string.Join(", ", partners), EditorStyles.wordWrappedMiniLabel);
                    bool silhouetteTarget = Array.Exists(board.silhouetteNeighbors,
                        item => item != null && item.silhouetteNodeId == slot.nodeId);
                    if (silhouetteTarget)
                    {
                        string authored = ClueBoardSilhouetteHint.FindAuthored(slot, clue);
                        EditorGUILayout.LabelField("실루엣", authored != null ? "힌트: " + authored : "⚠ 힌트 없음 — 기본 문구", EditorStyles.wordWrappedMiniLabel);
                    }
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndVertical();
            }
        }

        private void ReplaceClueIdInBoards(string previousClueId, string nextClueId)
        {
            if (string.IsNullOrEmpty(previousClueId) || previousClueId == nextClueId) return;
            foreach (ClueBoardDefinition board in _boardDb.boards)
                foreach (ClueBoardSlot slot in board.slots)
                    if (slot != null && slot.clueId == previousClueId)
                    {
                        slot.clueId = nextClueId ?? "";
                        _boardDirty = true;
                    }
        }

        private static void ReplaceNodeIdReferences(ClueBoardDefinition board, string previousNodeId, string nextNodeId)
        {
            if (string.IsNullOrEmpty(previousNodeId) || previousNodeId == nextNodeId) return;
            foreach (ClueBoardRelation relation in board.relations)
            {
                if (relation.firstNodeId == previousNodeId) relation.firstNodeId = nextNodeId;
                if (relation.secondNodeId == previousNodeId) relation.secondNodeId = nextNodeId;
            }
            foreach (ClueBoardSilhouetteNeighbor neighbor in board.silhouetteNeighbors)
            {
                if (neighbor.revealedByNodeId == previousNodeId) neighbor.revealedByNodeId = nextNodeId;
                if (neighbor.silhouetteNodeId == previousNodeId) neighbor.silhouetteNodeId = nextNodeId;
            }
            foreach (ClueBoardRelationChain chain in board.chains ?? Array.Empty<ClueBoardRelationChain>())
                if (chain?.revealSilhouetteNodeIds != null)
                    for (int i = 0; i < chain.revealSilhouetteNodeIds.Length; i++)
                        if (chain.revealSilhouetteNodeIds[i] == previousNodeId) chain.revealSilhouetteNodeIds[i] = nextNodeId;
        }

        private static string NextNodeId(ClueBoardDefinition board)
        {
            int number = board.slots.Length + 1;
            string candidate;
            do candidate = "node-" + number++;
            while (Array.Exists(board.slots, item => item != null && item.nodeId == candidate));
            return candidate;
        }

        private static string NextRelationId(ClueBoardDefinition board)
        {
            int number = board.relations.Length + 1;
            string candidate;
            do candidate = "rel-" + number++;
            while (Array.Exists(board.relations, item => item != null && item.relationId == candidate));
            return candidate;
        }

        // 슬롯의 보드별 실루엣 힌트. 외형 덮어쓰기 토글과 별개로, 값이 있으면 단서 공통 힌트보다 우선한다.
        private void DrawSlotSilhouetteHint(ClueBoardDefinition board, ClueBoardSlot slot, ClueData clue)
        {
            bool isSilhouetteTarget = board.silhouetteNeighbors != null && Array.Exists(board.silhouetteNeighbors,
                item => item != null && string.Equals(item.silhouetteNodeId, slot.nodeId, StringComparison.Ordinal));
            slot.silhouetteHint = TF("이 보드 실루엣 힌트 (비우면 단서 공통)", slot.silhouetteHint);
            if (!isSilhouetteTarget) return;
            string authored = ClueBoardSilhouetteHint.FindAuthored(slot, clue);
            EditorGUILayout.LabelField(authored != null
                    ? "실루엣 힌트 미리보기: " + authored
                    : "⚠ 실루엣으로 드러나는 노드인데 힌트가 없습니다 — 기본 문구(" + ClueBoardSilhouetteHint.DefaultHint + ")가 표시됩니다.",
                EditorStyles.wordWrappedMiniLabel);
        }

        private string DescribeSilhouetteHint(ClueBoardDefinition board, string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId) || board.slots == null) return null;
            ClueBoardSlot slot = Array.Find(board.slots,
                item => item != null && string.Equals(item.nodeId, nodeId, StringComparison.Ordinal));
            if (slot == null) return null;
            string authored = ClueBoardSilhouetteHint.FindAuthored(slot, FindClue(slot.clueId));
            return authored != null ? "힌트: " + authored : "⚠ 힌트 없음 — 기본 문구가 표시됩니다.";
        }

        private ClueData FindClue(string clueId)
        {
            if (_clueDb?.clues == null || string.IsNullOrWhiteSpace(clueId)) return null;
            return Array.Find(_clueDb.clues, item => item != null && string.Equals(item.id, clueId, StringComparison.Ordinal));
        }

        private static bool UsesSlotAppearance(ClueBoardSlot slot) =>
            slot.overrideAppearance || slot.sizePercent > 0f || slot.fontSize > 0f ||
            !string.IsNullOrWhiteSpace(slot.fontAddress) || slot.hideLabel || !string.IsNullOrWhiteSpace(slot.silhouetteHint);

        private static void ClearSlotAppearance(ClueBoardSlot slot)
        {
            slot.sizePercent = 0f;
            slot.fontSize = 0f;
            slot.fontAddress = "";
            slot.hideLabel = false;
        }

        private static void DrawClueClassification(ClueData clue)
        {
            int current = clue.classification == null ? 0 : (int)clue.classification.type;
            int selected = EditorGUILayout.Popup("유형", current,
                new[] { "미분류", "상징/글", "그림/사진", "인쇄물", "물체" });
            if (selected != current)
                clue.classification = selected == 0 ? null : new ClueClassification { type = (ClueType)selected };
            if (clue.classification == null) return;
            var values = new List<ClueSubtype> { (ClueSubtype)0 };
            var labels = new List<string> { "미분류" };
            foreach (ClueSubtype subtype in Enum.GetValues(typeof(ClueSubtype)))
                if (ClueTypeConfig.GetParent(subtype) == clue.classification.type)
                {
                    values.Add(subtype);
                    labels.Add(ClueTypeConfig.GetSubtypeName(subtype));
                }
            int index = values.IndexOf(clue.classification.subtype);
            int next = EditorGUILayout.Popup("세부 유형", Math.Max(0, index), labels.ToArray());
            if (next != index && next > 0) clue.classification.subtype = values[next];
            else if (next == 0 && index > 0) clue.classification.subtype = (ClueSubtype)0;
        }

        // ─── 단서 보드 편집 (C02) ─────────────────────────────────
        //
        // 슬롯은 **고정 좌표만** 편집한다. 그래프 위에서 끌어 옮기는 자유 배치는 만들지 않는다 —
        // 확정된 기획이 고정 슬롯이고, 자유 배치 UI가 생기면 좌표의 소유자가 데이터와 화면 둘로
        // 갈라져 C04 저장 전환 때 어느 쪽을 정답으로 볼지가 다시 문제가 된다.
        private void DrawBoardDetail(ClueBoardDefinition board)
        {
            EditorGUI.BeginChangeCheck();

            SectionHeader("보드");
            board.boardId = TF("보드 ID", board.boardId);
            board.kind = (ClueBoardKind)EditorGUILayout.EnumPopup("종류", board.kind);
            board.revealAllAsSilhouette = EditorGUILayout.ToggleLeft("미획득 슬롯을 전부 실루엣으로 표시 (잠김 없음 — 개발/튜토리얼 보드용)", board.revealAllAsSilhouette);

            // 편집 중에도 판정 계약과 같은 규칙으로 즉시 진단한다 — 저장 버튼을 눌러야만 알 수 있으면
            // 슬롯을 수십 개 넣은 뒤에야 첫 오류를 보게 된다.
            if (!ClueBoardConnectionEngine.TryValidateDefinition(board, out string definitionError))
                EditorGUILayout.HelpBox(definitionError, MessageType.Warning);

            DrawBoardMapUsage(board);
            DrawOutcomeDiagnostics(board);
            DrawStableIdToggle();

            EditorGUILayout.Space(6f);
            DrawBoardPreview(board);

            EditorGUILayout.Space(6f);
            _foldBoardSlots = EditorGUILayout.Foldout(_foldBoardSlots,
                $"슬롯  ({board.slots.Length}개)", true, EditorStyles.foldoutHeader);
            if (_foldBoardSlots)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox(
                    "슬롯 ID는 자동으로 붙는 보드 안 식별자입니다(관계·실루엣·체인·세이브가 이 ID로 슬롯을 가리키므로 고정됩니다).\n" +
                    "같은 단서를 글로벌과 로컬 보드에 함께 올릴 수 있지만, 한 보드 안에서는 단서가 중복될 수 없습니다.\n" +
                    "좌표는 고정 배치용입니다 — 플레이 중 옮기는 기능은 없습니다.", MessageType.None);
                int removeSlot = -1;
                for (int i = 0; i < board.slots.Length; i++)
                {
                    var slot = board.slots[i];
                    bool selected = string.Equals(slot.nodeId, _previewSelectedNodeId, StringComparison.Ordinal);
                    var slotBg = GUI.backgroundColor;
                    if (selected) GUI.backgroundColor = new Color(0.75f, 0.9f, 1f);
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    GUI.backgroundColor = slotBg;
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button(selected ? "●" : $"[{i}]", EditorStyles.miniLabel, GUILayout.Width(28f)))
                        _previewSelectedNodeId = slot.nodeId;
                    string previousNodeId = slot.nodeId;
                    slot.nodeId = StableIdField("", slot.nodeId);
                    if (!string.Equals(previousNodeId, slot.nodeId, StringComparison.Ordinal))
                        ReplaceNodeIdReferences(board, previousNodeId, slot.nodeId);
                    if (GUILayout.Button("−", GUILayout.Width(22f))) removeSlot = i;
                    EditorGUILayout.EndHorizontal();
                    slot.clueId = ClueIdPopup("단서", slot.clueId);
                    // 숫자 직접 입력. 미리보기 캔버스에서 끌어도 같은 값이 바뀐다.
                    slot.anchoredPosition = EditorGUILayout.Vector2Field("고정 좌표 (x 0~320, y -240~0)", slot.anchoredPosition);
                    ClueData slotClue = FindClue(slot.clueId);
                    bool previousOverride = slot.overrideAppearance;
                    slot.overrideAppearance = EditorGUILayout.Toggle("이 보드에서만 크기·외형 덮어쓰기", slot.overrideAppearance);
                    if (previousOverride && !slot.overrideAppearance) ClearSlotAppearance(slot);
                    if (!slot.overrideAppearance)
                        EditorGUILayout.LabelField("단서 공통 크기 사용",
                            $"{(slotClue != null && slotClue.boardSizePercent > 0f ? slotClue.boardSizePercent : 100f):0}%  (단서 + 보드 탭의 '단서 보드 공통 외형')",
                            EditorStyles.miniLabel);
                    if (slot.overrideAppearance)
                    {
                        // 보드별 예외값(slot.sizePercent)만 저장한다. 단서 공통값은 건드리지 않는다.
                        float displayedSize = slot.sizePercent > 0f ? slot.sizePercent
                            : (slotClue != null && slotClue.boardSizePercent > 0f ? slotClue.boardSizePercent : 100f);
                        slot.sizePercent = EditorGUILayout.Slider("카드 크기 (%)", displayedSize, 25f, 300f);
                        float displayedFontSize = slot.fontSize > 0f ? slot.fontSize
                            : (slotClue != null && slotClue.boardFontSize > 0f ? slotClue.boardFontSize : 6.5f);
                        slot.fontSize = EditorGUILayout.Slider("글자 크기", displayedFontSize, 4f, 24f);
                        slot.fontAddress = AddressableField<TMP_FontAsset>("폰트 (선택)", slot.fontAddress ?? "");
                        slot.hideLabel = EditorGUILayout.Toggle("제목 숨김 (사진 전용)", slot.hideLabel);
                    }
                    DrawSlotSilhouetteHint(board, slot, FindClue(slot.clueId));
                    EditorGUILayout.EndVertical();
                }
                if (removeSlot >= 0)
                {
                    // 슬롯을 지우면 그 노드를 가리키던 관계·실루엣이 곧바로 깨진 참조가 된다.
                    // 조용히 두면 저장 시점에야 알게 되므로, 같이 지우고 무엇을 지웠는지 남긴다.
                    string goneNodeId = board.slots[removeSlot].nodeId;
                    ArrayUtility.RemoveAt(ref board.slots, removeSlot);
                    RemoveBoardReferencesToNode(board, goneNodeId);
                }
                if (GUILayout.Button("+ 슬롯 추가", GUILayout.ExpandWidth(false)))
                    ArrayUtility.Add(ref board.slots, new ClueBoardSlot
                    {
                        nodeId = "node-" + (board.slots.Length + 1), clueId = "", anchoredPosition = Vector2.zero,
                        overrideAppearance = false,
                    });
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(4f);
            _foldBoardRelations = EditorGUILayout.Foldout(_foldBoardRelations,
                $"관계  ({board.relations.Length}개)", true, EditorStyles.foldoutHeader);
            if (_foldBoardRelations)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox(
                    "정의하지 않은 쌍은 자동으로 무관 처리되어 선이 생기지 않습니다. " +
                    "Unrelated 관계는 \"명시적으로 무관\"을 기록할 때만 쓰세요.\n" +
                    "Required는 항상 영구입니다. Foreshadowing만 영구 여부를 데이터로 정합니다.", MessageType.None);
                int removeRelation = -1;
                for (int i = 0; i < board.relations.Length; i++)
                {
                    var relation = board.relations[i];
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label($"[{i}]", GUILayout.Width(28f));
                    string previousRelationId = relation.relationId ?? "";
                    relation.relationId = StableIdField("", previousRelationId);
                    // 관계 ID를 바꾸면 초기 연결·체인의 참조도 따라간다(노드 ID를 바꿀 때와 같은 규칙).
                    if (!string.Equals(previousRelationId, relation.relationId, StringComparison.Ordinal))
                        ClueBoardOutcomeEditing.ReplaceRelationIdReferences(board, previousRelationId, relation.relationId);
                    if (GUILayout.Button("−", GUILayout.Width(22f))) removeRelation = i;
                    EditorGUILayout.EndHorizontal();
                    relation.firstNodeId = BoardNodePopup("단서 A", board, relation.firstNodeId);
                    relation.secondNodeId = BoardNodePopup("단서 B", board, relation.secondNodeId);
                    relation.kind = (ClueBoardRelationKind)EditorGUILayout.EnumPopup("판정", relation.kind);
                    using (new EditorGUI.DisabledScope(relation.kind != ClueBoardRelationKind.Foreshadowing))
                        relation.permanent = EditorGUILayout.Toggle("영구 연결", relation.permanent);
                    DrawRelationReading(relation);
                    EditorGUILayout.EndVertical();
                }
                if (removeRelation >= 0)
                {
                    string goneRelationId = board.relations[removeRelation].relationId;
                    ArrayUtility.RemoveAt(ref board.relations, removeRelation);
                    ReportRemovedRelation(board, goneRelationId);
                }
                if (GUILayout.Button("+ 관계 추가", GUILayout.ExpandWidth(false)))
                    ArrayUtility.Add(ref board.relations, new ClueBoardRelation
                    {
                        relationId = "rel-" + (board.relations.Length + 1),
                        firstNodeId = board.slots.Length > 0 ? board.slots[0].nodeId : "",
                        secondNodeId = board.slots.Length > 1 ? board.slots[1].nodeId : "",
                        kind = ClueBoardRelationKind.Required,
                    });
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(4f);
            _foldBoardSilhouettes = EditorGUILayout.Foldout(_foldBoardSilhouettes,
                $"실루엣 공개  ({board.silhouetteNeighbors.Length}개)", true, EditorStyles.foldoutHeader);
            if (_foldBoardSilhouettes)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox(
                    "\"공개하는 노드 → 실루엣으로 드러나는 노드\" 방향입니다. 반대 방향이 필요하면 따로 한 줄 더 넣으세요.\n" +
                    "실루엣은 연결 대상이 아니며 관계 데이터에서 역산하지 않습니다. 마우스를 올리면 해금 힌트가 보이므로 " +
                    "실루엣 노드의 단서(또는 슬롯)에 실루엣 힌트를 채우세요.", MessageType.None);
                int removeNeighbor = -1;
                for (int i = 0; i < board.silhouetteNeighbors.Length; i++)
                {
                    var neighbor = board.silhouetteNeighbors[i];
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label($"[{i}]", GUILayout.Width(28f));
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("−", GUILayout.Width(22f))) removeNeighbor = i;
                    EditorGUILayout.EndHorizontal();
                    neighbor.revealedByNodeId = BoardNodePopup("공개하는 단서", board, neighbor.revealedByNodeId);
                    neighbor.silhouetteNodeId = BoardNodePopup("실루엣이 되는 단서", board, neighbor.silhouetteNodeId);
                    // 힌트 누락은 데이터 경고다 — 기본 문구가 나가므로 저장은 막지 않되, 어느 자리가 비었는지 바로 보인다.
                    string hintState = DescribeSilhouetteHint(board, neighbor.silhouetteNodeId);
                    if (hintState != null) EditorGUILayout.LabelField(hintState, EditorStyles.miniLabel);
                    EditorGUILayout.EndVertical();
                }
                if (removeNeighbor >= 0) ArrayUtility.RemoveAt(ref board.silhouetteNeighbors, removeNeighbor);
                if (GUILayout.Button("+ 실루엣 추가", GUILayout.ExpandWidth(false)))
                    ArrayUtility.Add(ref board.silhouetteNeighbors, new ClueBoardSilhouetteNeighbor
                    {
                        revealedByNodeId = board.slots.Length > 0 ? board.slots[0].nodeId : "",
                        silhouetteNodeId = board.slots.Length > 1 ? board.slots[1].nodeId : "",
                    });
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(4f);
            _foldBoardInitial = EditorGUILayout.Foldout(_foldBoardInitial,
                $"초기 연결  ({board.initialRelationIds.Length}개)", true, EditorStyles.foldoutHeader);
            if (_foldBoardInitial)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox(
                    "노드 쌍이 아니라 관계 ID를 고릅니다. 초기 연결은 Foreshadowing이라도 영구입니다.", MessageType.None);
                int removeInitial = -1;
                for (int i = 0; i < board.initialRelationIds.Length; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label($"[{i}]", GUILayout.Width(28f));
                    board.initialRelationIds[i] = BoardRelationPopup(board, board.initialRelationIds[i]);
                    if (GUILayout.Button("−", GUILayout.Width(22f))) removeInitial = i;
                    EditorGUILayout.EndHorizontal();
                }
                if (removeInitial >= 0) ArrayUtility.RemoveAt(ref board.initialRelationIds, removeInitial);
                if (GUILayout.Button("+ 초기 연결 추가", GUILayout.ExpandWidth(false)))
                    ArrayUtility.Add(ref board.initialRelationIds,
                        board.relations.Length > 0 ? board.relations[0].relationId : "");
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(4f);
            _foldBoardChains = EditorGUILayout.Foldout(_foldBoardChains,
                $"멀티 체인 결과  ({board.chains.Length}개)", true, EditorStyles.foldoutHeader);
            if (_foldBoardChains)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox(
                    "체인은 고른 관계가 **모두** 성립했을 때만 한 번 열리는 해몽 결과입니다. 관계 2개 이상이어야 하며, " +
                    "관계 하나짜리 결과는 관계 항목의 '해몽 결과'에 넣으세요.\n" +
                    "여기서 나는 경고는 저장을 막지 않습니다 — 런타임은 잘못된 체인만 무시하고 진단을 남깁니다.", MessageType.None);
                int removeChain = -1;
                for (int i = 0; i < board.chains.Length; i++)
                    if (DrawChainRow(board, i, out bool removed) && removed) removeChain = i;
                if (removeChain >= 0) ArrayUtility.RemoveAt(ref board.chains, removeChain);
                if (GUILayout.Button("+ 체인 추가", GUILayout.ExpandWidth(false)))
                    ArrayUtility.Add(ref board.chains, NewChain(board, null));
                EditorGUI.indentLevel--;
            }

            if (EditorGUI.EndChangeCheck()) _boardDirty = true;
        }

        // ─── [C06] 관계 결과 배선 편집 ───────────────────────────
        // 데이터는 ClueBoardRelation.readingId / ClueBoardDefinition.chains(런타임 공개 구조)를 그대로 편집한다.
        // 규칙과 진단은 ClueBoardOutcomeEditing(GUI 없음, 회귀 검증 대상)에 있고 여기는 그리기만 한다.

        private List<string> ReadingIds => _readingIds ??= ClueBoardOutcomeEditing.LoadReadingIds();

        // 해몽 ID 입력: 드롭다운(목록에서 선택/해제)과 직접 입력 칸이 같은 값을 편집한다. 목록에 없는 값은
        // 조용히 바뀌지 않고 ⚠ 항목으로 남는다(ClueIdPopup과 같은 규칙).
        private string ReadingIdField(string label, string current)
        {
            current ??= "";
            List<string> known = ReadingIds;
            var ids = new List<string> { "" };
            var labels = new List<string> { "(결과 없음)" };
            foreach (string id in known) { ids.Add(id); labels.Add(id); }
            int index = ids.IndexOf(current);
            if (index < 0)
            {
                index = ids.Count;
                ids.Add(current);
                labels.Add($"⚠ 없는 해몽: {current}");
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(label);
            int next = EditorGUILayout.Popup(index, labels.ToArray(), GUILayout.MinWidth(120f));
            string value = ids[next];
            if (next != index) GUI.FocusControl(null); // 드롭다운으로 바꾼 값이 입력 칸의 이전 텍스트에 덮이지 않게 한다
            value = EditorGUILayout.TextField(value, GUILayout.MinWidth(90f));
            EditorGUILayout.EndHorizontal();
            if (known.Count == 0)
                EditorGUILayout.HelpBox("Resources/DreamReadings.asset을 읽지 못했거나 해몽이 없습니다. ID를 직접 입력할 수는 있습니다.", MessageType.Info);
            return value;
        }

        private void DrawRelationReading(ClueBoardRelation relation)
        {
            relation.readingId = ReadingIdField("해몽 결과", relation.readingId);
            string warning = ClueBoardOutcomeEditing.DiagnoseRelation(relation, ReadingIds);
            if (warning != null) EditorGUILayout.HelpBox(warning, MessageType.Warning);
            // 성공 반응은 반드시 있어야 한다: 해몽 결과 → 이 코멘트 → 기본 문구(기타 설정) 순.
            EditorGUILayout.LabelField(string.IsNullOrWhiteSpace(relation.readingId)
                ? "연결 코멘트 (해몽 결과가 없으므로 이 문구가 뜸 — 비우면 기타 설정의 기본 문구)"
                : "연결 코멘트 (해몽 결과가 있어 이 문구는 쓰이지 않음)");
            relation.comment = EditorGUILayout.TextArea(relation.comment ?? "", GUILayout.MinHeight(36f));
        }

        // 체인 한 줄. 삭제 요청은 removed로 돌려주고 배열 변경(추가/삭제)이 있었으면 true를 돌려준다 —
        // 호출부가 같은 프레임에 배열을 계속 그리지 않게 한다.
        private bool DrawChainRow(ClueBoardDefinition board, int index, out bool removed)
        {
            removed = false;
            ClueBoardRelationChain chain = board.chains[index];
            if (chain == null)
            {
                EditorGUILayout.HelpBox($"chains[{index}]가 null입니다. 삭제하세요.", MessageType.Warning);
                if (GUILayout.Button("− 삭제", GUILayout.Width(60f))) { removed = true; return true; }
                return false;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"[{index}]", GUILayout.Width(28f));
            chain.chainId = StableIdField("", chain.chainId);
            if (GUILayout.Button("−", GUILayout.Width(22f))) removed = true;
            EditorGUILayout.EndHorizontal();
            chain.readingId = ReadingIdField("해몽 결과", chain.readingId);

            int distinct = chain.relationIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().Count();
            GUILayout.Label($"관계  ({distinct}개 — 2개 이상 필요)", distinct >= 2 ? EditorStyles.boldLabel : EditorStyles.miniBoldLabel);
            int removeRelation = -1;
            for (int i = 0; i < chain.relationIds.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"[{i}]", GUILayout.Width(28f));
                chain.relationIds[i] = BoardRelationPopup(board, chain.relationIds[i]);
                if (GUILayout.Button("−", GUILayout.Width(22f))) removeRelation = i;
                EditorGUILayout.EndHorizontal();
            }
            bool changed = false;
            if (removeRelation >= 0) { ArrayUtility.RemoveAt(ref chain.relationIds, removeRelation); changed = true; }
            string candidate = ClueBoardOutcomeEditing.FirstRelationNotInChain(board, chain);
            using (new EditorGUI.DisabledScope(candidate == null))
                if (GUILayout.Button("+ 관계 추가", GUILayout.ExpandWidth(false)))
                {
                    ArrayUtility.Add(ref chain.relationIds, candidate);
                    changed = true;
                }
            if (candidate == null && removeRelation < 0)
                EditorGUILayout.LabelField("(추가할 수 있는 관계가 없습니다 — Unrelated가 아닌 관계가 전부 들어 있습니다)", EditorStyles.miniLabel);

            // 체인 완성 시 실루엣으로 열리는 노드(기획 "새 노드 그림자 실루엣이 열리는 조합"). 이미 해금된 노드나
            // 실루엣 이웃으로 이미 열리는 노드를 넣어도 해가 없으므로 막지 않는다.
            GUILayout.Label($"완성 시 실루엣 공개  ({chain.revealSilhouetteNodeIds.Length}개)", EditorStyles.boldLabel);
            int removeReveal = -1;
            for (int i = 0; i < chain.revealSilhouetteNodeIds.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"[{i}]", GUILayout.Width(28f));
                chain.revealSilhouetteNodeIds[i] = BoardNodePopup("", board, chain.revealSilhouetteNodeIds[i]);
                if (GUILayout.Button("−", GUILayout.Width(22f))) removeReveal = i;
                EditorGUILayout.EndHorizontal(); // 없는 노드는 팝업이 ⚠로 보존한다(런타임은 그 항목만 무시)
            }
            if (removeReveal >= 0) { ArrayUtility.RemoveAt(ref chain.revealSilhouetteNodeIds, removeReveal); changed = true; }
            if (GUILayout.Button("+ 공개 노드 추가", GUILayout.ExpandWidth(false)))
            {
                ArrayUtility.Add(ref chain.revealSilhouetteNodeIds, "");
                changed = true;
            }

            foreach (string warning in ClueBoardOutcomeEditing.DiagnoseChain(board, chain, index, ReadingIds))
                EditorGUILayout.HelpBox(warning, MessageType.Warning);
            EditorGUILayout.EndVertical();
            return removed || changed;
        }

        private static ClueBoardRelationChain NewChain(ClueBoardDefinition board, string firstRelationId)
        {
            var chain = new ClueBoardRelationChain
            {
                chainId = ClueBoardOutcomeEditing.NextChainId(board),
                readingId = "",
                relationIds = string.IsNullOrEmpty(firstRelationId) ? Array.Empty<string>() : new[] { firstRelationId },
            };
            string second = ClueBoardOutcomeEditing.FirstRelationNotInChain(board, chain);
            if (second != null) ArrayUtility.Add(ref chain.relationIds, second);
            return chain;
        }

        // 관계/체인/해몽 ID 문제를 한 상자에서 본다. 저장을 막지 않는 데이터 오류라 Warning으로만 낸다.
        private void DrawOutcomeDiagnostics(ClueBoardDefinition board)
        {
            List<string> messages = ClueBoardOutcomeEditing.Diagnose(board, ReadingIds);
            if (messages.Count == 0) return;
            EditorGUILayout.HelpBox("결과 배선 진단 (저장은 막지 않음 — 런타임이 해당 결과만 무시):\n" + string.Join("\n", messages), MessageType.Warning);
        }

        // 관계를 지웠을 때 체인/초기 연결 참조를 정리하고, 관계가 부족해진 체인을 알린다.
        private void ReportRemovedRelation(ClueBoardDefinition board, string relationId)
        {
            List<string> weakened = ClueBoardOutcomeEditing.RemoveRelationReferences(board, relationId);
            if (weakened.Count == 0) return;
            _boardWarning = AppendWarning(_boardWarning,
                $"관계 '{relationId}'를 지워 체인 {string.Join(", ", weakened.Select(id => $"'{id}'"))}의 관계가 2개 미만이 되었습니다. 체인을 고치거나 지우세요.");
        }

        // 편집기 옵션(EditorPrefs) — 콘텐츠가 아니라 이 컴퓨터의 편집기 취향이라 에셋/JSON에 넣지 않는다.
        // 저장/검증 경고를 종류별로 끌 수 있다. 코덱의 검증 자체는 그대로다(런타임 로그에는 남는다).
        private sealed class WarningFilter
        {
            public string prefKey, label, help;
            public Func<string, bool> matches;
            public bool Enabled
            {
                get => EditorPrefs.GetBool(prefKey, true);
                set => EditorPrefs.SetBool(prefKey, value);
            }
        }

        private static readonly WarningFilter[] WarningFilters =
        {
            new()
            {
                prefKey = "MapDatabaseEditor.ShowIsolatedSlotWarning",
                label = "관계가 한 건도 선언되지 않은 슬롯",
                help = "관계가 없는 슬롯은 플레이 중 어떤 상대와 이어도 '연관 없음'으로 거절됩니다.",
                matches = line => line.Contains("관계가 한 건도 선언되지 않은 슬롯"),
            },
            new()
            {
                prefKey = "MapDatabaseEditor.ShowMissingHintWarning",
                label = "실루엣 힌트가 없어 기본 문구가 표시됩니다",
                help = "힌트가 없는 실루엣은 기타 설정의 '실루엣 기본 힌트'(또는 코드 기본 문구)를 보여 줍니다.",
                matches = line => line.Contains("실루엣 힌트가 없어"),
            },
            new()
            {
                prefKey = "MapDatabaseEditor.ShowOutcomeIgnoredWarning",
                label = "결과 배선 무시 (readingId/chainId 없음, 빈·없는·Unrelated 관계, 중복 체인)",
                help = "그 관계/체인은 선 연결에는 영향이 없고 해몽 결과만 나오지 않습니다. 배선을 아직 안 채운 개발 보드에서는 흔합니다.",
                matches = line => line.Contains("무시합니다"),
            },
        };

        private const string WarningFooter = "(경고 종류별 표시 여부는 '기타 설정' 탭 > 편집기 옵션에서 바꿀 수 있습니다)";

        private static string FilterBoardWarnings(string warning)
        {
            if (string.IsNullOrEmpty(warning)) return warning;
            var kept = new List<string>();
            foreach (string line in warning.Split('\n'))
            {
                if (line == WarningFooter) continue;
                bool hidden = false;
                foreach (WarningFilter filter in WarningFilters)
                    if (!filter.Enabled && filter.matches(line)) { hidden = true; break; }
                if (!hidden) kept.Add(line);
            }
            if (kept.Count == 0) return null;
            kept.Add(WarningFooter);
            return string.Join("\n", kept);
        }

        private static string AppendWarning(string warning, string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return warning;
            return string.IsNullOrWhiteSpace(warning) ? message : warning + "\n" + message;
        }

        // 이 보드를 로컬 보드로 지목한 맵을 보여준다. 반대 방향(보드 → 맵) 참조 필드는 두지 않는다 —
        // 배선의 소유자는 MapNodeData.localBoardId 한 곳이며, 양쪽에 두면 서로 어긋날 수 있다.
        // ─── 보드 미리보기 캔버스 ────────────────────────────────

        // 실제 ClueBoardView와 같은 좌표계다: 슬롯 anchoredPosition은 좌상단 앵커·카드 중심 피벗이라 x∈[0,320], y∈[-240,0].
        // 카드 크기는 36×60 × sizePercent(슬롯 예외 → 단서 공통 → 100%). 색: 실루엣으로 드러나는 자리(실루엣 이웃·체인 공개)는
        // 어둡게, 나머지는 해금 색. 플레이어 진행이 없는 편집기이므로 "해금/실루엣"은 데이터상 가능한 상태를 뜻한다.
        private void DrawBoardPreview(ClueBoardDefinition board)
        {
            _showPreview = EditorGUILayout.Foldout(_showPreview, "미리보기 (카드를 끌어 좌표 변경)", true, EditorStyles.foldoutHeader);
            if (!_showPreview) return;

            float width = Mathf.Clamp(EditorGUIUtility.currentViewWidth - 60f, 240f, 640f);
            float scale = width / BoardCanvasWidth;
            Rect canvas = GUILayoutUtility.GetRect(width, BoardCanvasHeight * scale, GUILayout.ExpandWidth(false));
            EditorGUI.DrawRect(canvas, new Color(0.11f, 0.12f, 0.16f));
            // 우측 탭 열(58×34)이 앉는 자리 — 슬롯이 이 위에 놓이면 탭에 가린다.
            EditorGUI.DrawRect(new Rect(canvas.xMax - 58f * scale, canvas.yMax - 34f * scale, 58f * scale, 34f * scale),
                new Color(0.08f, 0.09f, 0.13f, 0.9f));

            var silhouetteTargets = new HashSet<string>(StringComparer.Ordinal);
            if (board.revealAllAsSilhouette)
                foreach (ClueBoardSlot slot in board.slots) if (slot != null && !string.IsNullOrEmpty(slot.nodeId)) silhouetteTargets.Add(slot.nodeId);
            foreach (ClueBoardSilhouetteNeighbor neighbor in board.silhouetteNeighbors)
                if (neighbor != null && !string.IsNullOrEmpty(neighbor.silhouetteNodeId)) silhouetteTargets.Add(neighbor.silhouetteNodeId);
            foreach (ClueBoardRelationChain chain in board.chains ?? Array.Empty<ClueBoardRelationChain>())
                if (chain?.revealSilhouetteNodeIds != null)
                    foreach (string id in chain.revealSilhouetteNodeIds) if (!string.IsNullOrEmpty(id)) silhouetteTargets.Add(id);
            var initial = new HashSet<string>(board.initialRelationIds.Where(id => !string.IsNullOrEmpty(id)), StringComparer.Ordinal);

            Rect CardRect(ClueBoardSlot slot)
            {
                float size = ResolveSlotSizePercent(slot) * 0.01f;
                float w = CardWidth * size * scale, h = CardHeight * size * scale;
                float cx = canvas.x + slot.anchoredPosition.x * scale;
                float cy = canvas.y - slot.anchoredPosition.y * scale;
                return new Rect(cx - w * 0.5f, cy - h * 0.5f, w, h);
            }
            Vector2 Center(ClueBoardSlot slot) => CardRect(slot).center;
            ClueBoardSlot SlotOf(string nodeId) => Array.Find(board.slots, item => item != null && item.nodeId == nodeId);

            // 관계 선(정의된 전부, 초기 연결은 진하게). 노드 뒤에 그린다.
            Handles.BeginGUI();
            foreach (ClueBoardRelation relation in board.relations)
            {
                if (relation == null) continue;
                ClueBoardSlot a = SlotOf(relation.firstNodeId), b = SlotOf(relation.secondNodeId);
                if (a == null || b == null) continue;
                bool isInitial = initial.Contains(relation.relationId);
                Handles.color = relation.kind == ClueBoardRelationKind.Unrelated ? new Color(0.6f, 0.3f, 0.3f, 0.5f)
                    : isInitial ? new Color(0.95f, 0.85f, 0.4f, 0.95f) : new Color(0.62f, 0.66f, 0.72f, 0.55f);
                Handles.DrawAAPolyLine(isInitial ? 3f : 1.5f, Center(a), Center(b));
            }
            // 실루엣 공개 방향(점선 느낌으로 얇게).
            Handles.color = new Color(0.4f, 0.7f, 0.9f, 0.5f);
            foreach (ClueBoardSilhouetteNeighbor neighbor in board.silhouetteNeighbors)
            {
                ClueBoardSlot a = neighbor == null ? null : SlotOf(neighbor.revealedByNodeId), b = neighbor == null ? null : SlotOf(neighbor.silhouetteNodeId);
                if (a != null && b != null) Handles.DrawDottedLine(Center(a), Center(b), 3f);
            }
            Handles.EndGUI();

            Event e = Event.current;
            var mini = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.UpperCenter, wordWrap = true, fontSize = 9 };
            mini.normal.textColor = Color.white;
            // 위에 그린(뒤 순서) 카드가 앞에 보이므로 입력은 뒤에서부터 검사한다.
            for (int i = 0; i < board.slots.Length; i++)
            {
                ClueBoardSlot slot = board.slots[i];
                if (slot == null || string.IsNullOrEmpty(slot.nodeId)) continue;
                Rect rect = CardRect(slot);
                bool selected = string.Equals(slot.nodeId, _previewSelectedNodeId, StringComparison.Ordinal);
                bool silhouette = silhouetteTargets.Contains(slot.nodeId);
                Color fill = silhouette ? new Color(0.12f, 0.13f, 0.16f, 0.95f) : new Color(0.20f, 0.24f, 0.32f, 0.95f);
                EditorGUI.DrawRect(rect, fill);
                // 기본 흰 테두리로 카드 경계를 보이고, 선택된 카드만 노란 테두리로 덮는다.
                DrawRectOutline(rect, new Color(1f, 1f, 1f, 0.85f), 1f);
                if (selected) DrawRectOutline(rect, new Color(0.95f, 0.85f, 0.40f), 2f);
                if (!canvas.Contains(rect.min) || !canvas.Contains(rect.max)) DrawRectOutline(rect, new Color(0.95f, 0.35f, 0.3f), 1f);

                ClueData clue = FindClue(slot.clueId);
                string name = clue != null && !string.IsNullOrWhiteSpace(clue.name) ? clue.name : (string.IsNullOrWhiteSpace(slot.clueId) ? "(단서 없음)" : "⚠ " + slot.clueId);
                GUI.Label(new Rect(rect.x - 10f, rect.y + 2f, rect.width + 20f, rect.height), name, mini);
                GUI.Label(new Rect(rect.x - 20f, rect.yMax - 12f, rect.width + 40f, 12f),
                    $"({slot.anchoredPosition.x:0},{slot.anchoredPosition.y:0}) {(silhouette ? "실루엣" : "해금")}", mini);
            }

            // 입력: 누른 카드를 선택하고 끌면 좌표가 바뀐다(정수로 반올림, 보드 안으로 제한).
            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            switch (e.GetTypeForControl(controlId))
            {
                case EventType.MouseDown when e.button == 0 && canvas.Contains(e.mousePosition):
                    for (int i = board.slots.Length - 1; i >= 0; i--)
                    {
                        ClueBoardSlot slot = board.slots[i];
                        if (slot == null || !CardRect(slot).Contains(e.mousePosition)) continue;
                        _previewSelectedNodeId = slot.nodeId;
                        _previewDragNodeId = slot.nodeId;
                        _previewDragOffset = Center(slot) - e.mousePosition;
                        GUIUtility.hotControl = controlId;
                        e.Use();
                        break;
                    }
                    break;
                case EventType.MouseDrag when GUIUtility.hotControl == controlId && _previewDragNodeId != null:
                {
                    ClueBoardSlot slot = SlotOf(_previewDragNodeId);
                    if (slot != null)
                    {
                        Vector2 center = e.mousePosition + _previewDragOffset;
                        float x = Mathf.Round(Mathf.Clamp((center.x - canvas.x) / scale, 0f, BoardCanvasWidth));
                        float y = Mathf.Round(Mathf.Clamp(-(center.y - canvas.y) / scale, -BoardCanvasHeight, 0f));
                        if (slot.anchoredPosition.x != x || slot.anchoredPosition.y != y)
                        {
                            slot.anchoredPosition = new Vector2(x, y);
                            _boardDirty = true;
                            GUI.changed = true;
                        }
                    }
                    e.Use();
                    Repaint();
                    break;
                }
                case EventType.MouseUp when GUIUtility.hotControl == controlId:
                    _previewDragNodeId = null;
                    GUIUtility.hotControl = 0;
                    e.Use();
                    break;
            }

            EditorGUILayout.LabelField(
                "굵은 노란 선 = 초기 연결, 회색 = 관계, 붉은 = Unrelated, 점선 = 실루엣 공개 방향. 붉은 테두리 = 보드 밖으로 나간 카드.",
                EditorStyles.wordWrappedMiniLabel);
        }

        private float ResolveSlotSizePercent(ClueBoardSlot slot)
        {
            if (UsesSlotAppearance(slot) && slot.sizePercent > 0f) return slot.sizePercent;
            ClueData clue = FindClue(slot.clueId);
            return clue != null && clue.boardSizePercent > 0f ? clue.boardSizePercent : 100f;
        }

        private static void DrawRectOutline(Rect rect, Color color, float thickness)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        // 배선의 소유자는 여전히 MapNodeData.localBoardId 한 곳이다(보드 → 맵 역참조 필드는 두지 않는다). 여기서
        // 편집하면 그 맵 데이터를 고치는 것이므로 맵 DB가 미저장 상태가 되고, 보드 탭 저장이 맵 DB도 함께 쓴다.
        private void DrawBoardMapUsage(ClueBoardDefinition board)
        {
            if (board.kind != ClueBoardKind.Local || string.IsNullOrWhiteSpace(board.boardId)) return;
            if (_db?.maps == null) return;

            GUILayout.Label("이 보드를 쓰는 맵", EditorStyles.boldLabel);
            MapNodeData removeFrom = null;
            int users = 0;
            foreach (MapNodeData map in _db.maps)
            {
                if (map == null || !string.Equals(map.localBoardId, board.boardId, StringComparison.Ordinal)) continue;
                users++;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(string.IsNullOrEmpty(map.nodeName) ? map.guid : $"{map.nodeName}  ({map.guid})");
                if (GUILayout.Button("−", GUILayout.Width(22f))) removeFrom = map;
                EditorGUILayout.EndHorizontal();
            }
            if (removeFrom != null)
            {
                removeFrom.localBoardId = "";
                _dirty = true; // 맵 DB 변경
            }

            // 다른 보드를 쓰는 맵도 고를 수 있다(교체). 로컬 보드가 없는 맵을 먼저 보이고 현재 배선을 함께 적는다.
            var candidates = new List<MapNodeData>();
            foreach (MapNodeData map in _db.maps)
                if (map != null && !string.Equals(map.localBoardId, board.boardId, StringComparison.Ordinal)) candidates.Add(map);
            candidates.Sort((a, b) =>
            {
                bool aFree = string.IsNullOrWhiteSpace(a.localBoardId), bFree = string.IsNullOrWhiteSpace(b.localBoardId);
                return aFree != bFree ? (aFree ? -1 : 1) : string.CompareOrdinal(a.nodeName, b.nodeName);
            });
            var labels = new List<string> { "+ 맵 추가…" };
            foreach (MapNodeData map in candidates)
            {
                string name = string.IsNullOrEmpty(map.nodeName) ? map.guid : map.nodeName;
                labels.Add(string.IsNullOrWhiteSpace(map.localBoardId) ? name : $"{name}  (현재: {map.localBoardId} → 교체)");
            }
            int picked = EditorGUILayout.Popup(0, labels.ToArray(), GUILayout.Width(260f));
            if (picked > 0)
            {
                candidates[picked - 1].localBoardId = board.boardId;
                _dirty = true;
            }

            if (users == 0)
                EditorGUILayout.HelpBox(
                    "이 로컬 보드를 참조하는 맵이 없습니다. 위에서 맵을 추가하거나 맵 노드 탭의 '로컬 보드 ID'에 지정하세요 — " +
                    "단서 출처나 주변 맵으로 자동 연결되지 않습니다.", MessageType.Info);
            if (_dirty)
                EditorGUILayout.HelpBox("맵 배선이 바뀌었습니다. 이 탭의 저장이 보드와 함께 map_database.json도 저장합니다.", MessageType.None);
        }

        private static void RemoveBoardReferencesToNode(ClueBoardDefinition board, string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId)) return;
            for (int i = board.relations.Length - 1; i >= 0; i--)
            {
                var relation = board.relations[i];
                if (relation.firstNodeId != nodeId && relation.secondNodeId != nodeId) continue;
                ArrayUtility.Remove(ref board.initialRelationIds, relation.relationId);
                ArrayUtility.RemoveAt(ref board.relations, i);
            }
            for (int i = board.silhouetteNeighbors.Length - 1; i >= 0; i--)
            {
                var neighbor = board.silhouetteNeighbors[i];
                if (neighbor.revealedByNodeId == nodeId || neighbor.silhouetteNodeId == nodeId)
                    ArrayUtility.RemoveAt(ref board.silhouetteNeighbors, i);
            }
            foreach (ClueBoardRelationChain chain in board.chains ?? Array.Empty<ClueBoardRelationChain>())
                if (chain?.revealSilhouetteNodeIds != null)
                    ArrayUtility.Remove(ref chain.revealSilhouetteNodeIds, nodeId);
        }

        // 슬롯의 단서는 인터넷 탭이 쓰는 ClueIdPopup을 그대로 재사용한다 — 없는 ID를 조용히
        // "(없음)"으로 바꾸지 않고 ⚠로 남기는 규칙이 보드 슬롯에도 그대로 필요하다.

        // 슬롯의 표시 이름 — 편집자는 노드 ID가 아니라 "어느 단서인지"로 고른다. ID는 괄호에 남겨 중복 단서를 구분한다.
        private string DescribeNode(ClueBoardDefinition board, string nodeId)
        {
            ClueBoardSlot slot = board?.slots == null ? null
                : Array.Find(board.slots, item => item != null && string.Equals(item.nodeId, nodeId, StringComparison.Ordinal));
            if (slot == null) return nodeId ?? "";
            ClueData clue = FindClue(slot.clueId);
            string name = clue != null && !string.IsNullOrWhiteSpace(clue.name) ? clue.name
                : string.IsNullOrWhiteSpace(slot.clueId) ? "(단서 없음)" : "⚠ " + slot.clueId;
            return $"{name}  ({slot.nodeId})";
        }

        // ID 필드: 기본은 읽기 전용 표시, 고급 토글을 켰을 때만 직접 편집.
        private string StableIdField(string label, string value)
        {
            if (_editStableIds) return string.IsNullOrEmpty(label) ? EditorGUILayout.TextField(value ?? "") : TF(label, value);
            using (new EditorGUI.DisabledScope(true))
                return string.IsNullOrEmpty(label) ? EditorGUILayout.TextField(value ?? "") : TF(label, value);
        }

        private void DrawStableIdToggle()
        {
            _editStableIds = EditorGUILayout.ToggleLeft("고급: 슬롯·관계·체인 ID 직접 편집", _editStableIds);
            if (_editStableIds)
                EditorGUILayout.HelpBox("ID는 관계·실루엣·체인과 세이브(진행/발행 기록)가 참조하는 키입니다. 바꾸면 보드 안 참조는 따라가지만 " +
                                        "기존 세이브의 기록은 그 ID를 잃습니다. 보통은 자동 생성된 값을 그대로 두세요.", MessageType.Warning);
        }

        private string BoardNodePopup(string label, ClueBoardDefinition board, string nodeId)
        {
            var ids = new List<string> { "" };
            var labels = new List<string> { "(선택 안 함)" };
            foreach (var slot in board.slots)
            {
                if (slot == null || string.IsNullOrWhiteSpace(slot.nodeId)) continue;
                ids.Add(slot.nodeId);
                labels.Add(DescribeNode(board, slot.nodeId));
            }

            int index = ids.IndexOf(nodeId ?? "");
            if (index < 0)
            {
                index = ids.Count;
                ids.Add(nodeId);
                labels.Add($"⚠ 없는 노드: {nodeId}");
            }
            int next = EditorGUILayout.Popup(label, index, labels.ToArray());
            return ids[next];
        }

        private string BoardRelationPopup(ClueBoardDefinition board, string relationId)
        {
            var ids = new List<string> { "" };
            var labels = new List<string> { "(선택 안 함)" };
            foreach (var relation in board.relations)
            {
                if (relation == null || string.IsNullOrWhiteSpace(relation.relationId)) continue;
                ids.Add(relation.relationId);
                labels.Add($"{DescribeNode(board, relation.firstNodeId)} – {DescribeNode(board, relation.secondNodeId)}  [{relation.kind}]  {relation.relationId}");
            }

            int index = ids.IndexOf(relationId ?? "");
            if (index < 0)
            {
                index = ids.Count;
                ids.Add(relationId);
                labels.Add($"⚠ 없는 관계: {relationId}");
            }
            int next = EditorGUILayout.Popup(index, labels.ToArray());
            return ids[next];
        }

        // 단서 카드와 인터넷 게시글이 같은 편집 UI를 쓴다(둘 다 ClueAttachment[] / CodexComment[]).
        // 본문 매체 블록 편집 — 첨부물 목록과 같은 구조지만 종류별로 채우는 필드가 다르다.
        // 순서가 곧 본문 순서라 위/아래 이동 버튼을 둔다(첨부물은 순서가 의미를 갖지 않아 없다).
        private void DrawMediaList(ref ClueMediaBlock[] arr, ref bool fold, string title, string help)
        {
            arr ??= Array.Empty<ClueMediaBlock>();
            fold = EditorGUILayout.Foldout(fold, $"{title}  ({arr.Length}개)", true, EditorStyles.foldoutHeader);
            if (!fold) return;

            EditorGUI.indentLevel++;
            if (!string.IsNullOrEmpty(help)) EditorGUILayout.HelpBox(help, MessageType.None);

            int removeAt = -1, moveFrom = -1, moveTo = -1;
            for (int i = 0; i < arr.Length; i++)
            {
                var block = arr[i];
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"[{i}]", GUILayout.Width(28f));
                block.kind = (ClueMediaKind)EditorGUILayout.EnumPopup(block.kind, GUILayout.Width(90f));
                GUILayout.Label(ClueMediaConfig.GetDisplayName(block.kind), EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(i == 0))
                    if (GUILayout.Button("▲", GUILayout.Width(22f))) { moveFrom = i; moveTo = i - 1; }
                using (new EditorGUI.DisabledScope(i == arr.Length - 1))
                    if (GUILayout.Button("▼", GUILayout.Width(22f))) { moveFrom = i; moveTo = i + 1; }
                if (GUILayout.Button("−", GUILayout.Width(22f))) removeAt = i;
                EditorGUILayout.EndHorizontal();

                switch (block.kind)
                {
                    case ClueMediaKind.Text:
                        block.text = TA("본문", block.text);
                        break;
                    case ClueMediaKind.Image:
                        block.address = AddressableField<Sprite>("이미지", block.address);
                        break;
                    case ClueMediaKind.Video:
                        block.address = AddressableField<UnityEngine.Video.VideoClip>("영상", block.address);
                        break;
                    case ClueMediaKind.Audio:
                        block.address = AddressableField<AudioClip>("오디오", block.address);
                        break;
                }

                // 캡션은 매체 위에 붙는 머리줄에 표시된다. 글 블록에는 쓰지 않아 아예 감춘다.
                if (block.kind != ClueMediaKind.Text)
                    block.caption = TF("캡션 (선택)", block.caption);

                EditorGUILayout.EndVertical();
            }
            if (removeAt >= 0) ArrayUtility.RemoveAt(ref arr, removeAt);
            else if (moveFrom >= 0)
            {
                var moved = arr[moveFrom];
                arr[moveFrom] = arr[moveTo];
                arr[moveTo] = moved;
            }
            if (GUILayout.Button("+ 본문 매체 추가", GUILayout.ExpandWidth(false)))
                ArrayUtility.Add(ref arr,
                    new ClueMediaBlock { kind = ClueMediaKind.Text, text = "", address = "", caption = "" });
            EditorGUI.indentLevel--;
        }

        private void DrawAttachmentList(ref ClueAttachment[] arr, ref bool fold, string title, string help)
        {
            fold = EditorGUILayout.Foldout(fold, $"{title}  ({arr.Length}개)", true, EditorStyles.foldoutHeader);
            if (!fold) return;

            EditorGUI.indentLevel++;
            if (!string.IsNullOrEmpty(help)) EditorGUILayout.HelpBox(help, MessageType.None);

            int removeAt = -1;
            for (int i = 0; i < arr.Length; i++)
            {
                var at = arr[i];
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"[{i}]", GUILayout.Width(28f));
                at.kind = (ClueAttachmentKind)EditorGUILayout.EnumPopup(at.kind, GUILayout.Width(90f));
                GUILayout.Label(ClueAttachmentConfig.GetDisplayName(at.kind), EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("−", GUILayout.Width(22f))) removeAt = i;
                EditorGUILayout.EndHorizontal();

                at.label = TF("표시 이름 (비우면 자동)", at.label);

                switch (at.kind)
                {
                    case ClueAttachmentKind.Image:
                        at.address = AddressableField<Sprite>("이미지", at.address);
                        break;
                    case ClueAttachmentKind.Audio:
                        at.address = AddressableField<AudioClip>("오디오", at.address);
                        break;
                    case ClueAttachmentKind.MapRef:
                        at.mapGuid = NodeGuidPopup("맵", at.mapGuid, allowEmpty: true);
                        break;
                }

                EditorGUILayout.EndVertical();
            }
            if (removeAt >= 0) ArrayUtility.RemoveAt(ref arr, removeAt);
            if (GUILayout.Button("+ 첨부물 추가", GUILayout.ExpandWidth(false)))
                ArrayUtility.Add(ref arr,
                    new ClueAttachment { kind = ClueAttachmentKind.Image, label = "", address = "", mapGuid = "" });
            EditorGUI.indentLevel--;
        }

        private void DrawCommentList(ref CodexComment[] arr, ref bool fold, string title, string help)
        {
            fold = EditorGUILayout.Foldout(fold, $"{title}  ({arr.Length}개)", true, EditorStyles.foldoutHeader);
            if (!fold) return;

            EditorGUI.indentLevel++;
            if (!string.IsNullOrEmpty(help)) EditorGUILayout.HelpBox(help, MessageType.None);

            int removeAt = -1;
            for (int i = 0; i < arr.Length; i++)
            {
                var cm = arr[i];
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"[{i}]", GUILayout.Width(28f));
                cm.author = EditorGUILayout.TextField("작성자", cm.author ?? "");
                if (GUILayout.Button("−", GUILayout.Width(22f))) removeAt = i;
                EditorGUILayout.EndHorizontal();
                cm.createdAt = EditorGUILayout.TextField("시간 (선택, 비우면 숨김)", cm.createdAt ?? "");
                EditorGUILayout.LabelField("내용");
                cm.text = EditorGUILayout.TextArea(cm.text ?? "", GUILayout.MinHeight(36f));
                EditorGUILayout.EndVertical();
            }
            if (removeAt >= 0) ArrayUtility.RemoveAt(ref arr, removeAt);
            if (GUILayout.Button("+ 코멘트 추가", GUILayout.ExpandWidth(false)))
                ArrayUtility.Add(ref arr, new CodexComment { author = "", text = "", createdAt = "" });
            EditorGUI.indentLevel--;
        }

        // ─── 인터넷 편집 ──────────────────────────────────────────
        // 사이트 하나를 고르면 그 안의 게시글까지 이 화면에서 전부 편집한다(사이트 → 게시글 2단 구조라
        // 목록 패널을 2단으로 만드는 대신 상세 패널 안에서 게시글을 접었다 펴는 방식으로 처리).

        private void DrawSiteDetail(InternetSite site)
        {
            SectionHeader("사이트 편집");
            EditorGUI.BeginChangeCheck();

            site.id   = TF("ID", site.id);
            site.name = TF("이름", site.name);
            site.iconAddress = AddressableField<Sprite>("아이콘 (선택)", site.iconAddress);

            EditorGUILayout.Space(4f);
            DrawUnlock(site.unlock, ref _foldSiteUnlock, "사이트 잠금 조건",
                "전부 비우면 처음부터 보입니다. 조건이 있으면 전부 만족해야 목록에 나오고, 그 전에는 '??? (잠김)'으로만 표시됩니다.");

            EditorGUILayout.Space(8f);
            SectionHeader($"게시글  ({site.posts.Length}개)");

            int removePost = -1;
            for (int i = 0; i < site.posts.Length; i++)
            {
                var post = site.posts[i];
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.BeginHorizontal();
                bool expanded = _expandedPostIds.Contains(post.id);
                if (GUILayout.Button(expanded ? "▾" : "▸", EditorStyles.miniButton, GUILayout.Width(22f)))
                {
                    if (!_expandedPostIds.Remove(post.id)) _expandedPostIds.Add(post.id);
                }
                GUILayout.Label($"[{i}] {post.title}", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("−", GUILayout.Width(22f))) removePost = i;
                EditorGUILayout.EndHorizontal();

                if (expanded) DrawPostBody(post);

                EditorGUILayout.EndVertical();
            }
            if (removePost >= 0) ArrayUtility.RemoveAt(ref site.posts, removePost);

            if (GUILayout.Button("+ 게시글 추가", GUILayout.ExpandWidth(false)))
            {
                var post = new InternetPost
                {
                    id           = "post-" + NewGuid(),
                    title        = "새 게시글",
                    author       = "익명",
                    postedAt     = "",
                    body         = "",
                    grantClueIds = Array.Empty<string>(),
                    unlock       = new InternetUnlockCondition
                    {
                        requiredClueIds   = Array.Empty<string>(),
                        requiredEventKeys = Array.Empty<string>(),
                    },
                    attachments  = Array.Empty<ClueAttachment>(),
                    comments     = Array.Empty<CodexComment>(),
                };
                ArrayUtility.Add(ref site.posts, post);
                _expandedPostIds.Add(post.id);
            }

            if (EditorGUI.EndChangeCheck()) MarkDirty();
        }

        private void DrawPostBody(InternetPost post)
        {
            EditorGUI.indentLevel++;

            post.id       = TF("ID (세이브의 읽음 표시 키)", post.id);
            post.title    = TF("제목", post.title);
            post.author   = TF("작성자", post.author);
            post.postedAt = TF("작성 시각 (표시용 텍스트)", post.postedAt);
            post.body     = TA("본문", post.body);

            EditorGUILayout.Space(4f);
            _foldPostGrants = EditorGUILayout.Foldout(_foldPostGrants,
                $"열람 시 획득할 단서  ({post.grantClueIds.Length}개)", true, EditorStyles.foldoutHeader);
            if (_foldPostGrants)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox(
                    "게시글을 열면 여기 적힌 단서가 즉시 획득됩니다(도감 등록·지도 공개까지 자동).\n" +
                    "인터넷 전용 단서는 어느 맵의 '획득 가능 단서 ID'에도 넣지 마세요 — 넣으면 도감에 '??? (미발견)' 빈칸이 생깁니다.",
                    MessageType.None);

                int removeGrant = -1;
                for (int i = 0; i < post.grantClueIds.Length; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    post.grantClueIds[i] = ClueIdPopup($"[{i}]", post.grantClueIds[i]);
                    if (GUILayout.Button("−", GUILayout.Width(22f))) removeGrant = i;
                    EditorGUILayout.EndHorizontal();
                }
                if (removeGrant >= 0) ArrayUtility.RemoveAt(ref post.grantClueIds, removeGrant);
                if (GUILayout.Button("+ 단서 추가", GUILayout.ExpandWidth(false)))
                    ArrayUtility.Add(ref post.grantClueIds, "");
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(4f);
            DrawUnlock(post.unlock, ref _foldPostUnlock, "게시글 잠금 조건",
                "전부 비우면 사이트가 열려 있는 한 항상 보입니다. 잠긴 게시글은 목록에 아예 나오지 않습니다(제목 자체가 스포일러가 될 수 있어서).");

            EditorGUILayout.Space(4f);
            DrawAttachmentList(ref post.attachments, ref _foldPostAttachments, "게시글 장식용 첨부",
                "단서가 가진 첨부(사진/소리/맵)는 여기 넣지 않습니다 — 단서 탭에서 그 단서에 붙이면 게시글 본문에도 같이 나옵니다.\n" +
                "여기에는 단서와 무관한 분위기용 첨부만 넣으세요.");

            EditorGUILayout.Space(4f);
            DrawCommentList(ref post.comments, ref _foldPostComments, "댓글",
                "게시글에 달린 댓글 — 도감 코멘트와 같은 데이터 형식을 씁니다.");

            EditorGUI.indentLevel--;
        }

        private void DrawUnlock(InternetUnlockCondition u, ref bool fold, string title, string help)
        {
            if (u == null) return;

            string summary = u.IsEmpty ? "조건 없음" :
                $"단서 {u.requiredClueIds.Length} · 이벤트 {u.requiredEventKeys.Length} · 시간 {u.minGameTime:0}s";
            fold = EditorGUILayout.Foldout(fold, $"{title}  ({summary})", true, EditorStyles.foldoutHeader);
            if (!fold) return;

            EditorGUI.indentLevel++;
            if (!string.IsNullOrEmpty(help)) EditorGUILayout.HelpBox(help, MessageType.None);

            EditorGUILayout.LabelField("필요 단서 (전부 획득해야 열림)");
            int removeClue = -1;
            for (int i = 0; i < u.requiredClueIds.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();
                u.requiredClueIds[i] = ClueIdPopup($"[{i}]", u.requiredClueIds[i]);
                if (GUILayout.Button("−", GUILayout.Width(22f))) removeClue = i;
                EditorGUILayout.EndHorizontal();
            }
            if (removeClue >= 0) ArrayUtility.RemoveAt(ref u.requiredClueIds, removeClue);
            if (GUILayout.Button("+ 필요 단서 추가", GUILayout.ExpandWidth(false)))
                ArrayUtility.Add(ref u.requiredClueIds, "");

            EditorGUILayout.Space(2f);
            EditorGUILayout.LabelField("필요 이벤트 (맵 + 이벤트 키)");
            int removeEvent = -1;
            for (int i = 0; i < u.requiredEventKeys.Length; i++)
            {
                // 저장 형식은 "mapGuid:eventKey" 한 문자열이지만, 손으로 치면 콜론을 빠뜨리기 쉬워
                // 맵은 드롭다운, 키는 텍스트로 나눠 받고 여기서 합친다.
                string raw = u.requiredEventKeys[i] ?? "";
                int sep = raw.IndexOf(':');
                string mapGuid = sep > 0 ? raw.Substring(0, sep) : "";
                string eventKey = sep >= 0 ? raw.Substring(sep + 1) : raw;

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"[{i}]", GUILayout.Width(28f));
                mapGuid = NodeGuidPopup("", mapGuid, allowEmpty: true);
                eventKey = EditorGUILayout.TextField(eventKey);
                if (GUILayout.Button("−", GUILayout.Width(22f))) removeEvent = i;
                EditorGUILayout.EndHorizontal();

                u.requiredEventKeys[i] = string.IsNullOrEmpty(mapGuid) && string.IsNullOrEmpty(eventKey)
                    ? "" : mapGuid + ":" + eventKey;
            }
            if (removeEvent >= 0) ArrayUtility.RemoveAt(ref u.requiredEventKeys, removeEvent);
            if (GUILayout.Button("+ 필요 이벤트 추가", GUILayout.ExpandWidth(false)))
                ArrayUtility.Add(ref u.requiredEventKeys, "");

            EditorGUILayout.Space(2f);
            u.minGameTime = EditorGUILayout.FloatField("최소 게임 시간 (초, 0이면 조건 없음)", u.minGameTime);

            EditorGUI.indentLevel--;
        }

        // 단서 이름 드롭다운 → 선택된 단서 ID 반환. 손으로 ID를 적다 틀리면 게시글이 아무것도
        // 주지 않는 채로 조용히 넘어가므로(런타임 경고만 뜬다) 목록에서 고르게 한다.
        private string ClueIdPopup(string label, string curId)
        {
            var clues = _clueDb.clues;
            var opts = new string[clues.Length + 1];
            opts[0] = "(없음)";
            int cur = 0;
            for (int i = 0; i < clues.Length; i++)
            {
                opts[i + 1] = $"{clues[i].name}  [{Sg(clues[i].id)}]";
                if (clues[i].id == curId) cur = i + 1;
            }

            // 목록에 없는 ID(오타·삭제된 단서)는 조용히 "(없음)"으로 바뀌면 안 된다 — 그대로 보여준다.
            if (cur == 0 && !string.IsNullOrEmpty(curId))
            {
                ArrayUtility.Add(ref opts, $"⚠ 없는 단서: {curId}");
                cur = opts.Length - 1;
            }

            int sel = string.IsNullOrEmpty(label)
                ? EditorGUILayout.Popup(cur, opts)
                : EditorGUILayout.Popup(label, cur, opts);
            if (sel == 0) return "";
            return sel - 1 < clues.Length ? clues[sel - 1].id : curId;
        }

        // ─── 기타 설정 탭 ────────────────────────────────────────

        // 단서 시스템의 전역 에셋(유형 아이콘·문구·해몽 카탈로그)을 한 화면에서 다룬다. 전부 Resources 폴더의
        // ScriptableObject라 JSON 저장 경로를 타지 않고, 값을 바꾸면 Undo 기록과 함께 즉시 에셋에 반영된다.
        private static readonly string ResourcesDir = "Assets/Scripts/RouteFinding/Resources";

        private void DrawSettingsTab()
        {
            SectionHeader("단서 유형 기본 아이콘");
            EditorGUILayout.HelpBox(
                "단서에 '대표 아이콘 주소'가 없을 때 획득 토스트·보드 노드가 쓰는 유형별 아이콘입니다. " +
                "단서별 아이콘이 있으면 언제나 그것이 우선이고, 미분류 단서는 유형 아이콘도 없습니다.", MessageType.None);
            var icons = LoadOrOfferCreate<ClueTypeIconSet>(ClueTypeIconSet.ResourcePath, "유형 아이콘 에셋 만들기");
            if (icons != null)
            {
                var so = new SerializedObject(icons);
                so.Update();
                DrawIconField(so, "symbol", "상징/글 (Symbol)");
                DrawIconField(so, "picture", "그림/사진 (Picture)");
                DrawIconField(so, "prints", "인쇄물 (Prints)");
                DrawIconField(so, "physical", "물체 (Object)");
                if (so.ApplyModifiedProperties()) ClueTypeIconSet.ClearCache();
                int missing = ClueTypeConfig.AllTypes.Count(t => icons.Get(t) == null);
                if (missing > 0)
                    EditorGUILayout.HelpBox($"비어 있는 유형 {missing}개 — 그 유형의 단서는 아이콘 없이 이름만 표시됩니다.", MessageType.Warning);
                DrawAssetRow(icons);
            }

            EditorGUILayout.Space(10f);
            SectionHeader("문구 설정");
            EditorGUILayout.HelpBox(
                "비워 두면 코드 기본값을 씁니다. 거절 코멘트의 [A]/[B]는 시도 순서대로 단서 이름으로 바뀌고, " +
                "(와/과)·(은/는)·(이/가)·(을/를)은 앞 글자 받침에 맞춰 골라집니다.", MessageType.None);
            var settings = LoadOrOfferCreate<ClueSystemSettings>(ClueSystemSettings.ResourcePath, "문구 설정 에셋 만들기");
            if (settings != null)
            {
                var so = new SerializedObject(settings);
                so.Update();
                EditorGUILayout.PropertyField(so.FindProperty("rejectionCommentTitle"), new GUIContent("거절 코멘트 제목"));
                EditorGUILayout.PropertyField(so.FindProperty("rejectionCommentTemplate"), new GUIContent("거절 코멘트 템플릿"));
                EditorGUILayout.LabelField("미리보기: " + ClueBoardRejectionComment.Format(
                        settings.rejectionCommentTemplate, "찢어진 사진", "낡은 인형"), EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.Space(4f);
                EditorGUILayout.PropertyField(so.FindProperty("connectionCommentTemplate"), new GUIContent("연결 성공 기본 코멘트"));
                EditorGUILayout.LabelField("미리보기: " + ClueBoardRejectionComment.Format(
                        settings.connectionCommentTemplate, "찢어진 사진", "낡은 인형"), EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.Space(4f);
                EditorGUILayout.PropertyField(so.FindProperty("defaultSilhouetteHint"), new GUIContent("실루엣 기본 힌트"));
                if (so.ApplyModifiedProperties()) ClueSystemSettings.ClearCache();
                if (GUILayout.Button("코드 기본값으로 되돌리기", GUILayout.ExpandWidth(false)))
                {
                    Undo.RecordObject(settings, "문구 기본값 복원");
                    settings.rejectionCommentTemplate = ClueBoardRejectionComment.DefaultTemplate;
                    settings.rejectionCommentTitle = ClueBoardRejectionComment.DefaultTitle;
                    settings.connectionCommentTemplate = ClueBoardConnectionComment.DefaultTemplate;
                    settings.defaultSilhouetteHint = ClueBoardSilhouetteHint.DefaultHint;
                    EditorUtility.SetDirty(settings);
                    ClueSystemSettings.ClearCache();
                }
                DrawAssetRow(settings);
            }

            EditorGUILayout.Space(10f);
            SectionHeader("해몽 카탈로그");
            var readings = Resources.Load<DreamReadingCatalogSO>(ClueBoardOutcomeEditing.CatalogResourcePath);
            if (readings == null)
                EditorGUILayout.HelpBox($"Resources/{ClueBoardOutcomeEditing.CatalogResourcePath}.asset이 없습니다. 관계 결과(readingId)가 전부 미해결로 진단됩니다.", MessageType.Warning);
            else
            {
                EditorGUILayout.LabelField($"해몽 {ReadingIds.Count}개. 본문·보상은 이 에셋의 인스펙터에서 편집하고, 관계/체인 배선은 '단서 + 보드' 탭에서 합니다.", EditorStyles.wordWrappedLabel);
                DrawAssetRow(readings);
            }

            EditorGUILayout.Space(10f);
            SectionHeader("편집기 옵션 (이 컴퓨터에만 저장)");
            EditorGUILayout.LabelField("보드 저장/검증 경고 표시", EditorStyles.boldLabel);
            bool anyChanged = false;
            foreach (WarningFilter filter in WarningFilters)
            {
                bool next = EditorGUILayout.ToggleLeft(filter.label, filter.Enabled);
                if (next != filter.Enabled) { filter.Enabled = next; anyChanged = true; }
                EditorGUILayout.LabelField(filter.help, EditorStyles.wordWrappedMiniLabel);
            }
            if (anyChanged) _boardWarning = FilterBoardWarnings(_boardWarning);
            EditorGUILayout.HelpBox("끄면 편집기 경고에서만 빠집니다. 검증 자체와 런타임 로그는 그대로입니다. 다시 켜면 다음 저장/검증부터 보입니다.", MessageType.None);

            EditorGUILayout.Space(10f);
            SectionHeader("데이터 파일");
            EditorGUILayout.LabelField("맵", _dbPath, EditorStyles.miniLabel);
            EditorGUILayout.LabelField("단서", _cluePath, EditorStyles.miniLabel);
            EditorGUILayout.LabelField("보드", _boardPath, EditorStyles.miniLabel);
            EditorGUILayout.LabelField("인터넷", string.IsNullOrEmpty(_netPath) ? "(없음)" : _netPath, EditorStyles.miniLabel);
        }

        private static void DrawIconField(SerializedObject so, string property, string label)
        {
            EditorGUILayout.BeginHorizontal();
            SerializedProperty prop = so.FindProperty(property);
            EditorGUILayout.PropertyField(prop, new GUIContent(label));
            var sprite = prop.objectReferenceValue as Sprite;
            Rect preview = GUILayoutUtility.GetRect(20f, 20f, GUILayout.Width(20f));
            if (sprite != null && sprite.texture != null)
            {
                Rect uv = sprite.textureRect;
                uv.x /= sprite.texture.width; uv.width /= sprite.texture.width;
                uv.y /= sprite.texture.height; uv.height /= sprite.texture.height;
                GUI.DrawTextureWithTexCoords(preview, sprite.texture, uv);
            }
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawAssetRow(UnityEngine.Object asset)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(asset), EditorStyles.miniLabel);
            if (GUILayout.Button("에셋 선택", GUILayout.Width(70f)))
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }
            EditorGUILayout.EndHorizontal();
        }

        // Resources/<name>.asset을 읽고, 없으면 만들기 버튼을 보인다. 여러 개가 있어도 런타임은 Resources.Load가 찾는
        // 하나만 쓰므로 여기서도 그 하나만 다룬다.
        private static T LoadOrOfferCreate<T>(string resourceName, string createLabel) where T : ScriptableObject
        {
            var asset = Resources.Load<T>(resourceName);
            if (asset != null) return asset;
            string path = ResourcesDir + "/" + resourceName + ".asset";
            EditorGUILayout.HelpBox($"{path} 가 없습니다. 없어도 코드 기본값으로 동작합니다.", MessageType.Info);
            if (GUILayout.Button(createLabel, GUILayout.ExpandWidth(false)))
            {
                if (!AssetDatabase.IsValidFolder(ResourcesDir))
                    AssetDatabase.CreateFolder(Path.GetDirectoryName(ResourcesDir).Replace('\\', '/'), Path.GetFileName(ResourcesDir));
                var created = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(created, path);
                AssetDatabase.SaveAssets();
                ClueTypeIconSet.ClearCache();
                ClueSystemSettings.ClearCache();
                return created;
            }
            return null;
        }

        // ─── 추가 / 삭제 ─────────────────────────────────────────

        private void AddItem()
        {
            switch (_tab)
            {
                case Tab.Maps:
                    ArrayUtility.Add(ref _db.maps, new MapNodeData
                    {
                        guid          = NewGuid(),
                        nodeName      = "새 맵",
                        description   = "",
                        sceneName     = "",
                        iconAddress   = "",
                        localBoardId  = "",
                        graphPosition = Vector2.zero,
                        events        = Array.Empty<MapEventFlag>(),
                        clueIds       = Array.Empty<string>(),
                        wavePaths     = Array.Empty<string>(),
                        enemyGroups   = Array.Empty<EnemyGroupEntry>(),
                        requiredGears = Array.Empty<EmotionColor>(),
                    });
                    _selMap = _db.maps.Length - 1;
                    break;

                case Tab.Connections:
                    ArrayUtility.Add(ref _db.connections, new MapConnectionData
                    {
                        guid     = NewGuid(),
                        fromGuid = _db.maps.Length > 0 ? _db.maps[0].guid : "",
                        toGuid   = _db.maps.Length > 1 ? _db.maps[1].guid : "",
                    });
                    _selConn = _db.connections.Length - 1;
                    break;

                case Tab.Clues:
                    ArrayUtility.Add(ref _clueDb.clues, new ClueData
                    {
                        id                   = NewGuid(),
                        name                 = "새 단서",
                        description          = "",
                        targetMapGuid        = "",
                        targetConnectionGuid = "",
                        requiredEventKey     = "",
                        classification       = null,
                        boardSizePercent     = 100f,
                        boardFontSize        = 6.5f,
                        boardFontAddress     = "",
                        boardHideLabel       = false,
                        silhouetteHint       = "",
                        timestamp            = "",
                        content              = "",
                        mediaBlocks          = Array.Empty<ClueMediaBlock>(),
                        source               = "",
                        codexMapGuid         = "",
                        keywords             = Array.Empty<string>(),
                        comments             = Array.Empty<CodexComment>(),
                        attachments          = Array.Empty<ClueAttachment>(),
                    });
                    _selClue = _clueDb.clues.Length - 1;
                    break;

                case Tab.Boards:
                    ArrayUtility.Add(ref _boardDb.boards, new ClueBoardDefinition
                    {
                        boardId             = "board-" + NewGuid(),
                        kind                = ClueBoardKind.Local,
                        slots               = Array.Empty<ClueBoardSlot>(),
                        relations           = Array.Empty<ClueBoardRelation>(),
                        silhouetteNeighbors = Array.Empty<ClueBoardSilhouetteNeighbor>(),
                        initialRelationIds  = Array.Empty<string>(),
                    });
                    _selBoard = _boardDb.boards.Length - 1;
                    break;

                case Tab.Internet:
                    ArrayUtility.Add(ref _netDb.sites, new InternetSite
                    {
                        id       = "site-" + NewGuid(),
                        name     = "새 사이트",
                        iconAddress = "",
                        unlock   = new InternetUnlockCondition
                        {
                            requiredClueIds   = Array.Empty<string>(),
                            requiredEventKeys = Array.Empty<string>(),
                        },
                        posts    = Array.Empty<InternetPost>(),
                    });
                    _selSite = _netDb.sites.Length - 1;
                    break;
            }
        }

        private void RemoveItem(int idx)
        {
            switch (_tab)
            {
                case Tab.Maps:        ArrayUtility.RemoveAt(ref _db.maps,        idx); break;
                case Tab.Connections: ArrayUtility.RemoveAt(ref _db.connections, idx); break;
                case Tab.Clues:
                {
                    string removedClueId = _clueDb.clues[idx]?.id;
                    if (!string.IsNullOrEmpty(removedClueId))
                        foreach (ClueBoardDefinition board in _boardDb.boards)
                            for (int slotIndex = board.slots.Length - 1; slotIndex >= 0; slotIndex--)
                                if (board.slots[slotIndex]?.clueId == removedClueId)
                                {
                                    string removedNodeId = board.slots[slotIndex].nodeId;
                                    ArrayUtility.RemoveAt(ref board.slots, slotIndex);
                                    RemoveBoardReferencesToNode(board, removedNodeId);
                                    _boardDirty = true;
                                }
                    ArrayUtility.RemoveAt(ref _clueDb.clues, idx);
                    break;
                }
                case Tab.Boards:      ArrayUtility.RemoveAt(ref _boardDb.boards,  idx); break;
                case Tab.Internet:    ArrayUtility.RemoveAt(ref _netDb.sites,    idx); break;
            }
        }

        // ─── GUI 유틸 ─────────────────────────────────────────────

        private void SectionHeader(string title)
        {
            var r = GUILayoutUtility.GetRect(0f, 26f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(r, ColHeader);
            GUI.Label(new Rect(r.x + 8f, r.y + 1f, r.width, r.height), title, EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);
        }

        private static void ReadonlyField(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(label);
            EditorGUILayout.SelectableLabel(value, EditorStyles.miniLabel, GUILayout.Height(18f));
            EditorGUILayout.EndHorizontal();
        }

        private static string TF(string label, string value) =>
            EditorGUILayout.TextField(label, value ?? "");

        // 노드가 가리킬 실제 맵을 고르는 드롭다운.
        //
        // 예전에는 자유 입력 텍스트였는데, 이 문자열이 MapDataSO.mapAddressableID와 **정확히**
        // 같아야만 씬이 로드된다(RouteFindingMapBridge). 오타가 나도 화면에는 "지도에서 이동은
        // 되는데 씬이 안 바뀐다"로만 보여서, 아직 맵을 안 만든 노드와 구별이 되지 않았다.
        // 목록에서 고르게 하면 그 오류 종류가 통째로 사라지고, "(미제작)"을 명시적으로 고를 수
        // 있어 둘이 갈린다.
        //
        // 손으로 적힌 옛 값이나 이름이 바뀐 맵을 만나면 그 값을 버리지 않는다 — 목록 끝에
        // 그대로 얹어 선택된 채로 두고 경고만 띄운다. 조용히 비우면 어느 노드가 어디를 가리키고
        // 있었는지 복구할 수 없다.
        private static string SceneNamePopup(string label, string current)
        {
            current ??= "";

            var entries = MapSceneCatalog.Entries;
            bool isStale = current.Length > 0 && !MapSceneCatalog.Contains(current);

            // 0번은 항상 "(미제작)". 그 뒤로 알려진 맵, 마지막에(있다면) 깨진 값.
            var options = new List<string> { "(미제작 — 씬 없음)" };
            foreach (var e in entries)
                options.Add(e.Address == e.AssetName ? e.Address : $"{e.Address}   [{e.AssetName}]");
            if (isStale) options.Add($"⚠ {current}  (대응 맵 없음)");

            int cur = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Address == current) { cur = i + 1; break; }
            }
            if (isStale) cur = options.Count - 1;

            EditorGUILayout.BeginHorizontal();
            int sel = EditorGUILayout.Popup(label, cur, options.ToArray());
            // 맵을 새로 만든 직후엔 캐시에 없다 — 창을 닫았다 열지 않고 여기서 다시 훑게 한다.
            if (GUILayout.Button("↻", GUILayout.Width(24f))) MapSceneCatalog.Refresh();
            EditorGUILayout.EndHorizontal();

            if (isStale)
            {
                EditorGUILayout.HelpBox(
                    $"'{current}' 에 대응하는 MapDataSO가 없습니다. 오타이거나 맵이 아직 없는 상태입니다.\n" +
                    "맵을 방금 만들었다면 ↻ 로 목록을 새로 고치세요. 씬을 만들 계획이 없다면 (미제작)을 고르세요.",
                    MessageType.Warning);
            }

            if (sel == 0) return "";
            if (isStale && sel == options.Count - 1) return current; // 깨진 값 유지
            return entries[sel - 1].Address;
        }

        private static string TA(string label, string value)
        {
            EditorGUILayout.LabelField(label);
            return EditorGUILayout.TextArea(value ?? "", GUILayout.MinHeight(54f));
        }

        // 세 파일(map_database/clues/internet)이 쓰는 Addressable 주소를 전부 훑어 등록 상태를 본다.
        // 비어 있는 주소는 "아직 안 붙인 첨부물"이라 정상 — 오류로 올리지 않는다. 잡고 싶은 건
        // 오타이거나 그룹 창에서 지워진 엔트리다.
        private void ValidateAttachmentAddresses()
        {
            var missing = new System.Collections.Generic.List<(string address, bool isAudio, string where)>();
            int checkedCount = 0;

            void Check(string address, bool isAudio, string where)
            {
                if (string.IsNullOrWhiteSpace(address)) return;
                checkedCount++;
                if (!ClueAttachmentAddressables.IsRegistered(address))
                    missing.Add((address, isAudio, where));
            }

            if (_db?.maps != null)
                foreach (var m in _db.maps) Check(m.iconAddress, false, $"맵 '{m.nodeName}' 아이콘");

            if (_clueDb?.clues != null)
                foreach (var cl in _clueDb.clues)
                {
                    Check(cl.iconAddress, false, $"단서 '{cl.name}' 대표 아이콘");
                    if (cl.mediaBlocks != null)
                        foreach (var block in cl.mediaBlocks)
                        {
                            if (block == null || !ClueMediaConfig.NeedsAddress(block.kind)) continue;
                            Check(block.address, block.kind == ClueMediaKind.Audio,
                                  $"단서 '{cl.name}' 본문({ClueMediaConfig.GetDisplayName(block.kind)})");
                        }
                    if (cl.attachments == null) continue;
                    foreach (var at in cl.attachments)
                    {
                        if (at == null || at.kind == ClueAttachmentKind.MapRef) continue;
                        Check(at.address, at.kind == ClueAttachmentKind.Audio,
                              $"단서 '{cl.name}' 첨부({ClueAttachmentConfig.GetDisplayName(at.kind)})");
                    }
                }

            if (_netDb?.sites != null)
                foreach (var site in _netDb.sites)
                {
                    Check(site.iconAddress, false, $"사이트 '{site.name}' 아이콘");
                    if (site.posts == null) continue;
                    foreach (var post in site.posts)
                    {
                        if (post?.attachments == null) continue;
                        foreach (var at in post.attachments)
                        {
                            if (at == null || at.kind == ClueAttachmentKind.MapRef) continue;
                            Check(at.address, at.kind == ClueAttachmentKind.Audio,
                                  $"게시글 '{post.title}' 첨부({ClueAttachmentConfig.GetDisplayName(at.kind)})");
                        }
                    }
                }

            if (missing.Count == 0)
            {
                Debug.Log($"[맵 DB 편집기] 첨부물 주소 검증 통과 — 주소 {checkedCount}개가 모두 " +
                          "Addressable 엔트리와 연결됩니다.");
                return;
            }

            string report = string.Join("\n", missing.ConvertAll(m => $"- {m.where}: '{m.address}'"));

            // 주소는 대개 파일 이름 그대로다(등록할 때 그렇게 만든다). 그래서 이름이 같은 에셋을
            // 찾아 자동 등록해 볼 수 있다 — 다만 후보가 여럿이면 고르지 않는다. 잘못 고르면
            // 엉뚱한 사진이 조용히 붙어서, 못 찾은 것보다 알아채기 어렵다.
            if (EditorUtility.DisplayDialog(
                    "등록되지 않은 주소",
                    $"주소 {checkedCount}개 중 {missing.Count}개가 Addressable에 등록돼 있지 않습니다.\n\n" +
                    "이름이 같은 에셋을 프로젝트에서 찾아 자동으로 등록할까요?\n" +
                    "(후보가 여러 개인 주소는 건너뛰고 콘솔에 남깁니다)",
                    "자동 등록 시도", "보고만 하기"))
            {
                int done = 0;
                foreach (var m in missing)
                    if (ClueAttachmentAddressables.TryRegisterByName(m.address, m.isAudio)) done++;

                Debug.Log($"[맵 DB 편집기] 자동 등록 {done}/{missing.Count}개 완료.\n[검사한 주소]\n{report}");
                return;
            }

            Debug.LogWarning(
                $"[맵 DB 편집기] 등록되지 않은 Addressable 주소 {missing.Count}개 (주소 {checkedCount}개 검사)\n" +
                report +
                $"\n\n에셋 칸에 파일을 끌어다 놓으면 '{ClueAttachmentAddressables.GroupName}' 그룹에 자동으로 " +
                "등록됩니다. 이미 등록된 에셋이라면 Groups 창에서 주소를 위 문자열과 맞추세요.");
        }

        // "Addressable 주소" 문자열 필드 + 에셋 오브젝트 칸을 한 줄에 같이 보여준다.
        // 주소를 직접 칠 수도 있고, 에셋을 끌어다 놓으면 그 에셋의 Addressable 주소를 채워 준다 —
        // 아직 등록되지 않은 에셋이면 등록할지 물어보고 ClueAttachments 그룹에 넣는다.
        // JSON에는 에셋 참조를 담을 수 없어 주소가 유일한 연결 고리라, 손으로 적다 틀리는 사고를
        // 막는 게 목적이다(MapDataSO.mapAddressableID를 손으로 적다 틀리는 것과 같은 문제다 —
        // 그쪽은 MapDataRegistrySOEditor의 검증 버튼이 사후에 잡는다).
        private static string AddressableField<T>(string label, string address) where T : UnityEngine.Object
        {
            EditorGUILayout.BeginHorizontal();
            string newAddress = EditorGUILayout.TextField(label, address ?? "");
            var current = ClueAttachmentAddressables.LoadByAddress<T>(newAddress);
            var picked = EditorGUILayout.ObjectField(current, typeof(T), false, GUILayout.Width(120f)) as T;
            EditorGUILayout.EndHorizontal();

            if (picked != current)
            {
                newAddress = picked == null
                    ? ""
                    : ClueAttachmentAddressables.EnsureAddressable(picked, typeof(T) == typeof(Sprite));
            }

            // 주소는 있는데 등록된 엔트리가 없으면(오타 등) 런타임에도 "(파일 없음)"으로 뜬다 — 미리 알려준다.
            if (!string.IsNullOrWhiteSpace(newAddress) && ClueAttachmentAddressables.LoadByAddress<T>(newAddress) == null)
                EditorGUILayout.HelpBox($"'{newAddress}' 주소로 등록된 Addressable 엔트리가 없습니다.", MessageType.Warning);

            return newAddress;
        }

        // 노드 이름 드롭다운 → 선택된 GUID 반환
        private string NodeGuidPopup(string label, string curGuid, bool allowEmpty = false)
        {
            var maps = _db.maps;
            if (maps.Length == 0 && !allowEmpty)
            {
                EditorGUILayout.LabelField(label, "(맵 없음 — 먼저 맵을 추가하세요)");
                return curGuid;
            }

            int off  = allowEmpty ? 1 : 0;
            var opts = new string[maps.Length + off];
            int cur  = allowEmpty ? 0 : 0;
            if (allowEmpty) opts[0] = "(없음)";
            for (int i = 0; i < maps.Length; i++)
            {
                opts[i + off] = $"{maps[i].nodeName}  [{Sg(maps[i].guid)}]";
                if (maps[i].guid == curGuid) cur = i + off;
            }
            int sel = EditorGUILayout.Popup(label, cur, opts);
            if (allowEmpty && sel == 0) return "";
            int mi = sel - off;
            return (mi >= 0 && mi < maps.Length) ? maps[mi].guid : curGuid;
        }

        // 연결 이름 드롭다운 → 선택된 GUID 반환
        private string ConnGuidPopup(string label, string curGuid, bool allowEmpty = false)
        {
            var conns = _db.connections;
            if (conns.Length == 0 && !allowEmpty)
            {
                EditorGUILayout.LabelField(label, "(연결 없음)");
                return curGuid;
            }

            int off  = allowEmpty ? 1 : 0;
            var opts = new string[conns.Length + off];
            int cur  = allowEmpty ? 0 : 0;
            if (allowEmpty) opts[0] = "(없음)";
            for (int i = 0; i < conns.Length; i++)
            {
                opts[i + off] = $"{NodeName(conns[i].fromGuid)} → {NodeName(conns[i].toGuid)}  [{Sg(conns[i].guid)}]";
                if (conns[i].guid == curGuid) cur = i + off;
            }
            int sel = EditorGUILayout.Popup(label, cur, opts);
            if (allowEmpty && sel == 0) return "";
            int ci = sel - off;
            return (ci >= 0 && ci < conns.Length) ? conns[ci].guid : curGuid;
        }

        // GUID → 노드 이름 역조회
        private string NodeName(string guid)
        {
            if (_db?.maps == null || string.IsNullOrEmpty(guid)) return "?";
            foreach (var n in _db.maps)
                if (n.guid == guid) return n.nodeName;
            return Sg(guid);
        }

        private static string NewGuid()    => Guid.NewGuid().ToString("N").Substring(0, 16);
        private static string Sg(string g) => string.IsNullOrEmpty(g) ? "?" :
                                              (g.Length > 8 ? g.Substring(0, 8) + "…" : g);
        private static string ShortPath(string p) => p.Length > 52 ? "…" + p.Substring(p.Length - 51) : p;
    }
}
