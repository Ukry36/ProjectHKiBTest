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

        [SerializeField] private string _globalBoardId = "";

        private ClueBoardView _view;
        private ClueBoardSearchPanel _searchPanel;
        private TextMeshProUGUI _statusText;
        private RectTransform _root;
        private Image _tabGlobal;
        private Image _tabLocal;
        private Image _tabSearch;

        private static readonly Color ColTabOff = new(0.14f, 0.17f, 0.24f, 0.98f);
        private static readonly Color ColTabOn = new(0.30f, 0.38f, 0.52f, 1f);

        // 획득 단서 공급원. 기본은 RouteModule의 진행 상태이며, 검증에서는 고정 집합을 주입한다
        // (세이브/진행 계층을 검증 코드가 흉내 내지 않아도 되게).
        private Func<IEnumerable<string>> _acquiredClueProvider;

        // 현재 위치 공급원. 기본은 RouteModule.CurrentLocation.
        private Func<MapNodeData> _currentMapProvider;

        // C07 환각 노드 공급원. 아직 구현이 없어 기본은 빈 집합이지만, 주입 경계는 남긴다.
        private Func<IEnumerable<string>> _hallucinationProvider;

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
            _root = new GameObject("ClueBoardScreen", typeof(RectTransform)).GetComponent<RectTransform>();
            _root.SetParent(parent, false);
            _root.anchorMin = Vector2.zero;
            _root.anchorMax = Vector2.one;
            _root.offsetMin = Vector2.zero;
            _root.offsetMax = Vector2.zero;

            _view = _root.gameObject.AddComponent<ClueBoardView>();
            _view.Initialize(_root, font);
            _view.OnConnectionEstablished += HandleConnectionEstablished;
            _view.OnEdgeClicked += HandleEdgeClicked;
            ClueBoardProgress.OnReplaced += HandleProgressReplaced;

            BuildSideColumn(_view.FilterBarRoot, font);

            var statusRect = new GameObject("StatusText", typeof(RectTransform)).GetComponent<RectTransform>();
            statusRect.SetParent(_root, false);
            statusRect.anchorMin = new Vector2(0f, 0f);
            statusRect.anchorMax = new Vector2(1f, 0f);
            statusRect.pivot = new Vector2(0.5f, 0f);
            statusRect.sizeDelta = new Vector2(0f, 32f);
            _statusText = statusRect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) _statusText.font = font;
            _statusText.fontSize = 8f;
            _statusText.color = new Color(0.90f, 0.55f, 0.50f);
            _statusText.alignment = TextAlignmentOptions.BottomLeft;
            _statusText.raycastTarget = false;
            _statusText.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_view != null)
            {
                _view.OnConnectionEstablished -= HandleConnectionEstablished;
                _view.OnEdgeClicked -= HandleEdgeClicked;
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
            ClueBoardOutcomeState outcomeState = null)
        {
            _acquiredClueProvider = acquiredClueProvider;
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

            IssueOutcomes(board, result.relationId);
        }

        // ─── 관계 결과(C06) ──────────────────────────────────────

        private Func<string, DreamReading> ReadingProvider =>
            _readingProvider ?? (id => DreamReadingModule.Instance != null ? DreamReadingModule.Instance.FindReading(id) : null);

        private Func<DreamReading, bool> RewardHandler =>
            _readingRewardHandler ?? (reading => DreamReadingModule.Instance != null && DreamReadingModule.Instance.TryResolveById(reading.id));

        // OnConnectionEstablished(실제 새 연결)에서만 불린다. 결과 없는 관계·손상 참조는 진단만 남기고 화면은 그대로다.
        private void IssueOutcomes(ClueBoardDefinition board, string relationId)
        {
            if (_outcomeCatalog == null || _outcomeCatalog.boardId != board.boardId)
                _outcomeCatalog = ClueBoardOutcomeCatalog.Build(board, null);

            _lastOutcomeDiagnostics.Clear();
            ClueBoardRuntimeState runtime = _view.RuntimeState;
            List<ClueBoardOutcomePresentation> issued = ClueBoardOutcomeResolver.Issue(
                _outcomeCatalog, runtime.IsConnected, relationId, Outcomes, ReadingProvider, RewardHandler, _lastOutcomeDiagnostics);
            foreach (string message in _lastOutcomeDiagnostics) Debug.LogWarning("[ClueBoardScreen] " + message);
            if (issued.Count == 0) return;

            foreach (ClueBoardOutcomePresentation presentation in issued)
                Debug.Log($"[ClueBoardScreen] 관계 결과 발행: board={board.boardId}, {presentation.outcome.kind} '{presentation.outcome.outcomeId}' → 해몽 '{presentation.reading.id}', reward={presentation.rewardGranted}");
            OnOutcomeIssued?.Invoke(issued);
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
                Debug.Log($"[ClueBoardScreen] 선 '{relationId}'에는 다시 볼 결과가 없습니다(결과 없는 관계·초기 연결·미발행).");
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
            column.sizeDelta = new Vector2(stripWidth, 0f);

            // 글로벌/로컬 탭 — 보드 오른쪽 아래에 가로로 고정한다.
            var strip = new GameObject("TabStrip", typeof(RectTransform)).GetComponent<RectTransform>();
            strip.SetParent(column, false);
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

            _tabGlobal = MakeTab(strip, "TabGlobal", "글로벌", font, () => Show(BoardScope.Global));
            _tabLocal = MakeTab(strip, "TabLocal", "로컬", font, () => Show(BoardScope.Local));
            // 검색/필터 임시 비활성. 아래 생성 코드는 기능을 다시 열 때 복구한다.
            // _tabSearch = MakeTab(strip, "TabSearch", "검색", font, ToggleSearch);
            // _searchPanel = column.gameObject.AddComponent<ClueBoardSearchPanel>();
            // _searchPanel.Initialize(column, font, _view, SearchPanelWidth);
            // _searchPanel.Root.anchoredPosition = new Vector2(-TabStripWidth, 0f);

            RefreshTabs();
        }

        private Image MakeTab(RectTransform parent, string name, string label, TMP_FontAsset font, Action onClick)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            var layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 25f;
            layout.preferredHeight = 28f;
            var image = rect.gameObject.AddComponent<Image>();
            image.color = ColTabOff;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => onClick());

            var textRect = new GameObject("Text", typeof(RectTransform)).GetComponent<RectTransform>();
            textRect.SetParent(rect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var text = textRect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.text = label;
            text.fontSize = 6f;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = false;
            text.raycastTarget = false;
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

            CurrentScope = BoardScope.Local;
            return Open(board);
        }

        /// <summary>우측 탭 전환에 대응하는 진입점. 실패해도 이전 화면을 임의로 유지하지 않고 진단을 남긴다.</summary>
        public bool Show(BoardScope scope) => scope == BoardScope.Global ? ShowGlobal() : ShowLocalForCurrentMap();

        private bool Open(ClueBoardDefinition board)
        {
            IEnumerable<string> acquired = _acquiredClueProvider != null
                ? _acquiredClueProvider()
                : RouteModule.Instance?.Progress?.AcquiredClueIds;

            IEnumerable<string> hallucinations = _hallucinationProvider?.Invoke();
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
