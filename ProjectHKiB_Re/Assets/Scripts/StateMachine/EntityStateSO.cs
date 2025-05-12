using UnityEngine;

[CreateAssetMenu(fileName = "State", menuName = "Scriptable Objects/State Machine/EntityState")]
public class EntityStateSO : StateSO
{
    [SerializeField] private string animationName;

    public override void EnterState(StateController stateController)
    {
        base.EnterState(stateController);
        if (stateController.TryGetInterface(out IEntityStateController controller))
            controller.PlayStateAnimation(animationName);
        else Debug.LogError("ERROR: Interface Not Found!!!");
    }
}