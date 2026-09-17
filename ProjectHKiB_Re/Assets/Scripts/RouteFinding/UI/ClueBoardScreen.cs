using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RouteFinding.UI
{
    // 글로벌/로컬 보드 진입 구조. 보드 한 장을 그리는 일은 ClueBoardView가 하고, 이 컴포넌트는
    // "지금 어느 보드를 볼 것인가"와 그 결정이 실패했을 때의 진단만 맡는다.
    //
    // [fallback 금지] 로컬 보드는 현재 위치 맵의 localBoardId로만 정한다. 그 값이 비었거나 오타이거나
    // 글로벌 보드를 가리키면 **다른 보드를 대신 열지 않는다.** 임의 fallback을 넣으면 배선이 빠진
    // 맵에서도 화면이 그럴듯하게 떠서, 잘못된 데이터를 영영 못 찾는다(ClueBoardCatalog와 같은 계약).
    //
    // [C03-B] 우측 세로 탭(글로벌/로컬/검색)과 검색 패널을 ClueBoardView.FilterBarRoot에 얹는다.
    // 탭은 "어느 보드를 볼 것인가"라 이 컴포넌트가 갖고, 검색 조건·결과·하이라이트는 ClueBoardSearchPanel이
    // 갖는다. 보드가 바뀔 때마다(성공·실패 모두) 검색 패널에 다시 계산하게 해서, 조건은 유지한 채
    // 결과만 새 보드 기준으로 바뀐다. 선 클릭 재열람(코멘트)은 C06이다.
    //
    // [C03-C/C04 연결 유지] 보드를 열 때마다 엔진 상태를 새로 만들되, 그 안의 연결은
    //   정의의 initialRelationIds(엔진이 넣음) + ClueBoardProgress(세션 동안 이은 것 + 세이브에서 복원한 것)
    // 의 합성이다. 새 연결은 View.OnConnectionEstablished(실제 새 연결에만 발행)에서 ClueBoardProgress에 기록한다.
    // 복원은 ShowBoard 경로라 판정(TryConnect)을 거치지 않고, 따라서 어떤 연결 이벤트도 내지 않는다.
    // 검색어/하이라이트/드래그 중 상태는 진행에 섞이지 않는다 — 진행은 관계 ID 집합뿐이다.
    //
    // [C06 관계 결과] 같은 HandleConnectionEstablished에서 진행 기록 **다음에** 결과를 판정한다(ClueBoardOutcomeResolver).
    // 판정에 쓰는 연결 집합은 View.RuntimeState(초기 + 복원 + 방금 성립)라 체인의 "마지막 관계"가 정확히 잡힌다.
    // 발행한 결과는 ClueBoardOutcomes(세션 + 세이브)에 기록하고, 보상은 readingRewardHandler(기본 DreamReadingModule)가
    // 한 번만 준다. 화면 표시는 OnOutcomeIssued/OnOutcomeReviewRequested를 구독하는 패널 몫이며, 구독자가 없으면
    // 기록만 남아 다음 열람 때 보류분(CollectPendingOutcomes)으로 보인다 — 데이터는 잃지 않는다.
    // 선 클릭(View.OnEdgeClicked)은 판정을 거치지 않고 발행 기록만 읽는다.
    //
    // [로컬 보드 목록] 기획: "로컬 보드는 유저가 직접 눌러서 다른 로컬 보드를 열 수도 있지만, 현재 해당하는 맵의 로컬
    // 보드가 UI에서 처음 뜨도록". 최초 로컬 진입은 언제나 현재 맵의 localBoardId(ShowLocalForCurrentMap)이고,
    // 로컬 탭을 한 번 더 누르면 열 수 있는 로컬 보드 목록이 탭 위에 펼쳐진다. 목록의 후보는 기본적으로 **방문한 맵**이
    // 가리키는 로컬 보드(+ 현재 맵)다 — 아직 가 보지 않은 지역의 보드까지 보이면 지역 존재가 새어 나간다. 어떤 보드를
    // 골랐는지는 화면 상태라 저장하지 않으며, 다시 열면 현재 맵 보드부터 시작한다.
    public sealed class ClueBoardScreen : MonoBehaviour
    {
        public enum BoardScope { Global, Local }

        // 우측 열 치수(캔버스 단위). 탭 줄은 항상 보이고, 검색 패널은 탭 왼쪽에 펼쳐진다.
        public const float TabStripWidth = 26f;
        public const float SearchPanelWidth = 118f;

        /// <summary>보드를 열지 못한 이유. 화면 문구와 로그가 같은 문자열을 쓴다.</summary>
        public event Action<string> OnDiagnostic;

        /// <summary>보드가 실제로 열렸을 때. 인자는 보드 ID.</summary>
        public event Action<string> OnBoardOpened;

        /// <summary>[C06] 새 관계 연결로 결과가 **처음** 발행됐을 때(한 조작에 여러 결과가 있으면 단일 → 체인 순서).</summary>
        public event Action<IReadOnlyList<ClueBoardOutcomePresentation>> OnOutcomeIssued;

        /// <summary>[C06] 성립한 선을 클릭해 이미 발행된 결과를 다시 보려 할 때. 결과 없는 선은 발행되지 않는다.</summary>
        public event Action<IReadOnlyList<ClueBoardOutcomePresentation>> OnOutcomeReviewRequested;

        /// <summary>
        /// 관계 없는 쌍(Unrelated)을 드롭해 거절됐을 때의 델타 코멘트. 결과(C06)가 아니라 매번 다시 나오며 보상·발행 기록이
        /// 없다. OnConnectionResult(거절 포함)를 구독하는 유일한 경로이고, 성립·재시도는 여기로 오지 않는다.
        /// </summary>
        public event Action<ClueBoardRejectionComment> OnConnectionRejected;

        /// <summary>
        /// 연결 성공 코멘트(해몽 결과가 없는 관계). 새 연결(isNew)과 선 재클릭(재열람) 모두 여기로 온다. 관계 자체에
        /// 해몽 결과가 발행되는 경우에는 그 카드가 대신 뜨므로 발행되지 않는다(체인 결과만 있는 경우엔 코멘트 → 체인 순).
        /// </summary>
        public event Action<ClueBoardConnectionComment> OnConnectionCommented;

        /// <summary>검증/진단용 — 마지막 연결 코멘트.</summary>
        public ClueBoardConnectionComment LastConnectionComment { get; private set; }

        /// <summary>해금 노드를 클릭해 단서 설명을 요청했을 때. 판정·기록과 무관한 읽기 전용 요청이다.</summary>
        public event Action<ClueData> OnClueInfoRequested;

        /// <summary>거절 코멘트 템플릿. [A]/[B]가 시도 순서대로 단서 이름으로 바뀐다. 비우면 기본 문구.</summary>
        public string RejectionCommentTemplate { get; set; }

        /// <summary>검증/진단용 — 마지막으로 만든 거절 코멘트.</summary>
        public ClueBoardRejectionComment LastRejectionComment { get; private set; }

        [SerializeField] private string _globalBoardId = "";

        private ClueBoardView _view;
        private ClueBoardSearchPanel _searchPanel;
        private TextMeshProUGUI _statusText;
        private RectTransform _root;
        private Image _tabGlobal;
        private Image _tabLocal;
        private Image _tabSearch;
        private RectTransform _localBoardList;
        private TMP_FontAsset _font;

        // 로컬 목록에서 고른 보드. null이면 현재 맵의 보드다(최초 진입·현재 맵 항목 선택). 화면 상태라 저장하지 않는다.
        private string _selectedLocalBoardId;
        private readonly List<string> _localBoardEntries = new();
        private int _localListGeneration;

        // 로컬 보드 후보 공급원(보드 ID 목록). 기본은 방문한 맵의 localBoardId ∪ 현재 맵. 검증에서는 고정 목록을 넣는다.
        private Func<IEnumerable<string>> _localBoardListProvider;

        private static readonly Color ColTabOff = new(0.14f, 0.17f, 0.24f, 0.98f);
        private static readonly Color ColTabOn = new(0.30f, 0.38f, 0.52f, 1f);

        // 획득 단서 공급원. 기본은 RouteModule의 진행 상태이며, 검증에서는 고정 집합을 주입한다
        // (세이브/진행 계층을 검증 코드가 흉내 내지 않아도 되게).
        private Func<IEnumerable<string>> _acquiredClueProvider;

        // 현재 위치 공급원. 기본은 RouteModule.CurrentLocation.
        private Func<MapNodeData> _currentMapProvider;

        // C07 환각 노드 공급원. 보드를 받는 쪽(DreamErosionModule.GetHallucinationNodeIds)이 우선이고, 보드 무관 공급원은
        // 검증용으로 남긴다. 둘 다 없으면 빈 집합.
        private Func<IEnumerable<string>> _hallucinationProvider;
        private Func<ClueBoardDefinition, IEnumerable<string>> _hallucinationBoardProvider;

        // [C07] 잠식 연동. 기본은 DreamErosionModule(플레이 중에만 존재). 검증에서는 카운터를 넣는다.
        // 실패는 Unrelated 거절만, 성공은 실제 새 연결만 — 재시도·실루엣·자기 자신 드롭은 여기까지 오지 않는다.
        private Action<string, string, string> _erosionFailureHandler;
        private Action<string, string> _erosionSuccessHandler;

        // 복원 연결 목록 덮어쓰기(검증용). null이면 ClueBoardProgress를 정의와 대조해 쓴다.
        private Func<string, IEnumerable<string>> _restoredConnectionProvider;

        // 새 연결을 기록할 진행 저장소. null이면 세션 공용 ClueBoardProgress.Current.
        private ClueBoardProgressState _progressOverride;
        private ClueBoardProgressState Progress => _progressOverride ?? ClueBoardProgress.Current;

        // [C06] 결과 발행 기록. null이면 세션 공용 ClueBoardOutcomes.Current.
        private ClueBoardOutcomeState _outcomeOverride;
        private ClueBoardOutcomeState Outcomes => _outcomeOverride ?? ClueBoardOutcomes.Current;

        // [C06] 해몽 조회와 보상. 기본은 DreamReadingModule(FindReading / TryResolveById).
        private Func<string, DreamReading> _readingProvider;
        private Func<DreamReading, bool> _readingRewardHandler;

        // 현재 보드의 결과 배선(정의에서 정규화). 보드를 열 때마다 다시 만든다.
        private ClueBoardOutcomeCatalog _outcomeCatalog;

        /// <summary>[C06] 마지막 결과 판정/재열람의 진단. 검증용.</summary>
        public IReadOnlyList<string> LastOutcomeDiagnostics => _lastOutcomeDiagnostics;
        private readonly List<string> _lastOutcomeDiagnostics = new();

        public ClueBoardView View => _view;
        public ClueBoardSearchPanel SearchPanel => _searchPanel;
        public bool IsSearchOpen => _searchPanel != null && _searchPanel.IsOpen;
        public BoardScope CurrentScope { get; private set; } = BoardScope.Global;
        public string CurrentBoardId => _view != null ? _view.BoardId : null;
        public string LastDiagnostic { get; private set; }

        public void Initialize(RectTransform parent, TMP_FontAsset font)
        {
            // 프리팹 우선: 컨테이너는 이름으로 찾아 재사용하고 없을 때만 만든다(ClueBoardUiKit 규칙).
            _root = ClueBoardUiKit.Child(parent, "ClueBoardScreen", out bool created);
            if (created) ClueBoardUiKit.Stretch(_root);

            _view = ClueBoardUiKit.Ensure<ClueBoardView>(_root.gameObject);
            _view.Initialize(_root, font);
            _view.OnConnectionEstablished += HandleConnectionEstablished;
            _view.OnConnectionResult += HandleConnectionResult;
            _view.OnEdgeClicked += HandleEdgeClicked;
            _view.OnNodeClicked += HandleNodeClicked;
            ClueBoardProgress.OnReplaced += HandleProgressReplaced;

            BuildSideColumn(_view.FilterBarRoot, font);

            RectTransform statusRect = ClueBoardUiKit.Child(_root, "StatusText", out created);
            if (created)
            {
                statusRect.anchorMin = new Vector2(0f, 0f);
                statusRect.anchorMax = new Vector2(1f, 0f);
                statusRect.pivot = new Vector2(0.5f, 0f);
                statusRect.sizeDelta = new Vector2(0f, 32f);
            }
            _statusText = ClueBoardUiKit.Text(statusRect, created, font, 8f, new Color(0.90f, 0.55f, 0.50f), TextAlignmentOptions.BottomLeft);
            _statusText.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_view != null)
            {
                _view.OnConnectionEstablished -= HandleConnectionEstablished;
                _view.OnConnectionResult -= HandleConnectionResult;
                _view.OnEdgeClicked -= HandleEdgeClicked;
                _view.OnNodeClicked -= HandleNodeClicked;
            }
            ClueBoardProgress.OnReplaced -= HandleProgressReplaced;
        }

        /// <summary>
        /// 검증·다른 진행 계층이 공급원을 바꿔 끼우는 자리. 넘기지 않은 항목은 기본 경로를 쓴다.
        /// clueProvider는 노드 이름/아이콘/감정과 검색이 함께 쓰는 단서 조회(기본 MapGraph).
        /// newClueProvider는 C05 신규 판정이며, 넘겨야만 검색 패널에 new! 버튼이 생긴다.
        /// restoredConnectionProvider는 복원 목록을 통째로 덮어쓰는 검증용 자리이고, progressState는
        /// 새 연결을 기록하고 복원 목록을 읽을 저장소다(기본 ClueBoardProgress.Current).
        /// [C06] readingProvider는 해몽 ID → 레시피 조회(기본 DreamReadingModule.FindReading), readingRewardHandler는
        /// 새 발행 시 보상(기본 DreamReadingModule.TryResolveById — 이미 성립한 해몽이면 false), outcomeState는
        /// 발행 기록 저장소(기본 ClueBoardOutcomes.Current)다.
        /// </summary>
        public void ConfigureSources(
            Func<IEnumerable<string>> acquiredClueProvider = null,
            Func<MapNodeData> currentMapProvider = null,
            Func<IEnumerable<string>> hallucinationProvider = null,
            Func<string, IEnumerable<string>> restoredConnectionProvider = null,
            Func<string, ClueData> clueProvider = null,
            Func<string, bool> newClueProvider = null,
            Action<string> clueViewedHandler = null,
            ClueBoardProgressState progressState = null,
            Func<string, DreamReading> readingProvider = null,
            Func<DreamReading, bool> readingRewardHandler = null,
            ClueBoardOutcomeState outcomeState = null,
            Func<IEnumerable<string>> localBoardListProvider = null,
            Func<ClueBoardDefinition, IEnumerable<string>> hallucinationBoardProvider = null,
            Action<string, string, string> erosionFailureHandler = null,
            Action<string, string> erosionSuccessHandler = null)
        {
            _acquiredClueProvider = acquiredClueProvider;
            _localBoardListProvider = localBoardListProvider;
            _hallucinationBoardProvider = hallucinationBoardProvider;
            _erosionFailureHandler = erosionFailureHandler;
            _erosionSuccessHandler = erosionSuccessHandler;
            _currentMapProvider = currentMapProvider;
            _hallucinationProvider = hallucinationProvider;
            _restoredConnectionProvider = restoredConnectionProvider;
            _progressOverride = progressState;
            _readingProvider = readingProvider;
            _readingRewardHandler = readingRewardHandler;
            _outcomeOverride = outcomeState;
            _view?.SetClueResolver(clueProvider);
            _view?.SetNewClueState(newClueProvider, clueViewedHandler);
            // 검색/필터 임시 비활성: 공급원을 연결하지 않아 숨은 필터 상태도 갱신되지 않게 한다.
            // _searchPanel?.SetNewClueProvider(newClueProvider);
        }

        // ─── 연결 진행(C03-C/C04) ────────────────────────────────

        // 실제 새 연결(Connected)에만 불린다. 재시도(AlreadyConnected)는 View가 발행하지 않으므로
        // 저장소에도 중복 기록이 생기지 않는다(MarkConnected 자체도 멱등이다).
        private void HandleConnectionEstablished(ClueBoardConnectResult result)
        {
            ClueBoardDefinition board = _view.Definition;
            if (board == null || string.IsNullOrEmpty(result.relationId)) return;

            string first = null, second = null;
            if (board.relations != null)
                foreach (ClueBoardRelation relation in board.relations)
                    if (relation != null && relation.relationId == result.relationId)
                    {
                        first = relation.firstNodeId;
                        second = relation.secondNodeId;
                        break;
                    }
            Progress.MarkConnected(board.boardId, result.relationId, first, second);

            // [C07] 연결 성공은 잠식을 해제한다(0단계). 기록·결과 발행 뒤에 알려 순서가 뒤바뀌지 않게 한다.
            if (_erosionSuccessHandler != null) _erosionSuccessHandler(board.boardId, result.relationId);
            else if (Application.isPlaying) DreamErosionModule.Instance?.RegisterConnectionSuccess(board.boardId, result.relationId);

            List<ClueBoardOutcomePresentation> issued = IssueOutcomes(board, result.relationId);
            // 이 관계 자체의 해몽 결과가 없으면 코멘트로 반응한다. 체인 결과는 그 뒤에 큐로 이어진다.
            bool hasOwnOutcome = issued.Exists(p => p.outcome.kind == ClueBoardOutcomeKind.Relation &&
                                                   string.Equals(p.outcome.outcomeId, result.relationId, StringComparison.Ordinal));
            if (!hasOwnOutcome) EmitConnectionComment(board, result.relationId, isNew: true);
            if (issued.Count > 0) OnOutcomeIssued?.Invoke(issued);
        }

        private void HandleNodeClicked(string nodeId)
        {
            ClueData clue = _view.GetClueOfNode(nodeId);
            if (clue != null) OnClueInfoRequested?.Invoke(clue);
        }

        private void EmitConnectionComment(ClueBoardDefinition board, string relationId, bool isNew)
        {
            ClueBoardConnectionComment comment = ClueBoardConnectionComment.Create(
                board, relationId, isNew, _view.ClueResolver, ClueSystemSettings.ConnectionTemplate);
            if (comment == null) return;
            LastConnectionComment = comment;
            OnConnectionCommented?.Invoke(comment);
        }

        // 판정 결과 전부가 오지만 여기서 다루는 것은 Unrelated 거절뿐이다. 진행 기록·결과 발행은 위 Established 경로가
        // 맡고, 이 경로는 아무것도 기록하지 않는다 — 거절 코멘트는 몇 번이고 같은 쌍에 다시 나와야 한다(기획 "일괄 출력").
        private void HandleConnectionResult(ClueBoardConnectResult result)
        {
            if (result.status != ClueBoardConnectStatus.Unrelated) return;
            ClueBoardDefinition board = _view.Definition;

            // [C07] Unrelated 거절 1회 = 조합 실패 1회. 코멘트 생성 여부와 무관하게 센다(이름을 못 찾아도 실패는 실패다).
            string boardId = board?.boardId;
            if (_erosionFailureHandler != null) _erosionFailureHandler(boardId, result.firstNodeId, result.secondNodeId);
            else if (Application.isPlaying) DreamErosionModule.Instance?.RegisterConnectionFailure(boardId, result.firstNodeId, result.secondNodeId);

            // 템플릿: 코드에서 지정한 것 → 설정 에셋(ClueSystemSettings) → 코드 기본값.
            ClueBoardRejectionComment comment = ClueBoardRejectionComment.TryCreate(
                board, result, _view.ClueResolver, RejectionCommentTemplate ?? ClueSystemSettings.RejectionTemplate);
            if (comment == null) return;
            LastRejectionComment = comment;
            OnConnectionRejected?.Invoke(comment);
        }

        // ─── 관계 결과(C06) ──────────────────────────────────────

        private Func<string, DreamReading> ReadingProvider =>
            _readingProvider ?? (id => DreamReadingModule.Instance != null ? DreamReadingModule.Instance.FindReading(id) : null);

        private Func<DreamReading, bool> RewardHandler =>
            _readingRewardHandler ?? (reading => DreamReadingModule.Instance != null && DreamReadingModule.Instance.TryResolveById(reading.id));

        // OnConnectionEstablished(실제 새 연결)에서만 불린다. 결과 없는 관계·손상 참조는 진단만 남기고 화면은 그대로다.
        private List<ClueBoardOutcomePresentation> IssueOutcomes(ClueBoardDefinition board, string relationId)
        {
            if (_outcomeCatalog == null || _outcomeCatalog.boardId != board.boardId)
                _outcomeCatalog = ClueBoardOutcomeCatalog.Build(board, null);

            _lastOutcomeDiagnostics.Clear();
            ClueBoardRuntimeState runtime = _view.RuntimeState;
            List<ClueBoardOutcomePresentation> issued = ClueBoardOutcomeResolver.Issue(
                _outcomeCatalog, runtime.IsConnected, relationId, Outcomes, ReadingProvider, RewardHandler, _lastOutcomeDiagnostics);
            foreach (string message in _lastOutcomeDiagnostics) Debug.LogWarning("[ClueBoardScreen] " + message);
            foreach (ClueBoardOutcomePresentation presentation in issued)
                Debug.Log($"[ClueBoardScreen] 관계 결과 발행: board={board.boardId}, {presentation.outcome.kind} '{presentation.outcome.outcomeId}' → 해몽 '{presentation.reading.id}', reward={presentation.rewardGranted}");
            return issued; // 이벤트 발행은 호출부가 코멘트 순서를 정한 뒤 한다
        }

        // 선 클릭 재열람 — 판정 없이 발행 기록만 읽는다. 발행 전이거나 결과 없는 선은 아무 동작도 하지 않는다.
        private void HandleEdgeClicked(string relationId)
        {
            ClueBoardDefinition board = _view.Definition;
            if (board == null) return;
            if (_outcomeCatalog == null || _outcomeCatalog.boardId != board.boardId)
                _outcomeCatalog = ClueBoardOutcomeCatalog.Build(board, null);

            _lastOutcomeDiagnostics.Clear();
            List<ClueBoardOutcomePresentation> found = ClueBoardOutcomeResolver.Review(
                _outcomeCatalog, relationId, Outcomes, ReadingProvider, _lastOutcomeDiagnostics);
            foreach (string message in _lastOutcomeDiagnostics) Debug.LogWarning("[ClueBoardScreen] " + message);
            if (found.Count == 0)
            {
                // 결과 없는 관계·초기 연결·미발행 — 대신 연결 코멘트를 다시 보여 준다(기획: 선을 다시 누르면 코멘트를 언제든 본다).
                EmitConnectionComment(board, relationId, isNew: false);
                return;
            }
            OnOutcomeReviewRequested?.Invoke(found);
        }

        /// <summary>
        /// 발행됐지만 아직 보여 주지 못한 결과(보류분). 패널이 열릴 때 한 번 표시하고 MarkOutcomeViewed로 닫는다.
        /// 정의에서 사라진 보드/결과·없는 해몽은 진단만 남기고 건너뛴다.
        /// </summary>
        public List<ClueBoardOutcomePresentation> CollectPendingOutcomes()
        {
            _lastOutcomeDiagnostics.Clear();
            List<ClueBoardOutcomePresentation> pending = ClueBoardOutcomeResolver.Pending(
                Outcomes,
                boardId => ClueBoardCatalog.TryGetBoard(boardId, out ClueBoardDefinition board, out _) ? board : null,
                ReadingProvider, _lastOutcomeDiagnostics);
            foreach (string message in _lastOutcomeDiagnostics) Debug.LogWarning("[ClueBoardScreen] " + message);
            return pending;
        }

        /// <summary>결과 화면을 플레이어에게 실제로 보였을 때 패널이 부른다. 보류분이 다시 뜨지 않게 한다.</summary>
        public void MarkOutcomeViewed(ClueBoardOutcomePresentation presentation)
        {
            if (presentation?.outcome == null) return;
            Outcomes.MarkViewed(presentation.outcome.boardId, presentation.outcome.kind, presentation.outcome.outcomeId);
        }

        // 세이브 복원처럼 진행이 통째로 바뀌면 열려 있는 보드를 같은 경로로 다시 그린다.
        // 화면이 닫혀 있거나(비활성) 보드가 없으면 다음 열람 때 자연히 반영된다.
        private void HandleProgressReplaced()
        {
            if (_view == null || CurrentBoardId == null || !isActiveAndEnabled) return;
            Show(CurrentScope);
        }

        // ─── 우측 탭 열과 검색 패널(C03-B) ───────────────────────

        public void ToggleSearch() { /* 검색/필터 임시 비활성 */ }

        public void SetSearchOpen(bool open)
        {
            // 검색/필터 임시 비활성. 기존 API 호출자는 깨지지 않지만 화면과 상태는 바꾸지 않는다.
            // if (_searchPanel == null) return;
            // _searchPanel.SetOpen(open);
            // RefreshTabs();
        }

        private void BuildSideColumn(RectTransform column, TMP_FontAsset font)
        {
            const float stripWidth = 58f;
            const float stripHeight = 34f;

            // 글로벌/로컬 탭 — 보드 오른쪽 아래에 가로로 고정한다. 프리팹에 TabStrip이 있으면 그 배치·배경을 쓴다.
            RectTransform strip = ClueBoardUiKit.Child(column, "TabStrip", out bool created);
            if (created)
            {
                column.sizeDelta = new Vector2(stripWidth, 0f);
                strip.anchorMin = new Vector2(1f, 0f);
                strip.anchorMax = new Vector2(1f, 0f);
                strip.pivot = new Vector2(1f, 0f);
                strip.sizeDelta = new Vector2(stripWidth, stripHeight);
                var stripBg = strip.gameObject.AddComponent<Image>();
                stripBg.color = new Color(0.08f, 0.09f, 0.13f, 0.98f);
                stripBg.raycastTarget = true;

                var layout = strip.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.padding = new RectOffset(2, 2, 3, 3);
                layout.spacing = 3f;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = true;
                layout.childAlignment = TextAnchor.MiddleRight;
            }

            _font = font;
            _tabGlobal = MakeTab(strip, "TabGlobal", "글로벌", font, () => Show(BoardScope.Global));
            _tabLocal = MakeTab(strip, "TabLocal", "로컬", font, OnLocalTabClicked);
            BuildLocalBoardList(column, stripWidth, stripHeight);
            // 검색/필터 임시 비활성. 아래 생성 코드는 기능을 다시 열 때 복구한다.
            // _tabSearch = MakeTab(strip, "TabSearch", "검색", font, ToggleSearch);
            // _searchPanel = column.gameObject.AddComponent<ClueBoardSearchPanel>();
            // _searchPanel.Initialize(column, font, _view, SearchPanelWidth);
            // _searchPanel.Root.anchoredPosition = new Vector2(-TabStripWidth, 0f);

            RefreshTabs();
        }

        // 탭/목록 버튼. 프리팹에 같은 이름이 있으면 크기·색·글자를 그대로 쓰고 클릭 콜백만 다시 건다.
        private Image MakeTab(RectTransform parent, string name, string label, TMP_FontAsset font, Action onClick)
        {
            RectTransform rect = ClueBoardUiKit.Child(parent, name, out bool created);
            var layout = ClueBoardUiKit.Ensure<LayoutElement>(rect.gameObject);
            var image = ClueBoardUiKit.Ensure<Image>(rect.gameObject);
            if (created)
            {
                layout.preferredWidth = 25f;
                layout.preferredHeight = 28f;
                image.color = ColTabOff;
            }
            var button = ClueBoardUiKit.Ensure<Button>(rect.gameObject);
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick());

            RectTransform textRect = ClueBoardUiKit.Child(rect, "Text", out bool textCreated);
            if (textCreated) ClueBoardUiKit.Stretch(textRect);
            ClueBoardUiKit.Text(textRect, textCreated, font, 6f, Color.white, TextAlignmentOptions.Center, label);
            return image;
        }

        // 탭 색은 "표시 중인 보드"를 따른다. 열기에 실패했으면(화면이 비었으면) 어느 탭도 켜지 않는다.
        private void RefreshTabs()
        {
            bool hasBoard = CurrentBoardId != null;
            if (_tabGlobal != null) _tabGlobal.color = hasBoard && CurrentScope == BoardScope.Global ? ColTabOn : ColTabOff;
            if (_tabLocal != null) _tabLocal.color = hasBoard && CurrentScope == BoardScope.Local ? ColTabOn : ColTabOff;
            if (_tabSearch != null) _tabSearch.color = IsSearchOpen ? ColTabOn : ColTabOff;
        }

        /// <summary>검증용 — 탭이 켜진 색인지.</summary>
        public bool IsTabLit(string tabName)
        {
            Image tab = tabName switch
            {
                "TabGlobal" => _tabGlobal,
                "TabLocal" => _tabLocal,
                "TabSearch" => _tabSearch,
                _ => null,
            };
            return tab != null && tab.color == ColTabOn;
        }

        /// <summary>검증용 — 탭 이름(TabGlobal/TabLocal/TabSearch)으로 클릭을 흉내 낸다.</summary>
        public bool ClickTab(string tabName)
        {
            Transform found = _view != null ? _view.FilterBarRoot.Find("TabStrip/" + tabName) : null;
            var button = found != null ? found.GetComponent<Button>() : null;
            if (button == null) return false;
            button.onClick.Invoke();
            return true;
        }

        public void SetGlobalBoardId(string boardId) => _globalBoardId = boardId ?? "";

        /// <summary>글로벌 보드를 연다. 보드 ID를 지정하지 않았으면 정의된 Global 보드 중 하나를 찾는다.</summary>
        public bool ShowGlobal()
        {
            string boardId = _globalBoardId;
            if (string.IsNullOrWhiteSpace(boardId))
            {
                var globals = new List<ClueBoardDefinition>(ClueBoardCatalog.BoardsOfKind(ClueBoardKind.Global));
                if (globals.Count == 0)
                    return Fail("글로벌 보드가 정의되어 있지 않습니다. " +
                                (ClueBoardCatalog.LoadError ?? "clue_boards.json에 Global 보드를 추가하세요."));
                // 여러 장이면 어느 것이 기본인지는 콘텐츠 결정이다. 조용히 고르지 않고 지정하게 한다.
                if (globals.Count > 1)
                    return Fail($"글로벌 보드가 {globals.Count}장입니다. SetGlobalBoardId로 기본 보드를 지정하세요.");
                boardId = globals[0].boardId;
            }

            if (!ClueBoardCatalog.TryGetBoard(boardId, out ClueBoardDefinition board, out string error))
                return Fail(error);
            if (board.kind != ClueBoardKind.Global)
                return Fail($"'{boardId}'는 글로벌 보드가 아닙니다(종류 {board.kind}).");

            CurrentScope = BoardScope.Global;
            return Open(board);
        }

        /// <summary>
        /// 현재 위치 맵의 로컬 보드를 연다. 최초 로컬 진입도 이 경로를 쓴다 —
        /// RouteModule.CurrentLocation의 MapNodeData.localBoardId가 유일한 기준이다.
        /// </summary>
        public bool ShowLocalForCurrentMap()
        {
            MapNodeData map = _currentMapProvider != null
                ? _currentMapProvider()
                : RouteModule.Instance != null ? RouteModule.Instance.CurrentLocation : null;

            if (map == null)
                return Fail("현재 위치 맵을 확인할 수 없어 로컬 보드를 열 수 없습니다.");

            // 누락·오타·Global 참조를 각각 다른 문구로 돌려준다(ClueBoardCatalog).
            if (!ClueBoardCatalog.TryGetLocalBoardForMap(map, out ClueBoardDefinition board, out string error))
                return Fail(error);

            _selectedLocalBoardId = null;
            CurrentScope = BoardScope.Local;
            SetLocalBoardListOpen(false);
            return Open(board);
        }

        /// <summary>
        /// 우측 탭 전환에 대응하는 진입점. 실패해도 이전 화면을 임의로 유지하지 않고 진단을 남긴다.
        /// 로컬은 목록에서 고른 보드가 있으면 그것, 없으면 현재 맵의 보드다(복원 후 다시 그리기도 이 경로).
        /// </summary>
        public bool Show(BoardScope scope)
        {
            if (scope == BoardScope.Global)
            {
                SetLocalBoardListOpen(false);
                return ShowGlobal();
            }
            return _selectedLocalBoardId != null ? ShowLocalBoard(_selectedLocalBoardId) : ShowLocalForCurrentMap();
        }

        // ─── 로컬 보드 목록 ──────────────────────────────────────

        public bool IsLocalBoardListOpen => _localBoardList != null && _localBoardList.gameObject.activeSelf;

        /// <summary>목록에 나열된 로컬 보드 ID(표시 순서). 검증/진단용.</summary>
        public IReadOnlyList<string> LocalBoardListEntries => _localBoardEntries;

        /// <summary>목록에서 고른 로컬 보드 ID. null이면 현재 맵의 보드.</summary>
        public string SelectedLocalBoardId => _selectedLocalBoardId;

        // 로컬 탭: 로컬이 아니면 현재 맵 보드로 진입, 이미 로컬이면 목록을 펼친다/접는다. 후보가 하나뿐이면 펼칠 게 없다.
        private void OnLocalTabClicked()
        {
            if (CurrentScope != BoardScope.Local || CurrentBoardId == null)
            {
                _selectedLocalBoardId = null;
                ShowLocalForCurrentMap();
                return;
            }
            if (IsLocalBoardListOpen) { SetLocalBoardListOpen(false); return; }
            SetLocalBoardListOpen(true);
        }

        /// <summary>
        /// 특정 로컬 보드를 연다(목록 선택). Local이 아닌 보드·없는 보드는 진단만 남긴다. 현재 맵의 보드를 고르면
        /// 선택을 비워 "현재 맵" 상태로 돌아간다.
        /// </summary>
        public bool ShowLocalBoard(string boardId)
        {
            if (!ClueBoardCatalog.TryGetBoard(boardId, out ClueBoardDefinition board, out string error))
                return Fail(error);
            if (board.kind != ClueBoardKind.Local)
                return Fail($"'{boardId}'는 로컬 보드가 아닙니다(종류 {board.kind}).");

            _selectedLocalBoardId = string.Equals(boardId, CurrentMapLocalBoardId, StringComparison.Ordinal) ? null : boardId;
            CurrentScope = BoardScope.Local;
            SetLocalBoardListOpen(false);
            return Open(board);
        }

        /// <summary>검증용 — 목록의 항목을 클릭한 것처럼 연다. 목록에 없는 ID면 false.</summary>
        public bool ClickLocalBoardEntry(string boardId)
        {
            if (!_localBoardEntries.Contains(boardId)) return false;
            return ShowLocalBoard(boardId);
        }

        public void SetLocalBoardListOpen(bool open)
        {
            if (_localBoardList == null) return;
            if (open) RebuildLocalBoardList();
            // 후보가 현재 맵 보드 하나뿐이면 펼칠 게 없다.
            _localBoardList.gameObject.SetActive(open && _localBoardEntries.Count > 1);
        }

        private MapNodeData CurrentMap =>
            _currentMapProvider != null ? _currentMapProvider() : RouteModule.Instance != null ? RouteModule.Instance.CurrentLocation : null;

        private string CurrentMapLocalBoardId => CurrentMap?.localBoardId;

        // 후보 = 공급원이 있으면 그것, 없으면 방문한 맵의 localBoardId. 어느 쪽이든 현재 맵의 보드를 맨 앞에 두고,
        // 카탈로그에 없거나 Local이 아닌 ID는 뺀다(맵 배선 오타는 ShowLocalForCurrentMap이 따로 진단한다).
        private void RebuildLocalBoardList()
        {
            _localBoardEntries.Clear();
            var ordered = new List<string>();
            string current = CurrentMapLocalBoardId;
            if (!string.IsNullOrWhiteSpace(current)) ordered.Add(current);

            IEnumerable<string> candidates = _localBoardListProvider != null ? _localBoardListProvider() : DefaultLocalBoardCandidates();
            if (candidates != null)
                foreach (string id in candidates)
                    if (!string.IsNullOrWhiteSpace(id) && !ordered.Contains(id)) ordered.Add(id);

            foreach (string id in ordered)
                if (ClueBoardCatalog.TryGetBoard(id, out ClueBoardDefinition board, out _) && board.kind == ClueBoardKind.Local)
                    _localBoardEntries.Add(id);

            ClueBoardUiKit.ClearChildren(_localBoardList);
            _localListGeneration++;
            foreach (string id in _localBoardEntries)
            {
                string boardId = id;
                bool isCurrentMap = string.Equals(boardId, current, StringComparison.Ordinal);
                bool isShowing = string.Equals(boardId, CurrentBoardId, StringComparison.Ordinal);
                string label = (isCurrentMap ? "● " : "  ") + DescribeLocalBoard(boardId);
                // 파괴 예약된 옛 항목과 이름이 겹치지 않게 세대 번호를 붙인다(Child()가 옛것을 찾지 않도록).
                Image entry = MakeTab(_localBoardList, $"Entry_{_localListGeneration}_{boardId}", label, _font, () => ShowLocalBoard(boardId));
                entry.color = isShowing ? ColTabOn : ColTabOff;
                var layout = entry.GetComponent<LayoutElement>();
                layout.preferredWidth = 0f;
                layout.preferredHeight = 12f;
                var text = entry.GetComponentInChildren<TextMeshProUGUI>();
                text.alignment = TextAlignmentOptions.MidlineLeft;
                text.margin = new Vector4(3f, 0f, 3f, 0f);
                text.overflowMode = TextOverflowModes.Ellipsis;
            }
        }

        private static IEnumerable<string> DefaultLocalBoardCandidates()
        {
            MapGraph graph = MapGraph.Instance;
            RouteProgressState progress = RouteModule.Instance?.Progress;
            if (graph == null || progress == null) yield break;
            foreach (MapNodeData map in graph.AllNodes)
                if (map != null && !string.IsNullOrWhiteSpace(map.localBoardId) && progress.IsNodeVisited(map))
                    yield return map.localBoardId;
        }

        // 보드에는 표시 이름이 없다. 그 보드를 가리키는 맵 이름으로 부르고, 맵이 없으면 보드 ID를 그대로 쓴다.
        private static string DescribeLocalBoard(string boardId)
        {
            MapGraph graph = MapGraph.Instance;
            if (graph != null)
                foreach (MapNodeData map in graph.AllNodes)
                    if (map != null && string.Equals(map.localBoardId, boardId, StringComparison.Ordinal) &&
                        !string.IsNullOrWhiteSpace(map.nodeName))
                        return map.nodeName;
            return boardId;
        }

        private void BuildLocalBoardList(RectTransform column, float stripWidth, float stripHeight)
        {
            _localBoardList = ClueBoardUiKit.Child(column, "LocalBoardList", out bool created);
            if (created)
            {
                _localBoardList.anchorMin = new Vector2(1f, 0f);
                _localBoardList.anchorMax = new Vector2(1f, 0f);
                _localBoardList.pivot = new Vector2(1f, 0f);
                _localBoardList.anchoredPosition = new Vector2(0f, stripHeight + 2f);
                _localBoardList.sizeDelta = new Vector2(stripWidth + 40f, 0f);
                var bg = _localBoardList.gameObject.AddComponent<Image>();
                bg.color = new Color(0.08f, 0.09f, 0.13f, 0.98f);
                bg.raycastTarget = true;
                var layout = _localBoardList.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(2, 2, 2, 2);
                layout.spacing = 2f;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
                var fitter = _localBoardList.gameObject.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
            // 항목은 열 때마다 후보로 다시 만든다.
            ClueBoardUiKit.ClearChildren(_localBoardList);
            _localBoardList.gameObject.SetActive(false);
        }

        private bool Open(ClueBoardDefinition board)
        {
            IEnumerable<string> acquired = _acquiredClueProvider != null
                ? _acquiredClueProvider()
                : RouteModule.Instance?.Progress?.AcquiredClueIds;

            IEnumerable<string> hallucinations = _hallucinationBoardProvider != null
                ? _hallucinationBoardProvider(board)
                : _hallucinationProvider?.Invoke();
            // 복원 목록: 검증용 덮어쓰기가 없으면 진행 저장소를 이 보드 정의와 대조한 결과다. 정의에 없거나
            // 다른 보드의 관계 ID, 쌍이 바뀐 ID는 여기서 걸러져 엔진(CreateState)에 닿지 않는다.
            IEnumerable<string> restored = _restoredConnectionProvider != null
                ? _restoredConnectionProvider(board.boardId)
                : ResolveRestoredConnections(board);

            _view.ShowBoard(board, acquired, hallucinations, restored);

            // [C06] 결과 배선은 보드마다 다르다. 진단은 여기서 한 번만 남기고(판정 때마다 반복하지 않는다) 잘못된
            // 항목은 빠진 카탈로그를 쓴다 — 결과 배선 오타가 선 연결을 막지 않는다.
            var outcomeDiagnostics = new List<string>();
            _outcomeCatalog = ClueBoardOutcomeCatalog.Build(board, outcomeDiagnostics);
            foreach (string message in outcomeDiagnostics) Debug.LogWarning("[ClueBoardScreen] " + message);

            LastDiagnostic = null;
            if (_statusText != null) _statusText.gameObject.SetActive(false);
            // 새 보드로 검색 결과와 하이라이트를 다시 계산한다(조건은 유지).
            // 검색/필터 임시 비활성: _searchPanel?.Recompute();
            RefreshTabs();
            OnBoardOpened?.Invoke(board.boardId);
            return true;
        }

        private List<string> ResolveRestoredConnections(ClueBoardDefinition board)
        {
            if (_progressOverride == null) return ClueBoardProgress.ResolveForBoard(board);
            var diagnostics = new List<string>();
            List<string> resolved = _progressOverride.ResolveForBoard(board, diagnostics);
            foreach (string message in diagnostics) Debug.LogWarning("[ClueBoardScreen] " + message);
            return resolved;
        }

        private bool Fail(string message)
        {
            LastDiagnostic = message;
            _view?.Clear();
            _outcomeCatalog = null;
            // 검색/필터 임시 비활성: _searchPanel?.Recompute();
            RefreshTabs();
            if (_statusText != null)
            {
                _statusText.text = message;
                _statusText.gameObject.SetActive(true);
            }
            Debug.LogWarning("[ClueBoardScreen] " + message);
            OnDiagnostic?.Invoke(message);
            return false;
        }
    }
}
