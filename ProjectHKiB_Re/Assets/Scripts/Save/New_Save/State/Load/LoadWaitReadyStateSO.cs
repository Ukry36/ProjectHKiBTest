using UnityEngine;

[CreateAssetMenu(fileName = "LoadWaitReadyState", menuName = "Scriptable Objects/Save/States/LoadWaitReadyState", order = 3)]
public class LoadWaitReadyStateSO : SaveBaseStateSO
{
    public override void OnEnter(SaveModule module)
    {
        module.StartCoroutine(module.WaitGearManagerReady());
    }

    public override void OnUpdate(SaveModule module)
    {
        // Transition은 StateMachine Decision에서 module.IsGearManagerReady 보고 다음으로 넘기면 제일 깔끔
        // 여기서는 아무것도 안 함
    }

    public override void OnExit(SaveModule module) { }
}