using System;
using System.Collections.Generic;
using UnityEngine;

// 런타임과 에디터의 공통 읽기/검증 계약. 실제 파일 쓰기는 에디터에서만 수행한다.
public static class ClueDatabaseCodec
{
    [Serializable]
    private class LegacyTypes { public LegacyType[] clues; }
    [Serializable]
    private class LegacyType { public int type; }

    public static bool TryRead(string json, out ClueDatabase database, out string error)
    {
        database = null;
        error = null;
        if (string.IsNullOrWhiteSpace(json)) { error = "단서 JSON이 비어 있습니다."; return false; }
        try
        {
            var parsed = JsonUtility.FromJson<ClueDatabase>(json);
            if (parsed == null) { error = "단서 데이터베이스가 없습니다."; return false; }
            if (parsed.schemaVersion != 0 && parsed.schemaVersion != ClueDatabase.CurrentSchemaVersion)
            {
                error = $"지원하지 않는 단서 스키마: {parsed.schemaVersion}";
                return false;
            }
            if (parsed.schemaVersion == 0 && parsed.clues != null)
            {
                // 구 type 키는 ClueData에 존재하지 않는다. 오직 구 정수 DTO로 읽는다.
                var legacy = JsonUtility.FromJson<LegacyTypes>(json);
                for (int i = 0; i < parsed.clues.Length; i++)
                {
                    if (parsed.clues[i] == null) continue;
                    parsed.clues[i].classification = null;
                    parsed.clues[i].emotionTag = ClueEmotionTag.Unset;
                    parsed.clues[i].legacyType = legacy?.clues != null && i < legacy.clues.Length &&
                        legacy.clues[i] != null ? legacy.clues[i].type : -1;
                }
            }
            // 읽기는 구조와 진행에 필요한 필드만 확인한다. 본문 매체는 주소 누락이나 새 종류가
            // 있어도 카드별 대체 표시로 열려야 하므로, 작성/저장 때의 엄격 검증을 여기 적용하지 않는다.
            if (!Validate(parsed, parsed.schemaVersion != 0, validateMediaBlocks: false, out error)) return false;
            database = parsed;
            return true;
        }
        catch (Exception ex)
        {
            error = "단서 JSON 읽기 실패: " + ex.Message;
            return false;
        }
    }

    public static bool Validate(ClueDatabase database, bool requireClassification, out string error) =>
        Validate(database, requireClassification, validateMediaBlocks: true, out error);

