using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

/// <summary>
/// Main StateMachine, Phase 서브 머신, Pattern State와 BossPatternSO를 한 화면에서 제작한다.
/// 1단계 탐색·타임라인·검증과 2단계 에셋 생성·자동 배선을 제공한다.
/// </summary>
public sealed class BossAuthoringWindow : EditorWindow
{
    private const float HierarchyWidth = 300f;
    private const float ValidationWidth = 360f;
    private const float MinimumCenterWidth = 420f;
    private const string WindowMenuPath = "Tools/Boss Authoring";

    [Tooltip("현재 제작 대상으로 선택한 메인 StateMachineSO.")]
    private StateMachineSO _mainMachine;

    [Tooltip("필수 런타임 컴포넌트를 검사할 선택적 보스 프리팹.")]
    private GameObject _bossPrefab;

    [Tooltip("창에서 현재 선택한 페이즈 StateMachineSO.")]
    private StateMachineSO _selectedPhase;

    [Tooltip("창에서 현재 선택한 패턴 StateSO.")]
    private StateSO _selectedPatternState;

    [Tooltip("타임라인 편집 대상으로 현재 선택한 BossPatternSO.")]
    private BossPatternSO _selectedPattern;

    [Tooltip("일반 Inspector 영역에서 현재 선택한 Unity 에셋.")]
    private UnityEngine.Object _selectedObject;

    [Tooltip("일반 Inspector 영역에 재사용할 캐시 Editor.")]
    private Editor _cachedInspector;

    [Tooltip("선택한 BossPatternSO의 직렬화 타임라인 접근 객체.")]
    private SerializedObject _patternSerializedObject;

    [Tooltip("선택한 BossPatternSO의 Beat를 재정렬하는 목록 UI.")]
    private ReorderableList _timelineList;

    [Tooltip("현재 Main StateMachine에서 탐색한 Phase/Pattern 계층.")]
    private List<BossPhaseAuthoringNode> _phases = new();

    [Tooltip("가장 최근 실행한 보스 에셋 검증 결과.")]
    private List<BossAuthoringValidationIssue> _validationIssues = new();

    [Tooltip("각 페이즈의 계층 Foldout 펼침 상태.")]
    private readonly Dictionary<int, bool> _phaseFoldouts = new();

    [Tooltip("계층 패널의 현재 스크롤 위치.")]
    private Vector2 _hierarchyScroll;

    [Tooltip("가운데 편집 패널의 현재 스크롤 위치.")]
    private Vector2 _editorScroll;

    [Tooltip("검증 및 생성 패널의 현재 스크롤 위치.")]
    private Vector2 _validationScroll;

    [Tooltip("새 Phase와 Pattern 에셋을 생성할 프로젝트 폴더 경로.")]
    private string _outputRoot = "Assets";

    [Tooltip("2단계 Phase 생성에 사용할 기본 이름.")]
    private string _newPhaseName = "Phase1";

    [Tooltip("2단계 Pattern 생성에 사용할 기본 이름.")]
    private string _newPatternName = "Pattern1";

    [Tooltip("복제할 PatternSO의 새 이름.")]
    private string _duplicatePatternName = "Pattern1_Copy";

    /// <summary>
    /// Unity 상단 메뉴에서 보스 제작 창을 열고 최소 작업 크기를 지정한다.
    /// 기존 창이 있으면 새 인스턴스를 만들지 않고 앞으로 가져온다.
    /// </summary>
    [MenuItem(WindowMenuPath)]
    public static void OpenWindow()
    {
        BossAuthoringWindow window = GetWindow<BossAuthoringWindow>();
        window.titleContent = new GUIContent("Boss Authoring");
        window.minSize = new Vector2(
            HierarchyWidth + ValidationWidth + MinimumCenterWidth + 32f,
            620f);
        window.Show();
    }

    /// <summary>
    /// Undo/Redo 뒤 타임라인과 계층이 즉시 다시 그려지도록 이벤트를 구독한다.
    /// 창을 다시 열었을 때 선택된 Main이 있다면 탐색 결과도 복원한다.
    /// </summary>
    private void OnEnable()
    {
        Undo.undoRedoPerformed += HandleUndoRedo;
        RefreshHierarchyAndValidation();
    }

