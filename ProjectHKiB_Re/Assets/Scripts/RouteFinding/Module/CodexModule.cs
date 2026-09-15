using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 단서 도감 전용 싱글턴 — RouteModule이 이동/장비/경로를 소유하는 것과 같은 패턴으로,
// 획득한 ClueData 목록(도감에 실제로 보여줄 것)의 단일 소유자 역할을 한다.
//
// RouteProgressState.OnClueAcquired를 구독해 자동 갱신되는 것이 기본 경로이지만,
// 세이브 로드 직후처럼 이벤트가 발행되지 않는 경로도 있으므로(RouteProgressState.ApplyEventFlag 참고),
// RebuildFromProgress()로 언제든 전체 재계산할 수 있게 열어둔다 — CodexPanel이 Open()마다 호출한다.
// ════════════════════════════════════════════════════════════════
// [외부 모듈 연동 API] — 대화/퀘스트/NPC 시스템이 "이 정보는 도감에 자동 등록되는 메모여야 한다"
// 같은 식으로 도감에 항목을 추가하고 싶을 때, 또는 다른 UI가 획득한 단서 목록을 읽고 싶을 때 사용.
//
// ▸ 접근: CodexModule.Instance (자동 생성 싱글턴)
//
// ▸ 조회
//   AcquiredClues            : 지금까지 획득한 정식 단서 전체(ClueData, 읽기 전용)
//   UserEntries               : 유저(또는 다른 시스템)가 직접 만든 자유 메모 전체(CodexUserEntry, 읽기 전용)
//   IsClueNew(clueId)         : "획득했지만 아직 카드로 안 열어본" NEW 상태인지
//   OnCodexChanged            : 도감 내용이 바뀔 때마다 발행(구독해서 UI 갱신 트리거로 사용)
//
// ▸ 유저 메모(자유 항목) 추가 — 노트의 "단서 생성" 기능이 이 API를 그대로 쓴다. 다른 시스템도
//   똑같이 호출해 "정식 ClueData는 아니지만 도감에 기록되는 항목"을 만들 수 있다.
//     var entry = CodexModule.Instance.AddUserEntry(title, content, mapCategory, keywords);
//   반환된 entry.guid를 NoteModule.Instance.AddManualPin(entry.guid)에 넘기면 노트에도 바로 편입된다
//   (NotePanel.HandleClueCreateRequested가 실제로 이렇게 조합해서 쓴다).
//   수정/삭제: UpdateUserEntry(guid, ...) / RemoveUserEntry(guid)
//
// ▸ 세이브 로드 등으로 전체 재계산이 필요할 때(예: RouteProgressState.OnClueAcquired 이벤트를
//   놓쳤을 가능성이 있는 시점)
//     CodexModule.Instance.RebuildFromProgress();
//   RouteProgressState.AcquiredClueIds 기준으로 AcquiredClues를 처음부터 다시 채운다.
//
// ▸ ImportUserEntries는 세이브 시스템 전용(SaveModule이 직접 호출) — 게임플레이 코드에서 직접
//   호출하지 말 것. MarkClueViewed는 카드 UI가 "이 단서를 열람했다" 표시할 때 쓰는 내부용에 가깝다.
// ════════════════════════════════════════════════════════════════
public class CodexModule : MonoBehaviour
{
    private static CodexModule _instance;
    private static bool _isQuitting; // 종료 중에는 다른 오브젝트의 OnDestroy가 Instance를 건드려도 재생성하지 않는다.

    public static CodexModule Instance
    {
        get
        {
            if (_instance == null && Application.isPlaying && !_isQuitting)
            {
                _instance = FindObjectOfType<CodexModule>();
                if (_instance == null)
                    _instance = new GameObject(nameof(CodexModule)).AddComponent<CodexModule>();
            }
            return _instance;
        }
    }

    // 플레이 모드 종료(에디터) / 앱 종료 시 OnDestroy들보다 먼저 호출되는 것이 보장된다 — 이후
    // CodexPanel.OnDestroy 등에서 Instance에 접근해도 새 GameObject를 만들지 않도록 막는다.
    // (안 막으면 씬을 닫을 때 "Some objects were not cleaned up when closing the scene" 경고가 뜬다 —
    // OnDestroy 도중 Instance 접근이 이미 파괴된 CodexModule을 새로 스폰해버리기 때문.)
    private void OnApplicationQuit() => _isQuitting = true;

    private readonly List<ClueData> _acquiredClues = new();
    public IReadOnlyList<ClueData> AcquiredClues => _acquiredClues;

