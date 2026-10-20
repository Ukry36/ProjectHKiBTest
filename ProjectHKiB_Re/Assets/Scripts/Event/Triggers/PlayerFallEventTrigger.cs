using UnityEngine;

/// <summary>
/// 바닥이 없는 곳으로 떨어진 플레이어를 감지해 이벤트로 변환합니다(낙하 복귀).
/// 맵에 감지 영역을 깔지 않고 플레이어의 물리 상태를 직접 보므로 System 씬에 하나만 두면 모든 맵에 적용됩니다.
/// </summary>
[AddComponentMenu("ProjectHKiB/Event/Player Fall Event Trigger")]
public sealed class PlayerFallEventTrigger : EventTriggerBase
{
    [Tooltip("마지막으로 밟은 높이보다 이만큼 내려가야 추락으로 봅니다. 구덩이를 점프로 넘는 중엔 발동하지 않게 합니다.")]
    [SerializeField, Min(0f)]
    private float _fallDistance = 2f;

    private IPhysics _physics;
    private float _lastGroundZ = float.NaN;

    // 복귀 이벤트가 끝나기 전에도 계속 떨어지므로, 다시 땅을 밟기 전까지는 한 번만 발동한다.
    private bool _fallen;

    private void FixedUpdate()
    {
        if (_physics == null)
        {
            Player player = GameManager.instance != null ? GameManager.instance.player : null;
            if (player == null || !player.TryGetInterface(out _physics)) return;
        }

        if (_physics.Ground || float.IsNaN(_lastGroundZ))
        {
            _lastGroundZ = _physics.ZCol.ZMin;
            _fallen = false;
            return;
        }

        if (_fallen || _physics.ZCol.ZMin > _lastGroundZ - _fallDistance) return;

        // 발밑 어디에도 바닥이 없을 때만 — 더 낮은 층으로 뛰어내리는 건 추락이 아니다.
        Vector2 checkSize = (Vector2)_physics.Size - new Vector2(0.2f, 0.2f);
        if (ZPhysics2D.ZBoxGetFloor(_physics.HPosition, checkSize, 0, _physics.FloorLayer, float.MinValue, _physics.ZCol.ZMin)) return;

        _fallen = true;
        TryTrigger(new EventTriggerContext(this, GameManager.instance.player.gameObject));
    }
}
