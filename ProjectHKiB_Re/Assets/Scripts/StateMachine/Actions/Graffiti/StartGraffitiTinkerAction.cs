using UnityEngine;
[CreateAssetMenu(fileName = "StartGraffitiTinkerAction", menuName = "State Machine/Action/Graffiti/StartGraffitiTinker")]
public class StartGraffitiTinkerAction : StateActionSO
{
    public bool apply;
    public override void Act(StateController stateController)
    {
        GameManager.instance.graffitiManager.StartTinker();
    }
}