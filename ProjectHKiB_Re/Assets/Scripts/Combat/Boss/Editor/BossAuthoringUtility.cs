using System;
using System.Collections.Generic;
using System.IO;
using AYellowpaper.SerializedCollections;
using Combat;
using StateMachine;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 자주 쓰는 패턴 흐름을 적은 선택지로 자동 설정하기 위한 생성 프리셋이다.
/// 세부 값은 생성 후 PatternSO 편집 패널에서 개별 조정할 수 있다.
/// </summary>
internal enum BossPatternCreationPreset
{
    NormalTimeline,
    OptionalSpecialAction,
    RequiredSpecialAction,
    TimelineAndRequiredSpecialAction
}

/// <summary>
/// 한 생성 프리셋을 PatternSO 런타임 규칙으로 변환한 불변 설정 묶음이다.
/// EditorWindow가 많은 개별 토글을 노출하지 않고도 일관된 에셋을 만들게 한다.
/// </summary>
internal readonly struct BossPatternCreationSettings
{
    public readonly BossPatternCompletionRule CompletionRule;
    public readonly bool CreateSpecialAction;
    public readonly bool RequiredForPatternClear;
    public readonly bool LoopTimelineUntilCleared;

    /// <summary>
    /// 자동 생성에 필요한 네 가지 핵심 패턴 규칙을 하나의 값으로 묶는다.
    /// UI 프리셋과 PatternSO 설정 사이의 매핑을 한 곳에서 유지한다.
    /// </summary>
    public BossPatternCreationSettings(
        BossPatternCompletionRule completionRule,
        bool createSpecialAction,
        bool requiredForPatternClear,
        bool loopTimelineUntilCleared)
    {
        CompletionRule = completionRule;
        CreateSpecialAction = createSpecialAction;
        RequiredForPatternClear = requiredForPatternClear;
        LoopTimelineUntilCleared = loopTimelineUntilCleared;
    }
}

/// <summary>
/// 보스 제작 창이 표시할 한 패턴 State와 BossPatternSO의 연결 정보다.
/// 런타임 에셋을 복제하지 않고 실제 참조만 읽어 편집 대상으로 제공한다.
/// </summary>
internal sealed class BossPatternAuthoringNode
{
    public StateSO State { get; }
    public BossPatternSO Pattern { get; }
    public PlayBossPatternAction PlayAction { get; }

    /// <summary>
    /// 패턴 State와 실행 Action, PatternSO 참조를 하나의 탐색 노드로 묶는다.
    /// 계층 창과 검증 창이 동일한 연결 정보를 공유한다.
    /// </summary>
    public BossPatternAuthoringNode(
        StateSO state,
        BossPatternSO pattern,
        PlayBossPatternAction playAction)
    {
        State = state;
        Pattern = pattern;
        PlayAction = playAction;
    }
}

/// <summary>
/// 메인 State의 StartSubStateMachineAction으로 발견한 한 페이즈 연결 정보다.
/// 페이즈 안에서 PlayBossPatternAction을 가진 State들을 패턴 자식으로 보관한다.
/// </summary>
internal sealed class BossPhaseAuthoringNode
{
    public StateSO LaunchState { get; }
    public StartSubStateMachineAction StartAction { get; }
    public StateMachineSO PhaseMachine => StartAction?.SubStateMachine;
    public List<BossPatternAuthoringNode> Patterns { get; } = new();

    /// <summary>
    /// 메인 Launch State와 서브 StateMachine 시작 Action을 페이즈 노드로 묶는다.
    /// 패턴 목록은 생성 뒤 별도 탐색 과정에서 채운다.
    /// </summary>
    public BossPhaseAuthoringNode(StateSO launchState, StartSubStateMachineAction startAction)
    {
        LaunchState = launchState;
        StartAction = startAction;
    }
}

/// <summary>
/// 보스 제작 검증 결과의 심각도와 선택 가능한 문제 대상을 보관한다.
/// EditorWindow는 이 정보를 HelpBox와 Ping 버튼으로 표현한다.
/// </summary>
internal sealed class BossAuthoringValidationIssue
{
    public MessageType Severity { get; }
    public string Message { get; }
    public UnityEngine.Object Context { get; }

    /// <summary>
    /// 검증 메시지와 심각도, 문제를 가진 에셋을 불변 값으로 생성한다.
    /// Context가 비어 있으면 선택 버튼 없이 일반 메시지만 표시한다.
    /// </summary>
    public BossAuthoringValidationIssue(
        MessageType severity,
        string message,
        UnityEngine.Object context = null)
    {
        Severity = severity;
        Message = message;
        Context = context;
    }
}

/// <summary>
/// 2단계 자동 생성 뒤 창이 즉시 선택해야 할 페이즈 에셋들을 반환한다.
/// 메인 Launch/Return State와 새 Phase StateMachine을 함께 보관한다.
/// </summary>
internal readonly struct BossPhaseCreationResult
{
    public readonly StateMachineSO PhaseMachine;
    public readonly StateSO LaunchState;
    public readonly StateSO ReturnState;

    /// <summary>
    /// 페이즈 자동 생성으로 만들어진 핵심 에셋 참조를 묶는다.
    /// 창은 이 결과로 선택과 계층 새로고침을 수행한다.
    /// </summary>
    public BossPhaseCreationResult(
        StateMachineSO phaseMachine,
        StateSO launchState,
        StateSO returnState)
    {
        PhaseMachine = phaseMachine;
        LaunchState = launchState;
        ReturnState = returnState;
    }
}

/// <summary>
/// 2단계 패턴 생성 뒤 창이 타임라인을 바로 열기 위한 결과다.
/// 생성된 StateSO와 BossPatternSO를 함께 보관한다.
/// </summary>
internal readonly struct BossPatternCreationResult
{
    public readonly StateSO PatternState;
    public readonly BossPatternSO Pattern;

    /// <summary>
    /// 패턴 자동 생성으로 만들어진 State와 PatternSO 참조를 묶는다.
    /// 창은 이 결과를 사용해 새 패턴을 즉시 편집 대상으로 선택한다.
    /// </summary>
    public BossPatternCreationResult(StateSO patternState, BossPatternSO pattern)
    {
        PatternState = patternState;
        Pattern = pattern;
    }
}

/// <summary>
/// 기존 자동 생성 Pattern 진입 전이에서 읽은 Source State와 사용자 조건 묶음이다.
/// EditorWindow가 Entry Mode를 나중에 수정할 때 현재 설정을 초깃값으로 사용한다.
/// </summary>
internal readonly struct BossPatternEntryWiringInfo
{
    public readonly StateSO SourceState;
    public readonly StateTransition.DecisionSet[] Conditions;

    /// <summary>
    /// 자동 진입 전이에서 추출한 Source와 가용성 보호를 제외한 조건들을 보관한다.
    /// 조건 배열은 호출자가 편집용으로 복제하여 사용한다.
    /// </summary>
    public BossPatternEntryWiringInfo(
        StateSO sourceState,
        StateTransition.DecisionSet[] conditions)
    {
        SourceState = sourceState;
        Conditions = conditions ?? Array.Empty<StateTransition.DecisionSet>();
    }
}

/// <summary>
/// 보스 제작 창의 계층 탐색, 검증, 페이즈·패턴 에셋 생성을 담당한다.
/// 에셋 변경은 Undo와 Dirty를 기록하고 StateMachineSO를 다시 갱신한다.
/// </summary>
internal static class BossAuthoringUtility
{
    private const string PhaseFolderName = "Phases";
    private const string PatternFolderName = "Patterns";
    private const string PhaseRestartFallbackTransitionName = "Boss Phase Restart Fallback";

