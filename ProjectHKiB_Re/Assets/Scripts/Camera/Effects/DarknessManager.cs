using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 손전등이 비추는 방식.
public enum FlashlightMode
{
    [Tooltip("바라보는 방향으로 부채꼴을 비춘다.")] Facing,
    [Tooltip("방향 없이 플레이어 주위를 원으로 밝힌다(랜턴·촛불).")] Around,
}

// 암전 한 장의 모양. 거리 단위는 전부 월드 유닛(타일 = 1)이라 해상도·줌과 무관하다.
[Serializable]
public class DarknessSettings
{
    [Tooltip("어둠의 짙기. 0이면 평소 밝기, 1이면 전역 조명이 완전히 꺼진다.")]
    [Range(0f, 1f)] public float darkness = 0.92f;
    [Tooltip("어두워질 때 전역 조명에 입힐 색조. 밝기는 무시하고 색만 쓴다(푸른 쪽이 쯔꾸르 밤 맵 느낌).")]
    public Color color = new(0.02f, 0.02f, 0.06f, 1f);

    [Header("플레이어 주변")]
    [Tooltip("손전등과 상관없이 은은하게 보이는 반경(월드 유닛). 0이면 손전등 빛만 남는다.")]
    [Min(0f)] public float playerLightRadius = 2.5f;
    [Tooltip("주변 빛 세기. 1이면 반경 안이 평소 밝기(핀포인트 조명처럼 보인다), 0.1~0.2면 발밑만 겨우 보인다.")]
    [Range(0f, 1f)] public float playerLightStrength = 0.15f;
    [Tooltip("가장자리 부드러움. 1이면 밝은 핵 없이 중심에서 바깥으로 서서히 어두워진다.")]
    [Range(0f, 1f)] public float softness = 1f;
    [Tooltip("주변 빛이 나오는 높이(발밑 기준 월드 유닛). 높을수록 머리 쪽이 밝고, 0에 가까울수록 발밑이 밝고 머리(뒤통수)는 어둡다.")]
    [Min(0f)] public float playerLightHeight = 1f;
    [Tooltip("주변 빛이 플레이어 자신을 얼마나 덜 비출지. 0이면 그대로, 1이면 몸에는 주변 빛이 닿지 않는다. " +
             "0보다 크면 주변 빛에도 그림자가 켜져 벽이 주변 빛을 약하게 가린다.")]
    [Range(0f, 1f)] public float playerSelfShadow;

    [Header("손전등")]
    public bool flashlight;
    [Tooltip("Facing: 바라보는 방향 부채꼴 / Around: 주위 원형.")]
    public FlashlightMode flashlightMode = FlashlightMode.Facing;
    [Tooltip("손전등 밝기. 1이면 빛 안이 평소 밝기로 돌아오고, 낮출수록 빛 안에도 어둠이 남는다.")]
    [Range(0f, 1f)] public float flashlightBrightness = 1f;
    [Tooltip("빛이 닿는 거리(월드 유닛). Around일 때는 원의 반지름.")]
    [Min(0f)] public float flashlightRange = 6f;
    [Tooltip("부채꼴 전체 각도(도). Facing일 때만 쓴다.")]
    [Range(1f, 179f)] public float flashlightAngle = 50f;
    [Tooltip("부채꼴 가운데에서 양쪽 끝으로 갈수록 어두워지는 정도. 0이면 각도 끝에서 딱 끊기고, 1이면 정중앙만 가장 밝고 " +
             "양 끝으로 서서히 어두워진다. 가장자리 쪽 벽 그림자 경계도 그만큼 덜 도드라진다.")]
    [Range(0f, 1f)] public float flashlightAngleFalloff = 0.6f;
    [Tooltip("멀어질수록 어두워지는 정도. 0이면 사거리 끝까지 같은 밝기, 1이면 광원에서부터 서서히 어두워진다. " +
             "먼 곳의 벽 그림자 경계도 그만큼 덜 도드라진다.")]
    [Range(0f, 1f)] public float flashlightDistanceFalloff = 0.6f;
    [Tooltip("어두워지는 곡선(Light2D Falloff Strength). 낮으면 완만하게, 높으면 가운데·가까운 곳에 빛이 몰리고 빨리 어두워진다.")]
    [Range(0f, 1f)] public float flashlightFalloffCurve = 0.5f;
    [Tooltip("깜빡임 세기. 0이면 안정, 1이면 거의 꺼질 듯 흔들린다(배터리 연출).")]
    [Range(0f, 1f)] public float flicker;
    [Tooltip("손전등 광원을 바라보는 방향으로 띄우는 거리(월드 유닛). 발밑이 아니라 캐릭터 앞 바닥에서 빛이 시작되는 느낌. " +
             "벽에 붙어 있으면 벽 앞에서 멈춘다. Around 모드에서는 쓰지 않는다.")]
    [Min(0f)] public float flashlightOffset = 0.6f;

