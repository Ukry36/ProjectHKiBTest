using System;
using System.Collections.Generic;

// [C06] 화면에 넘길 결과 하나 — 새 발행과 재열람이 같은 모양을 쓴다.
public sealed class ClueBoardOutcomePresentation
{
    public ClueBoardOutcomeDefinition outcome { get; }
    public DreamReading reading { get; }

    // true = 이번 조작으로 처음 발행됨(보상·코멘트 대상), false = 재열람/보류분 표시.
    public bool isNew { get; }

    // 새 발행에서 보상(DreamReadingModule 해몽 성립)이 실제로 새로 주어졌는지. 같은 해몽이 다른 경로(노트,
    // 다른 보드)로 이미 성립했으면 false — 결과 화면은 뜨되 보상은 중복되지 않는다.
    public bool rewardGranted { get; }

    public ClueBoardOutcomePresentation(ClueBoardOutcomeDefinition outcome, DreamReading reading, bool isNew, bool rewardGranted)
    {
        this.outcome = outcome;
        this.reading = reading;
        this.isNew = isNew;
        this.rewardGranted = rewardGranted;
    }
}

// [C06] "새 관계 연결이 성립한 순간" 어떤 결과를 발행할지 정하는 순수 C# 판정.
//
// [호출 시점이 곧 계약이다] Issue는 ClueBoardView.OnConnectionEstablished(실제 새 연결에만 발행)에서만 불린다.
// AlreadyConnected 재시도, 세이브 복원, 보드 재열기, 글로벌↔로컬 전환은 이 함수에 닿지 않으므로 결과·보상·코멘트가
// 재발행되지 않는다. 그 위에 발행 기록(ClueBoardOutcomeState.IsIssued)이 두 번째 방어선이다.
//
// [단일과 체인] 성립한 관계 R에 대해
//   - R 자체의 단일 결과(relation.readingId): 아직 발행 전이면 발행
//   - R이 끼어 있는 체인: 체인의 모든 관계가 지금 성립해 있고 아직 발행 전이면 발행 — 즉 마지막 관계에서 정확히 한 번
// 둘이 같은 조작에서 동시에 성립하면 각각 발행한다(결과 식별자가 다르다). 다만 같은 해몽을 가리키면 보상은
// rewardHandler가 한 번만 준다(DreamReadingModule이 이미 성립한 해몽을 거절한다).
//
// [결과 없음/손상 참조] readingProvider가 null을 돌려주면 그 결과는 발행하지 않고 기록도 남기지 않는다 —
// 진단만 남긴다. 기록을 남기면 콘텐츠를 고친 뒤에도 영영 발행되지 않고, 기록을 안 남기면 최소한 개발 중에
// 문제를 고쳐 다시 시도할 수 있다(재시도는 AlreadyConnected라 실제로는 보드 진행을 되돌려야 한다 — 그래서 진단이 중요하다).
public static class ClueBoardOutcomeResolver
{
    /// <summary>
    /// 새로 성립한 관계에 대해 발행할 결과를 계산하고 state에 기록한다. 발행 순서는 단일 → 체인이다.
    /// rewardHandler는 새 발행마다 불리며 "보상을 새로 주었는가"를 돌려준다(null이면 false).
    /// </summary>
    public static List<ClueBoardOutcomePresentation> Issue(
        ClueBoardOutcomeCatalog catalog,
        Func<string, bool> isConnected,
        string establishedRelationId,
        ClueBoardOutcomeState state,
        Func<string, DreamReading> readingProvider,
        Func<DreamReading, bool> rewardHandler,
        List<string> diagnostics = null)
    {
        var issued = new List<ClueBoardOutcomePresentation>();
        if (catalog == null || state == null || isConnected == null || string.IsNullOrEmpty(establishedRelationId)) return issued;

        foreach (ClueBoardOutcomeDefinition outcome in catalog.OutcomesInvolving(establishedRelationId))
        {
            if (state.IsIssued(outcome.boardId, outcome.kind, outcome.outcomeId)) continue;
            if (!outcome.IsSatisfied(isConnected)) continue; // 체인 일부만 성립

            DreamReading reading = ResolveReading(outcome, readingProvider, diagnostics);
            if (reading == null) continue;

            state.MarkIssued(outcome.boardId, outcome.kind, outcome.outcomeId, outcome.readingId);
            bool rewarded = rewardHandler != null && rewardHandler(reading);
            issued.Add(new ClueBoardOutcomePresentation(outcome, reading, isNew: true, rewardGranted: rewarded));
        }
        return issued;
    }

