using UnityEngine;

[CreateAssetMenu(
    fileName = "LoadFailState",
    menuName = "Scriptable Objects/Save/States/LoadFailState",
    order = 99)]
public class LoadFailStateSO : SaveBaseStateSO
{
    [TextArea] public string message = "Load failed: save file not found or invalid.";

    public override void OnEnter(SaveModule module)
    {
        int slot = (module != null) ? module.Slot : -1;
        Debug.LogWarning($"[LOAD][FAIL] slot={slot} - {message}");
    }

    public override void OnUpdate(SaveModule module) { }
    public override void OnExit(SaveModule module) { }
}