    [Header("손전등 그림자")]
    [Tooltip("벽·사물(ShadowCaster2D)에 손전등 빛이 가려진다. 맵의 벽에는 MapDarkness가 WallShadowCaster를 자동으로 단다.")]
    public bool flashlightShadows = true;
    [Tooltip("그림자 진하기. 1이면 가려진 곳이 완전히 어둡고, 낮출수록 빛이 조금 비친다.")]
    [Range(0f, 1f)] public float flashlightShadowIntensity = 0.9f;

    public DarknessSettings Clone() => (DarknessSettings)MemberwiseClone();
}

/// <summary>
/// URP 2D 라이팅으로 암전과 손전등을 만든다. 새 렌더링을 그리지 않고 이미 씬에 있는 조명만 조절한다.
///   - 어둠: System 씬의 Global Light2D 밝기를 낮추고 색조를 입힌다.
///   - 플레이어 주변: Player에 달린 Point Light2D(평소엔 꺼져 있음)를 켜서 반경만 맞춘다.
///   - 손전등: 그 Point Light2D를 복제해 부채꼴로 만들고 바라보는 방향으로 돌린다.
/// 맵에 둔 다른 Light2D(촛불·창문)도 같은 라이팅이라 어둠 속에서 자연스럽게 빛난다.
/// 조명을 받는 건 Sprite-Lit 재질의 스프라이트뿐이라 UI·대화창은 어두워지지 않는다.
///
/// ScreenEffectManager처럼 처음 쓸 때 스스로 생기므로 씬에 배치할 필요가 없다.
/// 이벤트 체인: SetDarknessAction / SetFlashlightAction. 맵 고정 암전: 맵 씬에 MapDarkness를 둔다.
/// 런타임 코드: Instance.Apply / Clear / SetFlashlight.
/// </summary>
public class DarknessManager : MonoBehaviour
{
    private static DarknessManager _instance;
    private static bool _isQuitting;

    public static bool HasInstance => _instance != null;

    public static DarknessManager Instance
    {
        get
        {
            if (_instance == null && Application.isPlaying && !_isQuitting)
            {
                _instance = FindObjectOfType<DarknessManager>();
                if (_instance == null)
                    _instance = new GameObject(nameof(DarknessManager)).AddComponent<DarknessManager>();
            }
            return _instance;
        }
    }

    private const float FlashlightTurnSpeed = 720f;
    // 광원이 벽 앞에서 멈출 때 벽과 남겨 둘 틈. 0이면 광원이 그림자 판 경계에 걸려 빛이 샌다.
    private const float WallGap = 0.1f;
    private static readonly int WallLayerMask = 1 << 3; // Wall

    // 지금 걸린 값(전환 중엔 중간값). _target은 전환이 끝났을 때의 값.
    private DarknessSettings _current = new() { darkness = 0f };
    private DarknessSettings _target = new() { darkness = 0f };
    private float _flashOn;          // 손전등 켜짐 정도 0~1 — 켜고 끌 때 툭 끊기지 않게 보간한다.
    private float _flashAngle = -90f; // 월드 기준 각도(도). 기본은 아래(정면).
    private bool _flashAngleInitialized;
    private Coroutine _transition;
    private UnityEngine.Object _owner;

