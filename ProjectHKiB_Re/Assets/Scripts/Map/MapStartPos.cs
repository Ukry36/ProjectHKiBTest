using UnityEngine;

public class MapStartPos : MonoBehaviour
{
    [NaughtyAttributes.Scene] public string fromScene;
    [SerializeField] private EnumManager.AnimDir _endDir;
    [SerializeField] private GameEvent endEvent;
    [SerializeField] private PhysicsManager physicsManager;

    public void SetPlayerToStartPos()
    {
        physicsManager.RealTeleport(GameManager.instance.player.GetInterface<IPhysics>(), transform.position);
        GameManager.instance.player.GetInterface<IDirAnimatable>().SetAnimationDirection(_endDir);
        if (endEvent != null) endEvent.TriggerEvent();
    }
}