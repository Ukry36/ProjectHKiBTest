using System;
using System.Collections.Generic;
using DG.Tweening;
using EntityControl;
using Movement;
using UnityEngine;
using UnityEngine.Serialization;

namespace StateMachine
{
    /// <summary>
    /// 위치 기반 이동을 어떤 이동 모듈에 위임할지 정한다.
    /// </summary>
    public enum PositionMovementMode
    {
        InterpolatedPhysicsStep,
        InstantTeleport,
        Navigation
    }

    /// <summary>
    /// 같은 StateController에 예약된 위치 이동 Tween은 하나만 유지한다.
    /// 서로 다른 Move To Position Action이 동시에 PhysicsManager의 목적지를 덮어쓰지 않게 한다.
    /// </summary>
    internal static class PositionMovementTweenRegistry
    {
        private static readonly Dictionary<int, Tween> ActiveTweens = new();

        public static void Play(StateController owner, Tween tween)
        {
            if (owner == null || tween == null) return;

            Cancel(owner);

            int ownerId = owner.GetInstanceID();
            StateSO startingState = owner.CurrentState;
            ActiveTweens[ownerId] = tween;

            // 예전 UpdateAction 방식은 State를 벗어나 호출이 끊기면 이동도 중단됐다.
            // 한 번 실행하는 Tween 방식에서도 같은 수명 범위를 유지한다.
            tween.OnUpdate(() =>
            {
                if (owner != null && owner.isActiveAndEnabled && owner.CurrentState == startingState)
                    return;

                Cancel(ownerId, owner);
            });

            tween.OnKill(() =>
            {
                if (ActiveTweens.TryGetValue(ownerId, out Tween current) && current == tween)
                    ActiveTweens.Remove(ownerId);
            });

            tween.Play();
        }

        public static void Cancel(StateController owner)
        {
            if (owner == null) return;
            Cancel(owner.GetInstanceID(), owner);
        }

        private static void Cancel(int ownerId, StateController owner)
        {
            if (ActiveTweens.TryGetValue(ownerId, out Tween activeTween))
            {
                ActiveTweens.Remove(ownerId);
                if (activeTween != null && activeTween.IsActive())
                    activeTween.Kill(false);
            }

            if (owner != null && owner.TryGetInterface(out IPhysics physics))
                physics.CancelMoveTowardByPhysics();
        }
    }

    internal static class PositionMovementExecutor
    {
        /// <summary>
        /// DOTween이 구간 시간을 진행하는 동안 목적지와 남은 시간을 PhysicsManager에 계속 전달한다.
        /// 실제 위치 변경과 충돌 처리는 PhysicsManager의 FixedUpdate가 담당한다.
        /// </summary>
        public static bool TryCreateInterpolatedTween(
            StateController owner,
            PositionReference destination,
            float duration,
            out Tween tween)
        {
            tween = null;
            if (!owner.TryGetInterface(out IPhysics physics)) return false;

            RuntimePositionReference runtimeDestination = new(destination, owner);
            if (!runtimeDestination.TryGetPosition(out _)) return false;

            float safeDuration = Mathf.Max(0.01f, duration);
            float arrivalTime = 0f;
            float previousProgress = -1f;
            bool timingInitialized = false;

            tween = DOVirtual.Float(0f, 1f, safeDuration, progress =>
                {
                    if (owner == null) return;

                    // Sequence가 반복되면 자식 Tween의 progress가 1에서 다시 0으로 돌아온다.
                    // 각 반복마다 새 절대 도착 시각을 계산해야 이전 루프의 시간이 재사용되지 않는다.
                    if (!timingInitialized || progress + 0.000001f < previousProgress)
                    {
                        arrivalTime = Time.time + safeDuration * Mathf.Max(0f, 1f - progress);
                        timingInitialized = true;
                    }

                    previousProgress = progress;
                    if (runtimeDestination.TryGetPosition(out Vector3 targetPosition))
                        physics.MoveTowardByPhysics(targetPosition, arrivalTime);
                })
                .SetEase(Ease.Linear);

            return true;
        }

