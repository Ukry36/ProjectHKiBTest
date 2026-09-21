using System;
using UnityEngine;

namespace StateMachine
{
    /// <summary>
    /// 현재 컨트롤러가 서브 StateMachine 계층 안에서 실행 중인지 검사한다.
    /// 대상을 지정하면 현재 실행 중인 서브 StateMachine이 정확히 일치하는지도 확인한다.
    /// </summary>
    [AddTypeMenu("State Machine/Is Sub State Machine Running")]
    [Serializable]
    public sealed class IsSubStateMachineRunningDecision : StateDecision
    {
        [Tooltip("검사할 서브 StateMachineSO. 비워두면 종류와 관계없이 서브 실행 여부만 검사한다.")]
        [SerializeField, NaughtyAttributes.Expandable]
        private StateMachineSO _subStateMachine;

        /// <summary>
        /// 현재 StateMachine 계층 깊이와 선택적 대상 일치를 검사한다.
        /// 메인 StateMachine에서는 false, 실행 중인 서브 안에서는 true가 된다.
        /// </summary>
        public override bool Decide(StateController stateController)
        {
            return stateController != null &&
                   stateController.IsRunningSubStateMachine(_subStateMachine);
        }
    }

    /// <summary>
    /// 부모로 가장 최근 반환된 서브 StateMachine의 종료 결과를 검사한다.
    /// 정상 완료와 취소 흐름을 Return State의 전이에서 구분할 때 사용한다.
    /// </summary>
    [AddTypeMenu("State Machine/Sub State Machine Result")]
    [Serializable]
    public sealed class SubStateMachineResultDecision : StateDecision
    {
        [Tooltip("검사할 서브 StateMachineSO. 비워두면 가장 최근 종료된 모든 서브를 허용한다.")]
        [SerializeField, NaughtyAttributes.Expandable]
        private StateMachineSO _subStateMachine;

        [Tooltip("전이가 요구하는 서브 StateMachine 종료 결과.")]
        [SerializeField]
        private SubStateMachineResult _expectedResult = SubStateMachineResult.Completed;

        public StateMachineSO SubStateMachine => _subStateMachine;
        public SubStateMachineResult ExpectedResult => _expectedResult;

        /// <summary>
        /// StateController에 기록된 마지막 서브 StateMachine과 종료 결과를 비교한다.
        /// 새 서브 StateMachine이 시작되면 이전 완료 기록은 자동으로 초기화된다.
        /// </summary>
        public override bool Decide(StateController stateController)
        {
            return stateController != null &&
                   stateController.HasFinishedSubStateMachine(_subStateMachine, _expectedResult);
        }
    }
}
