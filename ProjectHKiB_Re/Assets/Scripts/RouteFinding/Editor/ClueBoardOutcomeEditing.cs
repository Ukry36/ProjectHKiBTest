using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RouteFinding.Editor
{
    // [C06 편집] 관계 결과 배선(relation.readingId / chains)을 맵 DB 편집기에서 다루기 위한 GUI 없는 도우미.
    //
    // 화면(MapDatabaseEditorWindow)이 아니라 여기에 둔 이유는 회귀 검증이 GUI 없이 같은 규칙을 돌리기 위해서다.
    // 진단의 정의는 런타임(ClueBoardOutcomeCatalog)이 소유한다 — 여기서는 그 진단을 그대로 쓰고, 편집 화면에만 필요한
    // 두 가지를 얹는다: (1) 해몽 ID가 DreamReadings.asset에 실제로 있는지, (2) 행 단위(체인 하나)로 나눠 보여 줄 문구.
    // 결과 배선 오류는 데이터 오류다 — 어떤 진단도 보드 JSON 저장을 막지 않는다(ClueBoardDatabaseCodec은 경고로만 낸다).
    public static class ClueBoardOutcomeEditing
    {
        // DreamReadingModule과 같은 Resources 키. 에셋 자체는 읽기만 하고 수정하지 않는다.
        public const string CatalogResourcePath = "DreamReadings";

        /// <summary>Resources/DreamReadings.asset의 해몽 ID 목록(등장 순서). 에셋이 없으면 빈 목록.</summary>
        public static List<string> LoadReadingIds()
        {
            var ids = new List<string>();
            var catalog = Resources.Load<DreamReadingCatalogSO>(CatalogResourcePath);
            if (catalog == null) return ids;
            foreach (DreamReading reading in catalog.Readings)
                if (reading != null && !string.IsNullOrWhiteSpace(reading.id) && !ids.Contains(reading.id))
                    ids.Add(reading.id);
            return ids;
        }

        /// <summary>JsonUtility가 null로 남긴 체인 배열/필드를 채운다(NormalizeBoard의 체인 부분).</summary>
        public static void NormalizeChains(ClueBoardDefinition board)
        {
            if (board == null) return;
            board.chains ??= Array.Empty<ClueBoardRelationChain>();
            foreach (ClueBoardRelationChain chain in board.chains)
            {
                if (chain == null) continue;
                chain.chainId ??= "";
                chain.readingId ??= "";
                chain.relationIds ??= Array.Empty<string>();
                for (int i = 0; i < chain.relationIds.Length; i++) chain.relationIds[i] ??= "";
            }
            foreach (ClueBoardRelation relation in board.relations ?? Array.Empty<ClueBoardRelation>())
                if (relation != null) relation.readingId ??= "";
        }

        public static string NextChainId(ClueBoardDefinition board)
        {
            int number = (board.chains?.Length ?? 0) + 1;
            string candidate;
            do candidate = "chain-" + number++;
            while (board.chains != null && Array.Exists(board.chains, item => item != null && item.chainId == candidate));
            return candidate;
        }

        // ─── 참조 정리 ───────────────────────────────────────────

        /// <summary>
        /// 관계를 지울 때 그 ID를 가리키던 체인/초기 연결 참조를 함께 정리한다. 체인은 지우지 않고 관계만 빼므로
        /// 관계가 2개 미만이 된 체인은 그대로 남아 진단에 잡힌다 — 돌려주는 목록이 그런 체인 ID다(화면 경고용).
        /// </summary>
        public static List<string> RemoveRelationReferences(ClueBoardDefinition board, string relationId)
        {
            var weakened = new List<string>();
            if (board == null || string.IsNullOrEmpty(relationId)) return weakened;
            if (board.initialRelationIds != null) ArrayUtility.Remove(ref board.initialRelationIds, relationId);
            if (board.chains == null) return weakened;
            foreach (ClueBoardRelationChain chain in board.chains)
            {
                if (chain?.relationIds == null || Array.IndexOf(chain.relationIds, relationId) < 0) continue;
                var kept = new List<string>(chain.relationIds);
                kept.RemoveAll(id => string.Equals(id, relationId, StringComparison.Ordinal));
                chain.relationIds = kept.ToArray();
                if (kept.Count < 2) weakened.Add(chain.chainId ?? "");
            }
            return weakened;
        }

        /// <summary>관계 ID를 바꿨을 때 체인/초기 연결이 새 ID를 따라가게 한다.</summary>
        public static int ReplaceRelationIdReferences(ClueBoardDefinition board, string previousId, string nextId)
        {
            int replaced = 0;
            if (board == null || string.IsNullOrEmpty(previousId) || previousId == nextId) return replaced;
            if (board.initialRelationIds != null)
                for (int i = 0; i < board.initialRelationIds.Length; i++)
                    if (board.initialRelationIds[i] == previousId) { board.initialRelationIds[i] = nextId ?? ""; replaced++; }
            if (board.chains != null)
                foreach (ClueBoardRelationChain chain in board.chains)
                {
                    if (chain?.relationIds == null) continue;
                    for (int i = 0; i < chain.relationIds.Length; i++)
                        if (chain.relationIds[i] == previousId) { chain.relationIds[i] = nextId ?? ""; replaced++; }
                }
            return replaced;
        }

        /// <summary>체인에 아직 들어 있지 않고 Unrelated가 아닌 첫 관계 ID. 없으면 null.</summary>
        public static string FirstRelationNotInChain(ClueBoardDefinition board, ClueBoardRelationChain chain)
        {
            if (board?.relations == null) return null;
            foreach (ClueBoardRelation relation in board.relations)
            {
                if (relation == null || string.IsNullOrWhiteSpace(relation.relationId) ||
                    relation.kind == ClueBoardRelationKind.Unrelated) continue;
                if (chain?.relationIds != null && Array.IndexOf(chain.relationIds, relation.relationId) >= 0) continue;
                return relation.relationId;
            }
            return null;
        }

        // ─── 진단 ────────────────────────────────────────────────

        /// <summary>관계 한 줄의 결과 배선 경고. 문제 없으면 null.</summary>
        public static string DiagnoseRelation(ClueBoardRelation relation, ICollection<string> knownReadingIds)
        {
            if (relation == null || string.IsNullOrWhiteSpace(relation.readingId)) return null;
            if (relation.kind == ClueBoardRelationKind.Unrelated)
                return "Unrelated 관계에는 결과를 배선할 수 없습니다 — 런타임이 무시합니다.";
            return UnknownReadingWarning(relation.readingId, knownReadingIds);
        }

        /// <summary>체인 한 줄의 결과 배선 경고 전부. 문제 없으면 빈 목록.</summary>
        public static List<string> DiagnoseChain(
            ClueBoardDefinition board, ClueBoardRelationChain chain, int chainIndex, ICollection<string> knownReadingIds)
        {
            var warnings = new List<string>();
            if (chain == null) { warnings.Add("체인이 null입니다."); return warnings; }
            if (string.IsNullOrWhiteSpace(chain.chainId)) warnings.Add("체인 ID가 비어 있습니다.");
            else if (board?.chains != null)
                // 런타임은 같은 ID 중 앞의 정의만 쓰므로, 무시될 뒤의 항목에 경고를 단다.
                for (int i = 0; i < chainIndex && i < board.chains.Length; i++)
                    if (board.chains[i] != null && board.chains[i].chainId == chain.chainId)
                    {
                        warnings.Add($"체인 ID '{chain.chainId}'가 [{i}]와 중복됩니다 — 이 항목은 런타임이 무시합니다.");
                        break;
                    }

            if (string.IsNullOrWhiteSpace(chain.readingId)) warnings.Add("해몽 결과가 비어 있습니다 — 결과 없는 체인은 무시됩니다.");
            else
            {
                string unknown = UnknownReadingWarning(chain.readingId, knownReadingIds);
                if (unknown != null) warnings.Add(unknown);
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            int distinct = 0;
            if (chain.relationIds != null)
                foreach (string relationId in chain.relationIds)
                {
                    if (string.IsNullOrWhiteSpace(relationId)) { warnings.Add("빈 관계 ID가 있습니다."); continue; }
                    if (!seen.Add(relationId)) { warnings.Add($"관계 '{relationId}'가 중복됩니다."); continue; }
                    distinct++;
                    ClueBoardRelation relation = FindRelation(board, relationId);
                    if (relation == null) warnings.Add($"관계 '{relationId}'가 이 보드에 없습니다.");
                    else if (relation.kind == ClueBoardRelationKind.Unrelated) warnings.Add($"관계 '{relationId}'는 Unrelated라 성립할 수 없습니다.");
                }
            if (distinct < 2)
                warnings.Add($"체인은 관계 2개 이상이어야 합니다(현재 {distinct}개). 관계 하나짜리 결과는 관계의 '해몽 결과'에 넣으세요.");
            return warnings;
        }

        /// <summary>
        /// 보드 한 장의 결과 배선 진단 전체 — 런타임 카탈로그가 내는 진단(구조) + 없는 해몽 ID(편집 전용).
        /// 한 화면에서 관계/체인/해몽 ID 문제를 함께 보는 요약 상자용이다.
        /// </summary>
        public static List<string> Diagnose(ClueBoardDefinition board, ICollection<string> knownReadingIds)
        {
            var messages = new List<string>();
            if (board == null) return messages;
            ClueBoardOutcomeCatalog catalog = ClueBoardOutcomeCatalog.Build(board, messages);
            foreach (ClueBoardOutcomeDefinition outcome in catalog.Outcomes)
            {
                string unknown = UnknownReadingWarning(outcome.readingId, knownReadingIds);
                if (unknown == null) continue;
                string label = outcome.kind == ClueBoardOutcomeKind.Chain ? $"체인 '{outcome.outcomeId}'" : $"관계 '{outcome.outcomeId}'";
                messages.Add($"보드 '{board.boardId}': {label}의 {unknown}");
            }
            return messages;
        }

        /// <summary>파일 전체의 "없는 해몽 ID" 경고 한 문자열(저장 뒤 경고 상자용). 없으면 null.</summary>
        public static string DescribeUnknownReadings(ClueBoardDatabase database, ICollection<string> knownReadingIds)
        {
            if (database?.boards == null) return null;
            var messages = new List<string>();
            foreach (ClueBoardDefinition board in database.boards)
            {
                if (board == null) continue;
                ClueBoardOutcomeCatalog catalog = ClueBoardOutcomeCatalog.Build(board, null);
                foreach (ClueBoardOutcomeDefinition outcome in catalog.Outcomes)
                {
                    string unknown = UnknownReadingWarning(outcome.readingId, knownReadingIds);
                    if (unknown == null) continue;
                    string label = outcome.kind == ClueBoardOutcomeKind.Chain ? $"체인 '{outcome.outcomeId}'" : $"관계 '{outcome.outcomeId}'";
                    messages.Add($"보드 '{board.boardId}': {label}의 {unknown}");
                }
            }
            return messages.Count == 0 ? null : string.Join("\n", messages);
        }

        private static string UnknownReadingWarning(string readingId, ICollection<string> knownReadingIds)
        {
            if (string.IsNullOrWhiteSpace(readingId)) return null;
            if (knownReadingIds == null) return "해몽 목록(DreamReadings.asset)을 읽지 못해 ID를 확인할 수 없습니다.";
            return knownReadingIds.Contains(readingId.Trim())
                ? null
                : $"해몽 '{readingId}'가 DreamReadings.asset에 없습니다 — 연결돼도 결과가 나오지 않습니다.";
        }

        private static ClueBoardRelation FindRelation(ClueBoardDefinition board, string relationId)
        {
            if (board?.relations == null) return null;
            foreach (ClueBoardRelation relation in board.relations)
                if (relation != null && string.Equals(relation.relationId, relationId, StringComparison.Ordinal)) return relation;
            return null;
        }
    }
}
