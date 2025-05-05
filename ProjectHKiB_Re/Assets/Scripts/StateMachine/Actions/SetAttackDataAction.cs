using UnityEngine;
[CreateAssetMenu(fileName = "SetAttackDataAction", menuName = "Scriptable Objects/State Machine/Action/SetAttackData", order = 3)]
public class SetAttackDataAction : StateActionSO
{
    [SerializeField] private int attackNumber;
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IAttackable attackable))
        {
            attackable.AttackController.SetAttackData(attackNumber);
        }
        else
            Debug.LogError("ERROR: Interface Not Found!!!");
    }
}
