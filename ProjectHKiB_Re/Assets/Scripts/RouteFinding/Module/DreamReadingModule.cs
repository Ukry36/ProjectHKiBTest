using System;
using System.Collections.Generic;
using UnityEngine;

// 해몽 판정 전담 싱글턴 — CodexModule/NoteModule과 같은 패턴.
//
// 노트에서 단서를 잇는 상호작용 자체는 이미 NoteModule/NoteRouteGraphView가 전부 하고 있다.
// 이 모듈이 얹는 것은 그 위의 판정층 하나다 — "지금 이어진 모양이 어떤 해몽 레시피와 맞는가".
// ════════════════════════════════════════════════════════════════
// [외부 모듈 연동 API]
//
// ▸ 접근: DreamReadingModule.Instance (자동 생성 싱글턴)
//
// ▸ 조회
//   ResolvedIds            : 지금까지 해몽에 성공한 레시피 id 전체(읽기 전용)
//   IsResolved(id)         : 특정 레시피를 이미 해몽했는지
//   OnReadingResolved      : 해몽이 성립한 순간 발행 — 해석 카드를 띄우는 UI가 구독한다
//
// ▸ 판정은 NoteModule.OnNoteChanged를 구독해 자동으로 돈다. 단서를 잇거나 끊을 때마다 재평가되므로
//   보통 외부에서 Evaluate()를 직접 부를 일은 없다(세이브 로드 직후 한 번 정도).
//
// ▸ 세이브: ExportResolved()/ImportResolved(). 해금된 플래그 자체는 EventManager가 이미 저장하므로
//   여기서는 "어떤 레시피를 풀었는지"만 남긴다 — 같은 해몽이 두 번 발행되지 않게 하는 용도다.
//
// ▸ [C06] 고정 단서 보드 경로: FindReading(id) / TryResolveById(id). 보드의 관계 연결 결과가 해몽 ID로 이 모듈을
//   가리키며, 성립 처리(플래그 해금·OnReadingResolved·resolved 기록)는 노트 경로와 같은 Resolve를 탄다.
//   이미 성립한 해몽은 거절하므로 노트/다른 보드에서 먼저 풀린 해몽에 보상이 중복되지 않는다.
// ════════════════════════════════════════════════════════════════
public class DreamReadingModule : MonoBehaviour
{
    // Resources 안에 있어야 하는 카탈로그 에셋 이름.
    private const string CatalogResourcePath = "DreamReadings";

    private static DreamReadingModule _instance;
    private static bool _isQuitting;

    public static DreamReadingModule Instance
    {
        get
        {
            if (_instance == null && Application.isPlaying && !_isQuitting)
            {
                _instance = FindObjectOfType<DreamReadingModule>();
                if (_instance == null)
                    _instance = new GameObject(nameof(DreamReadingModule)).AddComponent<DreamReadingModule>();
            }
            return _instance;
        }
    }

    private DreamReadingCatalogSO _catalog;
    private readonly HashSet<string> _resolvedIds = new();

    public IReadOnlyCollection<string> ResolvedIds => _resolvedIds;
    public bool IsResolved(string id) => !string.IsNullOrEmpty(id) && _resolvedIds.Contains(id);

    /// <summary>
    /// Returns whether this clue is required by any dream-reading combination.
    /// Dream-reading clues use only player-created links, never automatic keyword links.
    /// </summary>
    public bool IsDreamReadingClue(string clueId)
    {
        if (string.IsNullOrEmpty(clueId) || _catalog == null) return false;

        var readings = _catalog.Readings;
        for (int i = 0; i < readings.Count; i++)
        {
            string[] required = readings[i]?.requiredClueIds;
            if (required == null) continue;

            for (int j = 0; j < required.Length; j++)
                if (required[j] == clueId) return true;
        }

        return false;
    }

    // 해몽이 성립한 순간 발행. 해석 카드를 띄우는 UI가 이걸 구독한다.
    public event Action<DreamReading> OnReadingResolved;

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;

