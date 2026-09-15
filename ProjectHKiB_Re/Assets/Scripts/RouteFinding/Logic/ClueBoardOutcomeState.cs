using System;
using System.Collections.Generic;

// [C06] 관계 결과의 발행/확인 상태 — ClueBoardProgressState와 같은 순수 C# 세션 상태 + 세이브 왕복.
//
// [무엇을 담나] (boardId, kind, outcomeId) → { readingId(발행 당시), viewed }. "발행했다"는 사실 자체가 재발행
// 방지의 근거다. 발행 여부는 ClueBoardOutcomeResolver가 새 연결 시점에만 판단하고, 복원(Import)은 판단을
// 거치지 않으므로 어떤 결과도 다시 내지 않는다.
//
// [무엇을 담지 않나] 해몽 본문, 관계 목록, 보상 플래그(EventManager가 저장), 노트/보드 진행. 결과 화면의
// 열림 상태·큐도 화면 안의 세션 상태다.
public sealed class ClueBoardOutcomeState
{
    public sealed class Record
    {
        public string boardId;
        public ClueBoardOutcomeKind kind;
        public string outcomeId;
        public string readingId;
        public bool viewed;
    }

    private readonly Dictionary<string, Record> _records = new(StringComparer.Ordinal);

    public int Count => _records.Count;
    public IEnumerable<Record> Records => _records.Values;

    private static string Key(string boardId, ClueBoardOutcomeKind kind, string outcomeId) =>
        boardId + "\u001f" + ClueBoardOutcomeCatalog.Key(kind, outcomeId);

    public bool IsIssued(string boardId, ClueBoardOutcomeKind kind, string outcomeId) =>
        boardId != null && outcomeId != null && _records.ContainsKey(Key(boardId, kind, outcomeId));

    public bool TryGet(string boardId, ClueBoardOutcomeKind kind, string outcomeId, out Record record)
    {
        record = null;
        return boardId != null && outcomeId != null && _records.TryGetValue(Key(boardId, kind, outcomeId), out record);
    }

    /// <summary>결과를 발행한 것으로 기록한다. 이미 있으면 false(멱등, 기존 viewed는 유지).</summary>
    public bool MarkIssued(string boardId, ClueBoardOutcomeKind kind, string outcomeId, string readingId, bool viewed = false)
    {
        if (string.IsNullOrEmpty(boardId) || string.IsNullOrEmpty(outcomeId)) return false;
        if (!Enum.IsDefined(typeof(ClueBoardOutcomeKind), kind)) return false;
        string key = Key(boardId, kind, outcomeId);
        if (_records.ContainsKey(key)) return false;
        _records[key] = new Record
        {
            boardId = boardId, kind = kind, outcomeId = outcomeId, readingId = readingId ?? "", viewed = viewed,
        };
        return true;
    }

    /// <summary>플레이어에게 결과 화면을 실제로 보였을 때. 발행 기록이 없으면 false.</summary>
    public bool MarkViewed(string boardId, ClueBoardOutcomeKind kind, string outcomeId)
    {
        if (!TryGet(boardId, kind, outcomeId, out Record record)) return false;
        bool changed = !record.viewed;
        record.viewed = true;
        return changed;
    }

    /// <summary>발행됐지만 아직 보여 주지 못한 결과(보류분).</summary>
    public List<Record> Unviewed()
    {
        var list = new List<Record>();
        foreach (Record record in _records.Values)
            if (!record.viewed) list.Add(record);
        return list;
    }

    /// <summary>보드 하나의 발행 기록.</summary>
    public List<Record> ForBoard(string boardId)
    {
        var list = new List<Record>();
        if (boardId == null) return list;
        foreach (Record record in _records.Values)
            if (string.Equals(record.boardId, boardId, StringComparison.Ordinal)) list.Add(record);
        return list;
    }

    public void Clear() => _records.Clear();

    // ─── 세이브 왕복 ─────────────────────────────────────────

    public ClueBoardOutcomeSaveData Export()
    {
        var data = new ClueBoardOutcomeSaveData { version = ClueBoardOutcomeSaveData.CurrentVersion };
        foreach (Record record in _records.Values)
            data.entries.Add(new ClueBoardOutcomeSaveInfo
            {
                boardId = record.boardId, kind = record.kind, outcomeId = record.outcomeId,
                readingId = record.readingId ?? "", viewed = record.viewed,
            });
        return data;
    }

    public sealed class ImportReport
    {
        public bool rejectedFutureVersion;
        public int imported;
        public int duplicatesSkipped;
        public int invalidSkipped;
        public readonly List<string> messages = new();
    }

    /// <summary>
    /// 세이브 내용으로 **교체**한다(이전 세션 상태를 남기지 않는다). 정의와 대조하지 않는다 — 로드 시점에 보드
    /// 정의가 없을 수 있고, 그때 걸러 버리면 발행 기록이 사라져 로드 후 같은 결과가 다시 발행된다.
    /// version 0(구 세이브)은 발행 없음, CurrentVersion보다 크면 거절하고 비운다. 빈 ID·null·알 수 없는 kind는
    /// 건너뛰고, 같은 식별자 중복은 한 번만 적용한다(viewed는 OR).
    /// </summary>
    public ImportReport Import(ClueBoardOutcomeSaveData data)
    {
        var report = new ImportReport();
        _records.Clear();
        if (data == null) return report;

        if (data.version > ClueBoardOutcomeSaveData.CurrentVersion)
        {
            report.rejectedFutureVersion = true;
            report.messages.Add($"관계 결과 저장 형식 v{data.version}은 이 빌드(v{ClueBoardOutcomeSaveData.CurrentVersion})가 읽을 수 없어 복원하지 않습니다.");
            return report;
        }
        if (data.entries == null || data.entries.Count == 0) return report;
        if (data.version <= 0)
        {
            report.invalidSkipped += data.entries.Count;
            report.messages.Add("관계 결과 저장에 version이 없어 내용을 무시합니다.");
            return report;
        }

        foreach (ClueBoardOutcomeSaveInfo entry in data.entries)
        {
            if (entry == null || string.IsNullOrEmpty(entry.boardId) || string.IsNullOrEmpty(entry.outcomeId) ||
                !Enum.IsDefined(typeof(ClueBoardOutcomeKind), entry.kind))
            {
                report.invalidSkipped++;
                continue;
            }
            if (MarkIssued(entry.boardId, entry.kind, entry.outcomeId, entry.readingId, entry.viewed))
            {
                report.imported++;
            }
            else
            {
                report.duplicatesSkipped++;
                if (entry.viewed) MarkViewed(entry.boardId, entry.kind, entry.outcomeId);
            }
        }
        return report;
    }
}