    private static bool Validate(ClueDatabase database, bool requireClassification, bool validateMediaBlocks, out string error)
    {
        var errors = new List<string>();
        if (database == null || database.clues == null)
        {
            error = "clues 배열이 필요합니다.";
            return false;
        }
        if (database.schemaVersion != 0 && database.schemaVersion != ClueDatabase.CurrentSchemaVersion)
            errors.Add("지원하지 않는 단서 스키마입니다.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var clue in database.clues)
        {
            if (clue == null) { errors.Add("null 단서 항목이 있습니다."); continue; }
            if (string.IsNullOrWhiteSpace(clue.id) || clue.id != clue.id.Trim())
                errors.Add("단서 ID가 비었거나 앞뒤 공백이 있습니다.");
            else if (!ids.Add(clue.id)) errors.Add("중복 단서 ID: " + clue.id);
            if (clue.classification == null)
            {
                if (requireClassification) errors.Add(clue.id + ": 미분류");
            }
            else if (!ClueTypeConfig.IsValid(clue.classification))
                errors.Add(clue.id + ": 유형/세부 유형 조합이 유효하지 않습니다.");
            if (clue.emotionTag < ClueEmotionTag.Unset || clue.emotionTag >= ClueEmotionTag.ReservedFourth)
                errors.Add(clue.id + ": 미정 또는 유효하지 않은 감정 태그입니다.");
            if (!IsFinite(clue.boardSizePercent) || (clue.boardSizePercent != 0f &&
                (clue.boardSizePercent < 25f || clue.boardSizePercent > 300f)))
                errors.Add(clue.id + ": 보드 카드 크기는 0 또는 25~300%여야 합니다.");
            if (!IsFinite(clue.boardFontSize) || (clue.boardFontSize != 0f &&
                (clue.boardFontSize < 4f || clue.boardFontSize > 24f)))
                errors.Add(clue.id + ": 보드 글자 크기는 0 또는 4~24여야 합니다.");
            if (clue.attachments != null)
                foreach (var attachment in clue.attachments)
                {
                    if (attachment == null) { errors.Add(clue.id + ": null 첨부물입니다."); continue; }
                    if (!Enum.IsDefined(typeof(ClueAttachmentKind), attachment.kind))
                        errors.Add(clue.id + ": 지원하지 않는 첨부물 유형입니다.");
                }
            // 본문 매체 블록 — 종류마다 채워야 하는 필드가 다르다. 런타임은 빈 주소를 "(파일 없음)"으로
            // 부드럽게 넘기지만(카드가 열리지 않는 일을 막기 위함), 작성 시점에는 여기서 거절한다.
            if (validateMediaBlocks && clue.mediaBlocks != null)
                foreach (var block in clue.mediaBlocks)
                {
                    if (block == null) { errors.Add(clue.id + ": null 본문 매체 블록입니다."); continue; }
                    if (!Enum.IsDefined(typeof(ClueMediaKind), block.kind))
                    {
                        errors.Add(clue.id + ": 지원하지 않는 본문 매체 종류입니다.");
                        continue;
                    }
                    if (ClueMediaConfig.NeedsAddress(block.kind))
                    {
                        if (string.IsNullOrWhiteSpace(block.address))
                            errors.Add(clue.id + $": {ClueMediaConfig.GetDisplayName(block.kind)} 블록의 주소가 비어 있습니다.");
                    }
                    else if (string.IsNullOrWhiteSpace(block.text))
                        errors.Add(clue.id + ": 글 블록의 본문이 비어 있습니다.");
                }
            if (clue.comments != null)
                foreach (var comment in clue.comments)
                    if (comment == null) errors.Add(clue.id + ": null 코멘트입니다.");
        }
        error = errors.Count == 0 ? null : string.Join("\n", errors);
        return errors.Count == 0;
    }

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    // 콘텐츠 작성 시만 참조를 엄격히 검사한다. 런타임 미디어 실패는 기존 대체 표시 경로를 유지한다.
    public static bool ValidateReferences(ClueDatabase database, MapDatabase maps, out string error)
    {
        if (!Validate(database, true, out error)) return false;
        if (maps?.maps == null || maps.connections == null)
        { error = "맵 데이터가 없어 단서 참조를 검증할 수 없습니다."; return false; }
        var nodes = new HashSet<string>(StringComparer.Ordinal);
        var connections = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in maps.maps) if (node != null) nodes.Add(node.guid);
        foreach (var connection in maps.connections) if (connection != null) connections.Add(connection.guid);
        var errors = new List<string>();
        foreach (var clue in database.clues)
        {
            if (!string.IsNullOrEmpty(clue.targetMapGuid) && !nodes.Contains(clue.targetMapGuid))
                errors.Add(clue.id + ": 없는 대상 맵 " + clue.targetMapGuid);
            if (!string.IsNullOrEmpty(clue.codexMapGuid) && !nodes.Contains(clue.codexMapGuid))
                errors.Add(clue.id + ": 없는 도감 맵 " + clue.codexMapGuid);
            if (!string.IsNullOrEmpty(clue.targetConnectionGuid) && !connections.Contains(clue.targetConnectionGuid))
                errors.Add(clue.id + ": 없는 대상 연결 " + clue.targetConnectionGuid);
            if (clue.attachments == null) continue;
            foreach (var attachment in clue.attachments)
                if (attachment.kind == ClueAttachmentKind.MapRef)
                {
                    if (string.IsNullOrEmpty(attachment.mapGuid) || !nodes.Contains(attachment.mapGuid))
                        errors.Add(clue.id + ": 없는 첨부 맵 " + attachment.mapGuid);
                }
                else if (string.IsNullOrWhiteSpace(attachment.address))
                    errors.Add(clue.id + ": 첨부물 주소가 비어 있습니다.");
        }
        error = errors.Count == 0 ? null : string.Join("\n", errors);
        return errors.Count == 0;
    }

    public static bool TryWrite(ClueDatabase database, out string json, out string error)
    {
        json = null;
        if (!Validate(database, true, out error)) return false;
        // 원본 객체의 버전을 바꾸지 않는다. 파일 저장 성공 후 에디터가 버전을 갱신한다.
        var output = new ClueDatabase { schemaVersion = ClueDatabase.CurrentSchemaVersion, clues = database.clues };
        json = JsonUtility.ToJson(output, true);
        return true;
    }
}