    /// <summary>
    /// 간편 생성 프리셋을 실제 PatternSO 흐름·완료·실패 규칙으로 변환한다.
    /// 기획자가 자주 쓰는 경우만 노출하여 생성 단계의 설정 수를 제한한다.
    /// </summary>
    public static BossPatternCreationSettings GetCreationSettings(
        BossPatternCreationPreset preset)
    {
        return preset switch
        {
            BossPatternCreationPreset.OptionalSpecialAction => new BossPatternCreationSettings(
                BossPatternCompletionRule.TimelineOnly,
                true,
                false,
                false),
            BossPatternCreationPreset.RequiredSpecialAction => new BossPatternCreationSettings(
                BossPatternCompletionRule.RequiredSpecialActions,
                true,
                true,
                true),
            BossPatternCreationPreset.TimelineAndRequiredSpecialAction => new BossPatternCreationSettings(
                BossPatternCompletionRule.TimelineAndRequiredSpecialActions,
                true,
                true,
                true),
            _ => new BossPatternCreationSettings(
                BossPatternCompletionRule.TimelineOnly,
                false,
                false,
                false)
        };
    }

    /// <summary>
    /// 생성 프리셋이 의도하는 플레이 흐름을 EditorWindow 도움말로 반환한다.
    /// enum 이름만으로 알기 어려운 특수행동과 패턴 클리어의 관계를 함께 설명한다.
    /// </summary>
    public static string GetCreationPresetDescription(BossPatternCreationPreset preset)
    {
        return preset switch
        {
            BossPatternCreationPreset.OptionalSpecialAction =>
                "특수행동 성공/실패를 남기지만 패턴은 타임라인 종료만으로 클리어됩니다.",
            BossPatternCreationPreset.RequiredSpecialAction =>
                "필수 특수행동이 성공할 때까지 타임라인을 반복하고 성공 즉시 패턴을 클리어합니다.",
            BossPatternCreationPreset.TimelineAndRequiredSpecialAction =>
                "타임라인 한 사이클과 필수 특수행동 성공이 모두 충족돼야 패턴을 클리어합니다.",
            _ =>
                "특수행동 결과 없이 타임라인과 후딜레이가 끝나면 완료되는 일반 공격 패턴입니다."
        };
    }

    /// <summary>
    /// 메인 StateMachine의 직접 Action에서 서브 페이즈와 패턴 계층을 탐색한다.
    /// Group/Sequence 내부가 아닌 State의 기본 Action 위치와 전이 Action을 대상으로 한다.
    /// </summary>
    public static List<BossPhaseAuthoringNode> BuildHierarchy(StateMachineSO mainMachine)
    {
        List<BossPhaseAuthoringNode> phases = new();
        if (mainMachine == null || mainMachine.allStates == null) return phases;

        for (int i = 0; i < mainMachine.allStates.Count; i++)
        {
            StateSO state = mainMachine.allStates[i];
            if (state == null) continue;

            foreach (StateAction action in EnumerateDirectActions(state))
            {
                if (action is not StartSubStateMachineAction startAction) continue;

                BossPhaseAuthoringNode phaseNode = new(state, startAction);
                PopulatePatterns(phaseNode);
                phases.Add(phaseNode);
            }
        }

        return phases;
    }

    /// <summary>
    /// 선택한 메인·보스 프리팹과 발견한 모든 페이즈·패턴 연결을 검사한다.
    /// 자동 수정 없이 문제와 선택 가능한 에셋 문맥만 반환한다.
    /// </summary>
    public static List<BossAuthoringValidationIssue> Validate(
        StateMachineSO mainMachine,
        GameObject bossPrefab,
        List<BossPhaseAuthoringNode> phases)
    {
        List<BossAuthoringValidationIssue> issues = new();

        if (mainMachine == null)
        {
            issues.Add(new BossAuthoringValidationIssue(
                MessageType.Error,
                "Main StateMachineSO를 선택하세요."));
            return issues;
        }

        if (mainMachine.initialState == null)
            issues.Add(new BossAuthoringValidationIssue(
                MessageType.Error,
                "Main StateMachine에 Initial State가 없습니다.",
                mainMachine));

        if (!ContainsAction<ResetBossBattleProgressAction>(mainMachine))
            issues.Add(new BossAuthoringValidationIssue(
                MessageType.Warning,
                "Main StateMachine의 보스전 최초 진입 경로에 Reset Boss Battle Progress Action을 한 번 배치하세요.",
                mainMachine));

        if (phases == null || phases.Count == 0)
            issues.Add(new BossAuthoringValidationIssue(
                MessageType.Warning,
                "StartSubStateMachineAction으로 연결된 페이즈가 없습니다.",
                mainMachine));

        ValidateBossPrefab(bossPrefab, issues);

        if (phases != null)
        {
            var specialActionRequiredCounts =
                new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < phases.Count; i++)
                ValidatePhase(mainMachine, phases[i], specialActionRequiredCounts, issues);
        }

        if (issues.Count == 0)
            issues.Add(new BossAuthoringValidationIssue(
                MessageType.Info,
                "현재 탐색 범위에서 문제가 발견되지 않았습니다.",
                mainMachine));

