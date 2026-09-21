using System;
using System.Collections.Generic;
using Movement;
using UnityEngine;

namespace StateMachine
{
    /// <summary>Prefab을 생성하고 ControlledTargetModule의 슬롯에 등록한다.</summary>
    [AddTypeMenu("Target Control/Instantiate And Register")]
    [Serializable]
    public sealed class InstanciateObjectAction : StateAction
    {
        [Tooltip("생성한 StateController를 등록할 슬롯 이름.")]
        [SerializeField] private string targetID = "Default";
        [SerializeField] private GameObject prefab;
        [SerializeField] private PositionReference position;

        [Tooltip("OwnerAnimationDirection은 소유자의 4방향, TowardDestination은 소유자의 CurrentTarget 방향을 사용한다.")]
        [SerializeField] private Combat.CombatAttackDirectionSource directionSource;

        [Tooltip("생성한 GameObject를 이 Action을 실행한 StateController의 자식으로 둔다.")]
        [SerializeField] private bool parentToOwner;

        [Tooltip("같은 슬롯이 이미 있으면 새 Instance로 교체한다.")]
        [SerializeField] private bool replaceExisting = true;

        [NaughtyAttributes.ShowIf(nameof(replaceExisting))]
        [NaughtyAttributes.AllowNesting]
        [Tooltip("교체되는 대상도 이 모듈이 생성했던 Instance라면 함께 파괴한다.")]
        [SerializeField] private bool destroyReplacedOwnedTarget = true;

        public override void Act(StateController stateController)
        {
            if (!stateController.TryGetInterface(out ITargetControl targets))
            {
                Debug.LogError("ERROR: InstanciateObjectAction - IControlledTargetModule을 찾을 수 없습니다. " +
                               "실행 주체에 ControlledTargetModule을 추가하세요.", stateController);
                return;
            }

            if (prefab == null)
            {
                Debug.LogError("ERROR: InstanciateObjectAction - Prefab이 비어 있습니다.", stateController);
                return;
            }

            if (!position.TryResolve(stateController, out Vector3 spawnPosition))
            {
                Debug.LogError($"ERROR: InstanciateObjectAction - '{targetID}'의 생성 위치를 계산할 수 없습니다.", stateController);
                return;
            }

            Quaternion rotation = ResolveRotation(stateController, spawnPosition);
            targets.InstantiateAndRegister(
                targetID,
                prefab,
                spawnPosition,
                rotation,
                parentToOwner ? stateController.transform : null,
                replaceExisting,
                destroyReplacedOwnedTarget);
        }

        private Quaternion ResolveRotation(StateController owner, Vector3 spawnPosition)
        {
            EnumManager.AnimDir direction;
            switch (directionSource)
            {
                case Combat.CombatAttackDirectionSource.OwnerAnimationDirection:
                    if (owner.TryGetInterface(out IDirAnimatable animatable))
                        return animatable.AnimationDirection.DirToQuaternion4();
                    direction = EnumManager.AnimDir.D;
                    break;

                case Combat.CombatAttackDirectionSource.TowardDestination:
                    if (owner.TryGetInterface(out ITargetable targetable) && targetable.CurrentTarget != null)
                        direction = DirectionFromVector(targetable.CurrentTarget.position - spawnPosition);
                    else
                        direction = EnumManager.AnimDir.D;
                    break;

                case Combat.CombatAttackDirectionSource.MovementDirection:
                    if (owner.TryGetInterface(out IPhysics physics))
                    {
                        Vector2 movement = physics.HVelocity;
                        if (movement.sqrMagnitude <= 0.000001f) movement = physics.WalkingDir;
                        if (movement.sqrMagnitude <= 0.000001f) movement = physics.LastSetDir;
                        direction = DirectionFromVector(movement);
                    }
                    else
                        direction = EnumManager.AnimDir.D;
                    break;

                case Combat.CombatAttackDirectionSource.Left:
                    direction = EnumManager.AnimDir.L;
                    break;
                case Combat.CombatAttackDirectionSource.Right:
                    direction = EnumManager.AnimDir.R;
                    break;
                case Combat.CombatAttackDirectionSource.Up:
                    direction = EnumManager.AnimDir.U;
                    break;
                default:
                    direction = EnumManager.AnimDir.D;
                    break;
            }

            return direction.DirToQuaternion4();
        }