    // 실제 플레이 중 처음 획득한 단서는 첫 해몽 저장 전이라도 NEW가 된다.
    // 획득 목록과 분리해 저장하며, 로드/중복 지급은 TryMarkAcquired를 호출하지 않는다.
    private readonly ClueNewState _newState = new();
    public bool IsClueNew(string clueId) =>
        !string.IsNullOrEmpty(clueId) && _newState.NewClueIds.Contains(clueId);
    public bool HasSavedDreamReadingCheckpoint => _newState.HasSavedDreamReadingCheckpoint;
    public IReadOnlyCollection<string> NewClueIds => _newState.NewClueIds;

    // 대표 아이콘 알림과 열린 보드 슬롯 채움 연출은 일반 획득이 아닌 이 이벤트를 구독한다.
    // 실제 신규 획득에만 발행하고 로드·중복 지급에는 발행하지 않는다.
    public event Action<ClueData> OnNewClueAcquired;
    // OnCodexChanged를 일부러 발행하지 않는다 — 이 메서드는 트리 행 클릭 콜백(CodexDrawerTreeView)
    // 도중 CodexPanel.OnEntrySelected에서 호출되는데, 여기서 리프레시 이벤트를 쏘면 그 클릭 콜백이
    // 참조하고 있던 CodexEntry 객체(RefreshTree가 매번 새로 만듦)가 곧바로 낡은 참조가 되어, 뒤이어
    // 실행되는 트리의 선택 하이라이트 비교(참조 비교)가 깨진다. NEW 배지는 다음 자연스러운 갱신
    // (다른 단서 획득, 패널 재오픈 등) 때 사라지는 정도로 충분하다고 판단해 단순화했다.
    public void MarkClueViewed(string clueId) => MarkClueViewed(clueId, "Codex/other");

    public void MarkClueViewedFromBoard(string clueId) => MarkClueViewed(clueId, "ClueBoard");

    private void MarkClueViewed(string clueId, string source)
    {
        if (string.IsNullOrEmpty(clueId)) return;
        bool removed = _newState.MarkViewed(clueId);
        Debug.Log($"[ClueNEW][State] 확인 처리: clue={clueId}, source={source}, removed={removed}, remaining={_newState.NewClueIds.Count}, frame={Time.frameCount}, time={Time.unscaledTime:F3}");
    }

    // 유저가 도감 안에서 직접 작성한 자유 메모("빈 단서") — 3단계.
    // 세이브 연동(6단계) 완료 — ImportUserEntries 참고.
    private readonly List<CodexUserEntry> _userEntries = new();
    public IReadOnlyList<CodexUserEntry> UserEntries => _userEntries;

    // CodexPanel 등 UI가 구독 — 획득/메모 목록이 바뀔 때마다 다시 그리라는 신호.
    public event Action OnCodexChanged;

