using System;
using System.Collections.Generic;
using System.Text;
using StateMachine;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Boss Authoring 창에서 선택한 Main·Phase·Pattern 구조를 Markdown으로 직렬화한다.
/// 에이전트가 Unity YAML을 직접 읽지 않고도 현재 에셋 구성과 검증 결과를 파악하게 한다.
/// </summary>
internal static class BossAuthoringSnapshotBuilder
{
    /// <summary>
    /// 현재 보스 제작 대상과 모든 발견된 StateMachine, Pattern, 검증 결과를 한 문서로 만든다.
    /// 반환 문자열은 클립보드 공유와 파일 저장에서 동일하게 사용한다.
    /// </summary>
    public static string Build(
        StateMachineSO mainMachine,
        GameObject bossPrefab,
        string outputRoot,
        List<BossPhaseAuthoringNode> phases,
        List<BossAuthoringValidationIssue> validationIssues)
    {
        StringBuilder builder = new StringBuilder(8192);
        builder.AppendLine("# Boss Authoring Snapshot");
        builder.AppendLine();
        builder.AppendLine($"- Generated: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}");
        builder.AppendLine($"- Main StateMachine: {DescribeObject(mainMachine)}");
        builder.AppendLine($"- Boss Prefab: {DescribeObject(bossPrefab)}");
        builder.AppendLine($"- Output Root: {NormalizeText(outputRoot)}");
        builder.AppendLine();
        builder.AppendLine("> Edit through Tools > Boss Authoring or the normal Inspector. Do not edit Unity YAML directly.");
        builder.AppendLine();

        AppendStateMachine(builder, "Main StateMachine", mainMachine);
        AppendPhases(builder, phases);
        AppendSpecialActionRegistry(builder, phases);
        AppendValidation(builder, validationIssues);
        return builder.ToString();
    }

    /// <summary>
    /// Main에서 탐색된 각 Phase와 그 안의 Pattern 기획 정보를 순서대로 기록한다.
    /// State 연결과 PatternSO 규칙을 함께 보여 주어 두 에셋의 대응 관계를 분명히 한다.
    /// </summary>
    private static void AppendPhases(
        StringBuilder builder,
        List<BossPhaseAuthoringNode> phases)
    {
        builder.AppendLine("## Phase and Pattern Design");
        builder.AppendLine();
        if (phases == null || phases.Count == 0)
        {
            builder.AppendLine("- No phase was discovered.");
            builder.AppendLine();
            return;
        }

        for (int i = 0; i < phases.Count; i++)
        {
            BossPhaseAuthoringNode phase = phases[i];
            builder.AppendLine($"### Phase {i + 1}: {NormalizeText(phase.PhaseMachine?.name)}");
            builder.AppendLine();
            builder.AppendLine($"- Asset: {DescribeObject(phase.PhaseMachine)}");
            builder.AppendLine($"- Launch State: {DescribeObject(phase.LaunchState)}");
            builder.AppendLine($"- Start State: {DescribeObject(phase.StartAction?.StartState)}");
            builder.AppendLine($"- Return State: {DescribeObject(phase.StartAction?.ReturnState)}");
            builder.AppendLine();

            for (int j = 0; j < phase.Patterns.Count; j++)
                AppendPattern(builder, j, phase.Patterns[j]);

            AppendStateMachine(builder, $"Phase Graph: {phase.PhaseMachine?.name}", phase.PhaseMachine);
        }
    }

