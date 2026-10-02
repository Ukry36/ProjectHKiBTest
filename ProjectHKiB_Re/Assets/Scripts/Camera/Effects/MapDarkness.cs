using UnityEngine;

/// <summary>
/// 맵 씬에 두면 그 맵에 들어올 때 암전을 걸고, 맵이 내려갈 때 걷는다.
/// 지하실·밤 숲처럼 "이 맵은 원래 어둡다"는 경우용. 이벤트 도중 잠깐 어두워지는 건 SetDarknessAction을 쓴다.
/// 컴포넌트나 오브젝트를 끄면 걷히고, 다시 켜면 다시 걸린다.
/// 켜질 때 이 맵의 벽(Wall 레이어 ZCollider2D, 계단·경사 제외)에 WallShadowCaster를 붙여 손전등이 벽에 가려지게 한다.
/// </summary>
public class MapDarkness : MonoBehaviour
{
    [SerializeField] private DarknessSettings _settings = new();
    [Tooltip("켜질 때(맵에 들어올 때) 어두워지는 시간. 맵 전환 페이드와 겹치므로 보통 0.")]
    [SerializeField, Min(0f)] private float _fadeIn;
    [Tooltip("이 맵의 벽에 손전등 그림자를 자동으로 단다. 끄면 WallShadowCaster를 직접 붙인 벽만 가린다.")]
    [SerializeField] private bool _autoWallShadows = true;
    [Tooltip("벽으로 볼 물리 레이어. 계단(isStair)·경사(useSlope)로 표시된 콜라이더는 이 레이어여도 빼고 본다.")]
    [SerializeField] private LayerMask _wallLayers = 1 << 3; // Wall

    // 맵 언로드도 OnDisable을 거친다. 다음 맵이 어두운 맵이면 그 맵의 MapDarkness가 OnEnable에서 바로 다시 건다.
    private void OnEnable()
    {
        DarknessManager.Instance.Apply(_settings, _fadeIn, this);
        if (_autoWallShadows) AttachWallShadows();
    }

    private void AttachWallShadows()
    {
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            foreach (ZCollider2D wall in root.GetComponentsInChildren<ZCollider2D>())
            {
                if ((_wallLayers.value & (1 << wall.gameObject.layer)) == 0) continue;
                if (wall.isStair || wall.useSlopeDU || wall.useSlopeRL) continue;
                if (wall.TryGetComponent(out WallShadowCaster _)) continue;
                wall.gameObject.AddComponent<WallShadowCaster>();
            }
        }
    }

#if UNITY_EDITOR
    // 플레이 중 인스펙터에서 값을 고치면 바로 화면에 반영한다(튜닝용).
    // 손전등 켜짐 여부는 F키로 바꾼 현재 상태를 유지한다 — 값만 만지는데 손전등이 꺼지면 안 된다.
    private void OnValidate()
    {
        if (!Application.isPlaying || !isActiveAndEnabled || !DarknessManager.HasInstance) return;
        DarknessManager manager = DarknessManager.Instance;
        if (manager.Owner != this) return;

        DarknessSettings live = _settings.Clone();
        live.flashlight = manager.FlashlightOn;
        manager.Apply(live, 0f, this);
    }
#endif

    private void OnDisable()
    {
        // 다른 쪽(다음 맵·이벤트)이 이미 덮어썼으면 건드리지 않는다.
        // 종료 중에 매니저를 새로 만들지 않도록 Instance 대신 HasInstance부터 본다.
        if (!DarknessManager.HasInstance) return;
        if (DarknessManager.Instance.Owner == this) DarknessManager.Instance.Clear(0f);
    }
}
