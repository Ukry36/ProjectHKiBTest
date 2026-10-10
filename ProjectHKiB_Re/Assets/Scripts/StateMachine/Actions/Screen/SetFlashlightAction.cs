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
        [Tooltip("켜면 손전등 방식·밝기·범위·각도도 함께 바꾼다.")]
        public bool changeShape;
        [Tooltip("Facing: 바라보는 방향 부채꼴 / Around: 주위 원형.")]
        public FlashlightMode flashlightMode = FlashlightMode.Facing;
        [Tooltip("손전등 밝기. 1이면 빛 안이 평소 밝기.")]
        [Range(0f, 1f)] public float brightness = 1f;
        [Tooltip("빛이 닿는 거리(월드 유닛). Around일 때는 원의 반지름.")]
        [Min(0f)] public float range = 6f;
        [Tooltip("부채꼴 전체 각도(도). Facing일 때만 쓴다.")]
        [Range(1f, 179f)] public float angle = 50f;
        [Tooltip("방식·밝기·범위·각도가 새 값으로 넘어가는 시간(초). 0이면 바로 바뀐다.")]
        [Min(0f)] public float shapeDuration = 0.5f;

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
            if (changeShape) manager.SetFlashlightShape(flashlightMode, brightness, range, angle, shapeDuration);
        }
    }
}