    private RouteProgressState _subscribedProgress;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning("[CodexModule] 중복 인스턴스 감지 — 새 인스턴스를 제거합니다.");
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    private void Start() => RebuildFromProgress();

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
        if (_subscribedProgress != null)
            _subscribedProgress.OnClueAcquired -= HandleClueAcquired;
        _subscribedProgress = null;
    }

    private void TrySubscribe()
    {
        RouteProgressState progress = RouteModule.Instance?.Progress;
        if (progress != null) BindProgress(progress);
    }

    /// <summary>
    /// RouteModule이 Progress를 생성한 즉시 호출한다. 이벤트 체인이 UI/도감보다 먼저 단서를 지급해도
    /// NEW 획득 이벤트를 놓치지 않게 하는 초기화 경계다.
    /// </summary>
    public void BindProgress(RouteProgressState progress)
    {
        if (progress == null || ReferenceEquals(_subscribedProgress, progress)) return;
        if (_subscribedProgress != null)
            _subscribedProgress.OnClueAcquired -= HandleClueAcquired;
        _subscribedProgress = progress;
        _subscribedProgress.OnClueAcquired += HandleClueAcquired;
        Debug.Log($"[ClueNEW][State] 진행 상태 직접 연결 완료: frame={Time.frameCount}");
    }

    private void HandleClueAcquired(ClueData clue)
    {
        if (!_acquiredClues.Contains(clue)) _acquiredClues.Add(clue);
        bool becameNew = clue != null && _newState.TryMarkAcquired(clue.id);
        Debug.Log($"[ClueNEW][State] 획득 이벤트: clue={clue?.id ?? "(null)"}, checkpoint={_newState.HasSavedDreamReadingCheckpoint}, becameNew={becameNew}, totalNew={_newState.NewClueIds.Count}, frame={Time.frameCount}");
        OnCodexChanged?.Invoke();
        if (becameNew) OnNewClueAcquired?.Invoke(clue);
    }

    // RouteProgressState.AcquiredClueIds 기준으로 전체 재계산.
    // 아직 구독하지 못한 상태(RouteModule/MapGraph가 늦게 준비된 경우)라면 여기서 구독도 함께 시도한다.
    public void RebuildFromProgress()
    {
        TrySubscribe();

        var graph = MapGraph.Instance;
        var progress = RouteModule.Instance != null ? RouteModule.Instance.Progress : null;
        if (graph == null || progress == null) return;

        _acquiredClues.Clear();
        foreach (var clueId in progress.AcquiredClueIds)
        {
            var clue = graph.GetClue(clueId);
            if (clue != null) _acquiredClues.Add(clue);
        }
        OnCodexChanged?.Invoke();
    }

    // ─── 유저 생성 메모 CRUD ────────────────────────────────────

    public CodexUserEntry AddUserEntry(string title, string content, string mapCategory, string[] keywords)
    {
        var entry = new CodexUserEntry
        {
            guid        = Guid.NewGuid().ToString("N"),
            title       = title,
            content     = content,
            mapCategory = mapCategory,
            keywords    = keywords ?? Array.Empty<string>(),
        };
        _userEntries.Add(entry);
        OnCodexChanged?.Invoke();
        return entry;
    }

    public bool UpdateUserEntry(string guid, string title, string content, string mapCategory, string[] keywords)
    {
        var entry = _userEntries.FirstOrDefault(e => e.guid == guid);
        if (entry == null) return false;

        entry.title       = title;
        entry.content     = content;
        entry.mapCategory = mapCategory;
        entry.keywords    = keywords ?? Array.Empty<string>();
        OnCodexChanged?.Invoke();
        return true;
    }

    public bool RemoveUserEntry(string guid)
    {
        var entry = _userEntries.FirstOrDefault(e => e.guid == guid);
        if (entry == null) return false;

        _userEntries.Remove(entry);
        OnCodexChanged?.Invoke();
        return true;
    }

    // ─── 세이브 연동 (6단계) ────────────────────────────────────
    // SaveModule.SaveEvents()/LoadEvents()가 직접 Instance로 접근해 호출한다. 획득 단서 목록
    // (_acquiredClues)은 여기 포함되지 않는다 — RouteProgressState.AcquiredClueIds에서 파생되므로
    // 이미 IEventSaveProvider 경로로 저장되고, 로드 후 RebuildFromProgress()가 다시 채운다.
    public void ImportUserEntries(List<CodexUserEntry> entries)
    {
        _userEntries.Clear();
        if (entries != null) _userEntries.AddRange(entries);
        OnCodexChanged?.Invoke();
    }

    public List<string> ExportNewClueIds() => _newState.ExportNewClueIds();

    public void ImportClueNewState(bool hasSavedDreamReadingCheckpoint, List<string> newClueIds)
    {
        IReadOnlyCollection<string> acquired = RouteModule.Instance?.Progress?.AcquiredClueIds;
        _newState.Import(hasSavedDreamReadingCheckpoint, newClueIds, acquired);
        OnCodexChanged?.Invoke();
    }

#if UNITY_EDITOR
    /// <summary>이미 획득한 테스트 데이터에서도 NEW 시각 효과를 반복 확인하기 위한 에디터 전용 진입점.</summary>
    public bool DebugMarkClueNewForValidation(string clueId)
    {
        ClueData clue = MapGraph.Instance?.GetClue(clueId);
        if (clue == null) return false;
        TrySubscribe();
        bool becameNew = _newState.TryMarkAcquired(clueId);
        Debug.Log($"[ClueNEW][State] 에디터 검증용 NEW 주입: clue={clueId}, becameNew={becameNew}, frame={Time.frameCount}");
        OnCodexChanged?.Invoke();
        // 이미 NEW인 경우에도 열린 보드가 다시 그려지도록 검증 이벤트는 발행한다.
        OnNewClueAcquired?.Invoke(clue);
        return true;
    }
#endif

    // SaveModule.WriteSaveFile이 성공한 뒤에만 호출한다. 저장 준비 단계에서 켜면 파일 쓰기 실패에도
    // 런타임 판정 기준이 앞당겨져 "확정·저장 이후" 계약을 어기게 된다.
    public void CommitSavedDreamReadingCheckpoint(bool hasResolvedDreamReading)
    {
        // 저장 코드가 이 호출에서 CodexModule을 처음 만들었을 수도 있다. Start까지 기다리면 같은
        // 프레임에 이어지는 단서 획득 이벤트를 놓치므로 기준점을 세울 때 즉시 구독한다.
        TrySubscribe();
        _newState.CommitSavedDreamReadingCheckpoint(hasResolvedDreamReading);
        Debug.Log($"[ClueNEW][State] 해몽 저장 기준점 커밋: requested={hasResolvedDreamReading}, checkpoint={_newState.HasSavedDreamReadingCheckpoint}, frame={Time.frameCount}");
    }
}
