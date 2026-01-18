using UnityEngine;
[CreateAssetMenu(fileName = "Apply Sprint Action", menuName = "State Machine/Action/Graffiti/StartGraffiti")]
public class StartGraffitiAction : StateActionSO
{
    public bool apply;
    public override void Act(StateController stateController)
    {
        GameManager.instance.graffitiManager.StartGraffiti(stateController.transform);

    }
}