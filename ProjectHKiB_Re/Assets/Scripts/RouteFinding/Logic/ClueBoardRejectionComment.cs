using System;
using System.Text;

// 관계 없는 연결(Unrelated)을 거절할 때 델타가 내는 코멘트. 기획("기타 1"): "이 [오브젝트]와 [사진]은 아무런 연관이
// 없어 보인다…"를 **일괄** 출력하되, 단서 이름 자리는 코드 치환으로 키워드만 바뀐다.
//
// [경계] 이 코멘트는 결과(C06)가 아니다 — 보상도, 발행 기록(ClueBoardOutcomes)도, viewed도 없고 매번 다시 나온다.
// 그래서 ClueBoardOutcomeResolver/DreamReadingModule을 거치지 않고, 판정 결과(ClueBoardConnectResult.status ==
// Unrelated)와 단서 이름만으로 만든다. 성립·재시도·실루엣/환각 거절에는 만들지 않는다(실루엣·자기 자신 드롭은 애초에
// 판정 요청조차 발행되지 않고, 환각은 C07 몫이다).
//
// [문구] 템플릿의 [A]/[B]가 시도한 순서대로 두 단서 이름으로 바뀌고, 이어지는 "(와/과)" 같은 조사 선택지는 앞 이름의
// 받침 유무로 고른다. 이름을 모르면(조회 실패) 단서 ID를 그대로 쓴다 — 코멘트 하나 때문에 거절 자체가 조용해지면 안 된다.
public sealed class ClueBoardRejectionComment
{
    public const string DefaultTemplate = "이 [A](와/과) [B](은/는) 아무런 연관이 없어 보인다…";
    public const string DefaultTitle = "델타";

    public string boardId { get; }
    public string firstNodeId { get; }
    public string secondNodeId { get; }
    public string firstClueName { get; }
    public string secondClueName { get; }
    public string text { get; }

    public ClueBoardRejectionComment(
        string boardId, string firstNodeId, string secondNodeId,
        string firstClueName, string secondClueName, string text)
    {
        this.boardId = boardId;
        this.firstNodeId = firstNodeId;
        this.secondNodeId = secondNodeId;
        this.firstClueName = firstClueName;
        this.secondClueName = secondClueName;
        this.text = text;
    }

    /// <summary>
    /// 거절 결과에서 코멘트를 만든다. Unrelated가 아니거나 보드/노드를 모르면 null.
    /// resolveClue는 화면이 이름 표시에 쓰는 것과 같은 조회를 넘겨야 "카드의 이름"과 "코멘트의 이름"이 어긋나지 않는다.
    /// </summary>
    public static ClueBoardRejectionComment TryCreate(
        ClueBoardDefinition board, ClueBoardConnectResult result,
        Func<string, ClueData> resolveClue, string template = null)
    {
        if (board == null || result.status != ClueBoardConnectStatus.Unrelated) return null;
        if (string.IsNullOrEmpty(result.firstNodeId) || string.IsNullOrEmpty(result.secondNodeId)) return null;

        string firstName = ResolveName(board, result.firstNodeId, resolveClue);
        string secondName = ResolveName(board, result.secondNodeId, resolveClue);
        if (firstName == null || secondName == null) return null;

        return new ClueBoardRejectionComment(
            board.boardId, result.firstNodeId, result.secondNodeId, firstName, secondName,
            Format(template, firstName, secondName));
    }

    private static string ResolveName(ClueBoardDefinition board, string nodeId, Func<string, ClueData> resolveClue)
    {
        if (board.slots == null) return null;
        foreach (ClueBoardSlot slot in board.slots)
        {
            if (slot == null || !string.Equals(slot.nodeId, nodeId, StringComparison.Ordinal)) continue;
            ClueData clue = resolveClue?.Invoke(slot.clueId);
            string name = clue != null && !string.IsNullOrWhiteSpace(clue.name) ? clue.name.Trim() : slot.clueId;
            return string.IsNullOrEmpty(name) ? nodeId : name;
        }
        return null;
    }

    /// <summary>[A]/[B]를 이름으로 바꾸고 "(와/과)"·"(은/는)" 꼴 조사 선택지를 앞 글자의 받침에 맞춰 고른다.</summary>
    public static string Format(string template, string firstName, string secondName)
    {
        if (string.IsNullOrWhiteSpace(template)) template = DefaultTemplate;
        string filled = template
            .Replace("[A]", "[" + (firstName ?? "") + "]")
            .Replace("[B]", "[" + (secondName ?? "") + "]");
        return ResolveParticles(filled);
    }

    // 조사 선택지 "(와/과)", "(은/는)" 등. 관습상 표기 순서가 조사마다 다르므로("와/과"는 무받침이 앞, "은/는"은 받침이
    // 앞) 어느 쪽이 받침용인지는 표로 정한다. 표에 없는 괄호("(1/2)" 등)는 조사가 아니므로 그대로 둔다.
    private static readonly string[] FinalForms = { "은", "이", "을", "과", "으로", "아", "이여", "이랑" };
    private static readonly string[] OpenForms = { "는", "가", "를", "와", "로", "야", "여", "랑" };

    private static string ResolveParticles(string text)
    {
        var sb = new StringBuilder(text.Length);
        int i = 0;
        while (i < text.Length)
        {
            char c = text[i];
            if (c == '(')
            {
                int close = text.IndexOf(')', i + 1);
                int slash = close > 0 ? text.IndexOf('/', i + 1, close - i - 1) : -1;
                if (close > 0 && slash > 0 && close - i <= 8 && text.IndexOf('/', slash + 1, close - slash - 1) < 0)
                {
                    string left = text.Substring(i + 1, slash - i - 1);
                    string right = text.Substring(slash + 1, close - slash - 1);
                    if (TryOrderForms(left, right, out string withFinal, out string withoutFinal))
                    {
                        sb.Append(HasFinalConsonant(PreviousLetter(sb)) ? withFinal : withoutFinal);
                        i = close + 1;
                        continue;
                    }
                }
            }
            sb.Append(c);
            i++;
        }
        return sb.ToString();
    }

    private static bool TryOrderForms(string left, string right, out string withFinal, out string withoutFinal)
    {
        if (Array.IndexOf(FinalForms, left) >= 0 && Array.IndexOf(OpenForms, right) >= 0)
        {
            withFinal = left; withoutFinal = right; return true;
        }
        if (Array.IndexOf(OpenForms, left) >= 0 && Array.IndexOf(FinalForms, right) >= 0)
        {
            withFinal = right; withoutFinal = left; return true;
        }
        withFinal = withoutFinal = null;
        return false;
    }

    private static char PreviousLetter(StringBuilder sb)
    {
        for (int k = sb.Length - 1; k >= 0; k--)
        {
            char c = sb[k];
            if (c == ']' || c == '"' || c == '\'' || c == '’' || c == '”' || c == ')' || c == ' ') continue;
            return c;
        }
        return '\0';
    }

    /// <summary>한글 음절이면 종성 유무. 숫자·영문은 발음 기준 근사(0,1,3,6,7,8과 l,m,n,r는 받침 있음), 그 밖은 없음.</summary>
    public static bool HasFinalConsonant(char c)
    {
        if (c >= '가' && c <= '힣') return (c - '가') % 28 != 0;
        if (c >= '0' && c <= '9') return "013678".IndexOf(c) >= 0;
        char lower = char.ToLowerInvariant(c);
        return lower == 'l' || lower == 'm' || lower == 'n' || lower == 'r';
    }
}
