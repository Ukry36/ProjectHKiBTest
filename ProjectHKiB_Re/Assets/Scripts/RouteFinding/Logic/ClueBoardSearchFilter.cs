using System;
using System.Collections.Generic;

// C03-B 보드 검색/필터 — UI 없이 계산·검증할 수 있는 순수 C# 계층(ClueBoardNodeStates와 같은 위치).
//
// [도감 검색과의 관계] CodexFilterService는 도감 항목(CodexEntry)을 대상으로 하고, 이쪽은 보드 한 장의
// **화면에 떠 있는 노드**를 대상으로 한다. 부분 문자열·대소문자 무시 규칙은 도감과 같게 맞췄지만
// 코드를 공유하지는 않는다 — 도감은 사용자 메모까지 검색하고, 보드는 잠긴/실루엣 노드를 절대 검색
// 결과에 내지 않아야 하는 등 대상 집합의 규칙이 다르다.
//
// [필터 의미] 기획서 p.7~8 "스팀의 버튼식 필터링": 유형 4 + 단서 감정 4 + new! 버튼을 눌러 켜고 끈다.
//   같은 축 안(유형끼리, 감정끼리)은 OR, 축 사이(검색어·유형·감정·신규)는 AND.
//   아무 조건도 없으면 "필터 없음"이며 이때는 해금 노드 전부가 결과다(하이라이트는 UI가 끈다).

public sealed class ClueBoardFilterCriteria
{
    private readonly HashSet<ClueType> _types = new();
    private readonly HashSet<ClueEmotionTag> _emotions = new();

    public string query { get; private set; } = "";
    public bool newOnly { get; private set; }
    public IReadOnlyCollection<ClueType> types => _types;
    public IReadOnlyCollection<ClueEmotionTag> emotions => _emotions;

    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(query) && _types.Count == 0 && _emotions.Count == 0 && !newOnly;

    public bool HasType(ClueType type) => _types.Contains(type);
    public bool HasEmotion(ClueEmotionTag tag) => _emotions.Contains(tag);

    public void SetQuery(string value) => query = value ?? "";

    /// <summary>유형 토글. 켜졌으면 true.</summary>
    public bool ToggleType(ClueType type)
    {
        if (!ClueTypeConfig.IsKnown(type)) return false;
        if (_types.Remove(type)) return false;
        _types.Add(type);
        return true;
    }

    /// <summary>
    /// 감정 토글. 확정된 감정(기쁨/슬픔/분노)만 받는다. Unset과 ReservedFourth는 아무것도 바꾸지 않고
    /// false를 돌려준다 — 네 번째 감정은 콘텐츠 확정 전 예약값이라 필터 조건이 될 수 없다(C01 저장 검증과
    /// ClueEmotionTagConfig의 색 조회가 이 값을 거절하는 것과 같은 이유).
    /// </summary>
    public bool TryToggleEmotion(ClueEmotionTag tag, out bool enabled)
    {
        enabled = false;
        if (!ClueEmotionTagConfig.IsSelectable(tag)) return false;
        if (_emotions.Remove(tag)) return true;
        _emotions.Add(tag);
        enabled = true;
        return true;
    }

    public void SetNewOnly(bool value) => newOnly = value;

    public void Clear()
    {
        query = "";
        newOnly = false;
        _types.Clear();
        _emotions.Clear();
    }
}

public readonly struct ClueBoardSearchHit
{
    public readonly string nodeId;
    public readonly string clueId;
    public readonly ClueData clue; // 단서 DB에 없으면 null — 화면이 ID를 이름 대신 쓰는 것과 같은 상태

    public ClueBoardSearchHit(string nodeId, string clueId, ClueData clue)
    {
        this.nodeId = nodeId;
        this.clueId = clueId;
        this.clue = clue;
    }
}

