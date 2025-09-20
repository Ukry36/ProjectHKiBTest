using UnityEngine;

[CreateAssetMenu(fileName = "StatBuff", menuName = "Scriptable Objects/StatBuff")]
public class StatBuffSO : ScriptableObject
{
    public enum TimeStackTypeEnum { Ignore, Stack, Overwrite, }
    public enum BuffStackTypeEnum { Ignore, Stack, Overwrite, Independant, }
    public enum BuffRemoveTypeEnum { Remove, Unstack, }

    public int ID { get => this.GetInstanceID(); }
    [field: NaughtyAttributes.ResizableTextArea()][field: SerializeField] public string Description { get; set; }
    [field: SerializeField] public float BuffTime { get; set; }
    //[field: SerializeField] public bool UseToken { get; set; }
    //[field: SerializeField] public bool TimeStackable { get; set; }
    //[field: SerializeField] public bool Multiplyable { get; set; }
    //[field: SerializeField] public bool UpdateBuffTime { get; set; }
    [field: SerializeField] public bool IsDebuff { get; set; }
    [field: NaughtyAttributes.MinValue(0)][field: SerializeField] public float Value { get; set; }
    [field: SerializeField] public bool IsValuePropositional { get; set; }
    [field: SerializeField] public StatBuffTypeSO BuffType { get; private set; }
    [field: SerializeField] public TimeStackTypeEnum TimeStackType { get; set; }
    [field: SerializeField] public BuffStackTypeEnum BuffStackType { get; set; }
    [field: SerializeField] public BuffRemoveTypeEnum BuffRemoveType { get; set; }
    public void AddBuff(InterfaceRegister interfaceReg, int multiplyer = 1, bool stack = true)
    => BuffType.AddBuff(interfaceReg, this, multiplyer, stack);

    public void RemoveBuff(InterfaceRegister interfaceReg, int multiplyer = 1, bool remove = true)
    => BuffType.RemoveBuff(interfaceReg, this, multiplyer, remove);

}