    /// <summary>
    /// 이미 발행된 결과 중 이 관계가 끼어 있는 것을 재열람용으로 돌려준다. 발행 전이거나 결과 없는 관계는 빈 목록.
    /// 발행 당시 해몽 ID가 현재 정의와 다르면 경고만 남기고 현재 정의를 따른다.
    /// </summary>
    public static List<ClueBoardOutcomePresentation> Review(
        ClueBoardOutcomeCatalog catalog,
        string relationId,
        ClueBoardOutcomeState state,
        Func<string, DreamReading> readingProvider,
        List<string> diagnostics = null)
    {
        var list = new List<ClueBoardOutcomePresentation>();
        if (catalog == null || state == null || string.IsNullOrEmpty(relationId)) return list;

        foreach (ClueBoardOutcomeDefinition outcome in catalog.OutcomesInvolving(relationId))
        {
            if (!state.TryGet(outcome.boardId, outcome.kind, outcome.outcomeId, out ClueBoardOutcomeState.Record record)) continue;
            ClueBoardOutcomePresentation presentation = ToPresentation(outcome, record, readingProvider, diagnostics);
            if (presentation != null) list.Add(presentation);
        }
        return list;
    }

    /// <summary>
    /// 발행됐지만 아직 보여 주지 못한 결과(state.Unviewed)를 표시용으로 해석한다. 정의에서 사라진 보드/결과나
    /// 없는 해몽은 진단만 남기고 건너뛴다(기록은 그대로 두어 정의가 돌아오면 살아난다).
    /// </summary>
    public static List<ClueBoardOutcomePresentation> Pending(
        ClueBoardOutcomeState state,
        Func<string, ClueBoardDefinition> boardProvider,
        Func<string, DreamReading> readingProvider,
        List<string> diagnostics = null)
    {
        var list = new List<ClueBoardOutcomePresentation>();
        if (state == null || boardProvider == null) return list;

        var catalogs = new Dictionary<string, ClueBoardOutcomeCatalog>(StringComparer.Ordinal);
        foreach (ClueBoardOutcomeState.Record record in state.Unviewed())
        {
            if (!catalogs.TryGetValue(record.boardId, out ClueBoardOutcomeCatalog catalog))
            {
                ClueBoardDefinition board = boardProvider(record.boardId);
                // 카탈로그 자체의 배선 진단은 보드를 열 때 이미 남겼으므로 여기서 반복하지 않는다.
                catalog = board != null ? ClueBoardOutcomeCatalog.Build(board, null) : null;
                catalogs[record.boardId] = catalog;
            }
            if (catalog == null)
            {
                diagnostics?.Add($"보류된 결과 '{record.outcomeId}'의 보드 '{record.boardId}'가 정의에 없어 표시하지 않습니다.");
                continue;
            }
            if (!catalog.TryGet(record.kind, record.outcomeId, out ClueBoardOutcomeDefinition outcome))
            {
                diagnostics?.Add($"보드 '{record.boardId}'에 보류된 결과 '{record.outcomeId}'({record.kind})가 정의에 없어 표시하지 않습니다.");
                continue;
            }
            ClueBoardOutcomePresentation presentation = ToPresentation(outcome, record, readingProvider, diagnostics);
            if (presentation != null) list.Add(presentation);
        }
        return list;
    }

    private static ClueBoardOutcomePresentation ToPresentation(
        ClueBoardOutcomeDefinition outcome, ClueBoardOutcomeState.Record record,
        Func<string, DreamReading> readingProvider, List<string> diagnostics)
    {
        if (!string.IsNullOrEmpty(record.readingId) &&
            !string.Equals(record.readingId, outcome.readingId, StringComparison.Ordinal))
            diagnostics?.Add($"보드 '{outcome.boardId}'의 결과 '{outcome.outcomeId}'는 발행 당시 해몽 '{record.readingId}'였지만 지금은 '{outcome.readingId}'입니다. 현재 정의를 표시합니다.");
        DreamReading reading = ResolveReading(outcome, readingProvider, diagnostics);
        return reading == null ? null : new ClueBoardOutcomePresentation(outcome, reading, isNew: false, rewardGranted: false);
    }

    private static DreamReading ResolveReading(
        ClueBoardOutcomeDefinition outcome, Func<string, DreamReading> readingProvider, List<string> diagnostics)
    {
        DreamReading reading = readingProvider?.Invoke(outcome.readingId);
        if (reading == null)
            diagnostics?.Add($"보드 '{outcome.boardId}'의 결과 '{outcome.outcomeId}'({outcome.kind})가 가리키는 해몽 '{outcome.readingId}'를 찾을 수 없어 결과를 내지 않습니다.");
        else if (string.IsNullOrEmpty(reading.id))
        {
            diagnostics?.Add($"보드 '{outcome.boardId}'의 결과 '{outcome.outcomeId}'가 가리키는 해몽 '{outcome.readingId}'에 id가 없어 결과를 내지 않습니다.");
            reading = null;
        }
        return reading;
    }
}
