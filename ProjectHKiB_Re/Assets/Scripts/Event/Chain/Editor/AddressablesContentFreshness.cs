using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// Player 빌드에 실릴 Addressables 콘텐츠(Library/com.unity.addressables/aa)가 현재 에셋과 같은지 검사한다.
///
/// [왜 필요한가] Unity 2021.3 + Addressables 1.29에서는 "Build Addressables on Player Build" 설정이 동작하지 않는다 —
/// 패키지의 AddressablesPlayerBuildProcessor가 2022.1 미만에서는 콘텐츠를 빌드하지 않고, 마지막으로 빌드해 둔
/// aa 폴더를 StreamingAssets로 옮기기만 한다(IPreprocessBuildWithReport 분기). 게다가 Addressables 맵 씬(TestMap1~3)은
/// Generated 이벤트 트리거 프리팹 → EventSO → StateSO → 효과음 SO를 직접 참조하므로 그 전부가 **씬 번들에 복제**된다.
/// 즉 체인/Generated 에셋을 고쳐도(예: Bind Event Effect Audio) Addressables를 다시 빌드하지 않으면 Player는
/// 옛 복제본을 실행한다 — 2026-09-02/09-03 빌드에서 이벤트 효과음이 나지 않은 원인이 이것이다(번들 안 StateSO의
/// audioCue._audioData가 null, 2026-09-01 12:11 빌드본).
///
/// [왜 빌드 중에 자동으로 다시 빌드하지 않나] ScriptableBuildPipeline은 Player 빌드 중(BuildPipeline.isBuildingPlayer)
/// 번들 빌드를 거부한다. 그래서 검사만 하고, 오래됐으면 Player 빌드를 막고 먼저 콘텐츠를 빌드하게 한다.
///
/// [판정] Addressables 콘텐츠 빌드가 끝날 때마다(BuildScript.buildCompleted) 모든 addressable 항목과 그 재귀 의존 에셋의
/// AssetDatabase.GetAssetDependencyHash를 지문 파일(aa 폴더 안 content_fingerprint.txt)로 남기고, 검사 때 다시 계산해
/// 비교한다. 패키지의 addressables_content_state.bin은 "Prevent Updates(StaticContent)" 그룹만 기록해 이 프로젝트에선
/// 비어 있으므로 쓰지 않는다. 지문이 없는 옛 빌드본은 오래된 것으로 본다.
/// </summary>
[InitializeOnLoad]
public static class AddressablesContentFreshness
{
    private const string LogPrefix = "[AddressablesContentFreshness]";
    private const string FingerprintFileName = "content_fingerprint.txt";
    public const string BuildMenuPath = "Tools/Event/Addressables 콘텐츠 빌드 (플레이어 빌드 전)";

    static AddressablesContentFreshness()
    {
        // Groups 창의 New Build, BuildPlayerContent 어느 경로로 빌드해도 지문을 남긴다(플레이 모드 빌드는 제외).
        BuildScript.buildCompleted += OnAddressablesBuildCompleted;
    }

    public sealed class Report
    {
        /// <summary>Addressables 설정이 없어 검사 자체가 해당 없음.</summary>
        public bool NotApplicable;

        /// <summary>aa 폴더/지문이 없어 콘텐츠가 빌드되지 않았거나(다른 머신에서 받은 클론 포함) 이 검사 도입 전에 빌드됨.</summary>
        public bool ContentMissing;

        public string BuildPath;
        public string FingerprintPath;
        public readonly List<string> StaleReasons = new List<string>();
        public readonly List<string> Notes = new List<string>();

        public bool IsFresh => !ContentMissing && StaleReasons.Count == 0;
    }

    private static string FingerprintPath => Path.Combine(Addressables.BuildPath, FingerprintFileName);

    // ─── 지문 ────────────────────────────────────────────────

