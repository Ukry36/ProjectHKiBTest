using UnityEngine;

public class MapStartPos : MonoBehaviour
{
    [NaughtyAttributes.Scene] public string fromScene;
    [SerializeField] private EnumManager.AnimDir _endDir;
    [SerializeField] private GameEvent endEvent;

    public void SetPlayerToStartPos()
    {
        GameManager.instance.player.transform.position = this.transform.position;
        GameManager.instance.player.GetInterface<IMovable>().MovePoint.transform.position = this.transform.position;
        GameManager.instance.player.GetInterface<IDirAnimatable>().SetAnimationDirection(_endDir);
        if (endEvent != null) endEvent.TriggerEvent();
    }
}