        public static bool TryCreateNavigationTween(
            StateController owner,
            PositionReference destination,
            float duration,
            bool forceRepath,
            out Tween tween)
        {
            tween = null;
            if (!owner.TryGetInterface(out INavigationAgent navigation)) return false;

            RuntimePositionReference runtimeDestination = new(destination, owner);
            if (!runtimeDestination.TryGetPosition(out _)) return false;

            float safeDuration = Mathf.Max(0.01f, duration);
            float previousProgress = -1f;
            bool destinationIssued = false;

            tween = DOVirtual.Float(0f, 1f, safeDuration, progress =>
                {
                    if (owner == null) return;

                    if (progress + 0.000001f < previousProgress)
                        destinationIssued = false;

                    previousProgress = progress;
                    if (!runtimeDestination.TryGetPosition(out Vector3 targetPosition)) return;

                    navigation.SetDestination(targetPosition, forceRepath && !destinationIssued);
                    destinationIssued = true;
                })
                .SetEase(Ease.Linear);

            return true;
        }

        public static bool Teleport(
            StateController owner,
            PositionReference destination,
            bool stopHorizontalMovement)
        {
            if (!owner.TryGetInterface(out IPhysics physics) ||
                !destination.TryResolve(owner, out Vector3 targetPosition))
                return false;

            // 직전 보간 이동의 마지막 요청이 다음 FixedUpdate에서 순간이동을 되돌리지 않게 한다.
            physics.CancelMoveTowardByPhysics();
            if (stopHorizontalMovement)
                physics.StopMove();

            physics.RealTeleport(targetPosition);
            return true;
        }
    }

    /// <summary>
    /// 복합 경로의 한 구간. InterpolatedPhysicsStep은 duration 동안 다음 위치로 이동하고,
    /// Navigation은 duration 동안 해당 목적지를 추적하며, InstantTeleport는 즉시 다음 구간으로 넘어간다.
    /// </summary>
    [Serializable]
    public sealed class PositionMovementKeyframe
    {
        [SerializeField] private PositionMovementMode mode;
        [SerializeField] private PositionReference destination;

        [NaughtyAttributes.ShowIf(nameof(UsesDuration))]
        [NaughtyAttributes.AllowNesting]
        [NaughtyAttributes.MinValue(0.01f)]
        [Tooltip("이 키프레임 구간에 사용할 시간(초).")]
        [SerializeField] private float duration = 1f;

        [NaughtyAttributes.ShowIf(nameof(mode), PositionMovementMode.Navigation)]
        [NaughtyAttributes.AllowNesting]
        [SerializeField] private bool forceRepath;

        [NaughtyAttributes.ShowIf(nameof(mode), PositionMovementMode.InstantTeleport)]
        [NaughtyAttributes.AllowNesting]
        [SerializeField] private bool stopHorizontalMovementBeforeTeleport = true;

        internal PositionMovementMode Mode => mode;
        internal PositionReference Destination => destination;
        internal float Duration => duration;
        internal bool ForceRepath => forceRepath;
        internal bool StopHorizontalMovementBeforeTeleport => stopHorizontalMovementBeforeTeleport;

        private bool UsesDuration => mode != PositionMovementMode.InstantTeleport;
    }

    /// <summary>
    /// 한 번 실행하면 DOTween이 duration 동안 PhysicsManager의 실제 이동을 구동한다.
    /// EnterActions나 ActionSequence에 배치하며, UpdateActions에서 매 프레임 호출할 필요가 없다.
    /// </summary>
    [AddTypeMenu("Movement/Move To Position")]
    [Serializable]
    public sealed class MoveToPositionAction : StateAction
    {
        [Tooltip("이동을 처리할 모듈과 이동 방식을 선택한다.")]
        [SerializeField] private PositionMovementMode mode;

        [Tooltip("Self, Player, CurrentTarget, World 또는 SceneAnchor 기반 목적지.")]
        [SerializeField] private PositionReference destination;

        [NaughtyAttributes.ShowIf(nameof(mode), PositionMovementMode.InterpolatedPhysicsStep)]
        [NaughtyAttributes.AllowNesting]
        [NaughtyAttributes.MinValue(0.01f)]
        [Tooltip("현재 위치에서 목적지까지 이동하는 데 사용할 시간(초).")]
        [FormerlySerializedAs("speed")]
        [SerializeField] private float duration = 1f;

        [NaughtyAttributes.ShowIf(nameof(mode), PositionMovementMode.Navigation)]
        [NaughtyAttributes.AllowNesting]
        [Tooltip("진행 중인 경로 요청을 무효화하고 즉시 새 경로를 요청한다.")]
        [SerializeField] private bool forceRepath;