    /// <summary>
    /// 한 PatternSO의 진입·클리어 규칙, 특수행동과 타임라인 Beat를 기록한다.
    /// Action은 관리되는 참조 타입 이름만 노출해 Inspector에서 찾을 위치를 제공한다.
    /// </summary>
    private static void AppendPattern(
        StringBuilder builder,
        int index,
        BossPatternAuthoringNode patternNode)
    {
        BossPatternSO pattern = patternNode?.Pattern;
        builder.AppendLine($"#### Pattern {index + 1}: {NormalizeText(pattern?.name ?? patternNode?.State?.name)}");
        builder.AppendLine();
        builder.AppendLine($"- Pattern Asset: {DescribeObject(pattern)}");
        builder.AppendLine($"- Pattern State: {DescribeObject(patternNode?.State)}");
        if (pattern == null)
        {
            builder.AppendLine("- Missing BossPatternSO reference.");
            builder.AppendLine();
            return;
        }

        builder.AppendLine($"- Design ID: {NormalizeText(pattern.Label)}");
        builder.AppendLine($"- Summary: {NormalizeText(pattern.Summary)}");
        builder.AppendLine($"- Entry Mode: {pattern.EntryMode}");
        builder.AppendLine($"- Repeat Within Phase: {pattern.AllowRepeatWithinPhase}");
        builder.AppendLine($"- Completion Rule: {pattern.CompletionRule}");
        builder.AppendLine($"- Loop Until Cleared: {pattern.LoopTimelineUntilCleared}");
        builder.AppendLine($"- Loop Start Beat: {pattern.TimelineLoopStartIndex}");
        builder.AppendLine($"- Loop Delay: {pattern.TimelineLoopDelay:0.###}s");
        builder.AppendLine($"- One Cycle Duration: {pattern.TotalDuration:0.###}s");

        AppendSpecialActions(builder, pattern.SpecialActions);
        AppendTimeline(builder, pattern.Timeline);
        builder.AppendLine();
    }

    /// <summary>
    /// Pattern에 선언된 특수행동의 ID, 성공 횟수, 클리어 필수 여부와 미대응 이유를 출력한다.
    /// 배열 순서를 보존하여 Inspector 항목과 Snapshot 항목을 바로 대응시킨다.
    /// </summary>
    private static void AppendSpecialActions(
        StringBuilder builder,
        BossSpecialActionDefinition[] specialActions)
    {
        if (specialActions == null || specialActions.Length == 0)
        {
            builder.AppendLine("- Special Actions: none");
            return;
        }

        builder.AppendLine("- Special Actions:");
        for (int i = 0; i < specialActions.Length; i++)
        {
            BossSpecialActionDefinition specialAction = specialActions[i];
            if (specialAction == null)
            {
                builder.AppendLine($"  - [{i}] missing");
                continue;
            }

            builder.AppendLine(
                $"  - {NormalizeText(specialAction.Id)}: count={specialAction.RequiredCount}, " +
                $"requiredForClear={specialAction.RequiredForPatternClear}, " +
                $"unanswered={NormalizeText(specialAction.UnansweredFailureReasonId)}");
        }
    }

    /// <summary>
    /// Pattern 타임라인의 상대 지연과 각 Beat에 포함된 Action 타입을 순서대로 출력한다.
    /// 비어 있는 Entry와 Action 슬롯도 숨기지 않아 검증 원인을 찾을 수 있게 한다.
    /// </summary>
    private static void AppendTimeline(
        StringBuilder builder,
        BossPatternTimelineEntry[] timeline)
    {
        if (timeline == null || timeline.Length == 0)
        {
            builder.AppendLine("- Timeline: empty");
            return;
        }

        builder.AppendLine("- Timeline:");
        for (int i = 0; i < timeline.Length; i++)
        {
            BossPatternTimelineEntry entry = timeline[i];
            if (entry == null)
            {
                builder.AppendLine($"  - [{i}] missing");
                continue;
            }

            builder.AppendLine(
                $"  - [{i}] +{entry.Delay:0.###}s {NormalizeText(entry.Label)}: " +
                DescribeActions(entry.Actions));
        }
    }

