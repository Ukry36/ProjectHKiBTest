using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static RouteFinding.UI.RouteUiKit;

namespace RouteFinding.UI
{
    // C03-B 검색 패널 — 검색어 + 유형 4 + 단서 감정 4 + new! 버튼과 결과 목록.
    //
    // 조건 계산은 ClueBoardSearchService(순수 C#)가 하고, 이 컴포넌트는 입력을 받아 조건을 만들고
    // 결과를 두 곳에 나눠 내보낸다: (1) 패널 안 결과 목록, (2) ClueBoardView의 보드 하이라이트.
    // 목록과 하이라이트는 같은 결과를 보지만 서로 독립이다 — 목록 행을 눌러 노드를 옮기거나
    // 카드를 여는 동작은 붙이지 않았다(단서 선택/클릭의 의미는 C05 NEW 해제·C06 코멘트가 정한다).
    //
    // [new! 필터] isNewClue 공급원이 없으면 버튼 자체를 만들지 않는다. C05 신규 상태를 세이브까지
    // 확정한 뒤 ClueBoardScreen.ConfigureSources(newClueProvider:)로 넘기면 그때부터 보인다.
    // 공급원 없이 버튼만 두면 "눌러도 아무것도 안 뜨는" 필터가 생겨 미구현을 완료로 오인하게 된다.
    //
    // [네 번째 감정] 자리는 남기되 누를 수 없다(interactable=false). ClueBoardFilterCriteria가
    // ReservedFourth 토글을 거절하므로 버튼이 눌리더라도 조건은 바뀌지 않는다.
    public sealed class ClueBoardSearchPanel : MonoBehaviour
    {
        /// <summary>조건이 바뀌어 결과를 다시 계산했을 때. 인자는 결과 수.</summary>
        public event Action<int> OnResultsChanged;

        private const float RowHeight = 12f;
        private const float ButtonHeight = 11f;

        private readonly ClueBoardFilterCriteria _criteria = new();
        private readonly List<ClueBoardSearchHit> _hits = new();
        private readonly Dictionary<ClueType, Image> _typeButtons = new();
        private readonly Dictionary<ClueEmotionTag, Image> _emotionButtons = new();

        private RectTransform _root;
        private TMP_FontAsset _font;
        private ClueBoardView _view;
        private Func<string, bool> _newClueProvider;

        private TMP_InputField _searchField;
        private RectTransform _filterRows;
        private Image _newButton;
        private TextMeshProUGUI _resultHeader;
        private RectTransform _resultContent;
        private bool _suppressFieldCallback;

        private static readonly Color ColButtonOff = new(0.16f, 0.19f, 0.26f, 1f);
        private static readonly Color ColButtonOn = new(0.55f, 0.48f, 0.22f, 1f);
        private static readonly Color ColButtonDisabled = new(0.10f, 0.11f, 0.13f, 1f);

        public ClueBoardFilterCriteria Criteria => _criteria;
        public IReadOnlyList<ClueBoardSearchHit> Hits => _hits;
        public bool IsOpen => _root != null && _root.gameObject.activeSelf;
        public bool HasNewFilterButton => _newButton != null;
        public RectTransform Root => _root;

        public void Initialize(RectTransform parent, TMP_FontAsset font, ClueBoardView view, float width)
        {
            _font = font;
            _view = view;

            _root = NewRect(parent, "SearchPanel");
            _root.anchorMin = new Vector2(1f, 0f);
            _root.anchorMax = new Vector2(1f, 1f);
            _root.pivot = new Vector2(1f, 0.5f);
            _root.sizeDelta = new Vector2(width, 0f);
            var background = _root.gameObject.AddComponent<Image>();
            background.color = new Color(0.07f, 0.08f, 0.12f, 0.97f);
            background.raycastTarget = true; // 패널 아래 노드로 드래그/드롭이 새지 않게 한다

            var layout = _root.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.spacing = 3f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            BuildSearchField(_root);
            BuildFilterRows(_root);
            BuildResultList(_root);

            _root.gameObject.SetActive(false);
        }

        /// <summary>패널을 열고 닫는다. 닫아도 조건과 하이라이트는 유지된다 — 필터를 걸어 둔 채 보드를 보는 용도다.</summary>
        public void SetOpen(bool open)
        {
            if (_root == null) return;
            _root.gameObject.SetActive(open);
        }

