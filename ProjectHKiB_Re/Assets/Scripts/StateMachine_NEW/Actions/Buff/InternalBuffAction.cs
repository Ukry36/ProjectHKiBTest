using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class InternalBuffAction : StateAction
    {
        [SerializeField] private bool _negate;
        [SerializeField] private StatBuffSO _buff;
        public override void Act(StateController stateController)
        {
            if (stateController.TryGetInterface(out IBuffable buffable))
            {
                if (_negate)
                    buffable.UnBuff(_buff);
                else
                    buffable.Buff(_buff);
            }
        }
    }
}