using System;
using DG.Tweening;
using DG.Tweening.Core.Easing;
using NaughtyAttributes;
using UnityEngine;

namespace Movement
{
    /// <summary>
    /// 경로의 상대 변위를 연속 이동 또는 즉시 이동으로 적용한다.
    /// 순간이동은 경로의 실행 시간을 소비하지 않는다.
    /// </summary>
    public enum MovementPathStepType { Move, Teleport }

    /// <summary>
    /// 이전 구간의 예정 도착점에 더할 월드 XY 변위를 보관한다.
    /// 특별한 구간만 개별 속도 비율과 Ease를 펼쳐 설정한다.
    /// </summary>
    [Serializable]
    public sealed class MovementPathStep
    {
        [Tooltip("Move는 시간을 사용해 이동하고 Teleport는 즉시 위치를 바꾼다.")]
        [SerializeField] private MovementPathStepType _type;

        [Tooltip("이전 구간의 예정 도착점에 더할 월드 XY 변위. 예: 왼쪽 20은 (-20, 0).")]
        [SerializeField] private Vector2 _displacement;

        [ShowIf(nameof(IsMove)), AllowNesting]
        [Tooltip("이 구간만 공통 Ease와 기본 속도 비율 1을 덮어쓴다.")]
        [SerializeField] private bool _overrideTiming;

        [ShowIf(nameof(UsesTimingOverride)), AllowNesting, MinValue(0.01f)]
        [Tooltip("다른 이동 구간에 대한 평균 속도 비율. 2면 같은 거리에서 절반의 시간 가중치를 갖는다. 경로 전체 시간은 유지된다.")]
        [SerializeField] private float _speedRatio = 1f;

        [ShowIf(nameof(UsesTimingOverride)), AllowNesting]
        [Tooltip("이 구간의 위치 진행률 곡선. 내부 전용/사용자 정의 Ease 값은 Linear로 처리한다.")]
        [SerializeField] private Ease _ease = Ease.Linear;

        private bool IsMove => _type == MovementPathStepType.Move;
        private bool UsesTimingOverride => IsMove && _overrideTiming;
        internal MovementPathStepType Type => _type;
        internal Vector2 Displacement => _displacement;
        internal float Weight => IsMove ? _displacement.magnitude / (UsesTimingOverride ? Mathf.Max(0.01f, _speedRatio) : 1f) : 0f;
        internal bool HasValidTiming => !UsesTimingOverride || (!float.IsNaN(_speedRatio) && !float.IsInfinity(_speedRatio) && _speedRatio > 0f);

        /// <summary>개별 설정이 켜진 구간만 자신의 Ease를 사용한다.</summary>
        internal Ease ResolveEase(Ease commonEase) => UsesTimingOverride ? _ease : commonEase;
    }

    /// <summary>
    /// Action 내부에 직렬화하는 경로 정의로 별도 SO 에셋을 만들지 않는다.
    /// 실행 시간은 1회 기준이며 실제 재생 상태는 엔티티별 재생기가 소유한다.
    /// </summary>
    [Serializable]
    public sealed class MovementPath
    {
        [MinValue(0.01f), AllowNesting]
        [Tooltip("경로를 한 번 실행하는 총 시간(초). 반복 전체 시간은 이 값 × 실행 횟수이며 순간이동은 시간을 소비하지 않는다.")]
        [SerializeField] private float _duration = 1f;

        [Tooltip("모든 이동 구간의 기본 위치 진행률 곡선. Linear는 일정한 속도다.")]
        [SerializeField] private Ease _ease = Ease.Linear;

        [MinValue(0), AllowNesting]
        [Tooltip("경로의 총 실행 횟수. 2는 두 번 실행하며 0은 취소할 때까지 반복한다. 변위는 반복마다 누적된다.")]
        [SerializeField] private int _repeatCount = 1;

        [Tooltip("켜면 기존 물리 이동과 충돌 처리를 사용한다. 끄면 재생 중 중력·외력·벽·엔티티 충돌 없이 경로를 따른다. 충돌 시 취소/이벤트는 별도 Action으로 처리한다.")]
        [SerializeField] private bool _usePhysics = true;

