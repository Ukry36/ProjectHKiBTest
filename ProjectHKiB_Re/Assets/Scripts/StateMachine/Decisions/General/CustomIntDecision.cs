using UnityEngine;
[CreateAssetMenu(fileName = "CustomIntDecision", menuName = "State Machine/Decision/General/CustomIntDecision")]
public class CustomIntDecision : StateDecisionSO
{
    [SerializeField] private string intName;
    [SerializeField] private bool compareWithCostomParameter;
    [SerializeField] [NaughtyAttributes.HideIf("compareWithCostomParameter")] private int value;
    [SerializeField] [NaughtyAttributes.ShowIf("compareWithCostomParameter")] private string parameterName;
    [SerializeField] private EnumManager.CompareType compareType;

    public override bool Decide(StateController stateController)
    {
        int origin = stateController.GetIntParameter(intName);
        int compare = compareWithCostomParameter ? stateController.GetIntParameter(parameterName) : value;
        switch(compareType)
        {
            case EnumManager.CompareType.SameAs: return origin == compare;
            case EnumManager.CompareType.BiggerThan: return origin > compare;
            case EnumManager.CompareType.BiggerOrSameAs: return origin >= compare;
            case EnumManager.CompareType.SmallerThan: return origin < compare;
            case EnumManager.CompareType.SmallerOrSameAs: return origin <= compare;
            case EnumManager.CompareType.NotSame: return origin != compare;
            default: return false;
        }

    }
}
