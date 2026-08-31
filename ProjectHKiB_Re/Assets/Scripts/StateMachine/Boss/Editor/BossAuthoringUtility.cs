using System;
using System.Collections.Generic;
using System.IO;
using AYellowpaper.SerializedCollections;
using Combat;
using StateMachine;
using UnityEditor;
using UnityEngine;

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
/// 보스 제작 창의 계층 탐색, 검증, 페이즈·패턴 에셋 생성을 담당한다.
/// 에셋 변경은 Undo와 Dirty를 기록하고 StateMachineSO를 다시 갱신한다.
/// </summary>
internal static class BossAuthoringUtility
{
    private const string PhaseFolderName = "Phases";
    private const string PatternFolderName = "Patterns";

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

        if (phases == null || phases.Count == 0)
            issues.Add(new BossAuthoringValidationIssue(
                MessageType.Warning,
                "StartSubStateMachineAction으로 연결된 페이즈가 없습니다.",
                mainMachine));

        ValidateBossPrefab(bossPrefab, issues);

        if (phases != null)
        {
            for (int i = 0; i < phases.Count; i++)
                ValidatePhase(mainMachine, phases[i], issues);
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
    /// Play Action과 완료 후 Selector로 돌아가는 Completed Decision을 자동 배선한다.
    /// </summary>
    public static BossPatternCreationResult CreatePattern(
        StateMachineSO phaseMachine,
        string outputRoot,
        string requestedName)
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
        AssetDatabase.CreateAsset(pattern, patternPath);
        Undo.RegisterCreatedObjectUndo(pattern, "Create Boss Pattern");

        StateSO patternState = CreatePatternState(
            phaseMachine,
            pattern,
            $"{safeName}_State");

        RefreshStateMachine(phaseMachine);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(patternPath);
        return new BossPatternCreationResult(patternState, pattern);
    }

    /// <summary>
    /// 선택한 BossPatternSO의 직렬화 값을 복제하고 새 Pattern State에 연결한다.
    /// 원본 State 전이는 복사하지 않고 표준 Play/Completed 배선을 새로 만든다.
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

        RefreshStateMachine(phaseMachine);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(patternPath);
        return new BossPatternCreationResult(patternState, pattern);
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
            ValidatePattern(phase.PhaseMachine, phase.Patterns[i], issues);
    }

    /// <summary>
    /// 패턴 참조, 완료 Decision, 타임라인 Action과 Composable Attack 슬롯을 검사한다.
    /// 동일 패턴의 설정 실수를 State와 PatternSO 양쪽 문맥으로 보고한다.
    /// </summary>
    private static void ValidatePattern(
        StateMachineSO phaseMachine,
        BossPatternAuthoringNode patternNode,
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
    /// PatternSO를 실행하고 완료 뒤 Phase Initial State로 돌아가는 표준 Pattern State를 만든다.
    /// 패턴 선택 진입 전이는 보스별 규칙이 다르므로 자동으로 추가하지 않는다.
    /// </summary>
    private static StateSO CreatePatternState(
        StateMachineSO phaseMachine,
        BossPatternSO pattern,
        string stateName)
    {
        StateSO patternState = CreateStateSubAsset(phaseMachine, stateName);
        patternState.EnterActions = new StateAction[] { new PlayBossPatternAction(pattern) };
        patternState.transitions = new[]
        {
            new StateTransition
            {
                name = "Pattern Completed",
                activationInput = EnumManager.InputType.None,
                availableTime = 0f,
                disableTime = 0f,
                decisions = new[]
                {
                    new StateTransition.DecisionSet
                    {
                        Decision = new IsBossPatternCompletedDecision(pattern),
                        negate = false
                    }
                },
                trueState = phaseMachine.initialState,
                falseState = null,
                showTrueStatePort = true,
                showFalseStatePort = false
            }
        };

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
