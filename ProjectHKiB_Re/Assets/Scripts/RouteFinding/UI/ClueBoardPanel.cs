using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

namespace RouteFinding.UI
{
    // 고정 단서 보드 창(C03-Host). NotePanel/CodexPanel과 같은 관례를 따른다 —
    // 같은 GameObject의 Window 컴포넌트가 IWindowContent 구현을 찾아 여닫기를 위임하고,
    // 창 스택 관리는 UIManager가 한다.
    //
    // [지금 단계의 위치] UIManager.windows 등록과 정식 단축키(Input Action)는 아직 하지 않았다.
    // 그래서 이 창은 "정식 경로가 준비되면 그대로 쓰이지만, 지금은 개발용 진입점으로만 열리는"
    // 상태다. 나중에 등록만 하면 Open()/Toggle()이 그대로 동작하도록 API를 먼저 맞춰 뒀다 —
    // 버려질 임시 하네스를 따로 만들지 않기 위해서다.
    //
    // [기존 노트와의 관계] NoteRouteGraphView/NoteModule/NotePanel의 코드를 쓰지 않는다.
    // 좌표의 주인(보드 정의 vs 사용자 드래그)과 연결 규칙(정의된 관계 vs 임의 토글)이 서로 달라,
    // 한쪽 편의로 합치면 두 화면 모두 신뢰할 수 없게 된다.
    //
    // [거절 코멘트] ClueBoardScreen.OnConnectionRejected(관계 없는 쌍)는 ClueBoardCommentToast로 하단에 1초 띄우고 스스로
    // 사라진다 — 입력을 막지 않는다. 결과 다이얼로그의 PresentComment는 남겨 두었지만 패널은 쓰지 않는다.
    // [C06 관계 결과] ClueBoardScreen.OnOutcomeIssued(새 발행)/OnOutcomeReviewRequested(선 클릭 재열람)를 받아
    // ClueBoardOutcomeDialog에 큐로 띄운다. 열릴 때는 보류분(발행됐지만 아직 못 보여 준 결과)을 한 번 띄우고,
    // 다이얼로그가 실제로 화면에 뜬 순간 viewed를 기록한다. 닫힐 때는 큐만 비운다 — 기록은 세션/세이브에 남는다.
    public class ClueBoardPanel : MonoBehaviour, IWindowContent
    {
        // UIManager.windows에 등록할 이름. 등록(씬 배선 + Input Action)은 정식 배선 단계에서 한다.
        // 기존 도감(ClueWindow)이 사용하던 UIManager 등록명과 OpenCodex 입력을 그대로 인수한다.
        // 저장 데이터/도감 모듈 이름과는 무관한 UI 창 식별자이므로 호환성을 위해 "Clue"를 유지한다.
        public const string WindowName = "Clue";

        [Header("표시")]
        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private Color _rootBgColor = new(0.04f, 0.05f, 0.09f, 0.96f);

        // 패널 프리팹(선택). NotePanel/CodexPanel과 같은 규칙 — 씬 자식 "ClueBoardPanelRoot"가 있으면 그것을, 없으면
        // 이 프리팹을 인스턴스화하고, 둘 다 없으면 런타임에 만든다.
        // 프리팹에 있는 컨테이너·버튼·카드는 **이름으로 찾아 그대로 쓴다**(배치·크기·색·글자 보존, ClueBoardUiKit 규칙):
        //   ClueBoardPanelRoot(배경) / BoardArea / ClueBoardScreen / ClueBoardView(EdgeLayer·NewVisualLayer·NodeLayer·
        //   NewTagLayer·FilterBar(TabStrip(TabGlobal·TabLocal)·LocalBoardList)·HintLayer) / StatusText / DevelopmentBar
        //   (DevBtn_닫기·ScopeLabel) / OutcomeDialog(Overlay·Card(Title·Body·Footer·BtnClose)) / CommentToast(Text) /
        //   ClueInfoPopup(Overlay·Card(Icon·Title·Subtitle·Body·BtnClose)).
        // 없는 것만 코드 기본값으로 만든다. 노드 카드·연결선·힌트 말풍선·로컬 목록 항목은 데이터라 매번 새로 그린다.
        [Tooltip("패널 프리팹. 같은 이름의 컨테이너/버튼/카드가 있으면 그 배치·크기·색을 쓰고, 없는 것만 코드로 만든다.")]
        [SerializeField] private GameObject _panelPrefab;

        [Tooltip("글로벌 탭이 열 보드 ID. 비우면 정의된 Global 보드가 정확히 한 장일 때만 자동으로 고른다")]
        [SerializeField] private string _globalBoardId = "";