        [Tooltip("순서대로 실행할 상대 이동. 이동 거리 / 속도 비율에 비례해 총 시간을 배분한다.")]
        [SerializeField] private MovementPathStep[] _steps = Array.Empty<MovementPathStep>();

        internal bool HasSteps => _steps != null && _steps.Length > 0;

        /// <summary>
        /// 편집 데이터의 스냅샷을 만들어 여러 엔티티가 재생 진행도를 공유하지 않게 한다.
        /// 시간을 배분할 이동이 없거나 유효하지 않은 입력이면 실행을 거부한다.
        /// </summary>
        internal bool TryCreatePlayback(StateController owner, IPhysics physics, out MovementPathPlayback playback)
        {
            playback = null;
            if (!HasSteps || !IsFinite(_duration) || _duration <= 0f || _repeatCount < 0) return false;
            double totalWeight = 0d;
            foreach (MovementPathStep step in _steps)
            {
                if (step == null) continue;
                if (!IsFinite(step.Displacement.x) || !IsFinite(step.Displacement.y) ||
                    !IsFinite(step.Weight) || !step.HasValidTiming ||
                    (step.Type != MovementPathStepType.Move && step.Type != MovementPathStepType.Teleport)) return false;
                totalWeight += step.Weight;
            }
            if (totalWeight <= 0d) return false;

            var segments = new MovementPathPlayback.Segment[_steps.Length];
            for (int i = 0; i < _steps.Length; i++)
            {
                MovementPathStep step = _steps[i];
                if (step == null) continue;
                segments[i] = new MovementPathPlayback.Segment(step.Displacement,
                    step.Type == MovementPathStepType.Teleport,
                    _duration * (step.Weight / totalWeight), step.ResolveEase(_ease));
            }
            playback = new MovementPathPlayback(owner, physics, segments, _repeatCount, _usePhysics);
            return true;
        }

        /// <summary>NaNや無限大による座標破損と終了しない再生を防ぐ。</summary>
        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }

    /// <summary>
    /// PhysicsManagerの固定更新内で相対経路を進める、エンティティ専用の実行状態。
    /// 同じ固定更新内の移動完了→瞬間移動も順に処理し、最後の物理移動を失わない。
    /// </summary>
    public sealed class MovementPathPlayback
    {
        /// <summary>再生開始時に確定した一つの区間。定義側の編集に影響されない。</summary>
        internal readonly struct Segment
        {
            [Tooltip("区間の相対変位。")]
            internal readonly Vector2 Displacement;
            [Tooltip("時間を使わない瞬間移動か。")]
            internal readonly bool Teleport;
            [Tooltip("全体時間から割り当てた区間秒数。")]
            internal readonly double Duration;
            [Tooltip("区間の位置進行曲線。")]
            internal readonly Ease Ease;

            /// <summary>定義から解決した値を不変の区間として保存する。</summary>
            internal Segment(Vector2 displacement, bool teleport, double duration, Ease ease)
            {
                Displacement = displacement;
                Teleport = teleport;
                Duration = duration;
                Ease = ease;
            }
        }

        [Tooltip("再生を開始したStateController。")]
        private readonly StateController _owner;
        [Tooltip("位置と物理状態を操作する対象。")]
        private readonly IPhysics _physics;
        [Tooltip("再生する区間のスナップショット。")]
        private readonly Segment[] _segments;
        [Tooltip("総実行回数。0は無限。")]
        private readonly int _repeatCount;
        [Tooltip("現在の区間番号。")]
        private int _index;
        [Tooltip("完了済みの反復数。")]
        private int _completedRepeats;
        [Tooltip("現在の区間の経過秒数。")]
        private double _elapsed;
        [Tooltip("処理上限で次の固定更新に繰り越した時間。")]
        private double _pendingTime;
        [Tooltip("現在の区間の予定開始点。衝突でずれた実位置とは分離する。")]
        private Vector2 _segmentStart;
        [Tooltip("取消しまたは完了済みか。")]
        private bool _finished;
        [Tooltip("この再生で物理を使用するか。")]
        private readonly bool _usePhysics;