    /// <summary>
    /// 창이 닫힐 때 Undo 이벤트와 캐시 Inspector 참조를 정리한다.
    /// 에디터 도메인 리로드 뒤 파괴된 Editor가 남지 않게 한다.
    /// </summary>
    private void OnDisable()
    {
        Undo.undoRedoPerformed -= HandleUndoRedo;
        DestroyCachedInspector();
    }

    /// <summary>
    /// 보스 제작 창의 상단 선택 영역과 세 개의 주요 패널을 IMGUI로 그린다.
    /// 창 폭이 충분하지 않으면 설정한 최소 크기로 레이아웃 붕괴를 방지한다.
    /// </summary>
    private void OnGUI()
    {
        DrawTopToolbar();
        EditorGUILayout.Space(4f);

        using (new EditorGUILayout.HorizontalScope())
        {
            DrawHierarchyPanel();
            DrawEditorPanel();
            DrawValidationAndCreationPanel();
        }
    }

    /// <summary>
    /// Main StateMachine, 보스 프리팹, 출력 폴더와 새로고침 도구를 표시한다.
    /// Main 선택이 바뀌면 출력 위치와 계층·검증 결과를 즉시 갱신한다.
    /// </summary>
    private void DrawTopToolbar()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                StateMachineSO newMain = (StateMachineSO)EditorGUILayout.ObjectField(
                    new GUIContent("Main StateMachine", "페이즈 서브 머신들을 실행하는 메인 StateMachineSO."),
                    _mainMachine,
                    typeof(StateMachineSO),
                    false,
                    GUILayout.MinWidth(320f));
                if (EditorGUI.EndChangeCheck())
                    SetMainMachine(newMain);

                EditorGUI.BeginChangeCheck();
                GameObject newBossPrefab = (GameObject)EditorGUILayout.ObjectField(
                    new GUIContent("Boss Prefab", "선택하면 StateController, BossPatternRunner, CombatAttackModule을 검사합니다."),
                    _bossPrefab,
                    typeof(GameObject),
                    false,
                    GUILayout.MinWidth(280f));
                if (EditorGUI.EndChangeCheck())
                {
                    _bossPrefab = newBossPrefab;
                    RefreshValidation();
                }

                if (GUILayout.Button("Refresh", GUILayout.Width(76f)))
                    RefreshHierarchyAndValidation();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(new GUIContent(
                    "Output Root",
                    "자동 생성할 Phases/Patterns 폴더의 부모 Assets 경로."));
                _outputRoot = EditorGUILayout.TextField(_outputRoot);

                if (GUILayout.Button("Main Folder", GUILayout.Width(96f)))
                    _outputRoot = BossAuthoringUtility.GetDefaultOutputRoot(_mainMachine);

