using System;
using UnityEngine;
[CreateAssetMenu(fileName = "CombinedAction", menuName = "Scriptable Objects/State Machine/CombinedAction", order = 3)]
public class CombinedAction : StateActionSO
{
    [SerializeField] private StateActionSO[] actions;

    public override void Act(StateController stateController)
    {
        for (int i = 0; i < actions.Length; i++)
        {
            if (actions[i] != null) actions[i].Act(stateController);
        }
    }
}