    // 전역 조명 — 원래 값을 기억했다가 어둠이 걷히면 그대로 돌려놓는다.
    private Light2D _globalLight;
    private float _globalBaseIntensity = 1f;
    private Color _globalBaseColor = Color.white;
    private float _nextGlobalLightSearch;

    // 플레이어 조명 — 주변 빛은 Player에 원래 있던 것을 빌려 쓰고, 손전등은 그걸 복제해 만든다.
    private Player _trackedPlayer;
    private IDirAnimatable _facing;
    private Light2D _ambientLight;
    private LightSnapshot _ambientOriginal;
    private Light2D _flashLight;
    private readonly RaycastHit2D[] _wallHits = new RaycastHit2D[8];
    // 손전등이 플레이어 자신(위를 볼 때 뒤통수 등)을 비추지 않게 하는 그림자 전용 대역.
    // 플레이어 몸 스프라이트를 매 프레임 그대로 따라 그리는 투명 스프라이트이고, Player가 아니라 이 매니저 아래에 있다.
    private readonly List<SpriteRenderer> _playerSprites = new();
    private readonly List<SpriteRenderer> _shadowProxies = new();
    private GameObject _shadowProxyRoot;

    public bool IsActive => _current.darkness > 0.001f;
    public bool IsTransitioning => _transition != null;
    public bool FlashlightOn => _target.flashlight;
    /// <summary>지금 걸린 암전을 마지막으로 건 쪽(MapDarkness 등). 맵이 내려갈 때 자기 것만 걷는 데 쓴다.</summary>
    public UnityEngine.Object Owner => _owner;
    public DarknessSettings Target => _target.Clone();

    /// <summary>손전등이 지금 비추고 있으면 그 광원 위치. WallShadowCaster가 벽 앞뒤를 판정할 때 쓴다.</summary>
    public bool TryGetFlashlightOrigin(out Vector2 origin)
    {
        bool on = _flashLight && _flashLight.isActiveAndEnabled && _flashLight.intensity > 0f;
        origin = on ? (Vector2)_flashLight.transform.position : default;
        return on;
    }

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnApplicationQuit() => _isQuitting = true;

    private void OnDestroy()
    {
        if (_instance != this) return;
        RestoreGlobalLight();
        ReleasePlayerLights();
        _instance = null;
    }

    // ─── 공개 API ────────────────────────────────────────────────

    /// <summary>암전 모양을 settings로 바꾼다. duration 동안 지금 모양에서 부드럽게 넘어간다.</summary>
    public void Apply(DarknessSettings settings, float duration, UnityEngine.Object owner = null)
    {
        if (settings == null) return;
        _owner = owner;
        StartTransition(settings.Clone(), duration);
    }

    /// <summary>암전을 걷는다. 다른 모양 값은 남겨 두므로 다시 짙기만 올리면 같은 모양으로 돌아온다.</summary>
    public void Clear(float duration)
    {
        _owner = null;
        DarknessSettings next = _target.Clone();
        next.darkness = 0f;
        next.flashlight = false;
        StartTransition(next, duration);
    }

    public void SetFlashlight(bool on)
    {
        _target.flashlight = on;
        if (_transition == null) _current.flashlight = on;
    }

    public void ToggleFlashlight() => SetFlashlight(!_target.flashlight);

    /// <summary>손전등 모양만 바꾼다(방식·밝기·범위·각도). 켜짐 여부와 암전 짙기는 그대로.</summary>
    public void SetFlashlightShape(FlashlightMode mode, float brightness, float range, float angle, float duration)
    {
        DarknessSettings next = _target.Clone();
        next.flashlightMode = mode;
        next.flashlightBrightness = Mathf.Clamp01(brightness);
        next.flashlightRange = Mathf.Max(0f, range);
        next.flashlightAngle = Mathf.Clamp(angle, 1f, 179f);
        StartTransition(next, duration);
    }

    // ─── 전환 ───────────────────────────────────────────────────