                if (GUILayout.Button("Browse", GUILayout.Width(76f)))
                    BrowseOutputFolder();
            }
        }
    }

    /// <summary>
    /// Main → Phase → Pattern 위계를 왼쪽 탐색 패널에 표시한다.
    /// 항목 선택 시 가운데 패널이 해당 State 또는 Pattern 타임라인으로 바뀐다.
    /// </summary>
    private void DrawHierarchyPanel()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(HierarchyWidth)))
        {
            EditorGUILayout.LabelField("1. Hierarchy", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Main → Phase → Pattern",
                EditorStyles.miniLabel);

            _hierarchyScroll = EditorGUILayout.BeginScrollView(_hierarchyScroll);
            if (_mainMachine == null)
            {
                EditorGUILayout.HelpBox("Main StateMachineSO를 선택하세요.", MessageType.Info);
            }
            else
            {
                DrawHierarchyObjectButton(_mainMachine, "Main", 0, null, null);

                for (int i = 0; i < _phases.Count; i++)
                    DrawPhaseNode(_phases[i], i);

                if (_phases.Count == 0)
                    EditorGUILayout.HelpBox("연결된 Phase가 없습니다.", MessageType.Warning);
            }
            EditorGUILayout.EndScrollView();
        }
    }

    /// <summary>
    /// 한 페이즈 Foldout과 Launch/Return State, Pattern 자식 항목들을 그린다.
    /// Foldout 상태는 StateMachine Instance ID를 키로 창이 열려 있는 동안 유지한다.
    /// </summary>
    private void DrawPhaseNode(BossPhaseAuthoringNode phase, int phaseIndex)
    {
        StateMachineSO phaseMachine = phase.PhaseMachine;
        int foldoutKey = phaseMachine != null
            ? phaseMachine.GetInstanceID()
            : -(phaseIndex + 1);
        if (!_phaseFoldouts.TryGetValue(foldoutKey, out bool expanded))
            expanded = true;

        using (new EditorGUILayout.HorizontalScope())
        {
            expanded = EditorGUILayout.Foldout(
                expanded,
                phaseMachine != null ? phaseMachine.name : "Missing Phase",
                true);
            if (GUILayout.Button("●", GUILayout.Width(22f)))
                PingObject(phaseMachine != null ? phaseMachine : phase.LaunchState);
        }
        _phaseFoldouts[foldoutKey] = expanded;
        if (!expanded) return;

        DrawHierarchyObjectButton(
            phaseMachine,
            "Phase Machine",
            1,
            phaseMachine,
            null);
        DrawHierarchyObjectButton(
            phase.LaunchState,
            $"Launch: {phase.LaunchState?.name}",
            2,
            phaseMachine,
            null);
        DrawHierarchyObjectButton(
            phase.StartAction?.ReturnState,
            $"Return: {phase.StartAction?.ReturnState?.name}",
            2,
            phaseMachine,
            null);

        for (int i = 0; i < phase.Patterns.Count; i++)
        {
            BossPatternAuthoringNode patternNode = phase.Patterns[i];
            string label = patternNode.Pattern != null
                ? patternNode.Pattern.name
                : $"{patternNode.State?.name}: Missing Pattern";
            DrawHierarchyObjectButton(
                patternNode.Pattern != null ? patternNode.Pattern : patternNode.State,
                label,
                2,
                phaseMachine,
                patternNode);
        }
    }

    /// <summary>
    /// 들여쓰기된 계층 항목 버튼과 선택 강조를 공통 형태로 그린다.
    /// Pattern 노드가 전달되면 타임라인을, 아니면 일반 Inspector를 선택한다.
    /// </summary>
    private void DrawHierarchyObjectButton(
        UnityEngine.Object target,
        string label,
        int indent,
        StateMachineSO phaseMachine,
        BossPatternAuthoringNode patternNode)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Space(indent * 16f);
            bool selected = patternNode != null
                ? _selectedPattern == patternNode.Pattern &&
                  _selectedPatternState == patternNode.State
                : _selectedObject == target;
            GUIStyle style = selected ? EditorStyles.miniButtonMid : EditorStyles.miniButton;

            using (new EditorGUI.DisabledScope(target == null))
            {
                if (GUILayout.Button(label ?? "Missing", style))
                {
                    _selectedPhase = phaseMachine;
                    if (patternNode != null)
                        SelectPattern(patternNode.State, patternNode.Pattern, phaseMachine);
                    else
                        SelectObject(target, phaseMachine);
                }
            }
        }
    }

    /// <summary>
    /// 선택한 Pattern이 있으면 세로형 타임라인을, 아니면 선택 에셋 Inspector를 표시한다.
    /// Pattern 편집에서는 상대 Delay와 계산된 누적 시각을 함께 제공한다.
    /// </summary>
    private void DrawEditorPanel()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.MinWidth(MinimumCenterWidth)))
        {
            EditorGUILayout.LabelField("1. Timeline / Inspector", EditorStyles.boldLabel);
            _editorScroll = EditorGUILayout.BeginScrollView(_editorScroll);

            if (_selectedPattern != null)
                DrawPatternTimeline();
            else
                DrawSelectedInspector();

            EditorGUILayout.EndScrollView();
        }
    }

    /// <summary>
    /// 선택한 BossPatternSO의 Recovery, 총 길이, Beat 목록과 복제 도구를 그린다.
    /// ReorderableList 변경은 SerializedObject와 Undo를 통해 원본 PatternSO에 저장한다.
    /// </summary>
    private void DrawPatternTimeline()
    {
        EnsureTimelineList();
        if (_patternSerializedObject == null || _timelineList == null) return;

        _patternSerializedObject.Update();
        EditorGUILayout.ObjectField("Pattern", _selectedPattern, typeof(BossPatternSO), false);
        EditorGUILayout.ObjectField("Pattern State", _selectedPatternState, typeof(StateSO), false);

        SerializedProperty recovery = _patternSerializedObject.FindProperty("_recoveryDuration");
        EditorGUILayout.PropertyField(recovery);

        if (_patternSerializedObject.ApplyModifiedProperties())
            EditorUtility.SetDirty(_selectedPattern);

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField(
            $"Total Duration: {_selectedPattern.TotalDuration:0.###}s",
            EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Delay는 이전 Beat 실행 뒤의 상대 시간이며, 각 항목 위에 누적 시각을 함께 표시합니다.",
            MessageType.Info);

        _timelineList.DoLayoutList();
        if (_patternSerializedObject.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(_selectedPattern);
            RefreshValidation();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(_timelineList.index < 0))
            {
                if (GUILayout.Button("Duplicate Beat"))
                    DuplicateSelectedBeat();
                if (GUILayout.Button("Delete Beat"))
                    DeleteSelectedBeat();
            }

            if (GUILayout.Button("Ping Pattern", GUILayout.Width(100f)))
                PingObject(_selectedPattern);
        }
    }

    /// <summary>
    /// 현재 선택 에셋에 Unity의 기존 CustomEditor를 재사용해 Inspector를 표시한다.
    /// StateSO의 Pack/Template UI를 복제하지 않고 원래 편집 동작을 그대로 유지한다.
    /// </summary>
    private void DrawSelectedInspector()
    {
        if (_selectedObject == null)
        {
            EditorGUILayout.HelpBox(
                "Hierarchy에서 StateMachine, State 또는 Pattern을 선택하세요.",
                MessageType.Info);
            return;
        }

        Editor.CreateCachedEditor(_selectedObject, null, ref _cachedInspector);
        if (_cachedInspector == null) return;

        EditorGUILayout.ObjectField("Selected", _selectedObject, _selectedObject.GetType(), false);
        EditorGUILayout.Space(4f);
        _cachedInspector.OnInspectorGUI();
    }

    /// <summary>
    /// 오른쪽 패널에 1단계 Validation과 2단계 생성·자동 배선 도구를 함께 표시한다.
    /// 생성 대상 Phase는 계층에서 선택한 현재 페이즈를 사용한다.
    /// </summary>
    private void DrawValidationAndCreationPanel()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(ValidationWidth)))
        {
            _validationScroll = EditorGUILayout.BeginScrollView(_validationScroll);
            DrawValidationSection();
            EditorGUILayout.Space(10f);
            DrawCreationSection();
            EditorGUILayout.EndScrollView();
        }
    }

    /// <summary>
    /// 최근 검증 결과를 심각도별 HelpBox와 원인 에셋 선택 버튼으로 표시한다.
    /// Refresh Validation 버튼으로 직렬화 편집 직후 결과를 다시 계산할 수 있다.
    /// </summary>
    private void DrawValidationSection()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("1. Validation", EditorStyles.boldLabel);
            if (GUILayout.Button("Refresh", GUILayout.Width(72f)))
                RefreshValidation();
        }

        if (_validationIssues == null || _validationIssues.Count == 0)
        {
            EditorGUILayout.HelpBox("검증을 실행하세요.", MessageType.Info);
            return;
        }

        for (int i = 0; i < _validationIssues.Count; i++)
        {
            BossAuthoringValidationIssue issue = _validationIssues[i];
            EditorGUILayout.HelpBox(issue.Message, issue.Severity);
            if (issue.Context != null)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Select", GUILayout.Width(72f)))
                    {
                        Selection.activeObject = issue.Context;
                        EditorGUIUtility.PingObject(issue.Context);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 2단계 Phase 생성, Pattern 생성과 선택 Pattern 복제 도구를 표시한다.
    /// 이름과 출력 폴더를 확인한 뒤 Utility가 표준 State/Action/Decision을 자동 배선한다.
    /// </summary>
    private void DrawCreationSection()
    {
        EditorGUILayout.LabelField("2. Create & Wire", EditorStyles.boldLabel);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Create Phase", EditorStyles.boldLabel);
            _newPhaseName = EditorGUILayout.TextField("Name", _newPhaseName);
            using (new EditorGUI.DisabledScope(_mainMachine == null))
            {
                if (GUILayout.Button("Create Phase StateMachine"))
                    CreatePhase();
            }
        }

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Create Pattern", EditorStyles.boldLabel);
            EditorGUILayout.ObjectField(
                "Target Phase",
                _selectedPhase,
                typeof(StateMachineSO),
                false);
            _newPatternName = EditorGUILayout.TextField("Name", _newPatternName);
            using (new EditorGUI.DisabledScope(_selectedPhase == null))
            {
                if (GUILayout.Button("Create Pattern + State"))
                    CreatePattern();
            }
        }

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Duplicate Pattern", EditorStyles.boldLabel);
            EditorGUILayout.ObjectField(
                "Source",
                _selectedPattern,
                typeof(BossPatternSO),
                false);
            _duplicatePatternName = EditorGUILayout.TextField("New Name", _duplicatePatternName);
            using (new EditorGUI.DisabledScope(
                       _selectedPhase == null || _selectedPattern == null))
            {
                if (GUILayout.Button("Duplicate Pattern + State"))
                    DuplicatePattern();
            }
        }

        EditorGUILayout.HelpBox(
            "새 Pattern State는 완료 시 Phase Initial State로 돌아갑니다. Selector에서 Pattern으로 들어가는 선택 전이는 보스별 규칙에 맞게 직접 연결하세요.",
            MessageType.Info);
    }

    /// <summary>
    /// 선택한 PatternSO의 SerializedProperty와 ReorderableList 콜백을 초기화한다.
    /// Pattern 선택이 바뀌었을 때만 새 목록을 만들어 불필요한 할당을 줄인다.
    /// </summary>
    private void EnsureTimelineList()
    {
        if (_selectedPattern == null)
        {
            _patternSerializedObject = null;
            _timelineList = null;
            return;
        }

        if (_patternSerializedObject != null &&
            _patternSerializedObject.targetObject == _selectedPattern &&
            _timelineList != null)
            return;

        _patternSerializedObject = new SerializedObject(_selectedPattern);
        SerializedProperty timeline = _patternSerializedObject.FindProperty("_timeline");
        _timelineList = new ReorderableList(
            _patternSerializedObject,
            timeline,
            true,
            true,
            true,
            true);

        _timelineList.drawHeaderCallback = rect =>
            EditorGUI.LabelField(rect, "Timeline Beats");
        _timelineList.elementHeightCallback = GetTimelineElementHeight;
        _timelineList.drawElementCallback = DrawTimelineElement;
        _timelineList.onAddCallback = AddTimelineBeat;
        _timelineList.onRemoveCallback = RemoveTimelineBeat;
        _timelineList.onReorderCallback = _ => ApplyPatternChanges();
    }

    /// <summary>
    /// 한 Timeline Entry 전체 Property 높이에 누적 시각 라벨 공간을 더해 반환한다.
    /// SerializeReference Action의 펼침 상태에 따라 높이가 자동으로 늘어난다.
    /// </summary>
    private float GetTimelineElementHeight(int index)
    {
        if (_timelineList?.serializedProperty == null ||
            index < 0 || index >= _timelineList.serializedProperty.arraySize)
            return EditorGUIUtility.singleLineHeight;

        SerializedProperty element = _timelineList.serializedProperty.GetArrayElementAtIndex(index);
        return EditorGUI.GetPropertyHeight(element, true) +
               EditorGUIUtility.singleLineHeight + 8f;
    }

    /// <summary>
    /// Timeline Entry 위에 계산된 누적 시각을 표시하고 전체 직렬화 필드를 그린다.
    /// 실제 저장값은 상대 Delay를 유지하므로 런타임 시간 규칙을 변경하지 않는다.
    /// </summary>
    private void DrawTimelineElement(Rect rect, int index, bool active, bool focused)
    {
        SerializedProperty element = _timelineList.serializedProperty.GetArrayElementAtIndex(index);
        float absoluteTime = CalculateAbsoluteTime(index);
        Rect timeRect = new(
            rect.x,
            rect.y + 2f,
            rect.width,
            EditorGUIUtility.singleLineHeight);
        EditorGUI.LabelField(
            timeRect,
            $"Beat {index}  •  At {absoluteTime:0.###}s",
            EditorStyles.boldLabel);

        Rect propertyRect = new(
            rect.x,
            timeRect.yMax + 2f,
            rect.width,
            EditorGUI.GetPropertyHeight(element, true));
        EditorGUI.PropertyField(propertyRect, element, GUIContent.none, true);
    }

    /// <summary>
    /// Timeline의 0번부터 선택 인덱스까지 상대 Delay를 더해 누적 시각을 계산한다.
    /// 아직 Apply되지 않은 Inspector 입력값도 SerializedProperty에서 즉시 읽는다.
    /// </summary>
    private float CalculateAbsoluteTime(int inclusiveIndex)
    {
        if (_timelineList?.serializedProperty == null) return 0f;

        float result = 0f;
        int lastIndex = Mathf.Min(
            inclusiveIndex,
            _timelineList.serializedProperty.arraySize - 1);
        for (int i = 0; i <= lastIndex; i++)
        {
            SerializedProperty entry = _timelineList.serializedProperty.GetArrayElementAtIndex(i);
            SerializedProperty delay = entry.FindPropertyRelative("_delay");
            if (delay != null)
                result += Mathf.Max(0f, delay.floatValue);
        }

        return result;
    }

    /// <summary>
    /// Timeline 끝에 빈 Beat를 추가하고 기본 Label과 Delay, Action 배열을 초기화한다.
    /// Unity 배열 삽입이 이전 요소를 복제하는 동작을 명시적으로 정리한다.
    /// </summary>
    private void AddTimelineBeat(ReorderableList list)
    {
        SerializedProperty timeline = list.serializedProperty;
        int index = timeline.arraySize;
        timeline.InsertArrayElementAtIndex(index);
        SerializedProperty entry = timeline.GetArrayElementAtIndex(index);
        entry.FindPropertyRelative("_label").stringValue = $"Beat {index}";
        entry.FindPropertyRelative("_delay").floatValue = 0f;
        SerializedProperty actions = entry.FindPropertyRelative("_actions");
        if (actions != null) actions.arraySize = 0;
        list.index = index;
        ApplyPatternChanges();
    }

    /// <summary>
    /// ReorderableList가 선택한 Beat를 Undo 가능한 배열 삭제로 제거한다.
    /// 삭제 뒤 선택 인덱스를 남은 마지막 Beat 범위로 보정한다.
    /// </summary>
    private void RemoveTimelineBeat(ReorderableList list)
    {
        if (list.index < 0 || list.index >= list.serializedProperty.arraySize) return;
        list.serializedProperty.DeleteArrayElementAtIndex(list.index);
        list.index = Mathf.Min(list.index, list.serializedProperty.arraySize - 1);
        ApplyPatternChanges();
    }

    /// <summary>
    /// 현재 선택한 Beat 직후에 직렬화 내용을 그대로 복제한다.
    /// 복제본 Label에는 Copy 접미사를 더해 타임라인에서 구분하기 쉽게 한다.
    /// </summary>
    private void DuplicateSelectedBeat()
    {
        EnsureTimelineList();
        int sourceIndex = _timelineList.index;
        if (sourceIndex < 0 || sourceIndex >= _timelineList.serializedProperty.arraySize) return;

        int targetIndex = sourceIndex + 1;
        _timelineList.serializedProperty.InsertArrayElementAtIndex(sourceIndex);
        SerializedProperty entry = _timelineList.serializedProperty.GetArrayElementAtIndex(targetIndex);
        SerializedProperty label = entry.FindPropertyRelative("_label");
        label.stringValue = string.IsNullOrWhiteSpace(label.stringValue)
            ? $"Beat {targetIndex} Copy"
            : $"{label.stringValue} Copy";
        _timelineList.index = targetIndex;
        ApplyPatternChanges();
    }

    /// <summary>
    /// 현재 선택한 Beat를 타임라인에서 삭제한다.
    /// ReorderableList 기본 삭제와 동일한 경로를 사용해 선택 보정을 공유한다.
    /// </summary>
    private void DeleteSelectedBeat()
    {
        if (_timelineList == null) return;
        RemoveTimelineBeat(_timelineList);
    }

    /// <summary>
    /// Pattern SerializedObject 변경을 원본 에셋에 적용하고 Dirty·검증 상태를 갱신한다.
    /// Undo 기록은 SerializedProperty가 처리하며 창은 새 총 길이를 다시 그린다.
    /// </summary>
    private void ApplyPatternChanges()
    {
        if (_patternSerializedObject == null || _selectedPattern == null) return;
        _patternSerializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(_selectedPattern);
        RefreshValidation();
        Repaint();
    }

    /// <summary>
    /// 2단계 표준 규칙으로 새 Phase StateMachine과 Main Launch/Return State를 만든다.
    /// 성공하면 생성된 Phase를 선택하고 계층과 검증 결과를 다시 읽는다.
    /// </summary>
    private void CreatePhase()
    {
        try
        {
            BossPhaseCreationResult result = BossAuthoringUtility.CreatePhase(
                _mainMachine,
                _outputRoot,
                _newPhaseName);
            _selectedPhase = result.PhaseMachine;
            SelectObject(result.LaunchState, result.PhaseMachine);
            Selection.activeObject = result.LaunchState;
            RefreshHierarchyAndValidation();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog("Create Phase Failed", exception.Message, "OK");
        }
    }

    /// <summary>
    /// 선택한 Phase에 새 BossPatternSO와 표준 Pattern State를 함께 생성한다.
    /// 성공하면 새 Pattern 타임라인을 가운데 편집 패널에서 즉시 연다.
    /// </summary>
    private void CreatePattern()
    {
        try
        {
            BossPatternCreationResult result = BossAuthoringUtility.CreatePattern(
                _selectedPhase,
                _outputRoot,
                _newPatternName);
            RefreshHierarchyAndValidation();
            SelectPattern(result.PatternState, result.Pattern, _selectedPhase);
            Selection.activeObject = result.Pattern;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog("Create Pattern Failed", exception.Message, "OK");
        }
    }

    /// <summary>
    /// 선택한 PatternSO 타임라인을 복제하고 같은 Phase에 새 Pattern State를 만든다.
    /// 원본 Pattern State의 임의 전이는 복제하지 않고 표준 완료 전이를 사용한다.
    /// </summary>
    private void DuplicatePattern()
    {
        try
        {
            BossPatternCreationResult result = BossAuthoringUtility.DuplicatePattern(
                _selectedPhase,
                _selectedPattern,
                _outputRoot,
                _duplicatePatternName);
            RefreshHierarchyAndValidation();
            SelectPattern(result.PatternState, result.Pattern, _selectedPhase);
            Selection.activeObject = result.Pattern;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog("Duplicate Pattern Failed", exception.Message, "OK");
        }
    }

    /// <summary>
    /// Main StateMachine 선택을 교체하고 기본 출력 폴더와 모든 탐색 결과를 갱신한다.
    /// 이전 Main의 선택 Pattern과 Phase 참조는 함께 초기화한다.
    /// </summary>
    private void SetMainMachine(StateMachineSO mainMachine)
    {
        _mainMachine = mainMachine;
        _selectedPhase = null;
        _selectedPatternState = null;
        _selectedPattern = null;
        _selectedObject = mainMachine;
        _outputRoot = BossAuthoringUtility.GetDefaultOutputRoot(mainMachine);
        _patternSerializedObject = null;
        _timelineList = null;
        DestroyCachedInspector();
        RefreshHierarchyAndValidation();
    }

    /// <summary>
    /// 일반 계층 에셋을 선택하고 Pattern 타임라인 선택을 해제한다.
    /// 선택한 Phase 문맥은 2단계 Pattern 생성 대상으로 유지한다.
    /// </summary>
    private void SelectObject(UnityEngine.Object target, StateMachineSO phaseMachine)
    {
        _selectedObject = target;
        _selectedPhase = phaseMachine;
        if (_selectedPhase == null && target is StateMachineSO selectedMachine &&
            selectedMachine != _mainMachine)
            _selectedPhase = selectedMachine;
        _selectedPatternState = null;
        _selectedPattern = null;
        _patternSerializedObject = null;
        _timelineList = null;
        DestroyCachedInspector();
    }

    /// <summary>
    /// Pattern State와 BossPatternSO를 타임라인 편집 대상으로 선택한다.
    /// 복제 이름도 현재 패턴 이름을 기준으로 알아보기 쉽게 갱신한다.
    /// </summary>
    private void SelectPattern(
        StateSO patternState,
        BossPatternSO pattern,
        StateMachineSO phaseMachine)
    {
        _selectedPatternState = patternState;
        _selectedPattern = pattern;
        _selectedPhase = phaseMachine;
        _selectedObject = pattern;
        _patternSerializedObject = null;
        _timelineList = null;
        _duplicatePatternName = pattern != null
            ? $"{pattern.name}_Copy"
            : "Pattern_Copy";
        DestroyCachedInspector();
        EnsureTimelineList();
    }

    /// <summary>
    /// Main StateMachine을 다시 스캔해 Phase/Pattern 계층과 Validation을 함께 갱신한다.
    /// 에셋 생성, Undo/Redo, 외부 Inspector 수정 뒤 호출한다.
    /// </summary>
    private void RefreshHierarchyAndValidation()
    {
        _phases = BossAuthoringUtility.BuildHierarchy(_mainMachine);
        RefreshValidation();
        Repaint();
    }

    /// <summary>
    /// 현재 Main, 보스 프리팹과 탐색된 계층을 기준으로 검증 결과만 다시 계산한다.
    /// 타임라인을 편집할 때 계층 전체 재탐색을 피하기 위한 경량 경로다.
    /// </summary>
    private void RefreshValidation()
    {
        _validationIssues = BossAuthoringUtility.Validate(
            _mainMachine,
            _bossPrefab,
            _phases);
    }

    /// <summary>
    /// Undo/Redo가 에셋 참조와 배열을 바꾼 뒤 계층·타임라인 캐시를 모두 무효화한다.
    /// 다음 GUI 프레임에서 원본 SerializedObject를 다시 만들게 한다.
    /// </summary>
    private void HandleUndoRedo()
    {
        _patternSerializedObject = null;
        _timelineList = null;
        RefreshHierarchyAndValidation();
    }

    /// <summary>
    /// OS 폴더 선택 창에서 프로젝트 Assets 내부 폴더를 골라 Output Root로 변환한다.
    /// Assets 밖의 폴더를 선택하면 에셋 생성이 불가능하므로 경고하고 유지한다.
    /// </summary>
    private void BrowseOutputFolder()
    {
        string projectAssetsPath = Application.dataPath.Replace('\\', '/');
        string currentAbsolute = _outputRoot.StartsWith("Assets", StringComparison.Ordinal)
            ? projectAssetsPath + _outputRoot.Substring("Assets".Length)
            : projectAssetsPath;
        string selected = EditorUtility.OpenFolderPanel(
            "Select Boss Authoring Output Folder",
            currentAbsolute,
            string.Empty).Replace('\\', '/');
        if (string.IsNullOrWhiteSpace(selected)) return;

        if (!selected.StartsWith(projectAssetsPath, StringComparison.OrdinalIgnoreCase))
        {
            EditorUtility.DisplayDialog(
                "Invalid Folder",
                "프로젝트의 Assets 폴더 안을 선택하세요.",
                "OK");
            return;
        }

        _outputRoot = "Assets" + selected.Substring(projectAssetsPath.Length);
    }

    /// <summary>
    /// 선택 또는 검증 문맥 에셋을 Project 창에서 강조한다.
    /// 대상이 비어 있으면 아무 작업도 하지 않는다.
    /// </summary>
    private static void PingObject(UnityEngine.Object target)
    {
        if (target == null) return;
        Selection.activeObject = target;
        EditorGUIUtility.PingObject(target);
    }

    /// <summary>
    /// 선택 에셋용으로 생성한 캐시 Editor를 안전하게 파괴하고 참조를 비운다.
    /// Pattern 타임라인과 일반 Inspector 사이를 오갈 때 중복 Editor를 방지한다.
    /// </summary>
    private void DestroyCachedInspector()
    {
        if (_cachedInspector != null)
            DestroyImmediate(_cachedInspector);
        _cachedInspector = null;
    }
}
