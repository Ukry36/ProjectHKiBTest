using UnityEngine;
[CreateAssetMenu(fileName = "InternalBuff", menuName = "Scriptable Objects/State Machine/Action/Buff/InternalBuff")]
public class InternalBuffAction : StateActionSO
{
    [SerializeField] private bool _negate;
    [SerializeField] private StatBuffSO _buff;
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IBuffable buffable))
        {
            if (_negate)
                _buff.RemoveBuff(buffable.StatBuffController);
            else
                _buff.ApplyBuff(buffable.StatBuffController);
        }
    }
}