using UnityEngine;
[CreateAssetMenu(fileName = "SetAttackDataAction", menuName = "State Machine/Action/Attack/SetAttackData")]
public class SetAttackDataAction : StateActionSO
{
    [SerializeField] private int attackNumber;
    public override void Act(StateController stateController)
    {
        Debug.Log("[SetAttackDataAction] Act() called");
        if (stateController.TryGetInterface(out IAttackable attackable))
        {
            Debug.Log("[SetAttackDataAction] IAttackable found");
            attackable.SetAttackData(attackNumber);
        }
        else
            Debug.LogError("ERROR: SetAttackDataAction - Interface Not Found!!!");
    }
}