        return issues;
    }

    /// <summary>
    /// 메인 StateMachine 안에 Launch/Return State를 만들고 새 페이즈 SO를 생성한다.
    /// 페이즈에는 Pattern Selector와 Complete Action을 가진 Finish State를 기본 배선한다.
    /// </summary>
    public static BossPhaseCreationResult CreatePhase(
        StateMachineSO mainMachine,
        string outputRoot,
        string requestedName)
    {
        if (mainMachine == null)
            throw new InvalidOperationException("Main StateMachineSO가 필요합니다.");

        string safeName = SanitizeAssetName(requestedName, "Phase");
        string phaseFolder = EnsureFolder(outputRoot, PhaseFolderName);
        string phasePath = AssetDatabase.GenerateUniqueAssetPath(
            $"{phaseFolder}/{safeName}.asset");

        StateMachineSO phaseMachine = ScriptableObject.CreateInstance<StateMachineSO>();
        phaseMachine.name = safeName;
        phaseMachine.customVariables = CreateVariableSets();
        phaseMachine.allStates = new List<StateSO>();
        phaseMachine._commandPairs = new List<CommandPair>();
        AssetDatabase.CreateAsset(phaseMachine, phasePath);
        Undo.RegisterCreatedObjectUndo(phaseMachine, "Create Boss Phase StateMachine");

        StateSO selectorState = CreateStateSubAsset(
            phaseMachine,
            $"{safeName}_PatternSelector");
        StateSO finishState = CreateStateSubAsset(
            phaseMachine,
            $"{safeName}_Finish");
        finishState.EnterActions = new StateAction[] { new CompleteSubStateMachineAction() };
        phaseMachine.initialState = selectorState;
        EditorUtility.SetDirty(finishState);
        EditorUtility.SetDirty(phaseMachine);

        Undo.RecordObject(mainMachine, "Add Boss Phase States");
        mainMachine.allStates ??= new List<StateSO>();
        StateSO returnState = CreateStateSubAsset(
            mainMachine,
            $"{safeName}_Returned");
        StateSO launchState = CreateStateSubAsset(
            mainMachine,
            $"{safeName}_Start");
        launchState.EnterActions = new StateAction[]
        {
            new BeginBossPhaseAction(phaseMachine),
            new StartSubStateMachineAction(phaseMachine, null, returnState)
        };
        EditorUtility.SetDirty(launchState);

        if (mainMachine.initialState == null)
            mainMachine.initialState = launchState;

        RefreshStateMachine(mainMachine);
        RefreshStateMachine(phaseMachine);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(phasePath);

        return new BossPhaseCreationResult(phaseMachine, launchState, returnState);
    }

    /// <summary>
    /// 선택한 페이즈에 Pattern State를 만들고 독립 BossPatternSO를 생성한다.
    /// 기본 프리셋과 PhaseStart 진입 배선을 함께 적용한다.
    /// </summary>
    public static BossPatternCreationResult CreatePattern(
        StateMachineSO phaseMachine,
        string outputRoot,
        string requestedName)
    {
        return CreatePattern(
            phaseMachine,
            outputRoot,
            requestedName,
            requestedName,
            BossPatternCreationPreset.NormalTimeline,
            BossPatternEntryMode.PhaseStart,
            null,
            Array.Empty<StateTransition.DecisionSet>(),
            "SpecialAction",
            1);
    }

    /// <summary>
    /// 선택한 생성 프리셋으로 PatternSO의 목표·실패 정책을 설정하고 State를 함께 만든다.
    /// 선택한 진입 방식에 따라 Phase Selector 또는 Source State 전이를 생성한다.
    /// </summary>
    public static BossPatternCreationResult CreatePattern(
        StateMachineSO phaseMachine,
        string outputRoot,
        string requestedName,
        string designId,
        BossPatternCreationPreset preset,
        BossPatternEntryMode entryMode,
        StateSO entrySourceState,
        StateTransition.DecisionSet[] entryConditions,
        string specialActionId,
        int requiredCount)
    {
        if (phaseMachine == null)
            throw new InvalidOperationException("패턴을 추가할 Phase StateMachineSO가 필요합니다.");
        if (phaseMachine.initialState == null)
            throw new InvalidOperationException("Phase StateMachineSO에 Initial State가 필요합니다.");

        string safeName = SanitizeAssetName(requestedName, "Pattern");
        string patternFolder = EnsureFolder(outputRoot, PatternFolderName);
        string patternPath = AssetDatabase.GenerateUniqueAssetPath(
            $"{patternFolder}/{safeName}.asset");

        BossPatternSO pattern = ScriptableObject.CreateInstance<BossPatternSO>();
        pattern.name = safeName;
        BossPatternCreationSettings settings = GetCreationSettings(preset);
        pattern.ConfigureForAuthoring(
            designId,
            entryMode,
            settings.CompletionRule,
            settings.CreateSpecialAction,
            specialActionId,
            requiredCount,
            settings.RequiredForPatternClear,
            settings.LoopTimelineUntilCleared);
        AssetDatabase.CreateAsset(pattern, patternPath);
        Undo.RegisterCreatedObjectUndo(pattern, "Create Boss Pattern");

        StateSO patternState = CreatePatternState(
            phaseMachine,
            pattern,
            $"{safeName}_State");

        WirePatternEntry(
            phaseMachine,
            patternState,
            pattern,
            entryMode,
            entrySourceState,
            entryConditions);
        RefreshStateMachine(phaseMachine);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(patternPath);
        return new BossPatternCreationResult(patternState, pattern);
    }

    /// <summary>
    /// 선택한 BossPatternSO의 직렬화 값을 복제하고 새 Pattern State에 연결한다.
    /// 원본 State 전이는 복사하지 않고 조건부 진입 자리표시자와 표준 완료 전이를 만든다.
    /// </summary>
    public static BossPatternCreationResult DuplicatePattern(
        StateMachineSO phaseMachine,
        BossPatternSO sourcePattern,
        string outputRoot,
        string requestedName)
    {
        if (phaseMachine == null || sourcePattern == null)
            throw new InvalidOperationException("복제할 Phase StateMachine과 BossPatternSO가 필요합니다.");
        if (phaseMachine.initialState == null)
            throw new InvalidOperationException("Phase StateMachineSO에 Initial State가 필요합니다.");

        string safeName = SanitizeAssetName(requestedName, $"{sourcePattern.name}_Copy");
        string patternFolder = EnsureFolder(outputRoot, PatternFolderName);
        string patternPath = AssetDatabase.GenerateUniqueAssetPath(
            $"{patternFolder}/{safeName}.asset");

        BossPatternSO pattern = UnityEngine.Object.Instantiate(sourcePattern);
        pattern.name = safeName;
        AssetDatabase.CreateAsset(pattern, patternPath);
        Undo.RegisterCreatedObjectUndo(pattern, "Duplicate Boss Pattern");

        StateSO patternState = CreatePatternState(
            phaseMachine,
            pattern,
            $"{safeName}_State");

        WirePatternEntry(
            phaseMachine,
            patternState,
            pattern,
            BossPatternEntryMode.Conditional,
            phaseMachine.initialState,
            Array.Empty<StateTransition.DecisionSet>());
        RefreshStateMachine(phaseMachine);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(patternPath);
        return new BossPatternCreationResult(patternState, pattern);
    }

    /// <summary>
    /// 선택 Pattern으로 들어오는 자동 관리 전이를 찾아 Source State와 사용자 Decision들을 반환한다.
    /// 내부의 Pattern Entry Available 보호 조건은 편집 목록에서 제외한다.
    /// </summary>
    public static BossPatternEntryWiringInfo GetPatternEntryWiring(
        StateMachineSO phaseMachine,
        StateSO patternState,
        BossPatternSO pattern)
    {
        if (phaseMachine?.allStates == null || patternState == null || pattern == null)
            return new BossPatternEntryWiringInfo(null, Array.Empty<StateTransition.DecisionSet>());

        for (int i = 0; i < phaseMachine.allStates.Count; i++)
        {
            StateSO sourceState = phaseMachine.allStates[i];
            StateTransition[] transitions = sourceState?.transitions;
            if (transitions == null) continue;

            for (int j = 0; j < transitions.Length; j++)
            {
                StateTransition transition = transitions[j];
                if (transition?.trueState != patternState ||
                    (!HasManagedEntryAvailability(transition, phaseMachine, pattern) &&
                     !IsLegacyManagedEntryTransition(transition, patternState)))
                    continue;

                var conditions = new List<StateTransition.DecisionSet>();
                if (pattern.EntryMode == BossPatternEntryMode.Conditional &&
                    transition.decisions != null)
                {
                    for (int k = 0; k < transition.decisions.Length; k++)
                    {
                        StateTransition.DecisionSet condition = transition.decisions[k];
                        if (condition.Decision is BossPatternEntryAvailableDecision available &&
                            available.Phase == phaseMachine && available.Pattern == pattern)
                            continue;
                        conditions.Add(condition);
                    }
                }

                return new BossPatternEntryWiringInfo(sourceState, conditions.ToArray());
            }
        }

        return new BossPatternEntryWiringInfo(null, Array.Empty<StateTransition.DecisionSet>());
    }

    /// <summary>
    /// 기존 자동 관리 진입 전이만 해제한 뒤 현재 Entry Mode와 편집값으로 새 전이를 생성한다.
    /// Pattern State의 출력 전이와 사용자가 만든 다른 수동 전이는 유지한다.
    /// </summary>
    public static void ApplyPatternEntryWiring(
        StateMachineSO phaseMachine,
        StateSO patternState,
        BossPatternSO pattern,
        BossPatternEntryMode entryMode,
        StateSO entrySourceState,
        StateTransition.DecisionSet[] entryConditions)
    {
        if (phaseMachine == null || patternState == null || pattern == null)
            throw new InvalidOperationException("Entry Wiring을 적용할 Phase, Pattern State와 PatternSO가 필요합니다.");
        if (!ContainsState(phaseMachine, patternState))
            throw new InvalidOperationException("Pattern State가 선택한 Boss Phase에 속하지 않습니다.");

        StateSO effectiveSource = entryMode == BossPatternEntryMode.PhaseStart
            ? phaseMachine.initialState
            : entrySourceState != null
                ? entrySourceState
                : phaseMachine.initialState;
        if (!ContainsState(phaseMachine, effectiveSource))
            throw new InvalidOperationException("Entry Source State는 선택한 Boss Phase에 속해야 합니다.");
        if (entryMode == BossPatternEntryMode.AfterPattern &&
            FindPlayPatternAction(effectiveSource)?.Pattern == null)
            throw new InvalidOperationException(
                "After Pattern 진입에는 PlayBossPatternAction을 가진 Previous Pattern State가 필요합니다.");

        RemoveManagedPatternEntryWiring(phaseMachine, patternState, pattern);
        WirePatternEntry(
            phaseMachine,
            patternState,
            pattern,
            entryMode,
            entrySourceState,
            entryConditions);
        RefreshStateMachine(phaseMachine);
        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// 기존 Phase에도 자동 재시작 fallback을 만들고 항상 Selector 전이의 마지막으로 옮긴다.
    /// 그래프에서 Phase 종료 전이를 수동 추가한 뒤 우선순위를 다시 정리할 때도 사용한다.
    /// </summary>
    public static void RefreshPhaseRestartFallback(StateMachineSO phaseMachine)
    {
        if (phaseMachine == null)
            throw new InvalidOperationException("재시작 fallback을 갱신할 Boss Phase가 필요합니다.");

        EnsurePhaseRestartFallback(phaseMachine);
        RefreshStateMachine(phaseMachine);
        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// 대상 Pattern의 Entry Available 표식을 가진 자동 전이만 Phase 전체에서 제거한다.
    /// 선행 Pattern 완료 전이는 삭제하지 않고 기본 Selector 목적지로 복원한다.
    /// </summary>
    private static void RemoveManagedPatternEntryWiring(
        StateMachineSO phaseMachine,
        StateSO patternState,
        BossPatternSO pattern)
    {
        for (int i = 0; i < phaseMachine.allStates.Count; i++)
        {
            StateSO sourceState = phaseMachine.allStates[i];
            if (sourceState?.transitions == null) continue;

            Undo.RecordObject(sourceState, "Rewire Boss Pattern Entry");
            var remaining = new List<StateTransition>();
            bool changed = false;
            for (int j = 0; j < sourceState.transitions.Length; j++)
            {
                StateTransition transition = sourceState.transitions[j];
                bool isManagedEntry = transition?.trueState == patternState &&
                                      (HasManagedEntryAvailability(
                                           transition,
                                           phaseMachine,
                                           pattern) ||
                                       IsLegacyManagedEntryTransition(
                                           transition,
                                           patternState));
                if (!isManagedEntry)
                {
                    remaining.Add(transition);
                    continue;
                }

                changed = true;
                if (ContainsDecision<IsBossPatternCompletedDecision>(transition))
                {
                    RemoveManagedEntryAvailability(transition, phaseMachine, pattern);
                    transition.trueState = phaseMachine.initialState;
                    remaining.Add(transition);
                }
            }

            if (!changed) continue;
            sourceState.transitions = remaining.ToArray();
            EditorUtility.SetDirty(sourceState);
        }
    }

    /// <summary>
    /// 전이에 대상 Phase와 Pattern이 일치하는 자동 Entry Available Decision이 있는지 검사한다.
    /// 같은 Pattern 이름의 수동 전이가 잘못 제거되지 않도록 실제 에셋 참조를 비교한다.
    /// </summary>
    private static bool HasManagedEntryAvailability(
        StateTransition transition,
        StateMachineSO phaseMachine,
        BossPatternSO pattern)
    {
        if (transition?.decisions == null) return false;
        for (int i = 0; i < transition.decisions.Length; i++)
            if (transition.decisions[i].Decision is BossPatternEntryAvailableDecision available &&
                available.Phase == phaseMachine && available.Pattern == pattern)
                return true;
        return false;
    }

    /// <summary>
    /// Entry Available 표식 도입 전에 자동 생성된 이름 기반 진입 전이를 제한적으로 식별한다.
    /// 과거 Chain Start, 표준 Pattern Completed와 Start Pattern 이름만 마이그레이션 대상으로 본다.
    /// </summary>
    private static bool IsLegacyManagedEntryTransition(
        StateTransition transition,
        StateSO patternState)
    {
        if (transition == null || patternState == null ||
            transition.trueState != patternState)
            return false;

        return transition.name == "Boss Pattern Chain Start" ||
               transition.name == "Pattern Completed" ||
               transition.name == $"Start {patternState.name}" ||
               transition.name == $"Start {patternState.name} When Condition";
    }

    /// <summary>
    /// 대상 Pattern에 속하는 자동 Entry Available Decision만 기존 전이에서 제거한다.
    /// 선행 패턴 완료 Decision과 사용자가 추가한 조건은 그대로 유지한다.
    /// </summary>
    private static void RemoveManagedEntryAvailability(
        StateTransition transition,
        StateMachineSO phaseMachine,
        BossPatternSO pattern)
    {
        if (transition?.decisions == null) return;
        var remaining = new List<StateTransition.DecisionSet>();
        for (int i = 0; i < transition.decisions.Length; i++)
        {
            StateDecision decision = transition.decisions[i].Decision;
            if (decision is BossPatternEntryAvailableDecision available &&
                available.Phase == phaseMachine && available.Pattern == pattern)
                continue;
            remaining.Add(transition.decisions[i]);
        }
        transition.decisions = remaining.ToArray();
    }

    /// <summary>
    /// 한 전이에 지정 StateDecision 타입이 들어 있는지 검사한다.
    /// 자동 AfterPattern 전이를 삭제 대신 Selector로 복원할지 결정할 때 사용한다.
    /// </summary>
    private static bool ContainsDecision<T>(StateTransition transition) where T : StateDecision
    {
        if (transition?.decisions == null) return false;
        for (int i = 0; i < transition.decisions.Length; i++)
            if (transition.decisions[i].Decision is T)
                return true;
        return false;
    }

    /// <summary>
    /// PatternSO의 진입 방식에 따라 Phase 시작·선행 패턴 완료·조건부 전이를 자동 생성한다.
    /// 새 패턴의 완료 전이는 Selector로 돌아가며 Phase 종료와 다음 Phase 선택은 별도 결과 전이가 담당한다.
    /// </summary>
    private static void WirePatternEntry(
        StateMachineSO phaseMachine,
        StateSO patternState,
        BossPatternSO pattern,
        BossPatternEntryMode entryMode,
        StateSO requestedSourceState,
        StateTransition.DecisionSet[] entryConditions)
    {
        StateSO selectorState = phaseMachine.initialState;
        StateSO sourceState = entryMode == BossPatternEntryMode.PhaseStart
            ? selectorState
            : requestedSourceState != null
                ? requestedSourceState
                : selectorState;
        if (!ContainsState(phaseMachine, sourceState))
            throw new InvalidOperationException(
                "Pattern Entry Source State는 선택한 Boss Phase에 속해야 합니다.");

        if (entryMode == BossPatternEntryMode.AfterPattern)
        {
            PlayBossPatternAction sourcePlayAction = FindPlayPatternAction(sourceState);
            if (sourcePlayAction?.Pattern == null)
                throw new InvalidOperationException(
                    "After Pattern 진입에는 PlayBossPatternAction을 가진 Source State가 필요합니다.");

            SetPatternCompletionTransition(sourceState, sourcePlayAction.Pattern, patternState);
            StateTransition sourceCompletion =
                FindPatternResultTransition<IsBossPatternCompletedDecision>(
                    sourceState,
                    sourcePlayAction.Pattern);
            RemoveDecisions<BossPatternEntryAvailableDecision>(sourceCompletion);
            AppendDecision(
                sourceCompletion,
                new BossPatternEntryAvailableDecision(phaseMachine, pattern, false));
            EditorUtility.SetDirty(sourceState);
            EnsurePhaseRestartFallback(phaseMachine);
            return;
        }

        string transitionName = $"Start {patternState.name}";
        if (entryMode == BossPatternEntryMode.Conditional)
            transitionName += " When Condition";

        var decisionSets = new List<StateTransition.DecisionSet>();
        if (entryMode == BossPatternEntryMode.Conditional && entryConditions != null)
        {
            for (int i = 0; i < entryConditions.Length; i++)
            {
                StateTransition.DecisionSet condition = entryConditions[i];
                if (condition.Decision == null) continue;
                decisionSets.Add(condition);
            }
        }

        if (entryMode == BossPatternEntryMode.Conditional && decisionSets.Count == 0)
            decisionSets.Add(new StateTransition.DecisionSet
            {
                Decision = new UnconfiguredBossPatternEntryDecision(),
                negate = false
            });
        if (pattern != null)
            decisionSets.Add(new StateTransition.DecisionSet
            {
                Decision = new BossPatternEntryAvailableDecision(phaseMachine, pattern),
                negate = false
            });

        Undo.RecordObject(sourceState, "Wire Boss Pattern Entry");
        StateTransition transition = new StateTransition
        {
            name = transitionName,
            activationInput = EnumManager.InputType.None,
            availableTime = 0f,
            disableTime = 0f,
            decisions = decisionSets.ToArray(),
            trueState = patternState,
            falseState = null,
            showTrueStatePort = true,
            showFalseStatePort = false
        };
        if (entryMode == BossPatternEntryMode.Conditional && sourceState != selectorState)
            InsertBeforePatternCompletion(sourceState, transition);
        else
            AppendTransition(sourceState, transition);
        EditorUtility.SetDirty(sourceState);
        EnsurePhaseRestartFallback(phaseMachine);
    }

    /// <summary>
    /// State의 직접 Action에서 첫 PlayBossPatternAction을 찾아 선행 패턴 자동 연결에 사용한다.
    /// Group 또는 Sequence 내부 Action은 명시적 그래프 연결 대상으로 남긴다.
    /// </summary>
    private static PlayBossPatternAction FindPlayPatternAction(StateSO state)
    {
        if (state == null) return null;
        foreach (StateAction action in EnumerateDirectActions(state))
            if (action is PlayBossPatternAction playAction)
                return playAction;
        return null;
    }

    /// <summary>
    /// Pattern Completed Decision을 가진 표준 전이를 찾아 다음 State 목적지만 갱신한다.
    /// 전이가 없으면 새로 추가하여 기존 임의 전이와 Action을 보존한다.
    /// </summary>
    private static void SetPatternCompletionTransition(
        StateSO patternState,
        BossPatternSO pattern,
        StateSO nextState)
    {
        Undo.RecordObject(patternState, "Wire Boss Pattern Completion");
        StateTransition transition = FindPatternResultTransition<IsBossPatternCompletedDecision>(
            patternState,
            pattern);
        if (transition == null)
        {
            transition = CreatePatternResultTransition(
                "Pattern Completed",
                new IsBossPatternCompletedDecision(pattern));
            AppendTransition(patternState, transition);
        }

        transition.trueState = nextState;
        transition.falseState = null;
        transition.showTrueStatePort = true;
        transition.showFalseStatePort = false;
        EditorUtility.SetDirty(patternState);
    }

    /// <summary>
    /// State의 transitions에서 지정 이름과 정확히 일치하는 첫 전이를 반환한다.
    /// 자동 생성기가 소유한 시작 전이만 안전하게 다시 찾는 데 사용한다.
    /// </summary>
    private static StateTransition FindTransitionByName(StateSO state, string transitionName)
    {
        if (state?.transitions == null) return null;
        for (int i = 0; i < state.transitions.Length; i++)
            if (state.transitions[i] != null && state.transitions[i].name == transitionName)
                return state.transitions[i];
        return null;
    }

    /// <summary>
    /// 같은 PatternSO를 검사하는 지정 결과 Decision의 전이를 찾는다.
    /// 완료와 실패 표준 전이를 이름 변경과 관계없이 안정적으로 식별한다.
    /// </summary>
    private static StateTransition FindPatternResultTransition<T>(
        StateSO state,
        BossPatternSO pattern) where T : StateDecision
    {
        if (state?.transitions == null) return null;
        for (int i = 0; i < state.transitions.Length; i++)
        {
            StateTransition transition = state.transitions[i];
            if (transition?.decisions == null) continue;
            for (int j = 0; j < transition.decisions.Length; j++)
            {
                StateDecision decision = transition.decisions[j].Decision;
                if (decision is IsBossPatternCompletedDecision completed &&
                    typeof(T) == typeof(IsBossPatternCompletedDecision) &&
                    completed.Pattern == pattern)
                    return transition;
                if (decision is IsBossPatternFailedDecision failed &&
                    typeof(T) == typeof(IsBossPatternFailedDecision) &&
                    failed.Pattern == pattern)
                    return transition;
            }
        }

        return null;
    }

    /// <summary>
    /// 하나의 결과 Decision을 가진 즉시 평가 표준 전이를 생성한다.
    /// true 목적지는 호출 측에서 정하고 false 포트는 기본적으로 숨긴다.
    /// </summary>
    private static StateTransition CreatePatternResultTransition(
        string name,
        StateDecision decision)
    {
        return new StateTransition
        {
            name = name,
            activationInput = EnumManager.InputType.None,
            availableTime = 0f,
            disableTime = 0f,
            decisions = new[]
            {
                new StateTransition.DecisionSet
                {
                    Decision = decision,
                    negate = false
                }
            },
            showTrueStatePort = true,
            showFalseStatePort = false
        };
    }

    /// <summary>
    /// 기존 전이의 DecisionSet 끝에 새 조건을 추가하고 null 배열을 안전하게 처리한다.
    /// 자동 생성한 선행 패턴 완료 조건과 대상 패턴의 1회성 진입 보호를 AND로 조합한다.
    /// </summary>
    private static void AppendDecision(StateTransition transition, StateDecision decision)
    {
        if (transition == null || decision == null) return;
        StateTransition.DecisionSet[] decisions =
            transition.decisions ?? Array.Empty<StateTransition.DecisionSet>();
        Array.Resize(ref decisions, decisions.Length + 1);
        decisions[decisions.Length - 1] = new StateTransition.DecisionSet
        {
            Decision = decision,
            negate = false
        };
        transition.decisions = decisions;
    }

    /// <summary>
    /// 자동 재배선 전에 지정 타입의 이전 진입 보호 Decision만 전이에서 제거한다.
    /// 사용자가 추가한 다른 완료·조건 Decision은 배열 순서와 함께 그대로 유지한다.
    /// </summary>
    private static void RemoveDecisions<T>(StateTransition transition) where T : StateDecision
    {
        if (transition?.decisions == null) return;
        var remaining = new List<StateTransition.DecisionSet>();
        for (int i = 0; i < transition.decisions.Length; i++)
            if (transition.decisions[i].Decision is not T)
                remaining.Add(transition.decisions[i]);
        transition.decisions = remaining.ToArray();
    }

    /// <summary>
    /// 기존 transitions 배열 끝에 새 전이를 추가하고 null 배열을 안전하게 처리한다.
    /// 배열 순서를 유지하여 먼저 만든 사용자 전이의 평가 우선순위를 바꾸지 않는다.
    /// </summary>
    private static void AppendTransition(StateSO state, StateTransition transition)
    {
        StateTransition[] source = state.transitions ?? Array.Empty<StateTransition>();
        Array.Resize(ref source, source.Length + 1);
        source[source.Length - 1] = transition;
        state.transitions = source;
    }

    /// <summary>
    /// 조건부 전이를 Source Pattern의 표준 완료 전이 바로 앞에 삽입한다.
    /// 특수행동 실패·확률 분기가 Selector 복귀보다 먼저 평가되게 한다.
    /// </summary>
    private static void InsertBeforePatternCompletion(StateSO state, StateTransition transition)
    {
        StateTransition[] source = state.transitions ?? Array.Empty<StateTransition>();
        int insertIndex = source.Length;
        for (int i = 0; i < source.Length; i++)
        {
            if (!ContainsDecision<IsBossPatternCompletedDecision>(source[i])) continue;
            insertIndex = i;
            break;
        }

        var result = new StateTransition[source.Length + 1];
        Array.Copy(source, 0, result, 0, insertIndex);
        result[insertIndex] = transition;
        Array.Copy(source, insertIndex, result, insertIndex + 1, source.Length - insertIndex);
        state.transitions = result;
    }

    /// <summary>
    /// Selector에서 진입 가능한 패턴이 없을 때 첫 PhaseStart 패턴으로 돌아가는 마지막 전이를 보장한다.
    /// 재시작 Action은 페이즈 지역 실행 이력만 지워 특수행동과 전역 클리어 기록을 유지한다.
    /// </summary>
    private static void EnsurePhaseRestartFallback(StateMachineSO phaseMachine)
    {
        StateSO selectorState = phaseMachine?.initialState;
        if (selectorState == null || phaseMachine.allStates == null) return;

        StateSO firstPhaseStartState = null;
        for (int i = 0; i < phaseMachine.allStates.Count; i++)
        {
            StateSO candidate = phaseMachine.allStates[i];
            PlayBossPatternAction playAction = FindPlayPatternAction(candidate);
            if (playAction?.Pattern?.EntryMode != BossPatternEntryMode.PhaseStart) continue;
            firstPhaseStartState = candidate;
            break;
        }

        StateTransition[] source = selectorState.transitions ?? Array.Empty<StateTransition>();
        var remaining = new List<StateTransition>(source.Length + 1);
        for (int i = 0; i < source.Length; i++)
            if (source[i]?.name != PhaseRestartFallbackTransitionName)
                remaining.Add(source[i]);

        if (firstPhaseStartState != null)
        {
            remaining.Add(new StateTransition
            {
                name = PhaseRestartFallbackTransitionName,
                activationInput = EnumManager.InputType.None,
                availableTime = 0f,
                disableTime = 0f,
                decisions = new[]
                {
                    new StateTransition.DecisionSet
                    {
                        Decision = new IsBossPatternCompletedDecision(),
                        negate = false
                    }
                },
                trueState = firstPhaseStartState,
                falseState = null,
                Action = new BeginBossPhaseAction(phaseMachine),
                showTrueStatePort = true,
                showFalseStatePort = false
            });
        }

        Undo.RecordObject(selectorState, "Update Boss Phase Restart Fallback");
        selectorState.transitions = remaining.ToArray();
        EditorUtility.SetDirty(selectorState);
    }

    /// <summary>
    /// State의 Enter/Update/Exit/ActionSequence와 전이 Action을 직접 순회한다.
    /// 중첩 Group/Sequence 내부는 임의의 private 직렬화 구조를 깨지 않도록 탐색하지 않는다.
    /// </summary>
    public static IEnumerable<StateAction> EnumerateDirectActions(StateSO state)
    {
        if (state == null) yield break;

        foreach (StateAction action in EnumerateActions(state.EnterActions)) yield return action;
        foreach (StateAction action in EnumerateActions(state.UpdateActions)) yield return action;
        foreach (StateAction action in EnumerateActions(state.ExitActions)) yield return action;

        if (state.actionSequence != null)
        {
            for (int i = 0; i < state.actionSequence.Length; i++)
                if (state.actionSequence[i].Action != null)
                    yield return state.actionSequence[i].Action;
        }

        if (state.transitions == null) yield break;
        for (int i = 0; i < state.transitions.Length; i++)
            if (state.transitions[i]?.Action != null)
                yield return state.transitions[i].Action;
    }

    /// <summary>
    /// 선택한 StateMachineSO 에셋이 저장된 프로젝트 폴더 경로를 반환한다.
    /// 저장되지 않은 에셋이면 Assets 폴더를 안전한 기본값으로 사용한다.
    /// </summary>
    public static string GetDefaultOutputRoot(StateMachineSO stateMachine)
    {
        string path = stateMachine != null
            ? AssetDatabase.GetAssetPath(stateMachine)
            : string.Empty;
        return string.IsNullOrWhiteSpace(path)
            ? "Assets"
            : Path.GetDirectoryName(path)?.Replace('\\', '/') ?? "Assets";
    }

    /// <summary>
    /// Unity 에셋 파일명에 사용할 수 없는 문자를 제거하고 비어 있으면 대체명을 사용한다.
    /// State와 SO 이름이 같은 규칙을 사용하도록 앞뒤 공백도 함께 정리한다.
    /// </summary>
    public static string SanitizeAssetName(string requestedName, string fallback)
    {
        string result = string.IsNullOrWhiteSpace(requestedName)
            ? fallback
            : requestedName.Trim();

        char[] invalidCharacters = Path.GetInvalidFileNameChars();
        for (int i = 0; i < invalidCharacters.Length; i++)
            result = result.Replace(invalidCharacters[i].ToString(), string.Empty);

        return string.IsNullOrWhiteSpace(result) ? fallback : result;
    }

    /// <summary>
    /// 한 페이즈 StateMachine에서 PlayBossPatternAction을 가진 State들을 수집한다.
    /// 동일 State에 Play Action이 여러 개라면 각 연결을 별도 패턴 노드로 표시한다.
    /// </summary>
    private static void PopulatePatterns(BossPhaseAuthoringNode phaseNode)
    {
        StateMachineSO phaseMachine = phaseNode.PhaseMachine;
        if (phaseMachine == null || phaseMachine.allStates == null) return;

        for (int i = 0; i < phaseMachine.allStates.Count; i++)
        {
            StateSO state = phaseMachine.allStates[i];
            if (state == null) continue;

            foreach (StateAction action in EnumerateDirectActions(state))
            {
                if (action is PlayBossPatternAction playAction)
                {
                    phaseNode.Patterns.Add(new BossPatternAuthoringNode(
                        state,
                        playAction.Pattern,
                        playAction));
                }
            }
        }
    }

    /// <summary>
    /// 보스 프리팹에 계층·패턴·공격 실행에 필요한 핵심 컴포넌트가 있는지 검사한다.
    /// 프리팹을 지정하지 않은 경우 선택 사항임을 알리는 정보만 남긴다.
    /// </summary>
    private static void ValidateBossPrefab(
        GameObject bossPrefab,
        List<BossAuthoringValidationIssue> issues)
    {
        if (bossPrefab == null)
        {
            issues.Add(new BossAuthoringValidationIssue(
                MessageType.Info,
                "Boss Prefab을 선택하면 필수 컴포넌트도 검사합니다."));
            return;
        }

        if (bossPrefab.GetComponent<StateController>() == null)
            issues.Add(new BossAuthoringValidationIssue(
                MessageType.Error,
                "Boss Prefab에 StateController가 없습니다.",
                bossPrefab));

        if (bossPrefab.GetComponent<BossPatternRunner>() == null)
            issues.Add(new BossAuthoringValidationIssue(
                MessageType.Error,
                "Boss Prefab에 BossPatternRunner가 없습니다.",
                bossPrefab));

        if (bossPrefab.GetComponent<CombatAttackModule>() == null)
            issues.Add(new BossAuthoringValidationIssue(
                MessageType.Error,
                "Boss Prefab에 CombatAttackModule이 없습니다.",
                bossPrefab));
    }

    /// <summary>
    /// 한 페이즈의 시작·반환 연결, 종료 Action, 패턴 State와 타임라인을 검사한다.
    /// 문제는 가능한 한 실제 원인 에셋을 Context로 지정한다.
    /// </summary>
    private static void ValidatePhase(
        StateMachineSO mainMachine,
        BossPhaseAuthoringNode phase,
        Dictionary<string, int> specialActionRequiredCounts,
        List<BossAuthoringValidationIssue> issues)
    {
        if (phase.PhaseMachine == null)
        {
            issues.Add(new BossAuthoringValidationIssue(
                MessageType.Error,
                $"'{phase.LaunchState?.name}'의 Sub StateMachine이 비어 있습니다.",
                phase.LaunchState));
            return;
        }

        if (phase.StartAction.ReturnState == null)
            issues.Add(new BossAuthoringValidationIssue(
                MessageType.Error,
                $"'{phase.PhaseMachine.name}'의 Return State가 비어 있습니다.",
                phase.LaunchState));
        else if (!ContainsState(mainMachine, phase.StartAction.ReturnState))
            issues.Add(new BossAuthoringValidationIssue(
                MessageType.Error,
                $"'{phase.PhaseMachine.name}'의 Return State가 Main StateMachine에 속하지 않습니다.",
                phase.StartAction.ReturnState));

        if (phase.StartAction.StartState != null &&
            !ContainsState(phase.PhaseMachine, phase.StartAction.StartState))
            issues.Add(new BossAuthoringValidationIssue(
                MessageType.Error,
                $"'{phase.PhaseMachine.name}'의 Start State가 해당 페이즈에 속하지 않습니다.",
                phase.StartAction.StartState));

        bool beginsPhaseResults = HasBeginPhaseBeforeStart(
            phase.LaunchState,
            phase.PhaseMachine);

        if (!beginsPhaseResults)
            issues.Add(new BossAuthoringValidationIssue(
                MessageType.Warning,
                $"'{phase.LaunchState?.name}'에서 Start Sub State Machine보다 먼저 같은 Phase의 Begin Boss Phase Action을 실행해야 합니다.",
                phase.LaunchState));

        if (!ContainsAction<CompleteSubStateMachineAction>(phase.PhaseMachine))
            issues.Add(new BossAuthoringValidationIssue(
                MessageType.Error,
                $"'{phase.PhaseMachine.name}'에 CompleteSubStateMachineAction이 없습니다.",
                phase.PhaseMachine));

        if (phase.Patterns.Count == 0)
            issues.Add(new BossAuthoringValidationIssue(
                MessageType.Warning,
                $"'{phase.PhaseMachine.name}'에 PlayBossPatternAction을 가진 패턴 State가 없습니다.",
                phase.PhaseMachine));

        for (int i = 0; i < phase.Patterns.Count; i++)
        {
            ValidatePattern(
                phase.PhaseMachine,
                phase.Patterns[i],
                specialActionRequiredCounts,
                issues);
        }
    }

    /// <summary>
    /// 패턴 참조, 완료 Decision, 타임라인 Action과 Composable Attack 슬롯을 검사한다.
    /// 동일 패턴의 설정 실수를 State와 PatternSO 양쪽 문맥으로 보고한다.
    /// </summary>
    private static void ValidatePattern(
        StateMachineSO phaseMachine,
        BossPatternAuthoringNode patternNode,
        Dictionary<string, int> bossSpecialActionRequiredCounts,
        List<BossAuthoringValidationIssue> issues)
    {
        BossPatternSO pattern = patternNode.Pattern;
        if (pattern == null)
        {
            issues.Add(new BossAuthoringValidationIssue(
                MessageType.Error,
                $"'{patternNode.State.name}'의 PlayBossPatternAction에 PatternSO가 없습니다.",
                patternNode.State));
            return;
        }

        if (!HasMatchingCompletionDecision(patternNode.State, pattern))
            issues.Add(new BossAuthoringValidationIssue(
                MessageType.Error,
                $"'{patternNode.State.name}'에 같은 PatternSO를 검사하는 완료 Decision이 없습니다.",
                patternNode.State));

        if (!HasIncomingTransition(phaseMachine, patternNode.State, out bool hasPlaceholder))
            issues.Add(new BossAuthoringValidationIssue(
                MessageType.Error,
                $"'{pattern.name}'으로 들어오는 StateTransition이 없습니다.",
                patternNode.State));
        else if (hasPlaceholder)
            issues.Add(new BossAuthoringValidationIssue(
                MessageType.Warning,
                $"'{pattern.name}'의 조건부 진입이 Always False 자리표시자 상태입니다. 실제 Decision으로 교체하세요.",
                patternNode.State));

        if (pattern.UsesRequiredSpecialActions &&
            !HasRequiredSpecialAction(pattern))
            issues.Add(new BossAuthoringValidationIssue(
                MessageType.Error,
                $"'{pattern.name}'은 필수 특수행동 완료 방식이지만 Pattern Clear 필수 항목이 없습니다.",
                pattern));

        if (pattern.EntryMode == BossPatternEntryMode.PhaseStart &&
            pattern.AllowRepeatWithinPhase)
            issues.Add(new BossAuthoringValidationIssue(
                MessageType.Warning,
                $"'{pattern.name}'은 PhaseStart이면서 반복 허용 상태라 Selector 복귀 직후 계속 재실행될 수 있습니다.",
                pattern));

        ValidatePatternSpecialActions(pattern, bossSpecialActionRequiredCounts, issues);

        BossPatternTimelineEntry[] timeline = pattern.Timeline;
        if (timeline == null || timeline.Length == 0)
            issues.Add(new BossAuthoringValidationIssue(
                MessageType.Warning,
                $"'{pattern.name}'의 타임라인이 비어 있습니다.",
                pattern));

        int attackCount = 0;
        int defaultSlotCount = 0;
        if (timeline != null)
        {
            for (int i = 0; i < timeline.Length; i++)
            {
                BossPatternTimelineEntry entry = timeline[i];
                if (entry == null)
                {
                    issues.Add(new BossAuthoringValidationIssue(
                        MessageType.Warning,
                        $"'{pattern.name}' 타임라인 {i}번 Entry가 비어 있습니다.",
                        pattern));
                    continue;
                }

                StateAction[] actions = entry.Actions;
                if (actions == null) continue;
                for (int j = 0; j < actions.Length; j++)
                {
                    if (actions[j] == null)
                    {
                        issues.Add(new BossAuthoringValidationIssue(
                            MessageType.Warning,
                            $"'{pattern.name}'의 '{entry.Label}' Entry에 빈 Action이 있습니다.",
                            pattern));
                        continue;
                    }

                    if (actions[j] is StartComposableAttackAction attackAction)
                    {
                        attackCount++;
                        if (string.IsNullOrWhiteSpace(attackAction.Slot) ||
                            attackAction.Slot.Trim() == "Default")
                            defaultSlotCount++;
                    }
                }
            }
        }

        if (attackCount > 1 && attackCount == defaultSlotCount)
            issues.Add(new BossAuthoringValidationIssue(
                MessageType.Warning,
                $"'{pattern.name}'의 Composable Attack {attackCount}개가 모두 Default 슬롯을 사용합니다.",
                pattern));
    }

    /// <summary>
    /// 한 Pattern 안의 중복 ID와 공유 논리 ID 사이의 Required Count 불일치를 검사한다.
    /// 여러 Pattern에서 같은 ID·횟수를 쓰면 하나의 특수행동 진행도를 공동 누적한다.
    /// </summary>
    private static void ValidatePatternSpecialActions(
        BossPatternSO pattern,
        Dictionary<string, int> bossSpecialActionRequiredCounts,
        List<BossAuthoringValidationIssue> issues)
    {
        BossSpecialActionDefinition[] specialActions = pattern?.SpecialActions;
        if (specialActions == null) return;
        var patternSpecialActionIds = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < specialActions.Length; i++)
        {
            BossSpecialActionDefinition specialAction = specialActions[i];
            if (specialAction == null)
            {
                issues.Add(new BossAuthoringValidationIssue(
                    MessageType.Error,
                    $"'{pattern.name}'의 Special Action {i}가 비어 있습니다.",
                    pattern));
                continue;
            }

            if (!patternSpecialActionIds.Add(specialAction.Id))
            {
                issues.Add(new BossAuthoringValidationIssue(
                    MessageType.Error,
                    $"'{pattern.name}' 안에 Special Action ID '{specialAction.Id}'가 중복됩니다.",
                    pattern));
                continue;
            }

            if (!bossSpecialActionRequiredCounts.TryGetValue(
                    specialAction.Id,
                    out int requiredCount))
            {
                bossSpecialActionRequiredCounts.Add(
                    specialAction.Id,
                    specialAction.RequiredCount);
                continue;
            }

            if (requiredCount != specialAction.RequiredCount)
                issues.Add(new BossAuthoringValidationIssue(
                    MessageType.Error,
                    $"공유 Special Action ID '{specialAction.Id}'의 Required Count가 " +
                    $"{requiredCount}와 {specialAction.RequiredCount}로 다릅니다.",
                    pattern));
        }
    }

    /// <summary>
    /// 패턴의 특수행동 중 Pattern Clear 필수로 지정된 항목이 하나 이상인지 확인한다.
    /// 잘못된 필수 완료 규칙이 영구 대기 상태를 만드는 것을 검증할 때 사용한다.
    /// </summary>
    private static bool HasRequiredSpecialAction(BossPatternSO pattern)
    {
        BossSpecialActionDefinition[] definitions = pattern?.SpecialActions;
        if (definitions == null) return false;
        for (int i = 0; i < definitions.Length; i++)
            if (definitions[i] != null && definitions[i].RequiredForPatternClear)
                return true;
        return false;
    }

    /// <summary>
    /// Phase 안의 모든 일반 전이에서 대상 Pattern State로 들어오는 연결을 찾는다.
    /// 발견한 연결에 안전용 Always False Decision이 남아 있는지도 함께 반환한다.
    /// </summary>
    private static bool HasIncomingTransition(
        StateMachineSO phaseMachine,
        StateSO targetState,
        out bool hasPlaceholder)
    {
        hasPlaceholder = false;
        if (phaseMachine?.allStates == null || targetState == null) return false;

        bool found = false;
        for (int i = 0; i < phaseMachine.allStates.Count; i++)
        {
            StateTransition[] transitions = phaseMachine.allStates[i]?.transitions;
            if (transitions == null) continue;
            for (int j = 0; j < transitions.Length; j++)
            {
                StateTransition transition = transitions[j];
                if (transition?.trueState != targetState) continue;
                found = true;
                if (transition.decisions == null) continue;
                for (int k = 0; k < transition.decisions.Length; k++)
                    if (transition.decisions[k].Decision is UnconfiguredBossPatternEntryDecision)
                        hasPlaceholder = true;
            }
        }

        return found;
    }

    /// <summary>
    /// Launch State EnterActions에서 같은 Phase의 Begin Action이 Start Action보다 앞서는지 검사한다.
    /// Phase 결과가 초기화되기 전에 서브 StateMachine이 예약되는 잘못된 순서를 차단한다.
    /// </summary>
    private static bool HasBeginPhaseBeforeStart(StateSO launchState, StateMachineSO phaseMachine)
    {
        StateAction[] actions = launchState?.EnterActions;
        if (actions == null) return false;

        bool foundBegin = false;
        for (int i = 0; i < actions.Length; i++)
        {
            if (actions[i] is BeginBossPhaseAction beginAction &&
                beginAction.Phase == phaseMachine)
                foundBegin = true;
            if (actions[i] is StartSubStateMachineAction startAction &&
                startAction.SubStateMachine == phaseMachine)
                return foundBegin;
        }

        return false;
    }

    /// <summary>
    /// 패턴 State의 전이 중 같은 PatternSO를 검사하는 완료 Decision이 있는지 찾는다.
    /// falseState 목적지와 다른 종류의 Decision은 완료 연결로 보지 않는다.
    /// </summary>
    private static bool HasMatchingCompletionDecision(StateSO state, BossPatternSO pattern)
    {
        if (state?.transitions == null) return false;

        for (int i = 0; i < state.transitions.Length; i++)
        {
            StateTransition transition = state.transitions[i];
            if (transition?.decisions == null) continue;

            for (int j = 0; j < transition.decisions.Length; j++)
            {
                if (transition.decisions[j].Decision is IsBossPatternCompletedDecision completed &&
                    completed.Pattern == pattern && !transition.decisions[j].negate)
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// StateMachine의 모든 State에서 지정한 직접 Action 형식이 하나라도 있는지 검사한다.
    /// 페이즈 종료 Action 같은 필수 연결의 존재 확인에 사용한다.
    /// </summary>
    private static bool ContainsAction<T>(StateMachineSO stateMachine) where T : StateAction
    {
        if (stateMachine?.allStates == null) return false;

        for (int i = 0; i < stateMachine.allStates.Count; i++)
            foreach (StateAction action in EnumerateDirectActions(stateMachine.allStates[i]))
                if (action is T)
                    return true;

        return false;
    }

    /// <summary>
    /// 전달된 Action 배열에서 null을 제외한 항목을 순서대로 반환한다.
    /// 호출 측이 여러 Action 위치를 같은 방식으로 순회할 수 있게 한다.
    /// </summary>
    private static IEnumerable<StateAction> EnumerateActions(StateAction[] actions)
    {
        if (actions == null) yield break;
        for (int i = 0; i < actions.Length; i++)
            if (actions[i] != null)
                yield return actions[i];
    }

    /// <summary>
    /// State가 StateMachine의 Initial State 또는 All States 목록에 포함되는지 확인한다.
    /// 자동 배선과 런타임 StateController가 같은 소속 규칙을 사용하게 한다.
    /// </summary>
    private static bool ContainsState(StateMachineSO stateMachine, StateSO state)
    {
        if (stateMachine == null || state == null) return false;
        if (stateMachine.initialState == state) return true;
        return stateMachine.allStates != null && stateMachine.allStates.Contains(state);
    }

    /// <summary>
    /// 새 StateMachineSO가 즉시 커스텀 변수를 사용할 수 있도록 빈 저장소를 생성한다.
    /// 세 변수 종류의 Dictionary를 모두 초기화하여 첫 Action 접근 시 null을 방지한다.
    /// </summary>
    private static CustomVariableSets CreateVariableSets()
    {
        return new CustomVariableSets
        {
            boolVariables = new SerializedDictionary<string, CustomVariable<bool>>(),
            intVariables = new SerializedDictionary<string, CustomVariable<int>>(),
            floatVariables = new SerializedDictionary<string, CustomVariable<float>>()
        };
    }

    /// <summary>
    /// StateSO를 생성해 지정한 StateMachineSO 에셋의 서브에셋과 All States에 추가한다.
    /// 생성과 부모 변경을 Undo에 기록하여 에디터 작업을 되돌릴 수 있게 한다.
    /// </summary>
    private static StateSO CreateStateSubAsset(StateMachineSO owner, string stateName)
    {
        Undo.RecordObject(owner, "Add State To StateMachine");
        owner.allStates ??= new List<StateSO>();

        StateSO state = ScriptableObject.CreateInstance<StateSO>();
        state.name = stateName;
        AssetDatabase.AddObjectToAsset(state, owner);
        Undo.RegisterCreatedObjectUndo(state, "Create State Sub Asset");
        owner.allStates.Add(state);
        EditorUtility.SetDirty(state);
        EditorUtility.SetDirty(owner);
        return state;
    }

    /// <summary>
    /// PatternSO를 실행하고 완료 결과를 검사하는 표준 Pattern State를 만든다.
    /// 완료 시 Selector로 돌아가 Phase가 다음 패턴 또는 Finish 분기를 다시 선택하게 한다.
    /// </summary>
    private static StateSO CreatePatternState(
        StateMachineSO phaseMachine,
        BossPatternSO pattern,
        string stateName)
    {
        StateSO patternState = CreateStateSubAsset(phaseMachine, stateName);
        patternState.EnterActions = new StateAction[] { new PlayBossPatternAction(pattern) };
        patternState.transitions = Array.Empty<StateTransition>();
        SetPatternCompletionTransition(patternState, pattern, phaseMachine.initialState);

        EditorUtility.SetDirty(patternState);
        return patternState;
    }

    /// <summary>
    /// StateMachine의 명령 바인딩 정보를 다시 만들고 변경된 에셋을 Dirty 처리한다.
    /// 새 State와 전이가 즉시 런타임 및 그래프 도구에서 보이게 한다.
    /// </summary>
    private static void RefreshStateMachine(StateMachineSO stateMachine)
    {
        if (stateMachine == null) return;
        stateMachine.UpdateStateMachine();
        if (stateMachine.graph != null)
            StateMachineGraphSynchronizer.Synchronize(stateMachine);
        EditorUtility.SetDirty(stateMachine);
    }

    /// <summary>
    /// Assets 아래의 상대 폴더를 단계별로 생성하고 마지막 경로를 반환한다.
    /// 프로젝트 밖의 경로는 에셋 생성 대상이 될 수 없으므로 거부한다.
    /// </summary>
    private static string EnsureFolder(string root, string childFolder)
    {
        string safeRoot = string.IsNullOrWhiteSpace(root) ? "Assets" : root.Replace('\\', '/');
        if (!safeRoot.StartsWith("Assets", StringComparison.Ordinal) ||
            !AssetDatabase.IsValidFolder(safeRoot))
            throw new InvalidOperationException($"유효한 Assets 폴더가 아닙니다: {safeRoot}");

        string childPath = $"{safeRoot}/{childFolder}";
        if (!AssetDatabase.IsValidFolder(childPath))
            AssetDatabase.CreateFolder(safeRoot, childFolder);
        return childPath;
    }
}
