using System;
using System.Collections.Generic;
using System.Text;

// 실루엣 노드에 마우스를 올렸을 때 보여 줄 해금 힌트를 정한다(기획 "잠김/실루엣/해금": 실루엣은 위치만 드러나고
// 포인터를 올리면 해금 조건의 암시가 보인다). UI 없이도 같은 규칙을 검증할 수 있게 순수 C#으로 둔다.
//
// 우선순위: 슬롯(보드별 예외) → 단서 공통(ClueData.silhouetteHint) → 기본 문구. 힌트가 비어 있어도 실루엣은 뜬다 —
// 콘텐츠가 아직 없다고 자리 자체를 숨기면 "실루엣 = 힌트"라는 화면 규칙이 보드마다 달라진다.
//
// 이 힌트는 표시 전용이다. 실루엣의 연결 불가 판정(ClueBoardConnectionEngine)과 무관하며, 단서 이름·아이콘을
// 대신 노출하지 않는다.
public static class ClueBoardSilhouetteHint
{
    // 기획이 예로 든 뉘앙스. 콘텐츠가 힌트를 채우기 전까지의 자리표시자다.
    public const string DefaultHint = "…이런 기억이 있었던 것 같은데.";

    /// <summary>슬롯 예외 → 단서 공통 순으로 작성된 힌트를 찾는다. 둘 다 비어 있으면 null.</summary>
    public static string FindAuthored(ClueBoardSlot slot, ClueData clue)
    {
        if (slot != null && !string.IsNullOrWhiteSpace(slot.silhouetteHint)) return slot.silhouetteHint.Trim();
        if (clue != null && !string.IsNullOrWhiteSpace(clue.silhouetteHint)) return clue.silhouetteHint.Trim();
        return null;
    }

    /// <summary>화면에 낼 문구. 작성된 힌트가 없으면 fallback(기본은 DefaultHint).</summary>
    public static string Resolve(ClueBoardSlot slot, ClueData clue, string fallback = DefaultHint) =>
        FindAuthored(slot, clue) ?? (string.IsNullOrWhiteSpace(fallback) ? DefaultHint : fallback);

    /// <summary>
    /// 실루엣으로 드러날 수 있는 노드(silhouetteNeighbors의 silhouetteNodeId) 가운데 작성된 힌트가 없는 것을 경고 문구로
    /// 만든다. 경고일 뿐 저장·로드를 막지 않는다 — 기본 문구가 대신 나가므로 게임은 돌아가고, 다만 콘텐츠 작업자가
    /// 놓친 자리를 한 화면에서 볼 수 있어야 한다. 없으면 null.
    /// </summary>
    public static string DescribeMissingHints(ClueBoardDatabase database, ClueDatabase clues)
    {
        if (database?.boards == null) return null;
        var cluesById = new Dictionary<string, ClueData>(StringComparer.Ordinal);
        if (clues?.clues != null)
            foreach (ClueData clue in clues.clues)
                if (clue != null && !string.IsNullOrWhiteSpace(clue.id)) cluesById[clue.id] = clue;

        var sb = new StringBuilder();
        for (int i = 0; i < database.boards.Length; i++)
        {
            ClueBoardDefinition board = database.boards[i];
            if (board?.slots == null) continue;
            var targets = new List<string>();
            if (board.revealAllAsSilhouette)
                foreach (ClueBoardSlot slot in board.slots) { if (slot != null) targets.Add(slot.nodeId); }
            else if (board.silhouetteNeighbors != null)
                foreach (ClueBoardSilhouetteNeighbor neighbor in board.silhouetteNeighbors)
                    if (neighbor != null) targets.Add(neighbor.silhouetteNodeId);
            var reported = new HashSet<string>(StringComparer.Ordinal);
            foreach (string targetNodeId in targets)
            {
                if (string.IsNullOrWhiteSpace(targetNodeId) || !reported.Add(targetNodeId)) continue;
                ClueBoardSlot slot = Array.Find(board.slots,
                    item => item != null && string.Equals(item.nodeId, targetNodeId, StringComparison.Ordinal));
                if (slot == null) continue; // 없는 노드는 Validate가 오류로 보고한다
                cluesById.TryGetValue(slot.clueId ?? "", out ClueData clue);
                if (FindAuthored(slot, clue) != null) continue;
                sb.Append(sb.Length == 0 ? "" : "\n")
                  .Append($"보드 '{board.boardId}'(#{i}): 실루엣 노드 '{slot.nodeId}'(단서 '{slot.clueId}')에 실루엣 힌트가 없어 기본 문구가 표시됩니다.");
            }
        }
        return sb.Length == 0 ? null : sb.ToString();
    }
}