public static class ClueBoardSearchService
{
    /// <summary>
    /// 보드 한 장에서 조건에 맞는 노드를 슬롯 정의 순서대로 돌려준다.
    ///
    /// 대상은 **해금 노드뿐**이다. 실루엣은 이름을 내지 않으므로 검색에 잡히면 "무엇인지"가 새어 나가고,
    /// 잠김은 존재 자체를 숨겨야 한다. 환각 노드(C07)는 이름이 보이는 상태로 보드에 있으므로 다른 해금
    /// 노드와 똑같이 검색된다 — 검색에서만 빠지면 그것이 가짜라는 사실이 검색으로 드러난다.
    ///
    /// newOnly는 isNewClue 공급원이 있어야 판정할 수 있다. 공급원이 없으면(C05 전) 아무것도 맞지 않는다 —
    /// 통과시키면 "전부 신규"로 보이고, 조용히 조건을 무시하면 "신규 필터가 동작하는 척"이 된다.
    /// UI는 공급원이 없을 때 new! 버튼 자체를 노출하지 않는다.
    /// </summary>
    public static List<ClueBoardSearchHit> Match(
        ClueBoardDefinition definition,
        ClueBoardNodeStates states,
        ClueBoardFilterCriteria criteria,
        Func<string, ClueData> resolveClue,
        Func<string, bool> isNewClue = null)
    {
        var hits = new List<ClueBoardSearchHit>();
        if (definition?.slots == null || states == null) return hits;
        criteria ??= new ClueBoardFilterCriteria();

        string query = criteria.query?.Trim() ?? "";
        foreach (ClueBoardSlot slot in definition.slots)
        {
            if (slot == null || string.IsNullOrEmpty(slot.nodeId)) continue;
            if (states.Get(slot.nodeId) != ClueBoardNodeVisibility.Unlocked) continue;

            ClueData clue = resolveClue?.Invoke(slot.clueId);
            if (!Matches(clue, slot.clueId, query, criteria, isNewClue)) continue;
            hits.Add(new ClueBoardSearchHit(slot.nodeId, slot.clueId, clue));
        }
        return hits;
    }

    /// <summary>단서 하나가 조건에 맞는지. 노드 상태와 무관한 부분만 본다(Match가 해금 여부를 먼저 거른다).</summary>
    public static bool Matches(
        ClueData clue, string clueId, string query, ClueBoardFilterCriteria criteria, Func<string, bool> isNewClue)
    {
        if (criteria == null) return true;

        if (criteria.types.Count > 0)
        {
            // 미분류(classification == null)는 어떤 유형 필터에도 맞지 않는다. 첫 enum 값으로 흘려보내면
            // 실제 16건이 전부 "상징/글"로 잡혀 분류가 끝난 것처럼 보인다.
            if (clue == null || !ClueTypeConfig.IsValid(clue.classification)) return false;
            if (!criteria.HasType(clue.classification.type)) return false;
        }

        if (criteria.emotions.Count > 0)
        {
            if (clue == null || !criteria.HasEmotion(clue.emotionTag)) return false;
        }

        if (criteria.newOnly)
        {
            if (isNewClue == null || string.IsNullOrEmpty(clueId) || !isNewClue(clueId)) return false;
        }

        if (!string.IsNullOrEmpty(query))
        {
            // 화면에 보이는 것을 검색한다. 단서 DB에 없는 ID는 노드가 ID를 이름으로 내보이므로 ID로 맞춘다.
            if (clue == null) return Contains(clueId, query);
            bool hit = Contains(clue.name, query) || Contains(clue.content, query) ||
                       Contains(clue.description, query) || Contains(clue.source, query);
            if (!hit && clue.keywords != null)
                foreach (string keyword in clue.keywords)
                    if (Contains(keyword, query)) { hit = true; break; }
            if (!hit) return false;
        }

        return true;
    }

    // CodexFilterService.Search와 같은 비교 규칙(부분 문자열, 대소문자 무시).
    private static bool Contains(string haystack, string needle) =>
        !string.IsNullOrEmpty(haystack) && haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
}