        [Header("개발용 진입점 — C03-Host 임시. 정식 배선(UIManager 등록 + Input Action) 후 끌 것")]
        [Tooltip("켜면 화면 상단에 닫기 버튼 줄이 붙고 아래 개발용 단축키가 동작한다. 정식 배선 후 꺼야 한다")]
        [SerializeField] private bool _developmentControls = true;

        [Tooltip("개발용 열기/닫기 토글 키. None이면 키 입력을 받지 않는다(버튼과 OpenForDevelopment()만 사용)")]
        [SerializeField] private KeyCode _developmentHotkey = KeyCode.F9;

        [Tooltip("개발용 보드 정의(JSON TextAsset). 지정하면 Resources/clue_boards.json 대신 이걸 읽는다. " +
                 "운영 콘텐츠가 아직 없는 동안 화면을 띄워 보기 위한 자리이며, 정식 배선 후 비워야 한다")]
        [SerializeField] private TextAsset _developmentBoardDefinition;

        [Tooltip("개발용 기본 획득 단서 목록. 비어 있지 않으면 이 목록과 실제 플레이 중 새로 획득한 단서를 " +
                 "합쳐 잠김/실루엣/해금을 계산한다 — 기본 목록 자체는 세이브나 진행을 건드리지 않는다")]
        [SerializeField] private string[] _developmentAcquiredClueIds = Array.Empty<string>();

        [Tooltip("개발용 로컬 보드 ID. 지정하면 RouteModule의 현재 위치 대신 이 보드를 로컬로 연다 — " +
                 "맵/세이브 배선 없이 화면만 띄워 보기 위한 자리다. 비우면 평소대로 현재 맵의 localBoardId를 쓴다")]
        [SerializeField] private string _developmentLocalBoardId = "";

        private GameObject _panelGO;
        private ClueBoardScreen _screen;
        private ClueBoardOutcomeDialog _outcomeDialog;
        private ClueBoardCommentToast _commentToast;
        private ClueBoardClueInfoPopup _clueInfoPopup;
        private InputManager _inputManager;
        private TextMeshProUGUI _scopeLabel;
        private bool _built;
        private bool _subscribedToNewClues;
        private Window _window;

        public ClueBoardScreen Screen => _screen;
        public ClueBoardOutcomeDialog OutcomeDialog => _outcomeDialog;
        public ClueBoardCommentToast CommentToast => _commentToast;
        public ClueBoardClueInfoPopup ClueInfoPopup => _clueInfoPopup;

        // 거절 코멘트가 하단에 떠 있는 시간(초, 실시간). 기획: 닫기 없이 잠깐 보였다 사라진다.
        [SerializeField] private float _rejectionToastSeconds = 1f;
        public bool IsOpen => _panelGO != null && _panelGO.activeSelf;

#if UNITY_EDITOR
        /// <summary>
        /// NotePanel 프리팹 생성 도구와 같은 방식으로, Play Mode에서 완성된 런타임 UI 계층을
        /// Assets/Scripts/RouteFinding/UI/ClueBoardPanel.prefab으로 저장한다.
        /// 런타임에 생성되는 노드/연결선은 보드 콘텐츠이므로 저장 전에 현재 보드를 비운다.
        /// </summary>
        [NaughtyAttributes.Button("ClueBoardPanel 프리팹 생성 (플레이 중)", NaughtyAttributes.EButtonEnableMode.Playmode)]
        private void GeneratePanelPrefab()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[ClueBoardPanel] 프리팹 생성은 UI 계층이 완성되는 Play Mode에서 실행해주세요.");
                return;
            }

            if (_panelGO == null)
            {
                Debug.LogError("[ClueBoardPanel] ClueBoardPanelRoot가 없어 프리팹을 생성할 수 없습니다.");
                return;
            }

            // 플레이 중인 실제 패널은 건드리지 않고 복제본을 정리해 저장한다. ClueBoardView.Clear는
            // Play Mode에서 Destroy를 사용해 프레임 끝까지 자식이 남으므로 즉시 저장하면 테스트 노드가
            // 프리팹에 섞인다. 복제본의 동적 레이어만 DestroyImmediate로 비운다.
            GameObject prefabSource = Instantiate(_panelGO);
            prefabSource.name = _panelGO.name;
            prefabSource.SetActive(false);
            ClearPrefabDynamicLayer(prefabSource.transform, "NodeLayer");
            ClearPrefabDynamicLayer(prefabSource.transform, "EdgeLayer");

            int removedTotal = 0;
            foreach (Transform child in prefabSource.GetComponentsInChildren<Transform>(true))
                removedTotal += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
            if (removedTotal > 0)
                Debug.LogWarning($"[ClueBoardPanel] 프리팹 저장 전 missing script {removedTotal}개를 제거했습니다.");

