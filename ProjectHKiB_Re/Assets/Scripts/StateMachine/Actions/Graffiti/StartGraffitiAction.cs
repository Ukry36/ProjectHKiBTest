using UnityEngine;
[CreateAssetMenu(fileName = "StartGraffitiAction", menuName = "State Machine/Action/Graffiti/StartGraffiti")]
public class StartGraffitiAction : StateActionSO
{
    public bool apply;
    public override void Act(StateController stateController)
    {
        if(stateController.TryGetInterface(out IGraffitiable graffitiable))
        {
            GameManager.instance.graffitiManager.StartGraffiti(graffitiable.GraffitiTinkerOffset + (Vector2)stateController.transform.position);
        }
    }
}