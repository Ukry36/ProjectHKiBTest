using System;
using System.Collections.Generic;

// [C06] 관계 결과의 종류. 저장 형식(ClueBoardOutcomeSaveInfo.kind)에 그대로 들어가므로 값을 바꾸지 않는다.
public enum ClueBoardOutcomeKind
{
    Relation = 0, // 관계 하나 — 식별자 (boardId, relationId)
    Chain = 1,    // 멀티 체인 — 식별자 (boardId, chainId), 관계 전부 성립해야 열린다
}

// 보드 정의에서 뽑아낸 **유효한** 결과 하나. 관계 하나짜리(ClueBoardRelation.readingId)와
// 체인(ClueBoardRelationChain)을 같은 모양으로 정규화해 판정·재열람·저장이 한 경로를 쓰게 한다.
public sealed class ClueBoardOutcomeDefinition
{
    public string boardId { get; }
    public ClueBoardOutcomeKind kind { get; }
    public string outcomeId { get; }
    public string readingId { get; }
    public IReadOnlyList<string> relationIds { get; }

    public ClueBoardOutcomeDefinition(
        string boardId, ClueBoardOutcomeKind kind, string outcomeId, string readingId, IReadOnlyList<string> relationIds)
    {
        this.boardId = boardId;
        this.kind = kind;
        this.outcomeId = outcomeId;
        this.readingId = readingId;
        this.relationIds = relationIds;
    }

    public bool Involves(string relationId)
    {
        if (relationId == null) return false;
        for (int i = 0; i < relationIds.Count; i++)
            if (string.Equals(relationIds[i], relationId, StringComparison.Ordinal)) return true;
        return false;
    }

    /// <summary>체인이면 모든 관계가, 단일이면 그 관계가 성립했는지.</summary>
    public bool IsSatisfied(Func<string, bool> isConnected)
    {
        for (int i = 0; i < relationIds.Count; i++)
            if (!isConnected(relationIds[i])) return false;
        return true;
    }
}

// 보드 한 장의 결과 배선을 정의에서 읽어 정규화한다. ClueBoardConnectionEngine과 같은 순수 C# 계층이며
// 해몽 카탈로그(DreamReadings.asset)는 모른다 — 해몽 ID가 실제로 존재하는지는 발행/재열람 시점에
// readingProvider가 판단한다(ClueBoardOutcomeResolver).
//
// [왜 TryValidateDefinition에 넣지 않았나] 판정 계약이 거절하면 엔진 생성자가 예외를 던져 보드 자체가 뜨지
// 않는다. 결과 배선의 오타(없는 관계 ID, 빈 해몽 ID)는 선 연결과 무관하므로 그 결과만 무시하고 진단을 남기는
// 편이 맞다. 에디터 저장 경고(ClueBoardDatabaseCodec.ValidateReferences)와 런타임 로그가 같은 진단을 쓴다.
public sealed class ClueBoardOutcomeCatalog
{
    private readonly List<ClueBoardOutcomeDefinition> _outcomes = new();
    private readonly Dictionary<string, ClueBoardOutcomeDefinition> _byKey = new(StringComparer.Ordinal);

    public string boardId { get; }
    public IReadOnlyList<ClueBoardOutcomeDefinition> Outcomes => _outcomes;

    private ClueBoardOutcomeCatalog(string boardId) => this.boardId = boardId;

    public static string Key(ClueBoardOutcomeKind kind, string outcomeId) => (int)kind + ":" + outcomeId;

    public bool TryGet(ClueBoardOutcomeKind kind, string outcomeId, out ClueBoardOutcomeDefinition outcome)
    {
        outcome = null;
        return outcomeId != null && _byKey.TryGetValue(Key(kind, outcomeId), out outcome);
    }

    /// <summary>주어진 관계가 끼어 있는 결과 전부(단일 → 체인 순서).</summary>
    public List<ClueBoardOutcomeDefinition> OutcomesInvolving(string relationId)
    {
        var list = new List<ClueBoardOutcomeDefinition>();
        foreach (ClueBoardOutcomeDefinition outcome in _outcomes)
            if (outcome.Involves(relationId)) list.Add(outcome);
        return list;
    }

