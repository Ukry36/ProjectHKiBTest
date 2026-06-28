using UnityEngine;

[CreateAssetMenu(fileName = "Event", menuName = "Event/EventVariable")]
public class EventVariableSO : ScriptableObject
{
    public EnumManager.VariableType type; 
    private bool IsBool => type == EnumManager.VariableType.Bool;
    private bool IsInt => type == EnumManager.VariableType.Int;
    private bool IsFloat => type == EnumManager.VariableType.Float;
    [NaughtyAttributes.ShowIf("IsBool")] public bool initialValueBool;
    [NaughtyAttributes.ShowIf("IsInt")] public int initialValueInt;
    [NaughtyAttributes.ShowIf("IsFloat")] public float initialValueFloat;
}