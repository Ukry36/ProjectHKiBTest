#if UNITY_EDITOR
using UnityEditor;

[InitializeOnLoad]
public static class EnableDOTweenTMP
{
    static EnableDOTweenTMP()
    {
        const string symbol = "DOTWEEN_TMP";
        var buildTarget = EditorUserBuildSettings.selectedBuildTargetGroup;
        string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTarget);

        if (!symbols.Contains(symbol))
        {
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTarget, symbols + ";" + symbol);
            UnityEngine.Debug.Log("✅ DOTWEEN_TMP define added automatically for TMP support.");
        }
    }
}
#endif

