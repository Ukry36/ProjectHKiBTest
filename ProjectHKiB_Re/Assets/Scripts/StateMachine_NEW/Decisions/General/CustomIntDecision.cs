using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class CustomIntDecision : StateDecision
    {
        [SerializeField] private string intName;
        [SerializeField] private bool compareWithCostomParameter;
        [SerializeField][NaughtyAttributes.HideIf("compareWithCostomParameter")] private int value;
        [SerializeField][NaughtyAttributes.ShowIf("compareWithCostomParameter")] private string parameterName;
        [SerializeField] private EnumManager.CompareType compareType;

        public override bool Decide(StateController stateController)
        {
            int origin = stateController.GetIntParameter(intName);
            int compare = compareWithCostomParameter ? stateController.GetIntParameter(parameterName) : value;
            return compareType switch
            {
                EnumManager.CompareType.SameAs => origin == compare,
                EnumManager.CompareType.BiggerThan => origin > compare,
                EnumManager.CompareType.BiggerOrSameAs => origin >= compare,
                EnumManager.CompareType.SmallerThan => origin < compare,
                EnumManager.CompareType.SmallerOrSameAs => origin <= compare,
                EnumManager.CompareType.NotSame => origin != compare,
                _ => false,
            };
        }
    }
}