        private static EnumManager.AnimDir DirectionFromVector(Vector2 direction)
        {
            if (direction.sqrMagnitude <= 0.000001f) return EnumManager.AnimDir.D;
            if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
                return direction.x < 0f ? EnumManager.AnimDir.L : EnumManager.AnimDir.R;
            return direction.y < 0f ? EnumManager.AnimDir.D : EnumManager.AnimDir.U;
        }
    }

    /// <summary>등록된 다른 StateController를 대상으로 중첩 StateAction을 실행한다.</summary>
    [AddTypeMenu("Target Control/Manipulate Registered Target")]
    [Serializable]
    public sealed class ManipulateRegisteredTargetAction : StateAction
    {
        [SerializeField] private string targetID = "Default";
        [SerializeReference, SubclassSelector] private StateAction targetAction;

        public override void Act(StateController stateController)
        {
            if (targetAction == null) return;
            if (!stateController.TryGetInterface(out ITargetControl targets))
            {
                Debug.LogError("ERROR: ManipulateRegisteredTargetAction - IControlledTargetModule을 찾을 수 없습니다.", stateController);
                return;
            }

            if (!targets.TryGetTarget(targetID, out StateController target))
            {
                Debug.LogError($"ERROR: ManipulateRegisteredTargetAction - '{targetID}'에 등록된 StateController가 없습니다.", stateController);
                return;
            }

            targetAction.Act(target);
        }
    }

    /// <summary>슬롯을 해제하고, 모듈이 생성한 Instance라면 함께 파괴한다.</summary>
    [AddTypeMenu("Target Control/Destroy Registered Target")]
    [Serializable]
    public sealed class DestroyRegisteredTargetAction : StateAction
    {
        [SerializeField] private string targetID = "Default";

        [Tooltip("Scene에서 직접 등록한 대상도 파괴한다. 끄면 Scene 대상은 슬롯에서만 해제한다.")]
        [SerializeField] private bool destroyRegisteredSceneObject;

        public override void Act(StateController stateController)
        {
            if (stateController.TryGetInterface(out ITargetControl targets))
                targets.DestroyTarget(targetID, destroyRegisteredSceneObject);
        }
    }

    /// <summary>이 모듈이 생성한 모든 Target Instance를 파괴한다. Scene 등록 대상은 유지한다.</summary>
    [AddTypeMenu("Target Control/Destroy All Owned Targets")]
    [Serializable]
    public sealed class DestroyAllOwnedTargetsAction : StateAction
    {
        public override void Act(StateController stateController)
        {
            if (stateController.TryGetInterface(out ITargetControl targets))
                targets.DestroyAllOwnedTargets();
        }
    }

    [AddTypeMenu("General/Group Action")]
    [Serializable]
    public sealed class GroupAction : StateAction
    {
        [SerializeField] private string label;
        [SerializeReference, SubclassSelector]
        private StateAction[] actions =
            Array.Empty<StateAction>();

        public override void Act(StateController stateController)
        {
            if (actions == null) return;

            for (int i = 0; i < actions.Length; i++)
                actions[i]?.Act(stateController);
        }
    }

    public enum SequenceStateExitBehaviour
    {
        Kill,
        CompleteThenKill
    }

    /// <summary>
    /// 여러 StateAction을 항목별 지연시간 뒤에 Update 기반으로 순차 실행한다.
    /// 실행을 시작한 State의 수명에 맞춰 취소하거나 남은 항목을 완료한다.
    /// </summary>
    [AddTypeMenu("General/Sequence Action")]
    [Serializable]
    public sealed class SequenceAction : StateAction
    {
        [SerializeField] private string label;
        [Serializable]
        public struct ActionTween
        {
            [SerializeReference, SubclassSelector]
            public StateAction action;

            [Min(0f)]
            public float delay;
        }

        private sealed class RunningSequence
        {
            public StateSO StartingState;
            public StateController Owner;
            public int TotalLoops;
            public int CompletedLoops;
            public int ItemIndex;
            public float ElapsedTime;
            public bool HasPositiveDelay;
            public Action<StateSO, StateSO> StateChangingHandler;
            public Action<float> TimingUpdatedHandler;
            public Action TimingStoppedHandler;
        }

