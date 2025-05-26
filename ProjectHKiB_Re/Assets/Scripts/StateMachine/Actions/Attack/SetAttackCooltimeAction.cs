using UnityEngine;
[CreateAssetMenu(fileName = "SetAttackCooltimeAction", menuName = "Scriptable Objects/State Machine/Action/Attack/SetAttackCooltime")]
public class SetAttackCooltimeAction : StateActionSO
{
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IAttackable attackable))
        {
            attackable.AttackController.StartCoroutine(attackable.AttackController.AttackCooltimeCoroutine());
        }
        else
            Debug.LogError("ERROR: Interface Not Found!!!");
    }
}
