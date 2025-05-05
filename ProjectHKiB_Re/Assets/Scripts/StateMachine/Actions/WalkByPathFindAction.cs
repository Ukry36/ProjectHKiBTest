using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
[CreateAssetMenu(fileName = "WalkByPathFind", menuName = "Scriptable Objects/State Machine/Action/WalkByPathFindAction", order = 3)]
public class WalkByPathFindAction : StateActionSO
{
    [SerializeField] private MovementManagerSO movementManager;
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetComponent(out IMovable movable) && stateController.TryGetComponent(out Enemy enemy))
        {
            if (enemy.PathList != null && enemy.PathList.Count > 0)
            {
                Vector3 targetPos = enemy.PathList[0];
                //Debug.DrawLine(targetPos - Vector3.one * 0.5f, targetPos + Vector3.one * 0.5f);
                movementManager.WalkMove(stateController.transform, movable, targetPos - stateController.transform.position);
            }
            else
            {
                movementManager.WalkMove(stateController.transform, movable, enemy.CurrentTarget.position - stateController.transform.position);
            }
        }
        else
            Debug.LogError("ERROR: Interface Not Found!!!");

    }
}