        [Tooltip("각 항목의 Delay만큼 기다린 뒤 Action을 실행한다. Action이 비어 있으면 대기 구간으로만 사용한다.")]
        [SerializeField] private ActionTween[] actions = Array.Empty<ActionTween>();

        [Tooltip("1은 한 번 실행, 2 이상은 지정 횟수만큼 반복, -1은 무한 반복. 0은 안전하게 1로 처리한다.")]
        [SerializeField] private int loops = 1;

        [Tooltip("이 Sequence를 시작한 State에서 벗어날 때 남은 Action을 버릴지, 즉시 모두 실행하고 종료할지 선택한다. 무한 반복은 항상 Kill한다.")]
        [SerializeField]
        private SequenceStateExitBehaviour stateExitBehaviour =
            SequenceStateExitBehaviour.Kill;

        [NonSerialized] private Dictionary<int, RunningSequence> _runningSequences;

        /// <summary>
        /// 이 Action을 실행한 컨트롤러에 새 Update 기반 Sequence를 등록한다.
        /// 같은 Action의 이전 실행이 남아 있다면 먼저 콜백 없이 중단한다.
        /// </summary>
        public override void Act(StateController stateController)
        {
            if (stateController == null) return;

            _runningSequences ??= new Dictionary<int, RunningSequence>();
            int ownerId = stateController.GetInstanceID();

            // 같은 SequenceAction이 Update나 반복 ActionSequence에서 다시 실행되어도
            // 이전 예약과 새 예약이 동시에 콜백을 호출하지 않게 한다.
            Stop(ownerId, false, stateController);

            if (actions == null || actions.Length == 0) return;

            bool hasTimeline = false;
            bool hasPositiveDelay = false;
            for (int i = 0; i < actions.Length; i++)
            {
                if (actions[i].action != null)
                    hasTimeline = true;

                if (actions[i].delay > 0f)
                {
                    hasTimeline = true;
                    hasPositiveDelay = true;
                }
            }

            if (!hasTimeline) return;

            int safeLoops = loops == -1 ? -1 : Mathf.Max(1, loops);
            RunningSequence running = new()
            {
                StartingState = stateController.CurrentState,
                Owner = stateController,
                TotalLoops = safeLoops,
                HasPositiveDelay = hasPositiveDelay
            };
            running.StateChangingHandler = (_, _) =>
            {
                bool complete = stateExitBehaviour == SequenceStateExitBehaviour.CompleteThenKill;
                Stop(ownerId, complete, stateController);
            };
            running.TimingUpdatedHandler = deltaTime =>
                UpdateRunningSequence(ownerId, running, deltaTime);
            running.TimingStoppedHandler = () => Stop(ownerId, false, stateController);

            _runningSequences[ownerId] = running;
            stateController.StateChanging += running.StateChangingHandler;
            stateController.TimingUpdated += running.TimingUpdatedHandler;
            stateController.TimingStopped += running.TimingStoppedHandler;
        }

        /// <summary>
        /// 한 컨트롤러의 Sequence를 스케일 시간만큼 진행하고 도달한 Action을 실행한다.
        /// Action이 State를 바꾸거나 Sequence를 재시작하면 현재 실행을 즉시 끝낸다.
        /// </summary>
        private void UpdateRunningSequence(int ownerId, RunningSequence running, float deltaTime)
        {
            StateController stateController = running.Owner;
            if (stateController == null || !stateController.isActiveAndEnabled)
            {
                Stop(ownerId, false, stateController);
                return;
            }

            if (stateController.CurrentState != running.StartingState)
            {
                bool complete = stateExitBehaviour == SequenceStateExitBehaviour.CompleteThenKill;
                Stop(ownerId, complete, stateController);
                return;
            }

            running.ElapsedTime += deltaTime;
            int processedCount = 0;

            while (processedCount < 1024)
            {
                ActionTween item = actions[running.ItemIndex];
                float delay = Mathf.Max(0f, item.delay);
                if (running.ElapsedTime < delay) return;

                running.ElapsedTime -= delay;
                running.ItemIndex++;
                processedCount++;
                item.action?.Act(stateController);

                if (!IsCurrent(ownerId, running)) return;

                if (running.ItemIndex < actions.Length) continue;

                running.CompletedLoops++;
                if (running.TotalLoops != -1 && running.CompletedLoops >= running.TotalLoops)
                {
                    RemoveIfCurrent(ownerId, running);
                    return;
                }

                running.ItemIndex = 0;
                if (running.TotalLoops == -1 && !running.HasPositiveDelay)
                    return;
            }

            Debug.LogWarning(
                "[SequenceAction] 한 프레임에 Action을 1024개 이상 처리하지 않도록 중단했습니다.",
                stateController);
        }

