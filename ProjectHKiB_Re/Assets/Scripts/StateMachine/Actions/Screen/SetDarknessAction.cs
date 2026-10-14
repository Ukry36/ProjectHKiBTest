using UnityEngine;
namespace StateMachine
{
    // 맵 위에 암전 필터를 덮거나 걷는다(고전 쯔꾸르식 — 플레이어 주변과 손전등 방향만 보인다).
    // 맵에 늘 걸려 있어야 하는 어둠은 이 액션 대신 맵 씬의 MapDarkness로 둔다.
    [System.Serializable]
    public class SetDarknessAction : StateAction
    {
        [Tooltip("켜면 아래 모양을 무시하고 암전을 걷는다.")]
        public bool clear;
        [Tooltip("걸 암전 모양(짙기·플레이어 주변 빛·손전등·그림자). 항목마다 마우스를 올리면 설명이 나온다.")]
        public DarknessSettings settings = new();
        [Tooltip("지금 모양에서 이 모양으로(또는 걷을 때 밝기로) 넘어가는 시간(초). 0이면 바로 바뀐다.")]
        [Min(0f)] public float duration = 1f;

        public override void Act(StateController stateController)
        {
            Play();
        }

        public void Play()
        {
            if (clear) DarknessManager.Instance.Clear(duration);
            else DarknessManager.Instance.Apply(settings, duration);
        }
    }
}