        public bool UsePhysics => _usePhysics;
        public bool IsFinished => _finished;

        /// <summary>開始点を一度だけ取得し、State離脱と無効化時の取消しを登録する。</summary>
        internal MovementPathPlayback(StateController owner, IPhysics physics, Segment[] segments, int repeatCount, bool usePhysics)
        {
            _owner = owner;
            _physics = physics;
            _segments = segments;
            _repeatCount = repeatCount;
            _usePhysics = usePhysics;
            _segmentStart = physics.HPosition;
            _owner.StateChanging += OnStateChanging;
            _owner.TimingStopped += Cancel;
        }

        /// <summary>
        /// 秒数を区間に配り、区間の境界と瞬間移動を飛ばさず順に実行する。
        /// 1更新1024区間までとし、極端な反復設定でもメインスレッドを占有しない。
        /// </summary>
        internal void Advance(PhysicsManager manager, float deltaTime)
        {
            if (_finished) return;
            if (_owner == null || !_owner.isActiveAndEnabled) { Cancel(); return; }
            double remaining = _pendingTime + deltaTime;
            _pendingTime = 0d;
            for (int processed = 0; processed < 1024 && !_finished; processed++)
            {
                Segment segment = _segments[_index];
                Vector2 end = _segmentStart + segment.Displacement;
                if (segment.Teleport)
                    manager.TeleportPathStep(_physics, end);
                else if (segment.Duration > 0d)
                {
                    if (remaining <= 0d) return;
                    double consumed = Math.Min(remaining, segment.Duration - _elapsed);
                    _elapsed += consumed;
                    remaining -= consumed;
                    float progress = (float)Math.Min(1d, _elapsed / segment.Duration);
                    float eased = EvaluateEase(segment.Ease, progress);
                    Vector2 target = Vector2.LerpUnclamped(_segmentStart, end, eased);
                    manager.MovePathStep(_physics, target, (float)consumed, _usePhysics);
                    if (_finished) return;
                    if (_elapsed < segment.Duration) return;
                }

                _segmentStart = end;
                _elapsed = 0d;
                _index++;
                if (_index < _segments.Length) continue;
                _index = 0;
                if (_repeatCount > 0 && ++_completedRepeats >= _repeatCount)
                {
                    // 고정 갱신 끝까지 참조를 남겨 마지막 프레임에도 비물리 충돌 제외를 유지한다.
                    _finished = true;
                    Detach();
                    return;
                }
            }
            _pendingTime = remaining;
        }

        /// <summary>표준 Ease를 평가한다. 양 끝점을 고정하고 내부 전용 Ease는 Linear로 처리한다.</summary>
        private static float EvaluateEase(Ease ease, float progress)
        {
            if (progress <= 0f) return 0f;
            if (progress >= 1f) return 1f;
            if ((int)ease < (int)Ease.Linear || (int)ease > (int)Ease.InOutBounce) return progress;
            return EaseManager.Evaluate(ease, null, progress, 1f, 1.70158f, 0f);
        }

        /// <summary>남은 모든 구간과 물리 위치 예약을 제거하고 수평 이동을 즉시 정지한다.</summary>
        internal void Cancel()
        {
            _finished = true;
            Detach();
            if (_physics.Phys.PathPlayback != this) return;
            _physics.Phys.PathPlayback = null;
            _physics.CancelMoveTowardByPhysics();
            _physics.StopMove();
        }

        /// <summary>같은 State 재진입도 이전 실행의 수명이 끝난 것으로 처리한다.</summary>
        private void OnStateChanging(StateSO previous, StateSO next) => Cancel();

        /// <summary>완료와 취소 양쪽에서 이벤트 참조를 해제해 실행 객체가 남지 않게 한다.</summary>
        private void Detach()
        {
            if (_owner == null) return;
            _owner.StateChanging -= OnStateChanging;
            _owner.TimingStopped -= Cancel;
        }
    }
}