        /// <summary>C05 신규 판정 공급원. null이면 new! 버튼을 만들지 않고, 켜져 있던 신규 조건도 끈다.</summary>
        public void SetNewClueProvider(Func<string, bool> provider)
        {
            _newClueProvider = provider;
            if (provider == null)
            {
                if (_newButton != null)
                {
                    Transform row = _newButton.transform.parent;
                    row.SetParent(null, false);
                    DestroyObject(row.gameObject);
                    _newButton = null;
                }
                if (_criteria.newOnly) _criteria.SetNewOnly(false);
            }
            else if (_newButton == null && _filterRows != null)
            {
                BuildNewRow(_filterRows);
            }
            RefreshButtons();
            Recompute();
        }

        /// <summary>표시 중인 보드로 조건을 다시 계산해 목록과 하이라이트를 갱신한다. 보드가 바뀔 때마다 호출한다.</summary>
        public void Recompute()
        {
            _hits.Clear();
            if (_view != null && _view.Definition != null && _view.NodeStates != null)
                _hits.AddRange(ClueBoardSearchService.Match(
                    _view.Definition, _view.NodeStates, _criteria, _view.ClueResolver, _newClueProvider));

            // 조건이 없으면 하이라이트를 끈다 — "전부 강조"는 강조가 아니다. 목록은 해금 단서 전체를 보여준다.
            if (_view != null)
            {
                if (_criteria.IsEmpty) _view.ClearHighlight();
                else
                {
                    var nodeIds = new List<string>(_hits.Count);
                    foreach (ClueBoardSearchHit hit in _hits) nodeIds.Add(hit.nodeId);
                    _view.SetHighlight(nodeIds);
                }
            }

            RenderResults();
            OnResultsChanged?.Invoke(_hits.Count);
        }

        public void ClearCriteria()
        {
            _criteria.Clear();
            if (_searchField != null)
            {
                _suppressFieldCallback = true;
                _searchField.text = "";
                _suppressFieldCallback = false;
            }
            RefreshButtons();
            Recompute();
        }

        // ─── 입력 ────────────────────────────────────────────────

        public void SetQuery(string query)
        {
            _criteria.SetQuery(query);
            if (_searchField != null && _searchField.text != (query ?? ""))
            {
                _suppressFieldCallback = true;
                _searchField.text = query ?? "";
                _suppressFieldCallback = false;
            }
            Recompute();
        }

        public void ToggleType(ClueType type)
        {
            _criteria.ToggleType(type);
            RefreshButtons();
            Recompute();
        }

        /// <summary>선택 가능한 감정만 토글된다. ReservedFourth/Unset은 아무것도 바꾸지 않는다.</summary>
        public bool ToggleEmotion(ClueEmotionTag tag)
        {
            if (!_criteria.TryToggleEmotion(tag, out _)) return false;
            RefreshButtons();
            Recompute();
            return true;
        }

        public void ToggleNewOnly()
        {
            if (_newClueProvider == null) return; // 버튼이 없을 때 외부 호출로도 켜지지 않는다
            _criteria.SetNewOnly(!_criteria.newOnly);
            RefreshButtons();
            Recompute();
        }

        // ─── UI 구축 ─────────────────────────────────────────────

        private void BuildSearchField(RectTransform parent)
        {
            var rect = NewRect(parent, "SearchInput");
            rect.gameObject.AddComponent<LayoutElement>().preferredHeight = 13f;
            var background = rect.gameObject.AddComponent<Image>();
            background.color = new Color(0.03f, 0.04f, 0.06f, 1f);

            var textArea = NewRect(rect, "TextArea");
            StretchFull(textArea);
            textArea.offsetMin = new Vector2(3f, 1f);
            textArea.offsetMax = new Vector2(-3f, -1f);
            textArea.gameObject.AddComponent<RectMask2D>();

            var text = MakeText(textArea, "Text", "", 7f, Color.white, TextAlignmentOptions.MidlineLeft);
            StretchFull((RectTransform)text.transform);

            var placeholder = MakeText(textArea, "Placeholder", "검색...", 7f, new Color(1f, 1f, 1f, 0.35f),
                TextAlignmentOptions.MidlineLeft);
            placeholder.fontStyle = FontStyles.Italic;
            StretchFull((RectTransform)placeholder.transform);

            _searchField = rect.gameObject.AddComponent<TMP_InputField>();
            _searchField.textViewport = textArea;
            _searchField.textComponent = text;
            _searchField.placeholder = placeholder;
            _searchField.lineType = TMP_InputField.LineType.SingleLine;
            _searchField.onValueChanged.AddListener(value =>
            {
                if (_suppressFieldCallback) return;
                _criteria.SetQuery(value);
                Recompute();
            });
        }

