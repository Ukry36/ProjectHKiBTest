using System;
using UnityEngine;

namespace StateMachine
{
    /// <summary>
    /// 그리드 관계 검사에서 사용할 대상 위치의 출처를 정의한다.
    /// 보스뿐 아니라 모든 StateController가 같은 방식으로 사용할 수 있다.
    /// </summary>
    public enum GridTargetSource
    {
        Self,
        Player,
        CurrentTarget,
        RegisteredTarget,
        SceneAnchor,
        World
    }

    /// <summary>
    /// Scene 오브젝트 직접 참조 없이 그리드 판정 대상의 현재 위치를 선택한다.
    /// 선택한 출처의 월드 위치에 공통 XY 오프셋을 더해 반환한다.
    /// </summary>
    [Serializable]
    public struct GridTargetReference
    {
        [Tooltip("그리드 위치를 읽을 대상의 종류.")]
        [SerializeField]
        private GridTargetSource _source;

        [Tooltip("Registered Target일 때 TargetControlModule에서 찾을 슬롯 이름.")]
        [SerializeField, NaughtyAttributes.ShowIf(nameof(_source), GridTargetSource.RegisteredTarget)]
        private string _registeredTargetSlot;

        [Tooltip("Scene Anchor일 때 PositionSceneAnchor에서 찾을 고유 ID.")]
        [SerializeField, NaughtyAttributes.ShowIf(nameof(_source), GridTargetSource.SceneAnchor)]
        private string _sceneAnchorId;

        [Tooltip("World 출처일 때 사용할 고정 월드 위치.")]
        [SerializeField, NaughtyAttributes.ShowIf(nameof(_source), GridTargetSource.World)]
        private Vector2 _worldPosition;

        [Tooltip("선택한 대상 위치에 더할 월드 XY 오프셋.")]
        [SerializeField]
        private Vector2 _offset;

        /// <summary>
        /// 선택한 대상 출처를 StateController 문맥에서 찾아 최종 월드 위치를 반환한다.
        /// 필요한 모듈이나 대상이 없으면 false를 반환한다.
        /// </summary>
        public readonly bool TryResolve(StateController owner, out Vector2 position)
        {
            Transform target = null;
            switch (_source)
            {
                case GridTargetSource.Self:
                    target = owner != null ? owner.transform : null;
                    break;
                case GridTargetSource.Player:
                    target = GameManager.instance != null && GameManager.instance.player != null
                        ? GameManager.instance.player.transform
                        : null;
                    break;
                case GridTargetSource.CurrentTarget:
                    if (owner != null && owner.TryGetInterface(out ITargetable targetable))
                        target = targetable.CurrentTarget;
                    break;
                case GridTargetSource.RegisteredTarget:
                    if (owner != null &&
                        owner.TryGetInterface(out ITargetControl targets) &&
                        targets.TryGetTarget(_registeredTargetSlot, out StateController registered))
                        target = registered.transform;
                    break;
                case GridTargetSource.SceneAnchor:
                    target = Movement.PositionSceneAnchor.Find(_sceneAnchorId);
                    break;
                case GridTargetSource.World:
                    position = _worldPosition + _offset;
                    return true;
            }

            if (target == null)
            {
                position = default;
                return false;
            }

            position = (Vector2)target.position + _offset;
            return true;
        }
    }

    /// <summary>
    /// 두 대상이 같은 셀·행·열인지 또는 지정 셀 거리 안인지 구분한다.
    /// 그리드 기반 이동, 공격, 퍼즐 조건에 공통으로 사용할 수 있다.
    /// </summary>
    public enum GridRelation
    {
        SameCell,
        SameRow,
        SameColumn,
        ManhattanDistanceAtMost,
        ChebyshevDistanceAtMost
    }

    /// <summary>
    /// 두 월드 대상을 정사각형 그리드 셀로 변환하여 위치 관계를 검사한다.
    /// 별도 보스 의존성 없이 일반 StateDecision으로 사용할 수 있다.
    /// </summary>
    [AddTypeMenu("Move/Grid Relation")]
    [Serializable]
    public sealed class GridRelationDecision : StateDecision
    {
        [Tooltip("관계의 기준이 되는 첫 번째 대상.")]
        [SerializeField]
        private GridTargetReference _first;

        [Tooltip("첫 번째 대상과 비교할 두 번째 대상.")]
        [SerializeField]
        private GridTargetReference _second;

        [Tooltip("두 대상의 셀 관계를 판정할 방식.")]
        [SerializeField]
        private GridRelation _relation;

        [Tooltip("거리 기반 관계에서 허용할 최대 셀 거리.")]
        [SerializeField, Min(0), NaughtyAttributes.ShowIf(nameof(UsesDistance))]
        private int _maxCellDistance = 1;

        [Tooltip("월드 좌표 기준 정사각형 그리드 한 칸 크기. 0 이하는 1로 처리한다.")]
        [SerializeField, Min(0.001f)]
        private float _cellSize = 1f;

        [Tooltip("그리드 (0, 0) 셀의 월드 XY 기준점.")]
        [SerializeField]
        private Vector2 _gridOrigin;

        private bool UsesDistance =>
            _relation == GridRelation.ManhattanDistanceAtMost ||
            _relation == GridRelation.ChebyshevDistanceAtMost;

        /// <summary>
        /// 두 대상의 현재 위치를 셀 좌표로 바꾸고 선택한 행·열·거리 관계를 검사한다.
        /// 어느 한 대상도 찾을 수 없으면 false를 반환한다.
        /// </summary>
        public override bool Decide(StateController stateController)
        {
            if (!_first.TryResolve(stateController, out Vector2 firstPosition) ||
                !_second.TryResolve(stateController, out Vector2 secondPosition))
                return false;

            Vector2Int firstCell = WorldToCell(firstPosition);
            Vector2Int secondCell = WorldToCell(secondPosition);
            Vector2Int difference = firstCell - secondCell;
            int maxDistance = Mathf.Max(0, _maxCellDistance);

            return _relation switch
            {
                GridRelation.SameCell => firstCell == secondCell,
                GridRelation.SameRow => firstCell.y == secondCell.y,
                GridRelation.SameColumn => firstCell.x == secondCell.x,
                GridRelation.ManhattanDistanceAtMost =>
                    Mathf.Abs(difference.x) + Mathf.Abs(difference.y) <= maxDistance,
                GridRelation.ChebyshevDistanceAtMost =>
                    Mathf.Max(Mathf.Abs(difference.x), Mathf.Abs(difference.y)) <= maxDistance,
                _ => false
            };
        }

        /// <summary>
        /// 월드 XY 위치를 설정된 크기와 원점 기준의 가장 가까운 셀로 변환한다.
        /// 프로젝트의 정수 셀 중심 이동과 맞도록 RoundToInt를 사용한다.
        /// </summary>
        private Vector2Int WorldToCell(Vector2 position)
        {
            float safeCellSize = _cellSize > 0f ? _cellSize : 1f;
            return new Vector2Int(
                Mathf.RoundToInt((position.x - _gridOrigin.x) / safeCellSize),
                Mathf.RoundToInt((position.y - _gridOrigin.y) / safeCellSize));
        }
    }
}
