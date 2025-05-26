using UnityEngine;
[CreateAssetMenu(fileName = "SetAttackDataAction", menuName = "Scriptable Objects/State Machine/Action/Attack/SetAttackData")]
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
