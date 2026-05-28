using UnityEngine;
[CreateAssetMenu(fileName = "GetBuffEvent", menuName = "State Machine/Action/Event/GetBuffEvent")]
public class GetBuffEventAction : StateActionSO
{
    public override void Act(StateController stateController)
    {   
        if (stateController.TryGetInterface(out IGetBuff getBuff) && stateController.TryGetInterface(out IEvent @event))
        {
            foreach (Collider2D col in @event.CurrentTargets)
            {
                Transform transform = col.transform;
                getBuff.GetBuff(transform, getBuff.Buff);
            }
        }
        else Debug.LogError("ERROR: Interface Not Found!!!");
    }
}