            const string saveDir = "Assets/Scripts/RouteFinding/UI";
            const string savePath = saveDir + "/ClueBoardPanel.prefab";
            Directory.CreateDirectory(saveDir);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(prefabSource, savePath, out bool success);
            DestroyImmediate(prefabSource);
            if (!success || prefab == null)
            {
                Debug.LogError($"[ClueBoardPanel] 프리팹 저장에 실패했습니다: {savePath}");
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
            Debug.Log($"[ClueBoardPanel] 프리팹을 저장했습니다: {savePath}");
        }

        private static void ClearPrefabDynamicLayer(Transform root, string layerName)
        {
            Transform layer = FindPrefabChild(root, layerName);
            if (layer == null) return;
            for (int i = layer.childCount - 1; i >= 0; i--)
                DestroyImmediate(layer.GetChild(i).gameObject);
        }

        private static Transform FindPrefabChild(Transform root, string childName)
        {
            if (root.name == childName) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindPrefabChild(root.GetChild(i), childName);
                if (found != null) return found;
            }
            return null;
        }
#endif

        private void Awake()
        {
            _inputManager = FindObjectOfType<InputManager>();
            BuildUI();
            _panelGO.SetActive(false);

            EnsureOfficialRegistration();
            if (_inputManager != null) _inputManager.onOpenCodex += HandleOpenClueBoardInput;
        }

        private void Start() => EnsureOfficialRegistration();

        private void Update()
        {
            // 개발용 토글. 정식 Input Action을 아직 추가하지 않았으므로 레거시 Input을 쓴다
            // (ProjectSettings의 activeInputHandler가 Both라 둘 다 동작한다). 정식 배선이 들어오면
            // 이 블록과 _developmentHotkey를 함께 지운다.
            // 정식 Clue 창을 인수한 뒤에는 기존 OpenCodex(Input Actions)만 사용한다.
            // 특히 F9는 SaveTester.Load와 겹치므로 정식 등록 상태에서 개발용 폴링을 절대 실행하지 않는다.
            if (IsRegisteredWithUIManager()) return;
            if (!_developmentControls || _developmentHotkey == KeyCode.None) return;
            if (!Input.GetKeyDown(_developmentHotkey)) return;
            if (IsOpen) CloseForDevelopment();
            else OpenForDevelopment();
        }

        // ─── UIManager 경유 정식 진입점 ───────────────────────────
        // UIManager.windows에 등록되면 이 경로가 그대로 살아난다. 등록 전에는 UI가 null이거나
        // 이름을 찾지 못해 아무 일도 일어나지 않는다 — 그래서 개발용 경로를 따로 둔다(아래).

        public void Open()
        {
            EnsureOfficialRegistration();
            UI?.OpenWindow(WindowName);
        }

        public void Close()
        {
            EnsureOfficialRegistration();
            UI?.CloseWindow(WindowName);
        }

        public void Toggle()
        {
            EnsureOfficialRegistration();
            UI?.ToggleWindow(WindowName);
        }

        private static UIManager UI => GameManager.instance == null ? null : GameManager.instance.UIManager;

        private void HandleOpenClueBoardInput(InputAction.CallbackContext context)
        {
            if (context.performed) Toggle();
        }

        /// <summary>
        /// 기존 UIManager의 "Clue" 슬롯을 이 패널의 Window로 교체한다.
        /// 씬에 남아 있는 구 ClueWindow 프리팹에는 의존하지 않으며, 별도 씬 재배선 없이도
        /// 기존 N키/ESC/창 스택/현실 전용 제한/일시정지 규칙을 그대로 사용한다.
        /// </summary>
        private void EnsureOfficialRegistration()
        {
            UIManager ui = UI;
            if (ui?.windows == null) return;

            if (_window == null)
            {
                _window = GetComponent<Window>();
                if (_window == null) _window = gameObject.AddComponent<Window>();
                _window.isPopup = false;
                _window.pausesGame = true;
            }

            UIManager.WindowItem item = ui.windows.Find(candidate => candidate != null && candidate.name == WindowName);
            if (item == null)
            {
                item = new UIManager.WindowItem { name = WindowName };
                ui.windows.Add(item);
            }

            item.window = _window;
        }

        // ─── 개발용 진입점 ───────────────────────────────────────
        //
        // OpenForDevelopment은 월드·UIManager 상태와 분리된 검증 진입점이다. 실제 Open()/Toggle()은
        // 언제나 UIManager를 거쳐 창 스택·정지·현실 전용 제한을 적용한다. 반면 자동 검증은 꿈 상태의
        // 씬에서도 보드 화면 자체를 검사해야 하므로 여기서 그 제한을 타면 안 된다.

        /// <summary>개발용 열기. 씬의 Button.onClick에 그대로 연결할 수 있다.</summary>
        [ContextMenu("개발용: 보드 패널 열기")]
        public void OpenForDevelopment()
        {
            if (IsOpen) return;
            Debug.Log("[ClueBoardPanel] 개발/검증 경로로 엽니다 — UIManager의 창 제한을 적용하지 않습니다.");
            if (!CanOpenWindow) return;
            OpenWindowContent();
        }

        /// <summary>
        /// 개발용 닫기. 씬의 Button.onClick에 그대로 연결할 수 있다.
        /// UIManager가 이 창을 열어 둔 상태(이벤트의 OpenWindowAction·N키)라면 반드시 UIManager로 닫는다 — 내용만 직접
        /// 닫으면 창 스택에 "Clue"가 남아 WindowClosedDecision이 영영 참이 되지 않고(EVT-003이 노트를 덮어도 진행되지
        /// 않던 원인), 일시정지 사유와 입력 모드도 복구되지 않는다.
        /// </summary>
        [ContextMenu("개발용: 보드 패널 닫기")]
        public void CloseForDevelopment()
        {
            if (!IsOpen) return;
            UIManager ui = UI;
            if (ui != null && ui.IsWindowOpen(WindowName)) { ui.CloseWindow(WindowName); return; }
            CloseWindowContent();
        }

        private bool IsRegisteredWithUIManager()
        {
            UIManager ui = UI;
            return ui?.windows != null && ui.windows.Exists(item => item != null && item.name == WindowName);
        }

        // ─── IWindowContent — Window가 호출하는 실제 작업 ─────────

        // 노트와 같은 판단 — 이동 중에도 열람 자체는 막지 않는다. "현실에서만" 같은 제한은
        // UIManager.realWorldOnlyWindows가 담당하므로 여기서 중복 구현하지 않는다(정식 등록 시 적용).
        public bool CanOpenWindow => true;

        public void OpenWindowContent()
        {
            BuildUI();
            _panelGO.SetActive(true);

            EnsureBoardDefinitionsLoaded();
            ApplySources();

            // 최초 진입은 현재 위치 맵의 localBoardId 기준. 실패해도 다른 보드로 대체하지 않고
            // 화면과 로그에 원인을 남긴다(ClueBoardScreen이 사유를 네 가지로 구분한다).
            if (!_screen.ShowLocalForCurrentMap())
                Debug.LogWarning("[ClueBoardPanel] 로컬 보드를 열지 못했습니다 — " + _screen.LastDiagnostic);

            SubscribeToNewClues();
            RefreshScopeLabel();
            _inputManager?.MENUMode();

            // [C06] 발행됐지만 보여 주지 못한 결과가 있으면 지금 보여 준다(재발행이 아니라 보류분 표시 —
            // 판정·보상·기록은 거치지 않는다).
            _outcomeDialog?.Enqueue(_screen.CollectPendingOutcomes());
        }

        public void CloseWindowContent()
        {
            UnsubscribeFromNewClues();
            // 결과 큐는 화면 상태라 함께 비운다. 이미 띄운 결과는 viewed로 기록돼 있고, 아직 못 띄운 결과는
            // 기록에 남아 다음 열람 때 보류분으로 다시 뜬다.
            _outcomeDialog?.Clear();
            _commentToast?.Hide();
            _clueInfoPopup?.Hide();
            if (_panelGO != null) _panelGO.SetActive(false);
            // UIManager가 창 스택을 비울 때도 PLAYMode를 부르지만, 개발용 경로로 열었을 때는
            // 스택을 거치지 않으므로 여기서 직접 복구한다(NotePanel과 같은 관례).
            _inputManager?.PLAYMode();
        }

        // ─── 보드 전환 ───────────────────────────────────────────

        public void ShowGlobal() => ShowScope(ClueBoardScreen.BoardScope.Global);
        public void ShowLocal() => ShowScope(ClueBoardScreen.BoardScope.Local);

        private void ShowScope(ClueBoardScreen.BoardScope scope)
        {
            if (_screen == null) return;
            if (!_screen.Show(scope))
                Debug.LogWarning($"[ClueBoardPanel] {scope} 보드를 열지 못했습니다 — " + _screen.LastDiagnostic);
            RefreshScopeLabel();
        }

        private void RefreshScopeLabel()
        {
            if (_scopeLabel == null) return;
            string board = _screen.CurrentBoardId;
            _scopeLabel.text = string.IsNullOrEmpty(board)
                ? "표시 중인 보드 없음"
                : $"{(_screen.CurrentScope == ClueBoardScreen.BoardScope.Global ? "글로벌" : "로컬")} · {board}";
        }

        // ─── 데이터 공급원 ───────────────────────────────────────

        // 개발용 정의가 지정돼 있으면 그걸 읽고, 아니면 평소대로 Resources에서 읽는다.
        // 운영 clue_boards.json을 만들거나 고치지 않는다.
        private void EnsureBoardDefinitionsLoaded()
        {
            if (_developmentControls && _developmentBoardDefinition != null)
            {
                if (!ClueBoardCatalog.LoadFromJson(_developmentBoardDefinition.text))
                    Debug.LogError("[ClueBoardPanel] 개발용 보드 정의를 읽지 못했습니다 — " + ClueBoardCatalog.LoadError);
                return;
            }
            ClueBoardCatalog.EnsureLoaded();
        }

        // 개발용 획득 목록이 있으면 그걸 쓰고, 없으면 실제 진행 상태를 쓴다. 어느 쪽이든 **읽기만**
        // 하므로 세이브나 RouteProgressState를 바꾸지 않는다 — "전체 단서 획득" 디버그 버튼처럼
        // 실제 진행을 오염시키지 않고도 잠김/실루엣/해금을 한 화면에서 재현할 수 있다.
        private void ApplySources()
        {
            _screen.SetGlobalBoardId(_globalBoardId);
            CodexModule codex = CodexModule.Instance;
            // 기존 CodexPanel을 대체한 구성에서는 그 패널의 Open()이 더 이상 호출되지 않는다.
            // 여기서 진행 동기화와 늦은 RouteModule 이벤트 구독을 보장해야 이후 획득이 NEW로 들어온다.
            codex?.RebuildFromProgress();
            Func<string, bool> isNew = codex != null ? new Func<string, bool>(codex.IsClueNew) : null;
            // [C07] 환각 노드는 잠식 모듈이 보드별로 계산한다(슬롯의 clueId가 획득한 환각 단서인 노드). 모듈이 없으면 빈 집합.
            Func<ClueBoardDefinition, IEnumerable<string>> hallucinations = board =>
                DreamErosionModule.Instance?.GetHallucinationNodeIds(board) ?? Enumerable.Empty<string>();

            if (!_developmentControls)
            {
                _screen.ConfigureSources(
                    newClueProvider: isNew,
                    clueViewedHandler: codex != null ? new Action<string>(codex.MarkClueViewedFromBoard) : null,
                    hallucinationBoardProvider: hallucinations);
                return;
            }

            Func<IEnumerable<string>> acquired = _developmentAcquiredClueIds is { Length: > 0 }
                ? new Func<IEnumerable<string>>(ResolveDevelopmentAcquiredClues)
                : null;

            // 개발용 로컬 보드 ID를 지정하면 맵을 흉내 낸 값을 넘긴다. 진짜 맵 데이터를 만들거나
            // RouteModule을 건드리지 않으며, ClueBoardScreen 쪽 추론 금지 계약도 그대로다 —
            // "localBoardId 하나만 본다"는 규칙은 이 가짜 맵에도 똑같이 적용된다.
            Func<MapNodeData> currentMap = string.IsNullOrWhiteSpace(_developmentLocalBoardId)
                ? null
                : new Func<MapNodeData>(() => new MapNodeData { guid = "(개발용 맵)", localBoardId = _developmentLocalBoardId });

            _screen.ConfigureSources(
                acquiredClueProvider: acquired,
                currentMapProvider: currentMap,
                newClueProvider: isNew,
                clueViewedHandler: codex != null ? new Action<string>(codex.MarkClueViewedFromBoard) : null,
                hallucinationBoardProvider: hallucinations);
        }

        private IEnumerable<string> ResolveDevelopmentAcquiredClues()
        {
            var ids = new HashSet<string>(_developmentAcquiredClueIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            IReadOnlyCollection<string> actual = RouteModule.Instance?.Progress?.AcquiredClueIds;
            if (actual != null)
                foreach (string clueId in actual)
                    if (!string.IsNullOrEmpty(clueId)) ids.Add(clueId);
            return ids;
        }

        private void SubscribeToNewClues()
        {
            if (_subscribedToNewClues) return;
            CodexModule codex = CodexModule.Instance;
            if (codex == null) return;
            codex.OnNewClueAcquired += HandleNewClueAcquired;
            _subscribedToNewClues = true;
            Debug.Log($"[ClueNEW][BoardPanel] 신규 단서 이벤트 구독: open={IsOpen}, board={_screen?.CurrentBoardId ?? "(none)"}, frame={Time.frameCount}");
        }

        private void UnsubscribeFromNewClues()
        {
            if (!_subscribedToNewClues) return;
            CodexModule codex = CodexModule.Instance;
            if (codex != null) codex.OnNewClueAcquired -= HandleNewClueAcquired;
            _subscribedToNewClues = false;
        }

        private void OnDestroy()
        {
            UnsubscribeFromNewClues();
            ClueBoardOutcomes.OnReplaced -= HandleOutcomesReplaced;
            if (_inputManager != null) _inputManager.onOpenCodex -= HandleOpenClueBoardInput;
        }

        private void HandleNewClueAcquired(ClueData clue)
        {
            Debug.Log($"[ClueNEW][BoardPanel] 신규 단서 수신: clue={clue?.id ?? "(null)"}, open={IsOpen}, board={_screen?.CurrentBoardId ?? "(none)"}, frame={Time.frameCount}");
            // 닫힌 패널은 구독 자체를 끊으므로 모션을 예약하거나 재생하지 않는다.
            if (!IsOpen || clue == null || _screen == null || string.IsNullOrEmpty(_screen.CurrentBoardId))
            {
                Debug.Log($"[ClueNEW][BoardPanel] 화면 반영 생략: open={IsOpen}, clueNull={clue == null}, screenNull={_screen == null}, frame={Time.frameCount}");
                return;
            }

            ClueBoardScreen.BoardScope scope = _screen.CurrentScope;
            if (!_screen.Show(scope))
            {
                Debug.LogWarning("[ClueBoardPanel] 신규 단서 반영 중 현재 보드를 다시 열지 못했습니다 — " + _screen.LastDiagnostic);
                return;
            }

            // Show가 획득 목록을 다시 읽어 잠겨 있던 노드를 만든 뒤, 현재 보드의 같은 clueId 슬롯만 찾는다.
            // 운영 콘텐츠에 이 단서 슬롯이 없으면 데이터 갱신만 유지하고 연출은 생략한다.
            bool animated = _screen.View.AnimateClueFilled(clue.id);
            Debug.Log($"[ClueNEW][BoardPanel] 보드 재구축 완료: clue={clue.id}, board={_screen.CurrentBoardId}, isNew={CodexModule.Instance?.IsClueNew(clue.id)}, animated={animated}, frame={Time.frameCount}");
            RefreshScopeLabel();
        }

        // ─── UI 구축 ─────────────────────────────────────────────

        private void BuildUI()
        {
            if (_built) return;
            _built = true;

            EnsureRenderableContext();

            RectTransform root = AcquireRoot();
            RectTransform boardArea = EnsureBoardArea(root);
            ClearDynamicParts(root);

            _screen = ClueBoardUiKit.Ensure<ClueBoardScreen>(_panelGO);
            _screen.Initialize(boardArea, _font);
            // 우측 탭으로 보드가 바뀌어도 개발용 라벨이 따라오게 한다.
            _screen.OnBoardOpened += _ => RefreshScopeLabel();
            _screen.OnDiagnostic += _ => RefreshScopeLabel();

            if (_developmentControls) BuildDevelopmentBar(root);

            // [C06] 결과 다이얼로그는 개발용 줄보다 뒤(위)에 만들어 패널 전체를 덮는다.
            _outcomeDialog = ClueBoardUiKit.Ensure<ClueBoardOutcomeDialog>(_panelGO);
            _outcomeDialog.Initialize(root, _font);
            _outcomeDialog.OnPresented += _screen.MarkOutcomeViewed;
            // 거절 코멘트 토스트는 결과 다이얼로그보다 뒤(위)에 만들어 결과 카드가 떠 있어도 하단에 보인다.
            _commentToast = ClueBoardUiKit.Ensure<ClueBoardCommentToast>(_panelGO);
            _commentToast.Initialize(root, _font);
            // 단서 설명 팝업 — 노드 클릭으로 열고 닫기/바깥 클릭으로 닫는다. 결과 카드보다 앞에 두되 둘이 동시에 뜨는 일은
            // 없다(결과 카드가 떠 있으면 오버레이가 노드 클릭을 막는다).
            _clueInfoPopup = ClueBoardUiKit.Ensure<ClueBoardClueInfoPopup>(_panelGO);
            _clueInfoPopup.Initialize(root, _font);
            _screen.OnClueInfoRequested += HandleClueInfoRequested;
            _screen.OnOutcomeIssued += HandleOutcomes;
            _screen.OnOutcomeReviewRequested += HandleOutcomes;
            _screen.OnConnectionRejected += HandleRejection;
            _screen.OnConnectionCommented += HandleConnectionComment;
            // 세이브 복원/초기화로 발행 기록이 통째로 바뀌면 큐만 비운다 — 복원은 결과 화면을 다시 띄우지 않는다.
            ClueBoardOutcomes.OnReplaced += HandleOutcomesReplaced;
        }

        // 루트 확보: 씬 자식 재사용 → 프리팹 인스턴스 → 런타임 생성. 재사용/프리팹 루트는 배경 Image가 있으면 그 디자인을
        // 그대로 두고(레이캐스트만 켠다), 없으면 기본 색으로 하나 붙인다.
        private RectTransform AcquireRoot()
        {
            Transform existing = transform.Find("ClueBoardPanelRoot");
            if (existing != null)
            {
                Debug.Log("[ClueBoardPanel] BuildUI: 씬에 있던 ClueBoardPanelRoot를 재사용합니다(틀만 유지, 내부는 새로 그림).");
                _panelGO = existing.gameObject;
            }
            else if (_panelPrefab != null)
            {
                Debug.Log($"[ClueBoardPanel] BuildUI: 지정된 프리팹({_panelPrefab.name})을 인스턴스화합니다(틀만 유지, 내부는 새로 그림).");
                _panelGO = Instantiate(_panelPrefab, transform, false);
                _panelGO.name = "ClueBoardPanelRoot";
            }
            else
            {
                _panelGO = new GameObject("ClueBoardPanelRoot", typeof(RectTransform));
                _panelGO.transform.SetParent(transform, false);
            }

            var root = _panelGO.GetComponent<RectTransform>();
            if (root == null) root = _panelGO.AddComponent<RectTransform>();
            StretchFull(root);
            var background = _panelGO.GetComponent<Image>();
            if (background == null)
            {
                background = _panelGO.AddComponent<Image>();
                background.color = _rootBgColor;
            }
            // 배경이 레이캐스트를 받아야 보드 빈 곳에 드롭했을 때 드래그가 취소된다
            // (아래로 통과시키면 뒤에 있는 게임 오브젝트가 클릭을 받는다).
            background.raycastTarget = true;
            return root;
        }

        // 보드가 들어갈 자리. 프리팹/씬에 "BoardArea"가 있으면 그 사각형(여백 등)을 그대로 쓰고, 없으면 전체를 채우되
        // 개발용 줄 높이만큼 위를 비운다.
        private RectTransform EnsureBoardArea(RectTransform root)
        {
            Transform found = FindDeep(root, "BoardArea");
            if (found != null) return (RectTransform)found;

            var boardArea = new GameObject("BoardArea", typeof(RectTransform)).GetComponent<RectTransform>();
            boardArea.SetParent(root, false);
            boardArea.anchorMin = Vector2.zero;
            boardArea.anchorMax = Vector2.one;
            boardArea.offsetMin = Vector2.zero;
            boardArea.offsetMax = new Vector2(0f, _developmentControls ? -DevBarHeight : 0f);
            return boardArea;
        }

        // 프리팹/씬 루트에 구워진 동적 파트(플레이 중 저장한 프리팹에 남는다)를 지운다. 이 파트들은 아래에서 전부 새로
        // 만들므로 남겨 두면 화면·컴포넌트가 두 벌이 된다. 먼저 비활성화한 뒤 Destroy(파괴 예약 중 이름 충돌·이벤트 방지).
        // 프리팹/씬 루트에 구워진 **런타임 컴포넌트**만 떼어낸다(플레이 중 저장한 프리팹에 남는다). 오브젝트·배치·색은
        // 그대로 두고 아래 Initialize들이 같은 오브젝트에 컴포넌트를 다시 붙여 콜백을 건다. 즉시 파괴해야 같은 프레임의
        // Ensure<T>()가 옛 컴포넌트를 집지 않는다.
        private void ClearDynamicParts(RectTransform root)
        {
            foreach (Component stale in root.GetComponentsInChildren<Component>(true))
            {
                if (stale is ClueBoardScreen || stale is ClueBoardView || stale is ClueBoardOutcomeDialog ||
                    stale is ClueBoardCommentToast || stale is ClueBoardClueInfoPopup || stale is ClueBoardDragLinkController ||
                    stale is ClueBoardDragNode || stale is ClueBoardNodeClick || stale is ClueBoardSilhouetteHover ||
                    stale is ClueBoardNewVisual || stale is ClueBoardEdgeClick)
                    DestroyImmediate(stale);
            }
        }

        private static Transform FindDeep(Transform parent, string name)
        {
            if (parent == null) return null;
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                Transform inner = FindDeep(child, name);
                if (inner != null) return inner;
            }
            return null;
        }

        private void HandleOutcomesReplaced() => _outcomeDialog?.Clear();

        private void HandleClueInfoRequested(ClueData clue)
        {
            if (!IsOpen || _clueInfoPopup == null) return;
            _clueInfoPopup.Show(clue);
        }

        // 연결 성공 코멘트(해몽 결과가 없는 관계). 결과 카드와 같은 카드로 띄우고 "확인"으로 닫는다 — 선을 다시 누르면
        // 같은 문구가 다시 뜬다. 결과 카드가 이미 떠 있으면(체인 결과 큐 등) 그 뒤로 미루지 않고 건너뛴다.
        private void HandleConnectionComment(ClueBoardConnectionComment comment)
        {
            if (!IsOpen || _outcomeDialog == null || comment == null) return;
            _outcomeDialog.PresentMessage(ClueSystemSettings.RejectionTitle, comment.text,
                comment.isNew ? "관계 연결" : "관계 연결 · 재열람");
        }

        // 관계 없는 쌍의 거절 코멘트. 결과 큐를 타지 않고 곧장 뜬다 — 기록도 보상도 없으므로 같은 쌍을 또 떨어뜨리면 또 나온다.
        private void HandleRejection(ClueBoardRejectionComment comment)
        {
            if (!IsOpen || _commentToast == null || comment == null) return;
            _commentToast.Show(comment.text, _rejectionToastSeconds);
        }

        // 새 발행과 재열람이 같은 큐를 탄다. 판정·보상·기록은 ClueBoardScreen이 이미 끝냈고 여기서는 보여 주기만 한다.
        private void HandleOutcomes(IReadOnlyList<ClueBoardOutcomePresentation> presentations)
        {
            if (!IsOpen || _outcomeDialog == null) return;
            _outcomeDialog.Enqueue(presentations);
        }

        private const float DevBarHeight = 22f;

        // **개발용 임시 줄**이다. 글로벌/로컬 전환과 검색은 C03-B 정식 탭(ClueBoardScreen의 우측 열)이
        // 맡으므로 여기에는 정식 배선 전까지 필요한 '닫기'와 진단 라벨만 남긴다.
        private void BuildDevelopmentBar(RectTransform parent)
        {
            // 프리팹 우선: DevelopmentBar/DevBtn_닫기/ScopeLabel이 있으면 그 배치·크기·색을 쓰고 콜백만 다시 건다.
            RectTransform bar = ClueBoardUiKit.Child(parent, "DevelopmentBar", out bool created);
            if (created)
            {
                bar.anchorMin = new Vector2(0f, 1f);
                bar.anchorMax = new Vector2(1f, 1f);
                bar.pivot = new Vector2(0.5f, 1f);
                bar.sizeDelta = new Vector2(0f, DevBarHeight);
                var barBg = bar.gameObject.AddComponent<Image>();
                barBg.color = new Color(0.10f, 0.12f, 0.18f, 0.98f);

                var layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = 4f;
                layout.padding = new RectOffset(4, 4, 3, 3);
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = true;
                layout.childAlignment = TextAnchor.MiddleLeft;
            }

            MakeDevButton(bar, "닫기", CloseForDevelopment);

            RectTransform labelRect = ClueBoardUiKit.Child(bar, "ScopeLabel", out created);
            if (created) labelRect.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            _scopeLabel = ClueBoardUiKit.Text(labelRect, created, _font, 8f, new Color(0.70f, 0.74f, 0.80f),
                TextAlignmentOptions.MidlineLeft, "(개발용) 표시 중인 보드 없음");
        }

        private void MakeDevButton(RectTransform parent, string label, Action onClick)
        {
            RectTransform rect = ClueBoardUiKit.Child(parent, "DevBtn_" + label, out bool created);
            if (created) rect.gameObject.AddComponent<LayoutElement>().preferredWidth = 46f;
            var image = ClueBoardUiKit.Ensure<Image>(rect.gameObject);
            if (created) image.color = new Color(0.20f, 0.26f, 0.36f, 1f);
            var button = ClueBoardUiKit.Ensure<Button>(rect.gameObject);
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick());

            RectTransform textRect = ClueBoardUiKit.Child(rect, "Text", out bool textCreated);
            if (textCreated) StretchFull(textRect);
            ClueBoardUiKit.Text(textRect, textCreated, _font, 8f, Color.white, TextAlignmentOptions.Center, label);
        }

        // 드래그 연결은 Canvas + GraphicRaycaster + EventSystem이 모두 있어야 동작한다.
        // 하나라도 없으면 드래그가 "조용히 아무 일도 안 하는" 상태가 되어 원인을 찾기 어렵다 —
        // 개발용으로 열 때만 부족한 것을 채우고, 무엇을 만들었는지 로그로 남긴다.
        private void EnsureRenderableContext()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                var canvasObject = new GameObject("ClueBoardDevCanvas",
                    typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                transform.SetParent(canvasObject.transform, false);
                var self = transform as RectTransform;
                if (self != null) StretchFull(self);
                Debug.Log("[ClueBoardPanel] 부모 Canvas가 없어 개발용 Canvas를 만들었습니다. " +
                          "정식 배선에서는 게임 UI Canvas 아래에 두세요.");
            }
            else if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
                Debug.LogWarning("[ClueBoardPanel] Canvas에 GraphicRaycaster가 없어 추가했습니다 — 없으면 드래그가 동작하지 않습니다.");
            }

            if (EventSystem.current == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                Debug.Log("[ClueBoardPanel] EventSystem이 없어 만들었습니다 — 드래그 입력에 필요합니다.");
            }
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
