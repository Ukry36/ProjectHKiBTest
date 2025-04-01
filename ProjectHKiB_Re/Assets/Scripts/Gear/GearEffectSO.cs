using UnityEngine;

[CreateAssetMenu(fileName = "GearEffect", menuName = "Scriptable Objects/GearEffect", order = 0)]
public abstract class GearEffectSO : ScriptableObject
{
    public abstract void ApplyEffect(MergedPlayerBaseData realGear);
}