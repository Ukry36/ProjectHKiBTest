using UnityEngine;

[CreateAssetMenu(fileName = "Event", menuName = "Event/EventFlag")]
public class EventFlagSO : ScriptableObject
{
    public EnumManager.VariableType type;
    public bool isBStatic;
    public EventVariableSO A;
    public EventVariableSO B;
    private bool IsBool => type == EnumManager.VariableType.Bool;
    private bool IsInt => type == EnumManager.VariableType.Int;
    private bool IsFloat => type == EnumManager.VariableType.Float;
    [NaughtyAttributes.ShowIf("IsBool")] public bool boolB;
    [NaughtyAttributes.ShowIf("IsInt")] public int intB;
    [NaughtyAttributes.ShowIf("IsFloat")] public float floatB;

    public EnumManager.CompareType comparator;
    public EnumManager.BoolCompareType boolComparator;
    
    public bool GetFlag(EventManager eventManager)
    {
        if (type == EnumManager.VariableType.Bool)
        {
            bool va = eventManager.boolEventVariables.GetSafe(A);
            bool vb = isBStatic ? boolB : eventManager.boolEventVariables.GetSafe(B);

            return boolComparator switch
            {
                EnumManager.BoolCompareType.And => va && vb,
                EnumManager.BoolCompareType.Or => va || vb,
                EnumManager.BoolCompareType.Xor => va ^ vb,
                EnumManager.BoolCompareType.Nand => !(va && vb),
                EnumManager.BoolCompareType.Nor => !(va || vb),
                EnumManager.BoolCompareType.Xnor => !(va ^ vb),
                _ => false,
            };
        }
        else if (type == EnumManager.VariableType.Int)
        {
            int va = eventManager.intEventVariables.GetSafe(A);
            int vb = isBStatic ? intB : eventManager.intEventVariables.GetSafe(B);

            return comparator switch
            {
                EnumManager.CompareType.SameAs => va == vb,
                EnumManager.CompareType.BiggerThan => va > vb,
                EnumManager.CompareType.BiggerOrSameAs => va >= vb,
                EnumManager.CompareType.SmallerThan => va < vb,
                EnumManager.CompareType.SmallerOrSameAs => va <= vb,
                EnumManager.CompareType.NotSame => va != vb,
                _ => false,
            };
        }
        else if (type == EnumManager.VariableType.Float)
        {
            float va = eventManager.floatEventVariables.GetSafe(A);
            float vb = isBStatic ? floatB : eventManager.floatEventVariables.GetSafe(B);

            return comparator switch
            {
                EnumManager.CompareType.SameAs => va == vb,
                EnumManager.CompareType.BiggerThan => va > vb,
                EnumManager.CompareType.BiggerOrSameAs => va >= vb,
                EnumManager.CompareType.SmallerThan => va < vb,
                EnumManager.CompareType.SmallerOrSameAs => va <= vb,
                EnumManager.CompareType.NotSame => va != vb,
                _ => false,
            };
        }
        else return false;
        
    }
}