    /// <summary>
    /// 보스전 전체의 논리 Special Action ID와 이를 선언한 Pattern들을 한 목록으로 묶는다.
    /// 공유 진행도와 Required Count 불일치를 Snapshot에서 빠르게 검토할 수 있게 한다.
    /// </summary>
    private static void AppendSpecialActionRegistry(
        StringBuilder builder,
        List<BossPhaseAuthoringNode> phases)
    {
        builder.AppendLine("## Battle-wide Special Action Registry");
        builder.AppendLine();

        Dictionary<string, int> requiredCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        Dictionary<string, List<string>> patternNames =
            new Dictionary<string, List<string>>(StringComparer.Ordinal);
        HashSet<string> inconsistentIds = new HashSet<string>(StringComparer.Ordinal);

        if (phases != null)
        {
            for (int i = 0; i < phases.Count; i++)
            {
                List<BossPatternAuthoringNode> patterns = phases[i]?.Patterns;
                if (patterns == null) continue;

                for (int j = 0; j < patterns.Count; j++)
                {
                    BossPatternSO pattern = patterns[j]?.Pattern;
                    BossSpecialActionDefinition[] specialActions = pattern?.SpecialActions;
                    if (specialActions == null) continue;

                    for (int k = 0; k < specialActions.Length; k++)
                    {
                        BossSpecialActionDefinition specialAction = specialActions[k];
                        if (specialAction == null) continue;

                        string id = specialAction.Id;
                        if (requiredCounts.TryGetValue(id, out int requiredCount))
                        {
                            if (requiredCount != specialAction.RequiredCount)
                                inconsistentIds.Add(id);
                        }
                        else
                        {
                            requiredCounts.Add(id, specialAction.RequiredCount);
                            patternNames.Add(id, new List<string>());
                        }

                        string patternName = pattern.name;
                        if (!patternNames[id].Contains(patternName))
                            patternNames[id].Add(patternName);
                    }
                }
            }
        }

        if (requiredCounts.Count == 0)
        {
            builder.AppendLine("- No Special Action was declared.");
            builder.AppendLine();
            return;
        }

        foreach (KeyValuePair<string, int> entry in requiredCounts)
        {
            string patterns = string.Join(", ", patternNames[entry.Key]);
            string consistency = inconsistentIds.Contains(entry.Key)
                ? "INCONSISTENT REQUIRED COUNT"
                : "consistent";
            builder.AppendLine(
                $"- {NormalizeText(entry.Key)}: requiredCount={entry.Value}, " +
                $"patterns={NormalizeText(patterns)}, {consistency}");
        }

        builder.AppendLine();
    }

    /// <summary>
    /// 한 StateMachine의 State별 Action 슬롯과 우선순위가 있는 전이 목록을 기록한다.
    /// Conditional 분기와 fallback 순서를 에이전트가 텍스트만으로 검토할 수 있게 한다.
    /// </summary>
    private static void AppendStateMachine(
        StringBuilder builder,
        string heading,
        StateMachineSO stateMachine)
    {
        builder.AppendLine($"## {NormalizeText(heading)}");
        builder.AppendLine();
        if (stateMachine == null)
        {
            builder.AppendLine("- Missing StateMachineSO.");
            builder.AppendLine();
            return;
        }

        builder.AppendLine($"- Asset: {DescribeObject(stateMachine)}");
        builder.AppendLine($"- Initial State: {DescribeObject(stateMachine.initialState)}");
        builder.AppendLine();

        List<StateSO> states = stateMachine.allStates;
        if (states == null || states.Count == 0)
        {
            builder.AppendLine("No states.");
            builder.AppendLine();
            return;
        }

        for (int i = 0; i < states.Count; i++)
            AppendState(builder, i, states[i]);
    }

    /// <summary>
    /// 한 State의 진입·갱신·종료 Action과 일반·추가 전이를 간결하게 기록한다.
    /// 배열 순서를 그대로 보존해 런타임 평가 우선순위를 검토할 수 있게 한다.
    /// </summary>
    private static void AppendState(StringBuilder builder, int index, StateSO state)
    {
        builder.AppendLine($"### State {index}: {NormalizeText(state?.name)}");
        if (state == null)
        {
            builder.AppendLine("- Missing state reference.");
            builder.AppendLine();
            return;
        }

        builder.AppendLine($"- Enter Actions: {DescribeActions(state.EnterActions)}");
        builder.AppendLine($"- Update Actions: {DescribeActions(state.UpdateActions)}");
        builder.AppendLine($"- Exit Actions: {DescribeActions(state.ExitActions)}");
        AppendTransitions(builder, "Transitions", state.transitions);
        AppendTransitions(builder, "Additional Transitions", state.additionalTransitions);
        builder.AppendLine();
    }

