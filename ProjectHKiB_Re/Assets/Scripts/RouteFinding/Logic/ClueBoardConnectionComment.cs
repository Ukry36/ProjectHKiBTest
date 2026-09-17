using System;

// 연결 성공(새 연결·선 재클릭)에 보여 줄 델타 코멘트. 해몽 결과(C06 outcome)가 없는 관계에도 반드시 반응이 있어야
// 한다는 규칙("성공 시에는 무조건 코멘트")을 여기서 보장한다: 관계의 comment → 기본 템플릿([A]/[B] 치환) 순.
// 보상·발행 기록·viewed와 무관한 표시 전용이며, 같은 관계를 다시 클릭하면 같은 문구가 다시 나온다.
public sealed class ClueBoardConnectionComment
{
    public const string DefaultTemplate = "[A](와/과) [B](이/가) 이어졌다… 아직은 무엇을 뜻하는지 모르겠다.";

    public string boardId { get; }
    public string relationId { get; }
    public string text { get; }
    public bool isNew { get; }        // true = 방금 성립, false = 선 재클릭
    public bool isAuthored { get; }   // true = 관계에 적힌 코멘트, false = 기본 템플릿

    public ClueBoardConnectionComment(string boardId, string relationId, string text, bool isNew, bool isAuthored)
    {
        this.boardId = boardId;
        this.relationId = relationId;
        this.text = text;
        this.isNew = isNew;
        this.isAuthored = isAuthored;
    }

    /// <summary>보드에 없는 관계면 null. 이름 조회는 화면과 같은 resolveClue를 넘겨야 카드 이름과 어긋나지 않는다.</summary>
    public static ClueBoardConnectionComment Create(
        ClueBoardDefinition board, string relationId, bool isNew, Func<string, ClueData> resolveClue, string template = null)
    {
        if (board?.relations == null || string.IsNullOrEmpty(relationId)) return null;
        ClueBoardRelation relation = Array.Find(board.relations,
            item => item != null && string.Equals(item.relationId, relationId, StringComparison.Ordinal));
        if (relation == null) return null;

        if (!string.IsNullOrWhiteSpace(relation.comment))
            return new ClueBoardConnectionComment(board.boardId, relationId, relation.comment.Trim(), isNew, true);

        string first = NameOf(board, relation.firstNodeId, resolveClue);
        string second = NameOf(board, relation.secondNodeId, resolveClue);
        string text = ClueBoardRejectionComment.Format(
            string.IsNullOrWhiteSpace(template) ? DefaultTemplate : template, first, second);
        return new ClueBoardConnectionComment(board.boardId, relationId, text, isNew, false);
    }

    private static string NameOf(ClueBoardDefinition board, string nodeId, Func<string, ClueData> resolveClue)
    {
        ClueBoardSlot slot = board.slots == null ? null
            : Array.Find(board.slots, item => item != null && string.Equals(item.nodeId, nodeId, StringComparison.Ordinal));
        if (slot == null) return nodeId ?? "";
        ClueData clue = resolveClue?.Invoke(slot.clueId);
        return clue != null && !string.IsNullOrWhiteSpace(clue.name) ? clue.name.Trim() : (slot.clueId ?? nodeId);
    }
}