        private void BuildFilterRows(RectTransform parent)
        {
            _filterRows = NewRect(parent, "FilterRows");
            var layout = _filterRows.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 2f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            _filterRows.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // 유형 4
            RectTransform typeRow = MakeRow(_filterRows, "TypeRow");
            foreach (ClueType type in ClueTypeConfig.AllTypes)
            {
                ClueType captured = type;
                _typeButtons[type] = MakeToggleButton(typeRow, "Type_" + type,
                    ClueTypeConfig.GetDisplayName(type), () => ToggleType(captured));
            }

            // 단서 감정 4 — 확정 3종 + 예약 자리
            RectTransform emotionRow = MakeRow(_filterRows, "EmotionRow");
            foreach (ClueEmotionTag tag in ClueEmotionTagConfig.FilterOrder)
            {
                ClueEmotionTag captured = tag;
                Image image = MakeToggleButton(emotionRow, "Emotion_" + tag,
                    ClueEmotionTagConfig.GetDisplayName(tag), () => ToggleEmotion(captured));
                _emotionButtons[tag] = image;
                if (!ClueEmotionTagConfig.IsSelectable(tag))
                {
                    image.GetComponent<Button>().interactable = false;
                    image.color = ColButtonDisabled;
                }
            }

            // new! 줄은 공급원이 들어올 때 BuildNewRow가 만든다.

            RectTransform actionRow = MakeRow(_filterRows, "ActionRow");
            MakeToggleButton(actionRow, "ClearButton", "초기화", ClearCriteria);
        }

        private void BuildNewRow(RectTransform parent)
        {
            RectTransform row = MakeRow(parent, "NewRow");
            // 초기화 줄보다 위에 둔다(유형 → 감정 → new! → 초기화).
            row.SetSiblingIndex(Mathf.Max(0, parent.childCount - 2));
            _newButton = MakeToggleButton(row, "NewOnly", "new!", ToggleNewOnly);
        }

        private void BuildResultList(RectTransform parent)
        {
            _resultHeader = MakeText(parent, "ResultHeader", "결과 0건", 7f, new Color(0.70f, 0.74f, 0.80f),
                TextAlignmentOptions.MidlineLeft);
            _resultHeader.gameObject.AddComponent<LayoutElement>().preferredHeight = 9f;

            var scrollRect = NewRect(parent, "ResultScroll");
            scrollRect.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            var scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 8f;

            var viewport = NewRect(scrollRect, "Viewport");
            StretchFull(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            var viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.15f);

            _resultContent = NewRect(viewport, "Content");
            _resultContent.anchorMin = new Vector2(0f, 1f);
            _resultContent.anchorMax = new Vector2(1f, 1f);
            _resultContent.pivot = new Vector2(0.5f, 1f);
            _resultContent.sizeDelta = new Vector2(0f, 0f);
            var layout = _resultContent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 1f;
            layout.padding = new RectOffset(2, 2, 2, 2);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            _resultContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = _resultContent;
        }

