using System;
using UnityEngine;

namespace StateMachine
{
    /// <summary>
    /// 현재 State 처리가 끝난 뒤 지정한 서브 StateMachine의 실행을 예약한다.
    /// 서브 종료 시 부모의 Return State에 새로 진입하여 계층 호출을 마무리한다.
    /// </summary>
    [AddTypeMenu("State Machine/Start Sub State Machine")]
    [Serializable]
    public sealed class StartSubStateMachineAction : StateAction
    {
        [Tooltip("실행할 페이즈 등의 서브 StateMachineSO.")]
        [SerializeField, NaughtyAttributes.Expandable]
        private StateMachineSO _subStateMachine;

        [Tooltip("비워두면 서브 StateMachine의 Initial State에서 시작한다.")]
        [SerializeField, NaughtyAttributes.Expandable]
        private StateSO _startState;

        [Tooltip("서브 StateMachine이 완료 또는 취소된 뒤 부모 StateMachine에서 진입할 State.")]
        [SerializeField, NaughtyAttributes.Expandable]
        private StateSO _returnState;

        public StateMachineSO SubStateMachine => _subStateMachine;
        public StateSO StartState => _startState;
        public StateSO ReturnState => _returnState;

        /// <summary>
        /// Unity의 SerializeReference 타입 선택기가 빈 Action을 생성할 때 사용하는 생성자다.
        /// 필드 값은 인스펙터에서 지정한다.
        /// </summary>
        public StartSubStateMachineAction() { }

        /// <summary>
        /// 에디터 자동 생성에서 서브 StateMachine 호출 정보를 한 번에 설정한다.
        /// Unity 직렬화용 기본 생성은 매개변수 없는 생성자를 계속 사용할 수 있다.
        /// </summary>
        public StartSubStateMachineAction(
            StateMachineSO subStateMachine,
            StateSO startState,
            StateSO returnState)
        {
            _subStateMachine = subStateMachine;
            _startState = startState;
            _returnState = returnState;
        }

        /// <summary>
        /// StateController에 서브 StateMachine 시작을 예약한다.
        /// 실제 교체는 다음 Update 시작 시 처리되어 현재 State 실행과 충돌하지 않는다.
        /// </summary>
        public override void Act(StateController stateController)
        {
            if (stateController == null) return;
            stateController.RequestStartSubStateMachine(_subStateMachine, _startState, _returnState);
        }
    }

    /// <summary>
    /// 현재 서브 StateMachine을 정상 완료 결과로 종료하도록 예약한다.
    /// 부모는 Start Action에 지정된 Return State에서 실행을 계속한다.
    /// </summary>
    [AddTypeMenu("State Machine/Complete Sub State Machine")]
    [Serializable]
    public sealed class CompleteSubStateMachineAction : StateAction
    {
        /// <summary>
        /// 현재 서브 StateMachine의 정상 완료를 StateController에 예약한다.
        /// 서브 계층이 아니면 StateController가 오류를 기록하고 요청을 무시한다.
        /// </summary>
        public override void Act(StateController stateController)
        {
            stateController?.RequestFinishSubStateMachine(SubStateMachineResult.Completed);
        }
    }

    /// <summary>
    /// 현재 서브 StateMachine을 취소 결과로 종료하도록 예약한다.
    /// 실패나 중단 흐름을 부모 Return State에서 별도로 분기할 때 사용한다.
    /// </summary>
    [AddTypeMenu("State Machine/Cancel Sub State Machine")]
    [Serializable]
    public sealed class CancelSubStateMachineAction : StateAction
    {
        /// <summary>
        /// 현재 서브 StateMachine의 취소를 StateController에 예약한다.
        /// 부모에서는 SubStateMachineResultDecision으로 취소 여부를 확인할 수 있다.
        /// </summary>
        public override void Act(StateController stateController)
        {
            stateController?.RequestFinishSubStateMachine(SubStateMachineResult.Cancelled);
        }
    }
}