    /// <summary>
    /// 정의에서 결과 배선을 읽는다. 잘못된 항목은 diagnostics에 남기고 건너뛴다 — 예외를 던지지 않는다.
    ///   - Unrelated 관계에 readingId가 있으면 무시(성립할 수 없는 관계)
    ///   - 체인: 빈/중복 chainId, 빈 readingId, 관계 2개 미만, 없는 관계 ID, Unrelated 관계, 같은 관계 중복
    /// </summary>
    public static ClueBoardOutcomeCatalog Build(ClueBoardDefinition definition, List<string> diagnostics = null)
    {
        var catalog = new ClueBoardOutcomeCatalog(definition?.boardId);
        if (definition == null || string.IsNullOrEmpty(definition.boardId))
        {
            diagnostics?.Add("보드 정의가 없어 결과 배선을 읽지 않습니다.");
            return catalog;
        }

        var relations = new Dictionary<string, ClueBoardRelation>(StringComparer.Ordinal);
        if (definition.relations != null)
            foreach (ClueBoardRelation relation in definition.relations)
                if (relation != null && !string.IsNullOrEmpty(relation.relationId))
                    relations[relation.relationId] = relation;

        string board = $"보드 '{definition.boardId}'";
        foreach (ClueBoardRelation relation in relations.Values)
        {
            if (string.IsNullOrWhiteSpace(relation.readingId)) continue;
            if (relation.kind == ClueBoardRelationKind.Unrelated)
            {
                diagnostics?.Add($"{board}: Unrelated 관계 '{relation.relationId}'에 해몽 '{relation.readingId}'가 배선되어 있어 무시합니다(성립할 수 없는 관계).");
                continue;
            }
            catalog.Add(new ClueBoardOutcomeDefinition(
                definition.boardId, ClueBoardOutcomeKind.Relation, relation.relationId, relation.readingId.Trim(),
                new[] { relation.relationId }));
        }

        if (definition.chains == null) return catalog;
        for (int i = 0; i < definition.chains.Length; i++)
        {
            ClueBoardRelationChain chain = definition.chains[i];
            if (chain == null) { diagnostics?.Add($"{board}: chains[{i}]가 null이라 무시합니다."); continue; }
            if (string.IsNullOrWhiteSpace(chain.chainId)) { diagnostics?.Add($"{board}: chains[{i}]에 chainId가 없어 무시합니다."); continue; }
            string label = $"{board}의 체인 '{chain.chainId}'";
            if (catalog._byKey.ContainsKey(Key(ClueBoardOutcomeKind.Chain, chain.chainId)))
            {
                diagnostics?.Add($"{label}: 중복 chainId라 뒤의 정의를 무시합니다.");
                continue;
            }
            if (string.IsNullOrWhiteSpace(chain.readingId)) { diagnostics?.Add($"{label}: readingId가 비어 있어 무시합니다."); continue; }

            var ids = new List<string>();
            bool valid = true;
            if (chain.relationIds != null)
                foreach (string relationId in chain.relationIds)
                {
                    if (string.IsNullOrWhiteSpace(relationId))
                    {
                        diagnostics?.Add($"{label}: 빈 관계 ID가 있어 무시합니다.");
                        valid = false; break;
                    }
                    if (!relations.TryGetValue(relationId, out ClueBoardRelation relation))
                    {
                        diagnostics?.Add($"{label}: 없는 관계 '{relationId}'를 참조해 무시합니다.");
                        valid = false; break;
                    }
                    if (relation.kind == ClueBoardRelationKind.Unrelated)
                    {
                        diagnostics?.Add($"{label}: Unrelated 관계 '{relationId}'를 포함해 무시합니다.");
                        valid = false; break;
                    }
                    if (ids.Contains(relationId))
                    {
                        diagnostics?.Add($"{label}: 관계 '{relationId}'가 중복되어 무시합니다.");
                        valid = false; break;
                    }
                    ids.Add(relationId);
                }
            if (!valid) continue;
            if (ids.Count < 2)
            {
                diagnostics?.Add($"{label}: 관계가 {ids.Count}개라 무시합니다(체인은 2개 이상, 하나짜리는 relation.readingId를 쓰세요).");
                continue;
            }
            catalog.Add(new ClueBoardOutcomeDefinition(
                definition.boardId, ClueBoardOutcomeKind.Chain, chain.chainId, chain.readingId.Trim(), ids.ToArray()));
        }
        return catalog;
    }

    /// <summary>파일 전체의 결과 배선 진단을 한 문자열로. 문제가 없으면 null(에디터 저장 경고용).</summary>
    public static string DescribeDiagnostics(ClueBoardDatabase database)
    {
        if (database?.boards == null) return null;
        var messages = new List<string>();
        foreach (ClueBoardDefinition board in database.boards)
            if (board != null) Build(board, messages);
        return messages.Count == 0 ? null : string.Join("\n", messages);
    }

    private void Add(ClueBoardOutcomeDefinition outcome)
    {
        _outcomes.Add(outcome);
        _byKey[Key(outcome.kind, outcome.outcomeId)] = outcome;
    }
}
