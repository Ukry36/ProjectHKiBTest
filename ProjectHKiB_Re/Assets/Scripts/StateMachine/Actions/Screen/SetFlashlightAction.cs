using UnityEngine;
namespace StateMachine
{
    // 손전등을 켜고 끈다. 암전이 걸려 있지 않으면 화면에 보이는 변화는 없다.
    [System.Serializable]
    public class SetFlashlightAction : StateAction
    {
        public enum Mode { On, Off, Toggle }

        [Tooltip("On: 켠다 / Off: 끈다 / Toggle: 켜져 있으면 끄고 꺼져 있으면 켠다.")]
        public Mode mode = Mode.On;

        public override void Act(StateController stateController)
        {
            Play();
        }

        public void Play()
        {
            DarknessManager manager = DarknessManager.Instance;
            switch (mode)
            {
                case Mode.On: manager.SetFlashlight(true); break;
                case Mode.Off: manager.SetFlashlight(false); break;
                case Mode.Toggle: manager.ToggleFlashlight(); break;
            }
        }
    }
}
