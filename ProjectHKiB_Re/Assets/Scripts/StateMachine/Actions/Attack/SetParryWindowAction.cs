using Combat;
using UnityEngine;

namespace StateMachine
{
    /// <summary>
    /// State 진입·이탈 시 ParryModule의 판정 창을 명시적으로 열거나 닫는다.
    /// 하나의 bool만 설정하여 패링 State 제작 시 필요한 항목을 최소화한다.
    /// </summary>
    [System.Serializable]
    [AddTypeMenu("Combat/Set Parry Window")]
    public sealed class SetParryWindowAction : StateAction
    {
        [Tooltip("켜면 패링 판정을 시작하고 끄면 즉시 종료한다.")]
        [SerializeField]
        private bool _active = true;

        /// <summary>
        /// 실행 주체의 IParryable을 찾아 설정된 패링 창 상태를 적용한다.
        /// Module이 없는 엔티티에서는 설정 오류를 한 번 기록한다.
        /// </summary>
        public override void Act(StateController stateController)
        {
            if (stateController != null && stateController.TryGetInterface(out IParryable parryable))
                parryable.SetParryActive(_active);
            else
                Debug.LogError("[SetParryWindowAction] ParryModule이 없습니다.", stateController);
        }
    }
}
