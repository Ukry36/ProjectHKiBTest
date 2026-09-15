using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 획득 여부와 분리된 단서 NEW 진행 상태. Unity 수명주기와 무관한 순수 상태 객체라
/// 세이브 복원과 중복 획득 규칙을 같은 경로로 검증할 수 있다.
/// </summary>
public sealed class ClueNewState
{
    private readonly HashSet<string> _newClueIds = new();

    public bool HasSavedDreamReadingCheckpoint { get; private set; }
    public IReadOnlyCollection<string> NewClueIds => _newClueIds;

    public bool TryMarkAcquired(string clueId)
    {
        // 실제 플레이 중 처음 발생한 획득 이벤트는 첫 해몽 저장 전이라도 NEW다.
        // 체크포인트는 저장 세대/해몽 이력을 나타낼 뿐, 초반 이벤트 체인 단서를
        // NEW에서 제외하는 게이트로 사용하지 않는다. 로드와 중복 지급은 호출부에서
        // 이 메서드를 다시 호출하지 않으므로 기존 단서가 NEW로 되살아나지 않는다.
        if (string.IsNullOrEmpty(clueId)) return false;
        return _newClueIds.Add(clueId);
    }

    public bool MarkViewed(string clueId) =>
        !string.IsNullOrEmpty(clueId) && _newClueIds.Remove(clueId);

    public void CommitSavedDreamReadingCheckpoint(bool hasResolvedDreamReading)
    {
        if (hasResolvedDreamReading) HasSavedDreamReadingCheckpoint = true;
    }

    public List<string> ExportNewClueIds() => new(_newClueIds);

    public void Import(
        bool hasSavedDreamReadingCheckpoint,
        IEnumerable<string> newClueIds,
        IReadOnlyCollection<string> acquiredClueIds = null)
    {
        HasSavedDreamReadingCheckpoint = hasSavedDreamReadingCheckpoint;
        _newClueIds.Clear();
        if (newClueIds == null) return;

        foreach (string clueId in newClueIds)
        {
            if (string.IsNullOrEmpty(clueId)) continue;
            if (acquiredClueIds != null && !acquiredClueIds.Contains(clueId)) continue;
            _newClueIds.Add(clueId);
        }
    }
}
