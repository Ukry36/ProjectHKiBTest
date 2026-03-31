using System;
using UnityEngine;

[CreateAssetMenu(fileName = "StatBuff", menuName = "Scriptable Objects/StatBuff")]
public class StatBuffSO : ScriptableObject
{
    public enum TimeStackTypeEnum { Ignore, Stack, Overwrite }
    public enum BuffStackTypeEnum { Ignore, Stack, Overwrite, Independant }
    public enum BuffRemoveTypeEnum { Remove, Unstack, Permanent }

    [Serializable]
    public class BuffEffect
    {
        [field: SerializeField] public StatBuffTypeSO BuffType { get; private set; }
        [field: SerializeField] public bool IsDebuff { get; set; }
        [field: Min(0)][field: SerializeField] public float Value { get; set; }
        [field: SerializeField] public bool IsValuePropositional { get; set; }
    }

    public int ID => this.GetInstanceID();

    [field: NaughtyAttributes.ResizableTextArea]
    [field: SerializeField] public string Description { get; set; }

    [field: SerializeField] public bool IsBuffTimeInfinite { get; set; }
    [field: SerializeField] public float BuffTime { get; set; }

    [field: SerializeField] public TimeStackTypeEnum TimeStackType { get; set; }
    [field: SerializeField] public BuffStackTypeEnum BuffStackType { get; set; }
    [field: SerializeField] public BuffRemoveTypeEnum BuffRemoveType { get; set; }

    [field: SerializeField] public BuffEffect[] Effects { get; private set; }

    public int GetEffectID(int effectIndex)
    {
        return HashCode.Combine(ID, effectIndex);
    }

    public BuffEffect GetEffect(int effectIndex)
    {
        if (Effects == null || effectIndex < 0 || effectIndex >= Effects.Length) return null;
        return Effects[effectIndex];
    }

    public void AddBuff(InterfaceRegister interfaceReg, int multiplyer = 1, bool stack = true)
    {
        if (Effects == null) return;

        for (int i = 0; i < Effects.Length; i++)
        {
            BuffEffect effect = Effects[i];
            if (effect == null || effect.BuffType == null) continue;

            effect.BuffType.AddBuff(interfaceReg, this, i, multiplyer, stack);
        }
    }

    public void RemoveBuff(InterfaceRegister interfaceReg, int multiplyer = 1, bool remove = true)
    {
        if (Effects == null) return;

        for (int i = 0; i < Effects.Length; i++)
        {
            BuffEffect effect = Effects[i];
            if (effect == null || effect.BuffType == null) continue;

            effect.BuffType.RemoveBuff(interfaceReg, this, i, multiplyer, remove);
        }
    }
}