    private void StartTransition(DarknessSettings next, float duration)
    {
        if (_transition != null) StopCoroutine(_transition);
        _transition = null;
        _target = next;

        if (duration <= 0f)
        {
            _current = next.Clone();
            return;
        }
        _transition = StartCoroutine(TransitionRoutine(_current.Clone(), next, duration));
    }

    private IEnumerator TransitionRoutine(DarknessSettings from, DarknessSettings to, float duration)
    {
        // 모양(손전등 여부·방식)은 처음부터 목표값을 쓰고, 수치만 보간한다.
        _current = to.Clone();
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            _current.darkness = Mathf.Lerp(from.darkness, to.darkness, t);
            _current.color = Color.Lerp(from.color, to.color, t);
            _current.playerLightRadius = Mathf.Lerp(from.playerLightRadius, to.playerLightRadius, t);
            _current.playerLightStrength = Mathf.Lerp(from.playerLightStrength, to.playerLightStrength, t);
            _current.softness = Mathf.Lerp(from.softness, to.softness, t);
            _current.playerLightHeight = Mathf.Lerp(from.playerLightHeight, to.playerLightHeight, t);
            _current.playerSelfShadow = Mathf.Lerp(from.playerSelfShadow, to.playerSelfShadow, t);
            _current.flashlightBrightness = Mathf.Lerp(from.flashlightBrightness, to.flashlightBrightness, t);
            _current.flashlightRange = Mathf.Lerp(from.flashlightRange, to.flashlightRange, t);
            _current.flashlightAngle = Mathf.Lerp(from.flashlightAngle, to.flashlightAngle, t);
            _current.flashlightAngleFalloff = Mathf.Lerp(from.flashlightAngleFalloff, to.flashlightAngleFalloff, t);
            _current.flashlightDistanceFalloff = Mathf.Lerp(from.flashlightDistanceFalloff, to.flashlightDistanceFalloff, t);
            _current.flashlightFalloffCurve = Mathf.Lerp(from.flashlightFalloffCurve, to.flashlightFalloffCurve, t);
            _current.flashlightOffset = Mathf.Lerp(from.flashlightOffset, to.flashlightOffset, t);
            _current.flashlightShadowIntensity = Mathf.Lerp(from.flashlightShadowIntensity, to.flashlightShadowIntensity, t);
            _current.flicker = Mathf.Lerp(from.flicker, to.flicker, t);
            yield return null;
        }
        _current = to.Clone();
        _transition = null;
    }

    // ─── 매 프레임 조명 반영 ─────────────────────────────────────

    private void LateUpdate()
    {
        _current.flashlight = _target.flashlight;
        _flashOn = Mathf.MoveTowards(_flashOn, _current.flashlight ? 1f : 0f, Time.unscaledDeltaTime * 8f);

        float d = IsActive ? _current.darkness : 0f;
        ApplyGlobalLight(d);
        TrackPlayer();
        ApplyAmbientLight(d);
        ApplyFlashLight(d);
        bool ambientSelfShadow = _ambientLight && _ambientLight.isActiveAndEnabled && _current.playerSelfShadow > 0.001f;
        SyncShadowProxies((_flashLight && _flashLight.isActiveAndEnabled) || ambientSelfShadow);
    }

    private void ApplyGlobalLight(float d)
    {
        if (!_globalLight)
        {
            // 전역 조명이 없는 씬에서 매 프레임 FindObjectsOfType를 돌지 않도록, 어두울 때만 1초 간격으로 찾는다.
            if (d <= 0.001f || Time.unscaledTime < _nextGlobalLightSearch) return;
            _nextGlobalLightSearch = Time.unscaledTime + 1f;
            CaptureGlobalLight();
            if (!_globalLight) return;
        }

        _globalLight.intensity = _globalBaseIntensity * (1f - d);
        _globalLight.color = Color.Lerp(_globalBaseColor, Hue(_current.color), d);
    }

    // 어둠 속에서 빛이 "원래 밝기로 돌아오도록" 플레이어 조명은 전역 조명이 잃은 만큼만 더한다.
    private float LostIntensity(float d) => _globalBaseIntensity * d;

    private void ApplyAmbientLight(float d)
    {
        if (!_ambientLight) return;
        bool on = d > 0.001f && _current.playerLightRadius > 0.01f && _current.playerLightStrength > 0.001f;
        if (!on)
        {
            _ambientOriginal.Restore(_ambientLight);
            return;
        }

        if (!_ambientLight.gameObject.activeSelf) _ambientLight.gameObject.SetActive(true);
        _ambientLight.enabled = true;
        // System 씬의 이 조명 오브젝트에는 위치 확인용 주황 점(SpriteRenderer)이 붙어 있다 — 빌려 쓰는 동안은 숨긴다.
        _ambientOriginal.HideRenderers(_ambientLight);
        _ambientLight.pointLightOuterRadius = _current.playerLightRadius;
        _ambientLight.pointLightInnerRadius = _current.playerLightRadius * (1f - _current.softness);
        _ambientLight.pointLightOuterAngle = 360f;
        _ambientLight.pointLightInnerAngle = 360f;
        _ambientLight.intensity = LostIntensity(d) * _current.playerLightStrength;
        _ambientOriginal.SetHeight(_ambientLight, _current.playerLightHeight);
        // 플레이어 자기 그림자 — 그림자 대역(selfShadows)이 이 빛에서도 플레이어 모양을 가리게 한다.
        // 그림자를 켜면 벽(WallShadowCaster)도 이 빛을 가리지만 주변 빛은 약해서 티가 작다.
        _ambientLight.shadowsEnabled = _current.playerSelfShadow > 0.001f;
        _ambientLight.shadowIntensity = _current.playerSelfShadow;
    }

    private void ApplyFlashLight(float d)
    {
        bool on = d > 0.001f && _flashOn > 0.001f && _current.flashlightBrightness > 0f && _trackedPlayer;
        if (!on)
        {
            if (_flashLight) _flashLight.gameObject.SetActive(false);
            return;
        }

        if (!_flashLight) _flashLight = CreateFlashLight("Flashlight (DarknessManager)");
        if (!_flashLight) return;
        if (!_flashLight.gameObject.activeSelf) _flashLight.gameObject.SetActive(true);

        float intensity = _flashOn * _current.flashlightBrightness;
        if (_current.flicker > 0f)
        {
            float n = Mathf.PerlinNoise(Time.unscaledTime * 9f, 0.37f);
            intensity *= 1f - _current.flicker * Mathf.Clamp01(n * 1.8f - 0.8f) * 1.5f;
        }

        UpdateFlashAngle();
        float rad = _flashAngle * Mathf.Deg2Rad;
        Vector2 dir = new(Mathf.Cos(rad), Mathf.Sin(rad));
        Vector2 pivot = _trackedPlayer.transform.position;
        float offset = _current.flashlightMode == FlashlightMode.Around ? 0f : _current.flashlightOffset;
        ConfigureFlashLight(_flashLight, ClampBeforeWall(pivot, pivot + dir * offset), LostIntensity(d) * Mathf.Clamp01(intensity));
    }

    private void ConfigureFlashLight(Light2D light, Vector2 position, float intensity)
    {
        // 방사형 감쇠 — 안쪽 반경·안쪽 각도까지는 최대 밝기, 바깥쪽으로 갈수록 Falloff Strength 곡선대로 어두워진다.
        light.pointLightOuterRadius = _current.flashlightRange;
        light.pointLightInnerRadius = _current.flashlightRange * (1f - _current.flashlightDistanceFalloff);
        light.falloffIntensity = _current.flashlightFalloffCurve;
        if (_current.flashlightMode == FlashlightMode.Around)
        {
            light.pointLightOuterAngle = 360f;
            light.pointLightInnerAngle = 360f;
        }
        else
        {
            light.pointLightOuterAngle = _current.flashlightAngle;
            light.pointLightInnerAngle = _current.flashlightAngle * (1f - _current.flashlightAngleFalloff);
        }
        light.intensity = intensity;
        // 그림자는 손전등에만 켠다 — 그림자 비용은 조명 수에 비례하고, 은은한 주변 빛은 가려져도 티가 안 난다.
        light.shadowsEnabled = _current.flashlightShadows;
        light.shadowIntensity = _current.flashlightShadowIntensity;

        Transform tr = light.transform;
        tr.position = new Vector3(position.x, position.y, tr.position.z);
        // Point Light2D의 부채꼴은 오브젝트의 위쪽(local up)을 향한다.
        tr.rotation = Quaternion.Euler(0f, 0f, _flashAngle - 90f);
    }

    // 광원은 벽 안에 들어가면 안 된다 — URP 2D 그림자는 광원이 그림자 판 안에 있으면 그림자를 못 만들어 빛이 벽 너머로 샌다.
    // 플레이어 기준점(충돌 때문에 늘 벽 밖)에서 목표 지점까지 선을 그어 벽에 닿으면 그 앞에서 멈춘다.
    private Vector2 ClampBeforeWall(Vector2 from, Vector2 to)
    {
        Vector2 delta = to - from;
        float length = delta.magnitude;
        if (length < 0.0001f) return from;

        var filter = new ContactFilter2D { useTriggers = true, useLayerMask = true, layerMask = WallLayerMask };
        int count = Physics2D.Linecast(from, to, filter, _wallHits);
        float allowed = length;
        for (int i = 0; i < count; i++)
            allowed = Mathf.Min(allowed, _wallHits[i].distance - WallGap);
        return from + delta / length * Mathf.Max(0f, allowed);
    }

    // 바라보는 방향으로 손전등을 돌린다. 8방향 입력을 그대로 쓰면 대각선 전환이 툭툭 끊겨서 빠르게 돌려 맞춘다.
    private void UpdateFlashAngle()
    {
        if (_facing == null) return;
        Vector2 dir = _facing.LastSetAnimationDir8;
        if (dir.sqrMagnitude < 0.0001f) return;

        float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        if (!_flashAngleInitialized)
        {
            _flashAngle = targetAngle;
            _flashAngleInitialized = true;
            return;
        }
        _flashAngle = Mathf.MoveTowardsAngle(_flashAngle, targetAngle, FlashlightTurnSpeed * Time.unscaledDeltaTime);
    }

    // ─── 조명 찾기 ──────────────────────────────────────────────

    private void CaptureGlobalLight()
    {
        foreach (Light2D light in FindObjectsOfType<Light2D>())
        {
            if (light.lightType != Light2D.LightType.Global || !light.isActiveAndEnabled) continue;
            _globalLight = light;
            _globalBaseIntensity = light.intensity;
            _globalBaseColor = light.color;
            return;
        }
    }

    private void RestoreGlobalLight()
    {
        if (!_globalLight) return;
        _globalLight.intensity = _globalBaseIntensity;
        _globalLight.color = _globalBaseColor;
    }

    // 맵을 옮기거나 플레이어가 바뀌어도 따라가도록 매 프레임 확인한다.
    private void TrackPlayer()
    {
        Player player = GameManager.instance ? GameManager.instance.player : null;
        if (player == _trackedPlayer && (player == null || _ambientLight)) return;

        ReleasePlayerLights();
        _trackedPlayer = player;
        if (player == null) return;

        player.TryGetInterface(out _facing);
        foreach (Light2D light in player.GetComponentsInChildren<Light2D>(true))
        {
            if (light.lightType != Light2D.LightType.Point) continue;
            _ambientLight = light;
            _ambientOriginal = LightSnapshot.Take(light);
            break;
        }

        if (!_ambientLight)
            Debug.LogWarning("[DarknessManager] Player에 Point Light2D가 없어 플레이어 주변 빛과 손전등을 켤 수 없습니다. " +
                             "Player 아래에 Point Light2D를 하나 두세요(꺼 둬도 된다 — 어두워지면 이 매니저가 켠다).");

        CollectPlayerSprites(player);
    }

    // 손전등은 플레이어가 들고 있는 빛이라 플레이어 자신을 비추면 안 된다(위를 보면 뒤통수가 밝아진다).
    // Light2D는 정렬 레이어 단위로만 대상을 고를 수 있는데 플레이어·NPC·벽이 모두 Entity라 레이어로는 못 뺀다.
    // 그래서 플레이어 몸 스프라이트마다 "그림자 전용 대역"을 이 매니저 아래에 만든다 — Player에는 아무것도 붙이지 않는다.
    //   - 대역은 원본과 같은 스프라이트·위치·크기·뒤집기를 매 프레임 따라가고, 렌더러 색 알파가 0이라 화면에는 안 보인다.
    //   - 그림자 셰이더(Shadow2D-Shadow-Sprite)는 스프라이트 텍스처 알파만 보므로, 대역의 selfShadows 실루엣이
    //     플레이어 모양만큼 손전등 그림자를 칠한다. 그림자를 쓰는 조명은 손전등뿐이라 전역·주변 빛은 그대로 플레이어를 비춘다.
    //   - URP 12는 castsShadows=false인 ShadowCaster2D가 혼자 그룹이면 그리지 않으므로, 대역들의 부모에 CompositeShadowCaster2D를 둔다.
    private void CollectPlayerSprites(Player player)
    {
        _playerSprites.Clear();
        foreach (SpriteRenderer sprite in player.GetComponentsInChildren<SpriteRenderer>(true))
            if (!sprite.GetComponent<Light2D>()) _playerSprites.Add(sprite); // 조명 위치 표시용 점은 뺀다

        if (!_shadowProxyRoot)
        {
            _shadowProxyRoot = new GameObject("Player Flashlight Shadow");
            _shadowProxyRoot.transform.SetParent(transform, false);
            _shadowProxyRoot.AddComponent<CompositeShadowCaster2D>();
        }

        for (int i = 0; i < _playerSprites.Count; i++)
        {
            var go = new GameObject($"Proxy {_playerSprites[i].name}");
            go.transform.SetParent(_shadowProxyRoot.transform, false);
            SpriteRenderer proxy = go.AddComponent<SpriteRenderer>();
            proxy.sprite = _playerSprites[i].sprite;
            proxy.color = new Color(1f, 1f, 1f, 0f);
            proxy.sortingLayerID = _playerSprites[i].sortingLayerID;

            ShadowCaster2D silhouette = go.AddComponent<ShadowCaster2D>();
            silhouette.castsShadows = false;
            silhouette.useRendererSilhouette = true;
            silhouette.selfShadows = true;
            _shadowProxies.Add(proxy);
        }
    }

    // 애니메이션(스프라이트 교체)·이동이 끝난 뒤인 LateUpdate에 원본을 그대로 베낀다.
    private void SyncShadowProxies(bool active)
    {
        for (int i = 0; i < _shadowProxies.Count; i++)
        {
            SpriteRenderer proxy = _shadowProxies[i];
            SpriteRenderer source = i < _playerSprites.Count ? _playerSprites[i] : null;
            if (!proxy) continue;

            bool on = active && source && source.enabled && source.gameObject.activeInHierarchy && source.sprite;
            if (proxy.gameObject.activeSelf != on) proxy.gameObject.SetActive(on);
            if (!on) continue;

            Transform from = source.transform, to = proxy.transform;
            to.SetPositionAndRotation(from.position, from.rotation);
            to.localScale = from.lossyScale; // 대역 부모(매니저)는 스케일 1이다
            proxy.sprite = source.sprite;
            proxy.flipX = source.flipX;
            proxy.flipY = source.flipY;
        }
    }

    // 손전등은 주변 빛을 복제해 만든다. Light2D의 "적용할 정렬 레이어"는 코드로 지정할 공개 API가 없어서,
    // 이미 올바르게 설정된 조명을 복제하는 것이 유일하게 깔끔한 방법이다. 위치는 매 프레임 ApplyFlashLight가 정한다.
    private Light2D CreateFlashLight(string lightName)
    {
        if (!_ambientLight) return null;
        bool wasActive = _ambientLight.gameObject.activeSelf;
        _ambientLight.gameObject.SetActive(false); // 복제본이 활성 상태로 한 프레임 나오지 않게
        Light2D light = Instantiate(_ambientLight, _ambientLight.transform.parent);
        _ambientLight.gameObject.SetActive(wasActive);
        light.name = lightName;
        // 복제 원본에 달린 표시용 스프라이트 등은 손전등에 필요 없다.
        foreach (Renderer marker in light.GetComponentsInChildren<Renderer>(true)) Destroy(marker);
        return light;
    }

    private void ReleasePlayerLights()
    {
        if (_ambientLight) _ambientOriginal.Restore(_ambientLight);
        if (_flashLight) Destroy(_flashLight.gameObject);
        _playerSprites.Clear();
        foreach (SpriteRenderer proxy in _shadowProxies)
            if (proxy) Destroy(proxy.gameObject);
        _shadowProxies.Clear();
        _ambientLight = null;
        _flashLight = null;
        _facing = null;
        _trackedPlayer = null;
        _flashAngleInitialized = false;
    }

    private static Color Hue(Color c)
    {
        float max = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
        return max > 0.0001f ? new Color(c.r / max, c.g / max, c.b / max, 1f) : Color.white;
    }

    // 빌려 쓴 Player 조명을 원래 상태(꺼져 있던 것까지)로 돌려놓기 위한 값.
    private struct LightSnapshot
    {
        private bool _taken;
        private bool _active;
        private bool _enabled;
        private float _intensity;
        private float _outerRadius;
        private float _innerRadius;
        private float _outerAngle;
        private float _innerAngle;
        private Vector3 _localPosition;
        private bool _shadowsEnabled;
        private float _shadowIntensity;
        private Renderer[] _renderers;
        private bool[] _rendererEnabled;

        public static LightSnapshot Take(Light2D light) => new()
        {
            _taken = true,
            _active = light.gameObject.activeSelf,
            _enabled = light.enabled,
            _intensity = light.intensity,
            _outerRadius = light.pointLightOuterRadius,
            _innerRadius = light.pointLightInnerRadius,
            _outerAngle = light.pointLightOuterAngle,
            _innerAngle = light.pointLightInnerAngle,
            _localPosition = light.transform.localPosition,
            _shadowsEnabled = light.shadowsEnabled,
            _shadowIntensity = light.shadowIntensity,
            _renderers = light.GetComponentsInChildren<Renderer>(true),
        };

        // 빛 높이는 발밑(Player 기준점)에서 위로 잰다. 가로 위치는 원래 자리를 지킨다.
        public void SetHeight(Light2D light, float height)
        {
            if (!_taken) return;
            Transform tr = light.transform;
            Vector3 feet = tr.parent ? tr.parent.InverseTransformPoint(tr.root.position) : Vector3.zero;
            tr.localPosition = new Vector3(_localPosition.x, feet.y + height, _localPosition.z);
        }

        public void HideRenderers(Light2D light)
        {
            if (!_taken || _renderers == null) return;
            if (_rendererEnabled == null)
            {
                _rendererEnabled = new bool[_renderers.Length];
                for (int i = 0; i < _renderers.Length; i++) _rendererEnabled[i] = _renderers[i] && _renderers[i].enabled;
            }
            foreach (Renderer r in _renderers) if (r) r.enabled = false;
        }

        public void Restore(Light2D light)
        {
            if (!_taken || !light) return;
            light.intensity = _intensity;
            light.pointLightOuterRadius = _outerRadius;
            light.pointLightInnerRadius = _innerRadius;
            light.pointLightOuterAngle = _outerAngle;
            light.pointLightInnerAngle = _innerAngle;
            light.transform.localPosition = _localPosition;
            light.shadowsEnabled = _shadowsEnabled;
            light.shadowIntensity = _shadowIntensity;
            light.enabled = _enabled;
            if (_rendererEnabled != null)
            {
                for (int i = 0; i < _renderers.Length; i++) if (_renderers[i]) _renderers[i].enabled = _rendererEnabled[i];
                _rendererEnabled = null;
            }
            if (light.gameObject.activeSelf != _active) light.gameObject.SetActive(_active);
        }
    }
}