        private void RenderResults()
        {
            if (_resultContent == null) return;
            // Destroy는 프레임 끝에 처리되므로 먼저 떼어 낸다 — 같은 프레임에 childCount를 읽는 검증과
            // 레이아웃 계산이 지워질 행을 세지 않게 한다.
            for (int i = _resultContent.childCount - 1; i >= 0; i--)
            {
                Transform old = _resultContent.GetChild(i);
                old.SetParent(null, false);
                DestroyObject(old.gameObject);
            }

            if (_resultHeader != null)
                _resultHeader.text = _criteria.IsEmpty ? $"해금 단서 {_hits.Count}건" : $"결과 {_hits.Count}건";

            foreach (ClueBoardSearchHit hit in _hits)
            {
                var row = NewRect(_resultContent, "Result_" + hit.nodeId);
                row.gameObject.AddComponent<LayoutElement>().preferredHeight = RowHeight;
                var background = row.gameObject.AddComponent<Image>();
                background.color = new Color(0.14f, 0.16f, 0.22f, 0.9f);
                background.raycastTarget = false;

                string name = hit.clue != null && !string.IsNullOrEmpty(hit.clue.name) ? hit.clue.name : hit.clueId;
                string meta = DescribeMeta(hit.clue);
                var label = MakeText(row, "Label", string.IsNullOrEmpty(meta) ? name : $"{name}  <alpha=#88>{meta}",
                    7f, Color.white, TextAlignmentOptions.MidlineLeft);
                label.richText = true;
                label.overflowMode = TextOverflowModes.Ellipsis;
                var labelRect = (RectTransform)label.transform;
                StretchFull(labelRect);
                labelRect.offsetMin = new Vector2(3f, 0f);
                labelRect.offsetMax = new Vector2(-3f, 0f);
            }
        }

        // 목록 행에 붙는 짧은 메타 — 유형 이름과 감정 이름. 미분류/미설정이면 그 항목을 비운다.
        private static string DescribeMeta(ClueData clue)
        {
            if (clue == null) return "";
            string type = ClueTypeConfig.GetDisplayName(clue.classification);
            string emotion = ClueEmotionTagConfig.IsSelectable(clue.emotionTag)
                ? ClueEmotionTagConfig.GetDisplayName(clue.emotionTag) : "";
            if (string.IsNullOrEmpty(type)) return emotion;
            return string.IsNullOrEmpty(emotion) ? type : $"{type}·{emotion}";
        }

        private void RefreshButtons()
        {
            foreach (KeyValuePair<ClueType, Image> pair in _typeButtons)
                pair.Value.color = _criteria.HasType(pair.Key) ? ColButtonOn : ColButtonOff;
            foreach (KeyValuePair<ClueEmotionTag, Image> pair in _emotionButtons)
            {
                if (!ClueEmotionTagConfig.IsSelectable(pair.Key)) continue; // 예약 자리는 항상 꺼진 색
                pair.Value.color = _criteria.HasEmotion(pair.Key) ? ColButtonOn : ColButtonOff;
            }
            if (_newButton != null) _newButton.color = _criteria.newOnly ? ColButtonOn : ColButtonOff;
        }

        /// <summary>검증용 — 버튼 이름으로 클릭을 흉내 낸다. 없으면 false.</summary>
        public bool ClickButton(string buttonName)
        {
            Transform found = FindChild(_root, buttonName);
            var button = found != null ? found.GetComponent<Button>() : null;
            if (button == null || !button.interactable) return false;
            button.onClick.Invoke();
            return true;
        }

        public bool IsButtonInteractable(string buttonName)
        {
            Transform found = FindChild(_root, buttonName);
            var button = found != null ? found.GetComponent<Button>() : null;
            return button != null && button.interactable;
        }

        public int ResultRowCount => _resultContent != null ? _resultContent.childCount : 0;

        // ─── UI 유틸 ─────────────────────────────────────────────

        private RectTransform MakeRow(RectTransform parent, string name)
        {
            var row = NewRect(parent, name);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = ButtonHeight;
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 2f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            return row;
        }

        private Image MakeToggleButton(RectTransform parent, string name, string label, Action onClick)
        {
            var rect = NewRect(parent, name);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = ColButtonOff;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => onClick());

            var text = MakeText(rect, "Text", label, 6f, Color.white, TextAlignmentOptions.Center);
            text.overflowMode = TextOverflowModes.Ellipsis;
            StretchFull((RectTransform)text.transform);
            return image;
        }

        private TextMeshProUGUI MakeText(RectTransform parent, string name, string content, float size, Color color,
            TextAlignmentOptions alignment)
        {
            var rect = NewRect(parent, name);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (_font != null) text.font = _font;
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }

        private static Transform FindChild(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            foreach (Transform child in root)
            {
                Transform found = FindChild(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private static void DestroyObject(GameObject target)
        {
            if (Application.isPlaying) Destroy(target);
            else DestroyImmediate(target);
        }
    }
}