    /// <summary>
    /// 한 전이 배열을 실제 평가 순서대로 출력하고 Decision의 반전 여부를 표시한다.
    /// true·false 목적지와 전이 Action까지 함께 기록해 누락된 배선을 찾기 쉽게 한다.
    /// </summary>
    private static void AppendTransitions(
        StringBuilder builder,
        string label,
        StateTransition[] transitions)
    {
        if (transitions == null || transitions.Length == 0)
        {
            builder.AppendLine($"- {label}: none");
            return;
        }

        builder.AppendLine($"- {label}:");
        for (int i = 0; i < transitions.Length; i++)
        {
            StateTransition transition = transitions[i];
            if (transition == null)
            {
                builder.AppendLine($"  - [{i}] missing");
                continue;
            }

            builder.AppendLine(
                $"  - [{i}] {NormalizeText(transition.name)} | " +
                $"if {DescribeDecisions(transition.decisions)} -> {NormalizeText(transition.trueState?.name)}, " +
                $"else -> {NormalizeText(transition.falseState?.name)}, " +
                $"action={transition.Action?.GetType().Name ?? "none"}");
        }
    }

    /// <summary>
    /// 검증 결과를 심각도별 개수와 원인 에셋 경로가 포함된 목록으로 기록한다.
    /// 창에서 선택할 수 없는 에이전트도 동일한 문제 문맥을 찾을 수 있게 한다.
    /// </summary>
    private static void AppendValidation(
        StringBuilder builder,
        List<BossAuthoringValidationIssue> validationIssues)
    {
        builder.AppendLine("## Validation");
        builder.AppendLine();
        if (validationIssues == null || validationIssues.Count == 0)
        {
            builder.AppendLine("- Validation has not been run.");
            return;
        }

        int errorCount = 0;
        int warningCount = 0;
        for (int i = 0; i < validationIssues.Count; i++)
        {
            if (validationIssues[i].Severity == MessageType.Error) errorCount++;
            if (validationIssues[i].Severity == MessageType.Warning) warningCount++;
        }

        builder.AppendLine($"- Errors: {errorCount}");
        builder.AppendLine($"- Warnings: {warningCount}");
        builder.AppendLine();
        for (int i = 0; i < validationIssues.Count; i++)
        {
            BossAuthoringValidationIssue issue = validationIssues[i];
            builder.AppendLine(
                $"- [{issue.Severity}] {NormalizeText(issue.Message)} ({DescribeObject(issue.Context)})");
        }
    }

    /// <summary>
    /// Action 배열을 null 슬롯까지 보존한 타입 이름 목록으로 바꾼다.
    /// 인스턴스를 생성하거나 직렬화 값을 변경하지 않는 읽기 전용 표현이다.
    /// </summary>
    private static string DescribeActions(StateAction[] actions)
    {
        if (actions == null || actions.Length == 0) return "none";

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < actions.Length; i++)
        {
            if (i > 0) builder.Append(", ");
            builder.Append(actions[i]?.GetType().Name ?? "missing");
        }

        return builder.ToString();
    }

    /// <summary>
    /// Decision 배열을 AND 연결된 타입 이름과 Negate 접두사로 표현한다.
    /// 비어 있는 조건 배열은 런타임과 같은 무조건 참 의미인 always로 표시한다.
    /// </summary>
    private static string DescribeDecisions(StateTransition.DecisionSet[] decisions)
    {
        if (decisions == null || decisions.Length == 0) return "always";

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < decisions.Length; i++)
        {
            if (i > 0) builder.Append(" AND ");
            if (decisions[i].negate) builder.Append('!');
            builder.Append(decisions[i].Decision?.GetType().Name ?? "missing");
        }

        return builder.ToString();
    }

    /// <summary>
    /// Unity 에셋을 이름과 프로젝트 상대 경로가 함께 있는 문자열로 표현한다.
    /// null 또는 씬 전용 객체도 예외 없이 읽을 수 있는 표시값을 반환한다.
    /// </summary>
    private static string DescribeObject(UnityEngine.Object target)
    {
        if (target == null) return "none";
        string path = AssetDatabase.GetAssetPath(target);
        return string.IsNullOrWhiteSpace(path)
            ? NormalizeText(target.name)
            : $"{NormalizeText(target.name)} ({path.Replace('\\', '/')})";
    }

    /// <summary>
    /// 여러 줄 입력과 빈 문자열을 Markdown 한 줄에서 안전하게 표시하도록 정리한다.
    /// 구조 의미를 바꾸지 않고 개행만 공백으로 바꾸며 빈 값은 none으로 표시한다.
    /// </summary>
    private static string NormalizeText(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "none";
        return value.Trim().Replace("\r", " ").Replace("\n", " ");
    }
}
