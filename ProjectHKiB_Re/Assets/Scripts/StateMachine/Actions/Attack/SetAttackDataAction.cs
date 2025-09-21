using UnityEngine;
[CreateAssetMenu(fileName = "SetAttackDataAction", menuName = "State Machine/Action/Attack/SetAttackData")]
public class SetAttackDataAction : StateActionSO
{
    [SerializeField] private int attackNumber;
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IAttackable attackable))
        {
            attackable.SetAttackData(attackNumber);
        }
        else
            Debug.LogError("ERROR: Interface Not Found!!!");
    }
}