        /// <summary>
        /// 실행 중인 Sequence를 제거하고 필요하면 남은 유한 반복 Action을 즉시 완료한다.
        /// 무한 반복은 완료할 수 없으므로 항상 콜백 없이 중단한다.
        /// </summary>
        private void Stop(int ownerId, bool complete, StateController logContext)
        {
            if (_runningSequences == null ||
                !_runningSequences.TryGetValue(ownerId, out RunningSequence running))
                return;

            _runningSequences.Remove(ownerId);
            DetachHandlers(running);

            if (complete && running.TotalLoops == -1)
            {
                Debug.LogWarning(
                    "[SequenceAction] 무한 반복 Sequence는 Complete할 수 없어 State 이탈 시 Kill합니다.",
                    logContext);
                complete = false;
            }

            if (complete)
                CompleteRemaining(running);
        }

        /// <summary>
        /// 유한 Sequence의 현재 위치 이후에 남은 모든 Action을 지연 없이 실행한다.
        /// State 종료 시 CompleteThenKill 동작을 DOTween 없이 동일하게 제공한다.
        /// </summary>
        private void CompleteRemaining(RunningSequence running)
        {
            while (running.CompletedLoops < running.TotalLoops)
            {
                while (running.ItemIndex < actions.Length)
                {
                    StateAction action = actions[running.ItemIndex].action;
                    running.ItemIndex++;
                    action?.Act(running.Owner);
                }

                running.CompletedLoops++;
                running.ItemIndex = 0;
            }
        }

        /// <summary>
        /// 전달된 실행 정보가 해당 컨트롤러의 현재 Sequence인지 확인한다.
        /// Action 안에서 같은 SequenceAction을 다시 시작한 경우 이전 실행을 구분한다.
        /// </summary>
        private bool IsCurrent(int ownerId, RunningSequence running)
        {
            return _runningSequences != null &&
                   _runningSequences.TryGetValue(ownerId, out RunningSequence current) &&
                   ReferenceEquals(current, running);
        }

        /// <summary>
        /// 자연 완료한 Sequence가 여전히 현재 실행일 때 목록과 이벤트에서 제거한다.
        /// 이미 교체된 새 실행의 핸들러는 건드리지 않는다.
        /// </summary>
        private void RemoveIfCurrent(int ownerId, RunningSequence running)
        {
            if (_runningSequences != null &&
                _runningSequences.TryGetValue(ownerId, out RunningSequence current) &&
                ReferenceEquals(current, running))
            {
                _runningSequences.Remove(ownerId);
                DetachHandlers(running);
            }
        }

        /// <summary>
        /// StateController에 등록한 State 변경·Update·중단 핸들러를 모두 해제한다.
        /// 공유 ScriptableObject가 컨트롤러 참조를 계속 보유하지 않게 한다.
        /// </summary>
        private static void DetachHandlers(RunningSequence running)
        {
            if (running.Owner != null)
            {
                if (running.StateChangingHandler != null)
                    running.Owner.StateChanging -= running.StateChangingHandler;

                if (running.TimingUpdatedHandler != null)
                    running.Owner.TimingUpdated -= running.TimingUpdatedHandler;

                if (running.TimingStoppedHandler != null)
                    running.Owner.TimingStopped -= running.TimingStoppedHandler;
            }

            running.StateChangingHandler = null;
            running.TimingUpdatedHandler = null;
            running.TimingStoppedHandler = null;
        }
    }
}