        [NaughtyAttributes.ShowIf(nameof(mode), PositionMovementMode.InstantTeleport)]
        [NaughtyAttributes.AllowNesting]
        [Tooltip("순간이동 전에 걷기 상태와 수평 속도를 정지한다.")]
        [SerializeField] private bool stopHorizontalMovementBeforeTeleport = true;

        public override void Act(StateController stateController)
        {
            switch (mode)
            {
                case PositionMovementMode.Navigation:
                    PositionMovementTweenRegistry.Cancel(stateController);
                    Navigate(stateController);
                    break;

                case PositionMovementMode.InstantTeleport:
                    PositionMovementTweenRegistry.Cancel(stateController);
                    PositionMovementExecutor.Teleport(
                        stateController,
                        destination,
                        stopHorizontalMovementBeforeTeleport);
                    break;

                default:
                    MoveInterpolated(stateController);
                    break;
            }
        }

        private void MoveInterpolated(StateController stateController)
        {
            if (!PositionMovementExecutor.TryCreateInterpolatedTween(
                    stateController,
                    destination,
                    duration,
                    out Tween tween))
            {
                Debug.LogError(
                    "ERROR: MoveToPositionAction - IPhysics 또는 이동 목적지를 찾을 수 없습니다.",
                    stateController);
                return;
            }

            PositionMovementTweenRegistry.Play(stateController, tween);
        }

        private void Navigate(StateController stateController)
        {
            if (destination.TryResolve(stateController, out Vector3 targetPosition) &&
                stateController.TryGetInterface(out INavigationAgent navigation))
                navigation.SetDestination(targetPosition, forceRepath);
        }
    }

    /// <summary>
    /// 인라인 상대 경로를 PhysicsManager의 고정 갱신으로 실행한다.
    /// EnterActions나 ActionSequence에서 한 번 호출하며, 기존 키프레임 데이터는 유지한다.
    /// </summary>
    [AddTypeMenu("Movement/Move Along Position Keyframes")]
    [Serializable]
    public sealed class MoveAlongPositionKeyframesAction : StateAction
    {
        [NaughtyAttributes.AllowNesting]
        [Tooltip("SO 없이 이 Action 안에 저장하는 시간 기반 상대 경로.")]
        [SerializeField] private MovementPath _path = new();

        /// <summary>
        /// 정의를 검증한 뒤 기존 예약을 취소하고 대상만의 경로 재생기를 등록한다.
        /// 잘못된 경로 입력은 기존 실행을 덮어쓰지 않는다.
        /// </summary>
        public override void Act(StateController stateController)
        {
            if (stateController == null) return;
            if (_path == null || !_path.HasSteps) return;
            if (!stateController.TryGetInterface(out IPhysics physics))
            {
                Debug.LogError("[MoveAlongPositionKeyframesAction] IPhysics를 찾을 수 없습니다.", stateController);
                return;
            }
            if (!_path.TryCreatePlayback(stateController, physics, out MovementPathPlayback playback))
            {
                Debug.LogError("[MoveAlongPositionKeyframesAction] 양수인 실행 시간과 거리가 있는 Move 구간이 필요합니다. 변위와 속도 비율도 확인하세요.", stateController);
                return;
            }

            PositionMovementTweenRegistry.Cancel(stateController);
            physics.StopMove();
            physics.Phys.PathPlayback = playback;
            if (!playback.UsePhysics) physics.ZVelocity = 0f;
        }
    }

    /// <summary>
    /// 상대 경로와 위치 Tween의 남은 이동·순간이동·반복 예약을 취소한다.
    /// 충돌 이벤트나 상태 전이에서 호출하며 충돌 종류의 판단은 이 Action 밖에서 수행한다.
    /// </summary>
    [AddTypeMenu("Movement/Cancel Position Movement")]
    [Serializable]
    public sealed class CancelPositionMovementAction : StateAction
    {
        /// <summary>현재 위치에서 예약을 제거하고 수평 보행까지 멈춰 재가속을 방지한다.</summary>
        public override void Act(StateController stateController)
        {
            PositionMovementTweenRegistry.Cancel(stateController);
            if (stateController != null && stateController.TryGetInterface(out IPhysics physics))
                physics.StopMove();
        }
    }
}