        _catalog = Resources.Load<DreamReadingCatalogSO>(CatalogResourcePath);
        if (_catalog == null)
            Debug.LogWarning($"[DreamReadingModule] Resources/{CatalogResourcePath} 에셋이 없어 해몽 레시피가 비어 있습니다.");
    }

    private void OnApplicationQuit() => _isQuitting = true;

    private void Start()
    {
        // 노트에서 단서를 잇거나 끊을 때마다 재평가한다(ToggleClueLink가 OnNoteChanged를 쏜다).
        if (NoteModule.Instance != null) NoteModule.Instance.OnNoteChanged += Evaluate;
        Evaluate();
    }

    private void OnDestroy()
    {
        if (NoteModule.Instance != null) NoteModule.Instance.OnNoteChanged -= Evaluate;
        if (_instance == this) _instance = null;
    }

    /// <summary>아직 안 풀린 레시피들을 지금 노트 상태로 다시 판정한다.</summary>
    public void Evaluate()
    {
        if (_catalog == null) return;

        var readings = _catalog.Readings;
        for (int i = 0; i < readings.Count; i++)
        {
            DreamReading reading = readings[i];
            if (reading == null || string.IsNullOrEmpty(reading.id)) continue;
            if (_resolvedIds.Contains(reading.id)) continue;
            if (!IsSatisfied(reading)) continue;

            Resolve(reading);
        }
    }

    private void Resolve(DreamReading reading)
    {
        _resolvedIds.Add(reading.id);

        EventManager eventManager = GameManager.instance == null ? null : GameManager.instance.eventManager;
        if (eventManager && reading.unlockFlags != null)
        {
            for (int i = 0; i < reading.unlockFlags.Length; i++)
            {
                if (!reading.unlockFlags[i]) continue;
                eventManager.SetEventFlag(reading.unlockFlags[i], reading.unlockValue);
            }
        }

        Debug.Log($"[DreamReadingModule] 해몽 성립: {reading.id} ({reading.title})");
        OnReadingResolved?.Invoke(reading);
    }

    // ─── [C06] 보드 경로 ─────────────────────────────────────────

    /// <summary>카탈로그에서 해몽 레시피를 찾는다. 없거나 카탈로그가 없으면 null.</summary>
    public DreamReading FindReading(string id)
    {
        if (string.IsNullOrEmpty(id) || _catalog == null) return null;
        var readings = _catalog.Readings;
        for (int i = 0; i < readings.Count; i++)
        {
            DreamReading reading = readings[i];
            if (reading != null && string.Equals(reading.id, id, StringComparison.Ordinal)) return reading;
        }
        return null;
    }

    /// <summary>
    /// 보드의 관계 결과가 해몽을 성립시킨다. 노트 판정(requiredClueIds/연결 덩어리)은 거치지 않는다 — 보드 관계가
    /// 성립 조건 그 자체다. 이미 성립한 해몽이면 아무것도 하지 않고 false(보상 중복 방지). 없는 ID도 false.
    /// </summary>
    public bool TryResolveById(string id)
    {
        DreamReading reading = FindReading(id);
        if (reading == null || _resolvedIds.Contains(reading.id)) return false;
        Resolve(reading);
        return true;
    }

    // ─── 판정 ────────────────────────────────────────────────────

    private bool IsSatisfied(DreamReading reading)
    {
        string[] required = reading.requiredClueIds;
        if (required == null || required.Length == 0) return false;

        RouteProgressState progress = RouteModule.Instance == null ? null : RouteModule.Instance.Progress;
        NoteModule note = NoteModule.Instance;
        if (progress == null || note == null) return false;

        for (int i = 0; i < required.Length; i++)
        {
            if (!progress.IsClueAcquired(required[i])) return false;
            if (!note.IsPinned(required[i])) return false;
        }

        // 재료가 하나면 이을 상대가 없다 — 노트에 올라와 있는 것으로 성립.
        if (required.Length == 1) return true;

        return IsConnectedGroup(required, note);
    }

    // required 안의 단서들이 (required 안의 간선만 써서) 하나의 덩어리로 이어지는지.
    // 전부가 서로 직접 이어질 필요는 없다 — A-B, B-C면 셋이 한 덩어리다.
    private static bool IsConnectedGroup(string[] required, NoteModule note)
    {
        var remaining = new HashSet<string>(required);
        var frontier = new Stack<string>();

        string seed = required[0];
        remaining.Remove(seed);
        frontier.Push(seed);

        while (frontier.Count > 0)
        {
            string current = frontier.Pop();

            // 남은 것 중 current와 직접 이어진 것을 덩어리로 흡수한다.
            var reached = new List<string>();
            foreach (string candidate in remaining)
                if (note.AreCluesLinked(current, candidate)) reached.Add(candidate);

            for (int i = 0; i < reached.Count; i++)
            {
                remaining.Remove(reached[i]);
                frontier.Push(reached[i]);
            }
        }

        return remaining.Count == 0;
    }

    // ─── 세이브 연동 ─────────────────────────────────────────────

    public List<string> ExportResolved() => new(_resolvedIds);

    public void ImportResolved(List<string> ids)
    {
        _resolvedIds.Clear();
        if (ids == null) return;

        for (int i = 0; i < ids.Count; i++)
            if (!string.IsNullOrEmpty(ids[i])) _resolvedIds.Add(ids[i]);
    }
}