    /// <summary>addressable 항목 + 재귀 의존 에셋의 경로 → 의존성 해시. 정렬된 사전이라 파일 비교가 안정적이다.</summary>
    public static SortedDictionary<string, string> ComputeFingerprint(AddressableAssetSettings settings)
    {
        var fingerprint = new SortedDictionary<string, string>(StringComparer.Ordinal);
        if (settings == null) return fingerprint;
        foreach (AddressableAssetGroup group in settings.groups)
        {
            if (group == null || !group.HasSchema<BundledAssetGroupSchema>() ||
                !group.GetSchema<BundledAssetGroupSchema>().IncludeInBuild) continue;
            foreach (AddressableAssetEntry entry in group.entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.AssetPath)) continue;
                AddPath(fingerprint, entry.AssetPath);
                foreach (string dependency in AssetDatabase.GetDependencies(entry.AssetPath, true))
                    AddPath(fingerprint, dependency);
            }
        }
        return fingerprint;
    }

    private static void AddPath(IDictionary<string, string> fingerprint, string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath) || fingerprint.ContainsKey(assetPath)) return;
        // 스크립트는 코드 변경이 아니라 에셋 변경만 보려는 것이므로 제외한다(형식 변경은 참조하는 에셋의 재직렬화로 드러난다).
        if (assetPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)) return;
        if (!assetPath.StartsWith("Assets/", StringComparison.Ordinal) && !assetPath.StartsWith("Packages/", StringComparison.Ordinal)) return;
        Hash128 hash = AssetDatabase.GetAssetDependencyHash(assetPath);
        fingerprint[assetPath] = hash.isValid ? hash.ToString() : "(invalid)";
    }

    private static void OnAddressablesBuildCompleted(AddressableAssetBuildResult result)
    {
        // 플레이 모드용 빌드(Use Asset Database 등)는 aa 폴더를 만들지 않는다 — Player 콘텐츠 빌드만 지문을 남긴다.
        if (!(result is AddressablesPlayerBuildResult) || !string.IsNullOrEmpty(result?.Error)) return;
        try
        {
            WriteFingerprint(AddressableAssetSettingsDefaultObject.Settings);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"{LogPrefix} 콘텐츠 지문을 남기지 못했습니다: {ex.Message}");
        }
    }

    public static void WriteFingerprint(AddressableAssetSettings settings)
    {
        SortedDictionary<string, string> fingerprint = ComputeFingerprint(settings);
        var builder = new StringBuilder();
        builder.AppendLine("# Addressables content fingerprint — written after each Addressables player-content build. Do not edit.");
        builder.AppendLine("# " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        foreach (KeyValuePair<string, string> pair in fingerprint)
            builder.Append(pair.Value).Append('\t').AppendLine(pair.Key);
        Directory.CreateDirectory(Addressables.BuildPath);
        File.WriteAllText(FingerprintPath, builder.ToString(), new UTF8Encoding(false));
        Debug.Log($"{LogPrefix} 콘텐츠 지문 기록: {fingerprint.Count}개 에셋 → {FingerprintPath}");
    }

    private static Dictionary<string, string> ReadFingerprint(string path)
    {
        var stored = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string line in File.ReadAllLines(path, Encoding.UTF8))
        {
            if (line.Length == 0 || line[0] == '#') continue;
            int tab = line.IndexOf('\t');
            if (tab <= 0) continue;
            stored[line.Substring(tab + 1)] = line.Substring(0, tab);
        }
        return stored;
    }

    // ─── 검사 ────────────────────────────────────────────────

    /// <summary>GUI 없이 검사한다. 예외를 던지지 않는다.</summary>
    public static Report Check()
    {
        var report = new Report();
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            report.NotApplicable = true;
            report.Notes.Add("Addressables 설정(AddressableAssetSettingsDefaultObject)이 없어 검사하지 않습니다.");
            return report;
        }

        report.BuildPath = Addressables.BuildPath;
        report.FingerprintPath = FingerprintPath;

        if (string.IsNullOrEmpty(report.BuildPath) || !Directory.Exists(report.BuildPath) ||
            !File.Exists(Path.Combine(report.BuildPath, "catalog.json")))
        {
            report.ContentMissing = true;
            report.StaleReasons.Add($"Addressables 콘텐츠 출력이 없습니다: {report.BuildPath}. 이 상태로 Player를 빌드하면 맵 씬 번들이 실리지 않습니다.");
            return report;
        }
        if (!File.Exists(report.FingerprintPath))
        {
            report.ContentMissing = true;
            report.StaleReasons.Add($"콘텐츠 지문이 없습니다({report.FingerprintPath}). 이 검사 도입 전에 빌드한 콘텐츠라 최신 여부를 보증할 수 없습니다.");
            return report;
        }

        Dictionary<string, string> stored;
        try { stored = ReadFingerprint(report.FingerprintPath); }
        catch (Exception ex)
        {
            report.ContentMissing = true;
            report.StaleReasons.Add($"콘텐츠 지문을 읽지 못했습니다: {ex.Message}");
            return report;
        }

        SortedDictionary<string, string> current = ComputeFingerprint(settings);
        foreach (KeyValuePair<string, string> pair in current)
        {
            if (!stored.TryGetValue(pair.Key, out string storedHash))
                report.StaleReasons.Add($"마지막 콘텐츠 빌드 뒤 추가된 에셋: {pair.Key}");
            else if (!string.Equals(storedHash, pair.Value, StringComparison.Ordinal))
                report.StaleReasons.Add($"마지막 콘텐츠 빌드 뒤 변경된 에셋: {pair.Key}");
        }
        foreach (string path in stored.Keys)
            if (!current.ContainsKey(path))
                report.StaleReasons.Add($"마지막 콘텐츠 빌드에 있던 에셋이 빠졌거나 삭제됨: {path}");

        if (report.IsFresh)
            report.Notes.Add($"Addressables 콘텐츠가 최신입니다 ({current.Count}개 에셋 일치): {report.BuildPath}");
        return report;
    }

    /// <summary>Player 빌드 차단 메시지에 붙일 안내.</summary>
    public static string HowToFix =>
        $"Player 빌드 전에 Addressables 콘텐츠를 다시 빌드하세요 — 메뉴 [{BuildMenuPath}] 또는 " +
        "Window > Asset Management > Addressables > Groups > Build > New Build > Default Build Script. " +
        "(Unity 2021.3에서는 'Build Addressables on Player Build' 설정이 동작하지 않아 자동으로 빌드되지 않습니다.)";

    [MenuItem("Tools/Event/Addressables 콘텐츠 최신 여부 확인")]
    public static void CheckFromMenu()
    {
        Report report = Check();
        foreach (string note in report.Notes) Debug.Log($"{LogPrefix} {note}");
        foreach (string reason in report.StaleReasons) Debug.LogWarning($"{LogPrefix} {reason}");
        if (report.NotApplicable) return;
        if (report.IsFresh) Debug.Log($"{LogPrefix} 최신 — Player 빌드에 현재 에셋이 그대로 실립니다.");
        else Debug.LogError($"{LogPrefix} 오래됨 ({report.StaleReasons.Count}건). {HowToFix}");
        if (Application.isBatchMode) EditorApplication.Exit(report.IsFresh ? 0 : 1);
    }

    /// <summary>
    /// Addressables 콘텐츠를 지금 빌드한다(New Build > Default Build Script와 같음). Player 빌드 전에 실행한다.
    /// 배치: -executeMethod AddressablesContentFreshness.BuildContent
    /// </summary>
    [MenuItem(BuildMenuPath)]
    public static void BuildContent()
    {
        if (BuildPipeline.isBuildingPlayer)
        {
            Debug.LogError($"{LogPrefix} Player 빌드 중에는 Addressables 콘텐츠를 빌드할 수 없습니다(ScriptableBuildPipeline 제한).");
            if (Application.isBatchMode) EditorApplication.Exit(1);
            return;
        }
        AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
        bool ok = result != null && string.IsNullOrEmpty(result.Error);
        if (ok)
        {
            Report after = Check();
            ok = after.IsFresh;
            Debug.Log($"{LogPrefix} Addressables 콘텐츠 빌드 완료 ({result.Duration:F1}s) → {result.OutputPath}. 최신 여부: {(after.IsFresh ? "최신" : "오래됨: " + string.Join(" | ", after.StaleReasons))}");
        }
        else Debug.LogError($"{LogPrefix} Addressables 콘텐츠 빌드 실패: {result?.Error}");
        if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
    }
}
