using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Walk Action", menuName = "Scriptable Objects/State Machine/Action/Walk", order = 3)]
public class WalkAction : StateActionSO
{
    public MovementManager movementManager;
    public override void Act(StateController stateController)
    {
        //movementManager.WalkMove();
    }
}
