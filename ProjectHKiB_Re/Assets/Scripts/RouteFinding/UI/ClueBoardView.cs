using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RouteFinding.UI
{
    // C03 고정 단서 보드 화면 — 보드 한 장을 그린다.
    //
    // [기존 노트와의 관계] NoteRouteGraphView/NoteModule/NotePanel과 코드를 공유하지 않는다.
    // 그쪽은 자유 배치 + 맵 노드와 단서를 섞은 임의 링크(ToggleClueLink)를 다루고, 이쪽은
    // 고정 슬롯 + 정의된 관계만 다룬다. 두 규칙을 한 뷰에 합치면 "좌표의 주인이 누구인가"와
    // "무엇이 연결 가능한가"가 화면마다 달라진다.
    //
    // [좌표] ClueBoardSlot.anchoredPosition을 그대로 쓴다. 노드를 끌어 옮기는 입력도, 좌표를
    // 저장하는 경로도 만들지 않는다 — 드래그의 의미는 오직 "선 연결"이다.
    //
    // [레이어 순서] Edge → NEW 노란 배경 → 단서 노드 → NEW 글자 → 필터 순서로 만든다. uGUI는
    // 형제 순서대로 그리므로 강조 배경이 카드를 덮지 않고, NEW 글자는 카드보다 앞에 보인다.
    // FilterBarRoot(우측 탭·검색 열, C03-B)는 마지막에 두어 노드 위에 그려지고 클릭도 먼저 받는다.
    //
    // [하이라이트(C03-B)] 검색/필터 결과는 **표시에만** 관여한다. 맞지 않는 노드와 선은 흐려질 뿐
    // 드래그 시작·드롭 대상·판정 규칙은 전혀 바뀌지 않는다 — 필터가 연결 가능 여부를 바꾸면
    // "화면에서 흐린데 왜 연결되나/안 되나"가 판정 계약과 어긋난다.
    //
    // [선 클릭(C06)] 성립한 선은 클릭할 수 있다(OnEdgeClicked). 선은 EdgeLayer(노드 뒤)에 있고 노드가 위를 덮으므로
    // 노드 위 조작은 언제나 노드가 받는다. 드롭 판정(ClueBoardDragLinkController.FindDropTarget)은 RaycastAll 결과 중
    // ClueBoardDragNode가 붙은 것만 찾으므로 선이 맞아도 드롭 대상이 되지 않고, 선에서 시작한 드래그는 IDragHandler가
    // 없어 시작조차 되지 않는다. 즉 선 클릭은 새 연결 시도와 완전히 분리된 입력이다.
    //
    // [실루엣 힌트] 실루엣 노드에 포인터를 올리면 해금 힌트(ClueBoardSilhouetteHint)를 말풍선으로 띄운다. 말풍선은
    // 맨 앞 HintLayer에 두고 레이캐스트를 받지 않는다 — 노드 위를 덮으면 호버가 끊겨 깜빡이고, 드롭 판정을 가로챌 수
    // 있어서다. 힌트는 이름·아이콘을 대신 노출하지 않으며, 실루엣이 드롭 대상이 아니라는 규칙도 바꾸지 않는다.
    public sealed class ClueBoardView : MonoBehaviour
    {
        // 연결 시도 결과 전부(성공·재시도·거절). 로그/진단용 경계다 — 거절·재시도까지 포함하므로 보상·코멘트·
        // 해몽 결과 발행에 쓰면 안 된다.
        public event Action<ClueBoardConnectResult> OnConnectionResult;

        // 이번 조작으로 **실제로 새 연결이 생겼을 때만** 발행. 보상·코멘트처럼 한 번만 일어나야
        // 하는 후속 처리는 이쪽을 구독한다 — 재시도(AlreadyConnected)에서는 발행되지 않는다.
        public event Action<ClueBoardConnectResult> OnConnectionEstablished;

        // [C06] 성립한 연결선을 클릭했을 때(인자: relationId). 새 연결 시도와 무관하며 판정을 부르지 않는다.
        public event Action<string> OnEdgeClicked;

        // 해금 노드를 **드래그 없이** 클릭했을 때(인자: nodeId). EventSystem은 드래그가 시작되면 클릭을 주지 않으므로
        // 연결 드래그와 겹치지 않는다. 실루엣·잠김은 오지 않는다(실루엣은 호버 힌트만).
        public event Action<string> OnNodeClicked;

        // 320x240 보드에서 우측 탭을 제외하고 5열 x 3행이 들어가는 트럼프 카드형 비율(3:5).
        // 권장 슬롯 중심은 X=28/88/148/208/268, Y=-34/-108/-182다.
        [SerializeField] private float _nodeWidth = 36f;
        [SerializeField] private float _nodeHeight = 60f;
        [SerializeField] private float _defaultNodeFontSize = 6.5f;
        [SerializeField] private float _edgeThickness = 3f;
        [Tooltip("연결선 클릭 판정 폭. 선 두께보다 넓게 두어 가는 선도 클릭할 수 있게 한다")]
        [SerializeField] private float _edgeHitThickness = 10f;
        [SerializeField] private Color _colUnlockedNode = new(0.20f, 0.24f, 0.32f, 0.95f);
        [SerializeField] private Color _colSilhouetteNode = new(0.12f, 0.13f, 0.16f, 0.85f);
        [SerializeField] private Color _colHallucinationNode = new(0.06f, 0.06f, 0.07f, 0.95f);
        [Tooltip("검색/필터에 맞지 않는 노드·선의 투명도. 1이면 흐려지지 않는다")]
        [SerializeField] private float _dimmedAlpha = 0.22f;
        [SerializeField] private Color _colHighlightOutline = new(0.95f, 0.85f, 0.40f, 0.95f);

        // 실루엣 힌트 말풍선. 폭을 넘으면 줄바꿈되고 높이는 글 길이에 맞춘다.
        [SerializeField] private float _hintMaxWidth = 110f;
        [SerializeField] private float _hintFontSize = 6f;
        [SerializeField] private Color _colHintBackground = new(0.05f, 0.05f, 0.08f, 0.95f);
        [SerializeField] private Color _colHintText = new(0.85f, 0.83f, 0.78f, 1f);
        // 작성된 힌트가 없는 실루엣에 쓰는 문구. 비우면 ClueBoardSilhouetteHint.DefaultHint.
        [SerializeField] [TextArea] private string _defaultSilhouetteHint = ClueBoardSilhouetteHint.DefaultHint;
        private const float HintPadding = 3f;
        private const float HintGap = 3f;

        private RectTransform _root;
        private RectTransform _edgeLayer;
        private RectTransform _newVisualLayer;
        private RectTransform _nodeLayer;
        private RectTransform _newTagLayer;
        private RectTransform _hintLayer;
        private RectTransform _hintRect;
        private TextMeshProUGUI _hintText;
        private string _hoveredSilhouetteNodeId;
        private ClueBoardDragLinkController _dragController;
        private TMP_FontAsset _font;

        private ClueBoardDefinition _definition;
        private ClueBoardNodeStates _states;
        private ClueBoardConnectionEngine _engine;
        private ClueBoardRuntimeState _runtimeState;

        private readonly Dictionary<string, RectTransform> _nodeVisuals = new(StringComparer.Ordinal);
        private List<string> _acquiredClueIds = new();
        private List<string> _hallucinationNodeIds = new();
        private readonly Dictionary<string, ClueBoardSlot> _slotsByNodeId = new(StringComparer.Ordinal);
        private readonly List<EdgeVisual> _edgeVisuals = new();

        // 하이라이트 중인 노드 집합. null이면 하이라이트 없음(전부 원래 밝기).
        private HashSet<string> _highlightedNodeIds;

        // 단서 조회. 기본은 MapGraph이며 검증·다른 진행 계층이 갈아끼울 수 있다. 검색 패널도 같은
        // 조회를 써야 "화면에 보이는 이름"과 "검색되는 이름"이 어긋나지 않는다.
        private Func<string, ClueData> _clueResolver;
        private Func<string, bool> _newClueProvider;
        private Action<string> _clueViewedHandler;

        private readonly struct EdgeVisual
        {
            public readonly RectTransform rect;
            public readonly string relationId;
            public readonly string firstNodeId;
            public readonly string secondNodeId;
            public EdgeVisual(RectTransform rect, string relationId, string firstNodeId, string secondNodeId)
            {
                this.rect = rect;
                this.relationId = relationId;
                this.firstNodeId = firstNodeId;
                this.secondNodeId = secondNodeId;
            }
        }

        public string BoardId => _definition?.boardId;
        public ClueBoardDefinition Definition => _definition;
        public ClueBoardRuntimeState RuntimeState => _runtimeState;
        public ClueBoardNodeStates NodeStates => _states;
        public Func<string, ClueData> ClueResolver => _clueResolver ?? DefaultResolveClue;

        /// <summary>우측 탭·검색 열(C03-B)이 앉는 자리. 노드 위에 그려지도록 마지막 형제로 둔다.</summary>
        public RectTransform FilterBarRoot { get; private set; }

        /// <summary>단서 조회를 갈아끼운다(null이면 MapGraph). 표시 중인 보드는 다시 그려야 반영된다.</summary>
        public void SetClueResolver(Func<string, ClueData> resolver) => _clueResolver = resolver;

        public void SetNewClueState(Func<string, bool> provider, Action<string> viewedHandler)
        {
            _newClueProvider = provider;
            _clueViewedHandler = viewedHandler;
            Debug.Log($"[ClueNEW][BoardView] 상태 공급 연결: provider={provider != null}, viewedHandler={viewedHandler != null}, frame={Time.frameCount}");
        }

        public void Initialize(RectTransform parent, TMP_FontAsset font)
        {
            _font = font;
            // 프리팹 우선: 레이어 컨테이너는 있으면 재사용하고 없을 때만 만든다. 레이어 안 내용(노드·선·말풍선)은 언제나
            // 데이터로 다시 그리므로 여기서 비운다.
            _root = ClueBoardUiKit.Child(parent, "ClueBoardView", out bool created);
            if (created) StretchFull(_root);

            // 선 → NEW 배경 → 노드 → NEW 글자 → 우측 열 → 힌트 순서(새로 만들 때의 형제 순서; 프리팹은 그 순서를 따른다).
            _edgeLayer = EnsureLayer(_root, "EdgeLayer");
            // 금색 글로우/테두리는 카드보다 먼저 그려져 카드 뒤에 깔린다.
            _newVisualLayer = EnsureLayer(_root, "NewVisualLayer");
            _nodeLayer = EnsureLayer(_root, "NodeLayer");
            // NEW 글자는 카드보다 뒤에 생성해 항상 카드 앞에 표시한다.
            _newTagLayer = EnsureLayer(_root, "NewTagLayer");

            // 우측 세로 열. 폭은 ClueBoardScreen이 탭/검색 패널을 채우며 정한다. 슬롯 좌표는 기획
            // 데이터라 이 열을 피해 두는 것은 콘텐츠 몫이며, 여기서 노드를 밀어내지 않는다.
            FilterBarRoot = ClueBoardUiKit.Child(_root, "FilterBar", out created);
            if (created)
            {
                FilterBarRoot.anchorMin = new Vector2(1f, 0f);
                FilterBarRoot.anchorMax = new Vector2(1f, 1f);
                FilterBarRoot.pivot = new Vector2(1f, 0.5f);
                FilterBarRoot.sizeDelta = new Vector2(0f, 0f);
            }

            // 실루엣 힌트 말풍선은 우측 열보다도 앞에 그린다(마지막 형제). 레이캐스트는 받지 않으므로 탭 클릭을 막지 않는다.
            _hintLayer = EnsureLayer(_root, "HintLayer");
            _hintRect = null;
            _hintText = null;

            // 드래그 컨트롤러를 EdgeLayer에 붙인다 — 그 컴포넌트는 _edgeLayer가 비어 있으면 자신의
            // RectTransform을 쓰므로, 임시 선이 자연히 선 레이어(=노드 뒤)에 그려진다.
            _dragController = ClueBoardUiKit.Ensure<ClueBoardDragLinkController>(_edgeLayer.gameObject);
            _dragController.OnConnectionRequested -= HandleConnectionRequested;
            _dragController.OnConnectionRequested += HandleConnectionRequested;
        }

        // 레이어 컨테이너: 있으면 재사용(안은 비움), 없으면 전체 스트레치로 만든다.
        private static RectTransform EnsureLayer(RectTransform root, string name)
        {
            RectTransform layer = ClueBoardUiKit.Child(root, name, out bool created);
            if (created) StretchFull(layer);
            else ClueBoardUiKit.ClearChildren(layer);
            return layer;
        }

        private void OnDestroy()
        {
            if (_dragController != null) _dragController.OnConnectionRequested -= HandleConnectionRequested;
        }

        /// <summary>
        /// 보드 한 장을 표시한다. 초기 연결(initialRelationIds)과 복원된 연결은 엔진의 런타임 상태에
        /// 이미 들어 있으므로, 이 호출만으로 화면에도 함께 그려진다.
        /// </summary>
        public void ShowBoard(
            ClueBoardDefinition definition,
            IEnumerable<string> acquiredClueIds,
            IEnumerable<string> hallucinationNodeIds = null,
            IEnumerable<string> restoredRelationIds = null)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));

            _definition = definition;
            _engine = new ClueBoardConnectionEngine(definition);
            // 체인 공개(revealSilhouetteNodeIds)는 성립한 관계 집합이 필요하다. 엔진이 초기 연결·복원을 합쳐 정하는 집합을
            // 그대로 쓰기 위해 런타임 상태를 먼저 만들고, 그 연결 집합으로 표시를 계산한 뒤 다시 런타임 상태를 만든다 —
            // 표시(실루엣)와 판정(실루엣 거절)이 같은 집합을 보게 하기 위해서다.
            _acquiredClueIds = acquiredClueIds == null ? new List<string>() : new List<string>(acquiredClueIds);
            _hallucinationNodeIds = hallucinationNodeIds == null ? new List<string>() : new List<string>(hallucinationNodeIds);
            ClueBoardRuntimeState seed = _engine.CreateState(null, null, null, restoredRelationIds);
            _states = ClueBoardNodeStates.Build(definition, _acquiredClueIds, _hallucinationNodeIds, seed.connectedRelationIds);
            _runtimeState = _states.CreateRuntimeState(_engine, restoredRelationIds);

            // 노드 집합이 바뀌므로 이전 보드의 하이라이트는 버린다. 검색 패널이 새 보드로 다시 계산해 넣는다.
            _highlightedNodeIds = null;
            RebuildNodes();
            RebuildEdges();
        }

        public void Clear()
        {
            _definition = null;
            _engine = null;
            _states = null;
            _runtimeState = null;
            _highlightedNodeIds = null;
            HideSilhouetteHint();
            ClearChildren(_nodeLayer);
            ClearChildren(_newVisualLayer);
            ClearChildren(_newTagLayer);
            _nodeVisuals.Clear();
            _slotsByNodeId.Clear();
            ClearEdges();
        }

        // ─── 하이라이트(C03-B) ───────────────────────────────────

        public bool HasHighlight => _highlightedNodeIds != null;
        public bool IsHighlighted(string nodeId) =>
            _highlightedNodeIds != null && nodeId != null && _highlightedNodeIds.Contains(nodeId);

        /// <summary>
        /// 주어진 노드만 강조하고 나머지 노드·선은 흐리게 한다. null이면 하이라이트를 끈다.
        /// 선은 양 끝 중 하나라도 강조 노드면 원래 밝기다 — 찾은 단서가 어디와 이어졌는지가 보여야 한다.
        /// 연결 규칙에는 손대지 않는다(흐린 노드도 그대로 드래그·드롭된다).
        /// </summary>
        public void SetHighlight(IEnumerable<string> nodeIds)
        {
            _highlightedNodeIds = nodeIds == null ? null : new HashSet<string>(nodeIds, StringComparer.Ordinal);
            ApplyHighlight();
        }

        public void ClearHighlight() => SetHighlight(null);

        private void ApplyHighlight()
        {
            foreach (KeyValuePair<string, RectTransform> pair in _nodeVisuals)
            {
                bool lit = _highlightedNodeIds == null || _highlightedNodeIds.Contains(pair.Key);
                SetAlpha(pair.Value, lit ? 1f : _dimmedAlpha);
                // 테두리는 "필터가 켜져 있고 맞는 노드"에만. 필터가 없을 때까지 테두리를 두르면
                // 강조가 아니라 기본 모양이 된다.
                var outline = pair.Value.GetComponent<Outline>();
                if (outline != null) outline.enabled = _highlightedNodeIds != null && lit;
            }
            foreach (EdgeVisual edge in _edgeVisuals)
            {
                if (edge.rect == null) continue;
                bool lit = _highlightedNodeIds == null ||
                           _highlightedNodeIds.Contains(edge.firstNodeId) ||
                           _highlightedNodeIds.Contains(edge.secondNodeId);
                SetAlpha(edge.rect, lit ? 1f : _dimmedAlpha);
            }
        }

        // CanvasGroup은 blocksRaycasts/interactable을 기본값(true)으로 두므로 흐려져도 입력은 그대로다.
        private static void SetAlpha(RectTransform rect, float alpha)
        {
            var group = rect.GetComponent<CanvasGroup>();
            if (group == null) group = rect.gameObject.AddComponent<CanvasGroup>();
            group.alpha = alpha;
        }

        /// <summary>검증용. 노드가 없으면 -1.</summary>
        public float GetNodeAlpha(string nodeId)
        {
            if (nodeId == null || !_nodeVisuals.TryGetValue(nodeId, out RectTransform node)) return -1f;
            var group = node.GetComponent<CanvasGroup>();
            return group != null ? group.alpha : 1f;
        }

        /// <summary>검증용. 순서 무관 쌍의 선이 없으면 -1.</summary>
        public float GetEdgeAlpha(string firstNodeId, string secondNodeId)
        {
            var pair = new ClueBoardNodePair(firstNodeId, secondNodeId);
            foreach (EdgeVisual edge in _edgeVisuals)
            {
                if (edge.rect == null || !new ClueBoardNodePair(edge.firstNodeId, edge.secondNodeId).Equals(pair)) continue;
                var group = edge.rect.GetComponent<CanvasGroup>();
                return group != null ? group.alpha : 1f;
            }
            return -1f;
        }

        // ─── 노드 ────────────────────────────────────────────────

        private void RebuildNodes()
        {
            // 노드가 파괴되면 PointerExit가 오지 않으므로 말풍선을 먼저 내린다(획득 직후 재구축 때 실루엣이 해금으로 바뀐다).
            HideSilhouetteHint();
            ClearChildren(_nodeLayer);
            ClearChildren(_newVisualLayer);
            ClearChildren(_newTagLayer);
            _nodeVisuals.Clear();
            _slotsByNodeId.Clear();

            foreach (ClueBoardSlot slot in _definition.slots)
            {
                if (slot == null || string.IsNullOrEmpty(slot.nodeId)) continue;
                _slotsByNodeId[slot.nodeId] = slot;

                ClueBoardNodeVisibility visibility = _states.Get(slot.nodeId);
                // 잠김은 자리도 만들지 않는다 — 빈 칸이 남으면 "여기에 무언가 있다"가 새어 나간다.
                if (visibility == ClueBoardNodeVisibility.Locked) continue;

                bool hallucination = _states.IsHallucination(slot.nodeId);
                RectTransform node = BuildNode(slot, visibility, hallucination);
                _nodeVisuals[slot.nodeId] = node;
            }
        }

        private RectTransform BuildNode(ClueBoardSlot slot, ClueBoardNodeVisibility visibility, bool hallucination)
        {
            ClueData clue = ResolveClue(slot.clueId);
            float sizePercent = ResolveSizePercent(slot, clue);
            float fontSize = ResolveFontSize(slot, clue);
            string fontAddress = ResolveFontAddress(slot, clue);
            bool hideLabel = ResolveHideLabel(slot, clue);
            var rect = NewRect(_nodeLayer, "Node_" + slot.nodeId);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            float sizeScale = sizePercent * 0.01f;
            rect.sizeDelta = new Vector2(_nodeWidth, _nodeHeight) * sizeScale;
            rect.anchoredPosition = slot.anchoredPosition; // 정의 좌표 그대로. 여기서 보정하지 않는다.

            var background = rect.gameObject.AddComponent<Image>();
            background.color = hallucination ? _colHallucinationNode
                : visibility == ClueBoardNodeVisibility.Unlocked ? _colUnlockedNode : _colSilhouetteNode;
            background.raycastTarget = true; // 드롭 대상 판정(RaycastAll)이 이 그래픽을 잡는다

            // 하이라이트와 신규 획득 채움 모션이 공통으로 사용한다. 획득 이벤트 중 보드를 같은
            // 프레임에 재구축할 때 AddComponent를 지연 호출하면 파괴 예약된 동명 노드와 엇갈릴 수
            // 있으므로 노드 생성 시점부터 항상 붙여 수명을 노드와 일치시킨다.
            rect.gameObject.AddComponent<CanvasGroup>();

            // 하이라이트 테두리. 기본은 꺼 두고 ApplyHighlight가 켠다.
            var outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = _colHighlightOutline;
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            outline.useGraphicAlpha = true;
            outline.enabled = false;

            bool unlocked = visibility == ClueBoardNodeVisibility.Unlocked;
            if (unlocked)
            {
                // 단서별 아이콘이 없으면 유형 기본 아이콘(ClueTypeIconSet)으로 떨어진다. 그것도 없으면 이름만.
                Sprite icon = ClueTypeIconSet.ResolveIcon(clue);
                bool hasIcon = icon != null;
                BuildNodeIcon(rect, icon, !hideLabel);
                if (HasAudio(clue)) BuildAudioBadge(rect);
                if (!hideLabel)
                {
                    var label = NewRect(rect, "Label");
                    label.anchorMin = Vector2.zero;
                    label.anchorMax = hasIcon ? new Vector2(1f, 0.32f) : Vector2.one;
                    label.offsetMin = new Vector2(2f, 2f);
                    label.offsetMax = new Vector2(-2f, -2f);
                    var text = label.gameObject.AddComponent<TextMeshProUGUI>();
                    TMP_FontAsset customFont = ClueAttachmentService.LoadFont(fontAddress);
                    if (customFont != null) text.font = customFont;
                    else if (_font != null) text.font = _font;
                    text.text = clue != null ? clue.name : slot.clueId;
                    text.fontSize = fontSize;
                    text.color = Color.white;
                    text.alignment = TextAlignmentOptions.Center;
                    text.overflowMode = TextOverflowModes.Ellipsis;
                    text.raycastTarget = false;
                }

                rect.gameObject.AddComponent<ClueBoardNodeClick>().Bind(slot.nodeId, HandleNodeClicked);

                bool isNew = _newClueProvider?.Invoke(slot.clueId) == true;
                if (isNew)
                {
                    Debug.Log($"[ClueNEW][BoardView] 마커 생성 판정 TRUE: board={BoardId}, node={slot.nodeId}, clue={slot.clueId}, frame={Time.frameCount}");
                    BuildNewMarker(rect, slot.clueId);
                }
            }
            // 실루엣은 이름도 아이콘도 내지 않는다 — 자리와 "무언가 있다"만 알리고, 포인터를 올리면 해금 힌트만 보여 준다.
            else if (visibility == ClueBoardNodeVisibility.Silhouette)
                rect.gameObject.AddComponent<ClueBoardSilhouetteHover>()
                    .Bind(slot.nodeId, ShowSilhouetteHint, HideSilhouetteHint);

            var drag = rect.gameObject.AddComponent<ClueBoardDragNode>();
            // 실루엣과 환각은 화면에 보이되 연결의 양 끝이 될 수 없다. 판정 엔진도 같은 이유로
            // 거절하지만, 여기서 먼저 막아 드래그가 시작조차 되지 않게 한다.
            bool connectable = unlocked && !hallucination;
            drag.Bind(_dragController, slot.nodeId, connectable, connectable);
            return rect;
        }

        private float ResolveSizePercent(ClueBoardSlot slot, ClueData clue)
        {
            float value = UsesSlotAppearance(slot) ? slot.sizePercent : clue?.boardSizePercent ?? 0f;
            return value > 0f ? value : 100f;
        }

        private float ResolveFontSize(ClueBoardSlot slot, ClueData clue)
        {
            float value = UsesSlotAppearance(slot) ? slot.fontSize : clue?.boardFontSize ?? 0f;
            return value > 0f ? value : _defaultNodeFontSize;
        }

        private static string ResolveFontAddress(ClueBoardSlot slot, ClueData clue) =>
            UsesSlotAppearance(slot) ? slot.fontAddress : clue?.boardFontAddress;

        private static bool ResolveHideLabel(ClueBoardSlot slot, ClueData clue) =>
            UsesSlotAppearance(slot) ? slot.hideLabel : clue?.boardHideLabel == true;

        // overrideAppearance가 생기기 전 JSON의 명시적 슬롯 외형도 그대로 읽는다.
        private static bool UsesSlotAppearance(ClueBoardSlot slot) =>
            slot.overrideAppearance || slot.sizePercent > 0f || slot.fontSize > 0f ||
            !string.IsNullOrWhiteSpace(slot.fontAddress) || slot.hideLabel;

        private void BuildNewMarker(RectTransform node, string clueId)
        {
            var border = NewRect(_newVisualLayer, "NewGlowBorder_" + clueId);
            border.anchorMin = border.anchorMax = node.anchorMin;
            border.pivot = node.pivot;
            border.anchoredPosition = node.anchoredPosition;
            // 카드와 정확히 같은 영역을 쓴다. NEW 강조는 외곽선과 맥동으로만 보이며
            // 카드 바깥으로 확장되지 않는다.
            border.sizeDelta = node.sizeDelta;
            var borderImage = border.gameObject.AddComponent<Image>();
            borderImage.color = new Color(1f, 1f, 1f, 0.01f);
            borderImage.raycastTarget = false;
            var outline = border.gameObject.AddComponent<Outline>();
            outline.useGraphicAlpha = false;
            outline.effectDistance = new Vector2(2.5f, -2.5f);

            var tag = NewRect(_newTagLayer, "NewTag_" + clueId);
            tag.anchorMin = tag.anchorMax = node.anchorMin;
            tag.pivot = new Vector2(0.5f, 1f);
            tag.anchoredPosition = node.anchoredPosition + new Vector2(0f, node.sizeDelta.y * 0.5f + 2f);
            tag.sizeDelta = new Vector2(node.sizeDelta.x + 4f, 11f);
            // 전면 레이어에는 NEW 글자만 둔다. 배경까지 전면에 두면 카드 위를 다시 덮게 되므로
            // 노란 강조는 아래 NewVisualLayer의 글로우/테두리만 담당한다.
            var tagTextRect = NewRect(tag, "Text");
            StretchFull(tagTextRect);
            var tagText = tagTextRect.gameObject.AddComponent<TextMeshProUGUI>();
            if (_font != null) tagText.font = _font;
            tagText.text = "(NEW!)";
            tagText.fontSize = 6.5f;
            tagText.fontStyle = FontStyles.Bold;
            tagText.color = new Color(1f, 0.84f, 0.28f, 1f);
            tagText.alignment = TextAlignmentOptions.Center;
            tagText.raycastTarget = false;

            var visual = node.gameObject.AddComponent<ClueBoardNewVisual>();
            visual.Bind(clueId, outline, border.gameObject, tag.gameObject, _clueViewedHandler);
            tag.SetAsLastSibling();
            Debug.Log($"[ClueNEW][BoardView] 마커 생성 완료: board={BoardId}, clue={clueId}, " +
                      $"backgroundLayer={border.parent.name}, tagLayer={tag.parent.name}, frame={Time.frameCount}");
        }

        private void BuildNodeIcon(RectTransform parent, Sprite sprite, bool reserveLabelSpace)
        {
            if (sprite == null) return; // 아이콘 하나 때문에 노드가 사라지지는 않는다

            var iconRect = NewRect(parent, "Icon");
            iconRect.anchorMin = new Vector2(0f, reserveLabelSpace ? 0.32f : 0f);
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(3f, 2f);
            iconRect.offsetMax = new Vector2(-3f, -3f);
            var image = iconRect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        private void BuildAudioBadge(RectTransform parent)
        {
            var badge = NewRect(parent, "AudioBadge");
            badge.anchorMin = new Vector2(0.08f, 0.64f);
            badge.anchorMax = new Vector2(0.92f, 0.94f);
            badge.offsetMin = badge.offsetMax = Vector2.zero;
            var image = badge.gameObject.AddComponent<Image>();
            image.color = new Color(0.20f, 0.42f, 0.62f, 0.92f);
            image.raycastTarget = false;
            var textRect = NewRect(badge, "Text");
            StretchFull(textRect);
            var text = textRect.gameObject.AddComponent<TextMeshProUGUI>();
            if (_font != null) text.font = _font;
            text.text = "♪ AUDIO";
            text.fontSize = 5.5f;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
        }

        private static bool HasAudio(ClueData clue)
        {
            if (clue?.mediaBlocks != null)
                foreach (ClueMediaBlock block in clue.mediaBlocks)
                    if (block != null && block.kind == ClueMediaKind.Audio) return true;
            if (clue?.attachments != null)
                foreach (ClueAttachment attachment in clue.attachments)
                    if (attachment != null && attachment.kind == ClueAttachmentKind.Audio) return true;
            return false;
        }

        private ClueData ResolveClue(string clueId) =>
            string.IsNullOrEmpty(clueId) ? null : ClueResolver(clueId);

        // ─── 실루엣 힌트 ─────────────────────────────────────────

        /// <summary>지금 말풍선이 떠 있는 실루엣 노드. 없으면 null. 검증/진단용이며 저장 대상이 아니다.</summary>
        public string HoveredSilhouetteNodeId => _hoveredSilhouetteNodeId;
        public bool IsSilhouetteHintVisible => _hintRect != null && _hintRect.gameObject.activeSelf;
        public string VisibleSilhouetteHintText => IsSilhouetteHintVisible ? _hintText.text : null;

        /// <summary>이 보드에서 노드가 실루엣일 때 보여 줄 문구(슬롯 예외 → 단서 공통 → 기본). 노드가 없으면 null.</summary>
        public string ResolveSilhouetteHint(string nodeId)
        {
            if (nodeId == null || !_slotsByNodeId.TryGetValue(nodeId, out ClueBoardSlot slot)) return null;
            // 기본 문구: 설정 에셋(ClueSystemSettings)이 있으면 그것, 없으면 인스펙터 값 → 코드 기본값.
            string fallback = ClueSystemSettings.Current != null && !string.IsNullOrWhiteSpace(ClueSystemSettings.Current.defaultSilhouetteHint)
                ? ClueSystemSettings.Current.defaultSilhouetteHint : _defaultSilhouetteHint;
            return ClueBoardSilhouetteHint.Resolve(slot, ResolveClue(slot.clueId), fallback);
        }

        /// <summary>
        /// 실루엣 노드 옆에 힌트 말풍선을 띄운다. 실루엣이 아닌 노드(해금·잠김·화면에 없음)는 무시한다 — 해금 노드의
        /// 정보는 카드 자체가 보여 주고, 잠김은 존재를 드러내면 안 된다.
        /// </summary>
        public bool ShowSilhouetteHint(string nodeId)
        {
            if (_states == null || _states.Get(nodeId) != ClueBoardNodeVisibility.Silhouette) return false;
            if (!_nodeVisuals.TryGetValue(nodeId, out RectTransform node) || node == null) return false;

            EnsureHintBubble();
            _hintText.text = ResolveSilhouetteHint(nodeId);
            SizeHintToText();
            _hintRect.anchoredPosition = PlaceHintBeside(node);
            _hintRect.gameObject.SetActive(true);
            _hoveredSilhouetteNodeId = nodeId;
            return true;
        }

        public void HideSilhouetteHint()
        {
            _hoveredSilhouetteNodeId = null;
            if (_hintRect != null) _hintRect.gameObject.SetActive(false);
        }

        private void HideSilhouetteHint(string nodeId)
        {
            // 다른 실루엣으로 곧장 옮겨 간 경우 Enter가 Exit보다 먼저 올 수 있다 — 지금 떠 있는 노드의 Exit만 내린다.
            if (_hoveredSilhouetteNodeId == null || string.Equals(_hoveredSilhouetteNodeId, nodeId, StringComparison.Ordinal))
                HideSilhouetteHint();
        }

        private void EnsureHintBubble()
        {
            if (_hintRect != null) return;
            _hintRect = NewRect(_hintLayer, "SilhouetteHint");
            _hintRect.anchorMin = _hintRect.anchorMax = new Vector2(0f, 1f);
            _hintRect.pivot = new Vector2(0f, 1f);
            var background = _hintRect.gameObject.AddComponent<Image>();
            background.color = _colHintBackground;
            background.raycastTarget = false; // 노드 호버를 가로채면 말풍선이 깜빡인다

            var textRect = NewRect(_hintRect, "Text");
            StretchFull(textRect);
            _hintText = textRect.gameObject.AddComponent<TextMeshProUGUI>();
            if (_font != null) _hintText.font = _font;
            _hintText.fontSize = _hintFontSize;
            _hintText.fontStyle = FontStyles.Italic;
            _hintText.color = _colHintText;
            _hintText.alignment = TextAlignmentOptions.TopLeft;
            _hintText.enableWordWrapping = true;
            _hintText.margin = new Vector4(HintPadding, HintPadding, HintPadding, HintPadding);
            _hintText.raycastTarget = false;
            _hintRect.gameObject.SetActive(false);
        }

        private void SizeHintToText()
        {
            float maxTextWidth = Mathf.Max(20f, _hintMaxWidth - HintPadding * 2f);
            Vector2 preferred = _hintText.GetPreferredValues(_hintText.text, maxTextWidth, 0f);
            _hintRect.sizeDelta = new Vector2(
                Mathf.Min(maxTextWidth, preferred.x) + HintPadding * 2f,
                preferred.y + HintPadding * 2f);
        }

        // 노드 오른쪽 위에 붙이고, 보드 밖으로 나가면 왼쪽/위로 접어 넣는다. 슬롯 좌표는 (0,1) 앵커 기준이라
        // 보드 안은 x∈[0, 폭], y∈[-높이, 0]이다.
        private Vector2 PlaceHintBeside(RectTransform node)
        {
            Vector2 half = node.sizeDelta * 0.5f;
            Vector2 size = _hintRect.sizeDelta;
            Rect board = _root.rect;
            float x = node.anchoredPosition.x + half.x + HintGap;
            float y = node.anchoredPosition.y + half.y;
            if (board.width > 0f && x + size.x > board.width)
                x = Mathf.Max(0f, node.anchoredPosition.x - half.x - HintGap - size.x);
            if (board.height > 0f && y - size.y < -board.height)
                y = Mathf.Min(0f, -board.height + size.y);
            return new Vector2(x, Mathf.Min(0f, y));
        }

        private static ClueData DefaultResolveClue(string clueId) => MapGraph.Instance?.GetClue(clueId);

        // ─── 연결선 ──────────────────────────────────────────────

        private void RebuildEdges()
        {
            ClearEdges();
            if (_runtimeState == null) return;

            foreach (ClueBoardRelation relation in _definition.relations)
            {
                if (relation == null || !_runtimeState.IsConnected(relation.relationId)) continue;
                // 양 끝이 화면에 떠 있을 때만 그린다. 초기 연결이 아직 잠긴 노드를 가리키는 경우
                // (콘텐츠상 가능하다) 선만 허공에 남지 않게 한다.
                if (!_nodeVisuals.TryGetValue(relation.firstNodeId, out RectTransform first) ||
                    !_nodeVisuals.TryGetValue(relation.secondNodeId, out RectTransform second)) continue;

                BuildEdge(first.anchoredPosition, second.anchoredPosition, ResolveEdgeColor(relation),
                    relation.relationId, relation.firstNodeId, relation.secondNodeId);
            }

            // 선을 다시 만들었으니 하이라이트 밝기도 다시 입힌다(필터 중에 새 연결이 생겨도 규칙이 유지된다).
            if (_highlightedNodeIds != null) ApplyHighlight();
        }

        // 선 색은 단서 전용 ClueEmotionTag만 본다. 전투 EmotionColor는 이 경로에 등장하지 않는다.
        private Color ResolveEdgeColor(ClueBoardRelation relation)
        {
            ClueEmotionTag first = ResolveEmotion(relation.firstNodeId);
            ClueEmotionTag second = ResolveEmotion(relation.secondNodeId);
            return ClueEmotionTagConfig.ResolveLineColor(first, second);
        }

        private ClueEmotionTag ResolveEmotion(string nodeId)
        {
            if (!_slotsByNodeId.TryGetValue(nodeId, out ClueBoardSlot slot)) return ClueEmotionTag.Unset;
            ClueData clue = ResolveClue(slot.clueId);
            return clue != null ? clue.emotionTag : ClueEmotionTag.Unset;
        }

        private void BuildEdge(Vector2 from, Vector2 to, Color color, string relationId, string firstNodeId, string secondNodeId)
        {
            var rect = NewRect(_edgeLayer, "Edge_" + relationId);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            Vector2 direction = to - from;
            rect.anchoredPosition = from + direction * 0.5f;
            rect.sizeDelta = new Vector2(Mathf.Max(0.01f, direction.magnitude), _edgeThickness);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            // 보이는 선은 레이캐스트를 받지 않는다. 클릭은 아래의 넓은 투명 판정 영역이 받는다 — 3px 선을 정확히
            // 맞히게 하면 재열람이 사실상 불가능하고, 선 자체를 넓히면 화면이 지저분해진다.
            image.raycastTarget = false;

            // [C06] 클릭 판정 영역. EdgeLayer 안(노드 뒤)이라 노드가 덮는 부분은 노드가 먼저 받고, 드롭 판정은
            // ClueBoardDragNode가 붙은 결과만 찾으므로 이 영역이 드롭을 가로채지 않는다.
            float hitThickness = Mathf.Max(_edgeThickness, _edgeHitThickness);
            var hit = NewRect(rect, "Hit");
            hit.anchorMin = new Vector2(0f, 0.5f);
            hit.anchorMax = new Vector2(1f, 0.5f);
            hit.pivot = new Vector2(0.5f, 0.5f);
            hit.offsetMin = new Vector2(0f, -hitThickness * 0.5f);
            hit.offsetMax = new Vector2(0f, hitThickness * 0.5f);
            var hitImage = hit.gameObject.AddComponent<Image>();
            hitImage.color = new Color(0f, 0f, 0f, 0f);
            hitImage.raycastTarget = true;
            hit.gameObject.AddComponent<ClueBoardEdgeClick>().Bind(relationId, HandleEdgeClicked);

            _edgeVisuals.Add(new EdgeVisual(rect, relationId, firstNodeId, secondNodeId));
        }

        private void HandleNodeClicked(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId) || _states == null || _states.Get(nodeId) != ClueBoardNodeVisibility.Unlocked) return;
            OnNodeClicked?.Invoke(nodeId);
        }

        /// <summary>노드의 단서 데이터(화면과 같은 조회). 없으면 null.</summary>
        public ClueData GetClueOfNode(string nodeId) =>
            nodeId != null && _slotsByNodeId.TryGetValue(nodeId, out ClueBoardSlot slot) ? ResolveClue(slot.clueId) : null;

        // 선 클릭은 판정(TryConnect)을 부르지 않는다 — 결과 재열람은 ClueBoardScreen이 발행 기록으로 처리한다.
        private void HandleEdgeClicked(string relationId)
        {
            if (string.IsNullOrEmpty(relationId) || _runtimeState == null || !_runtimeState.IsConnected(relationId)) return;
            OnEdgeClicked?.Invoke(relationId);
        }

        /// <summary>검증용 — 성립한 관계의 선 RectTransform. 없으면 false.</summary>
        public bool TryGetEdgeVisual(string relationId, out RectTransform edge)
        {
            edge = null;
            foreach (EdgeVisual visual in _edgeVisuals)
                if (visual.rect != null && string.Equals(visual.relationId, relationId, StringComparison.Ordinal))
                {
                    edge = visual.rect;
                    return true;
                }
            return false;
        }

        private void ClearEdges()
        {
            foreach (EdgeVisual edge in _edgeVisuals)
                if (edge.rect != null) DestroyObject(edge.rect.gameObject);
            _edgeVisuals.Clear();
        }

        public int VisibleEdgeCount => _edgeVisuals.Count;
        public bool TryGetNodeVisual(string nodeId, out RectTransform node) => _nodeVisuals.TryGetValue(nodeId, out node);

        /// <summary>마지막으로 채움 모션을 시작한 노드. 검증/진단용이며 저장 대상이 아니다.</summary>
        public string LastAnimatedNodeId { get; private set; }

        /// <summary>
        /// 현재 보드에서 방금 획득한 단서 슬롯의 채움 모션을 시작한다. 같은 단서가 다른 보드에도 있어도
        /// 현재 Definition의 슬롯만 찾으며, 아직 화면에 없는 경우 false다.
        /// </summary>
        public bool AnimateClueFilled(string clueId)
        {
            if (_definition?.slots == null || string.IsNullOrEmpty(clueId)) return false;

            foreach (ClueBoardSlot slot in _definition.slots)
            {
                if (slot == null || slot.clueId != clueId) continue;
                if (!_nodeVisuals.TryGetValue(slot.nodeId, out RectTransform node) || node == null) return false;

                LastAnimatedNodeId = slot.nodeId;
                StartCoroutine(AnimateFilledNode(node));
                return true;
            }
            return false;
        }

        private System.Collections.IEnumerator AnimateFilledNode(RectTransform node)
        {
            if (node == null) yield break;
            var group = node.GetComponent<CanvasGroup>();
            if (group == null)
            {
                Debug.LogWarning($"[ClueNEW][BoardView] 채움 모션 취소: CanvasGroup 없음, node={node.name}, frame={Time.frameCount}");
                yield break;
            }
            float targetAlpha = group.alpha;
            const float duration = 0.42f;
            // 프레임이 잠시 멈춘 뒤 재개돼도 채움 연출이 한 번에 끝나면 신규 획득을
            // 플레이어가 볼 수 없다. 특히 에디터 배치 검증은 첫 틱의 delta가 0.42초보다
            // 커질 수 있으므로, 시각 연출의 진행 폭은 프레임당 1/30초로 제한한다.
            const float maxVisualStep = 1f / 30f;
            float elapsed = 0f;

            node.localScale = Vector3.one * 0.35f;
            group.alpha = 0f;
            while (node != null && group != null && elapsed < duration)
            {
                elapsed += Mathf.Min(Time.unscaledDeltaTime, maxVisualStep);
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                float scale = t < 0.72f
                    ? Mathf.Lerp(0.35f, 1.10f, eased / 0.72f)
                    : Mathf.Lerp(1.10f, 1f, (t - 0.72f) / 0.28f);
                node.localScale = Vector3.one * scale;
                group.alpha = Mathf.Lerp(0f, targetAlpha, eased);
                yield return null;
            }

            if (node != null && group != null)
            {
                node.localScale = Vector3.one;
                group.alpha = targetAlpha;
            }
        }

        // ─── 연결 판정 ───────────────────────────────────────────

        private void HandleConnectionRequested(string firstNodeId, string secondNodeId)
        {
            if (_engine == null || _runtimeState == null) return;

            _engine.TryConnect(_runtimeState, firstNodeId, secondNodeId, out ClueBoardConnectResult result);

            // 새로 생긴 연결만 화면을 다시 그린다. 재시도(AlreadyConnected)에서 다시 그리면 같은
            // 선을 지웠다 만드는 헛일이고, 아래 Established 이벤트도 한 번만 나가야 한다.
            if (result.createdConnection)
            {
                // 이 연결로 체인이 완성돼 실루엣이 새로 열리면 노드까지 다시 그린다(그 밖에는 선만).
                if (!RefreshChainReveals()) RebuildEdges();
            }

            OnConnectionResult?.Invoke(result);
            if (result.createdConnection) OnConnectionEstablished?.Invoke(result);
        }

        /// <summary>
        /// 성립한 관계 집합으로 표시 상태를 다시 계산해 체인 공개로 열린 실루엣을 반영한다. 표시가 바뀌었을 때만
        /// 노드·선을 다시 그리고 true. 판정 상태(연결 집합)는 그대로 두고 표시 집합만 새 상태로 바꾼다.
        /// </summary>
        public bool RefreshChainReveals()
        {
            if (_definition == null || _runtimeState == null) return false;
            ClueBoardNodeStates next = ClueBoardNodeStates.Build(
                _definition, _acquiredClueIds, _hallucinationNodeIds, _runtimeState.connectedRelationIds);
            if (next.SameVisibilityAs(_states)) return false;

            _states = next;
            _runtimeState = _states.CreateRuntimeState(_engine, _runtimeState.connectedRelationIds);
            RebuildNodes();
            RebuildEdges();
            return true;
        }

        // ─── UI 유틸 ─────────────────────────────────────────────

        private static RectTransform NewRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void ClearChildren(RectTransform parent)
        {
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
                DestroyObject(parent.GetChild(i).gameObject);
        }

        // 에디터 검증에서도 같은 경로가 돌기 때문에 상황에 맞는 파괴 함수를 고른다.
        private static void DestroyObject(GameObject target)
        {
            if (Application.isPlaying) Destroy(target);
            else DestroyImmediate(target);
        }
    }

    // [C06] 성립한 연결선의 클릭 판정 영역에 붙는다. 드래그 핸들러를 구현하지 않으므로 선에서 드래그가
    // 시작되지 않고, EventSystem은 눌렀다 뗀 대상이 같은 이 오브젝트일 때만 클릭을 주므로 노드에서 시작한
    // 드래그를 선 위에서 놓아도 클릭이 되지 않는다.
    public sealed class ClueBoardEdgeClick : MonoBehaviour, IPointerClickHandler
    {
        private string _relationId;
        private Action<string> _handler;

        public string RelationId => _relationId;

        public void Bind(string relationId, Action<string> handler)
        {
            _relationId = relationId;
            _handler = handler;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;
            _handler?.Invoke(_relationId);
        }
    }

    // 해금 노드의 클릭(단서 설명 팝업). 드래그가 시작되면 EventSystem이 eligibleForClick을 내리므로 드롭 뒤에는 오지 않는다.
    public sealed class ClueBoardNodeClick : MonoBehaviour, IPointerClickHandler
    {
        private string _nodeId;
        private Action<string> _handler;

        public void Bind(string nodeId, Action<string> handler)
        {
            _nodeId = nodeId;
            _handler = handler;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData != null && (eventData.button != PointerEventData.InputButton.Left || eventData.dragging)) return;
            _handler?.Invoke(_nodeId);
        }
    }

    // 실루엣 노드의 호버. 드래그/클릭 핸들러는 구현하지 않으므로 실루엣이 연결의 시작·끝이 되는 규칙은 그대로다.
    // 드래그 중 위를 지나도 Enter/Exit는 오므로(EventSystem은 드래그 중에도 pointerEnter를 갱신한다) 임시 선을
    // 끌고 실루엣에 닿았을 때도 "아직 얻지 못한 자리"라는 힌트가 보인다.
    public sealed class ClueBoardSilhouetteHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private string _nodeId;
        private Func<string, bool> _enter;
        private Action<string> _exit;

        public string NodeId => _nodeId;

        public void Bind(string nodeId, Func<string, bool> onEnter, Action<string> onExit)
        {
            _nodeId = nodeId;
            _enter = onEnter;
            _exit = onExit;
        }

        public void OnPointerEnter(PointerEventData eventData) => _enter?.Invoke(_nodeId);
        public void OnPointerExit(PointerEventData eventData) => _exit?.Invoke(_nodeId);

        private void OnDisable() => _exit?.Invoke(_nodeId);
    }

    public sealed class ClueBoardNewVisual : MonoBehaviour, IPointerClickHandler
    {
        private string _clueId;
        private Outline _outline;
        private GameObject _border;
        private GameObject _tag;
        private Action<string> _viewedHandler;
        private readonly Color _glowColor = new(1f, 0.82f, 0.24f, 0.95f);

        public void Bind(string clueId, Outline outline, GameObject border, GameObject tag,
            Action<string> viewedHandler)
        {
            _clueId = clueId;
            _outline = outline;
            _border = border;
            _tag = tag;
            _viewedHandler = viewedHandler;
            ApplyPulse(1f);
        }

        private void Update()
        {
            if (_outline == null || !_outline.enabled) return;
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.8f);
            ApplyPulse(Mathf.Lerp(0.45f, 1f, wave));
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;
            Debug.Log($"[ClueNEW][BoardView] 최초 확인 클릭 → NEW 해제 요청: clue={_clueId}, frame={Time.frameCount}, pointer={(eventData == null ? "manual" : eventData.position.ToString())}");
            _viewedHandler?.Invoke(_clueId);
            if (_outline != null) _outline.enabled = false;
            if (_border != null) _border.SetActive(false);
            if (_tag != null) _tag.SetActive(false);
            enabled = false;
            Debug.Log($"[ClueNEW][BoardView] 시각 효과 비활성 완료: clue={_clueId}, frame={Time.frameCount}");
        }

        private void ApplyPulse(float alphaScale)
        {
            if (_outline == null) return;
            Color color = _glowColor;
            color.a *= alphaScale;
            _outline.effectColor = color;
        }
    }
}
