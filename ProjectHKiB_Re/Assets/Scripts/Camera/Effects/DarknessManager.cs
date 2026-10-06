using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
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
    [Tooltip("어둠의 색조. 밝기는 무시하고 색만 쓴다.")]
    public Color color = new(0.02f, 0.02f, 0.06f, 1f);

    [Header("플레이어 주변")]
    [Tooltip("손전등과 별개로 은은하게 보이는 반경(월드 유닛). 0이면 끈다.")]
    [Min(0f)] public float playerLightRadius = 2.5f;
    [Tooltip("주변 빛 세기. 1이면 평소 밝기, 0.1~0.2면 발밑만 겨우 보인다.")]
    [Range(0f, 1f)] public float playerLightStrength = 0.15f;
    [Tooltip("가장자리 부드러움. 1이면 중심부터 서서히 어두워진다.")]
    [Range(0f, 1f)] public float softness = 1f;
    [Tooltip("주변 빛이 나오는 높이(발밑 기준). 낮을수록 발밑이 밝고 머리는 어둡다.")]
    [Min(0f)] public float playerLightHeight = 1f;
    [Tooltip("주변 빛이 플레이어 몸을 덜 비추는 정도. 0보다 크면 벽도 주변 빛을 약하게 가린다.")]
    [Range(0f, 1f)] public float playerSelfShadow;

    [Header("손전등")]
    public bool flashlight;
    [Tooltip("Facing: 바라보는 방향 부채꼴 / Around: 주위 원형.")]
    public FlashlightMode flashlightMode = FlashlightMode.Facing;
    [Tooltip("손전등 밝기. 1이면 빛 안이 평소 밝기.")]
    [Range(0f, 1f)] public float flashlightBrightness = 1f;
    [Tooltip("빛이 닿는 거리(월드 유닛). Around일 때는 원의 반지름.")]
    [Min(0f)] public float flashlightRange = 6f;
    [Tooltip("부채꼴 전체 각도(도). Facing일 때만 쓴다.")]
    [Range(1f, 179f)] public float flashlightAngle = 50f;
    [Tooltip("부채꼴 양 끝으로 갈수록 어두워지는 정도. 0이면 각도 끝에서 딱 끊긴다.")]
    [Range(0f, 1f)] public float flashlightAngleFalloff = 0.6f;
    [Tooltip("멀어질수록 어두워지는 정도. 0이면 사거리 끝까지 같은 밝기.")]
    [Range(0f, 1f)] public float flashlightDistanceFalloff = 0.6f;
    [Tooltip("어두워지는 곡선. 높을수록 가운데·가까운 곳에 빛이 몰린다.")]
    [Range(0f, 1f)] public float flashlightFalloffCurve = 0.5f;
    [Tooltip("빛 시작 부분의 둥근 반지름(월드 유닛). 0이면 꼭짓점이 뾰족하다. Facing 전용.")]
    [Min(0f)] public float flashlightStartRoundness = 0.35f;
    [Tooltip("깜빡임 세기(배터리 연출). 0이면 안정.")]
    [Range(0f, 1f)] public float flicker;
    [Tooltip("광원을 바라보는 방향으로 띄우는 거리(월드 유닛). 벽 앞에서 멈춘다. Facing 전용.")]
    [Min(0f)] public float flashlightOffset = 0.6f;

    [Header("손전등 그림자")]
    [Tooltip("벽(Wall 레이어, 계단·경사 제외)이 손전등 빛을 가린다.")]
    public bool flashlightShadows = true;
    [Tooltip("그림자 진하기. 1이면 완전히 어둡다.")]
    [Range(0f, 1f)] public float flashlightShadowIntensity = 0.9f;
    [Tooltip("켜면 셰이더로 그림자 경계를 흐린다. 끄면 URP 그림자(경계가 또렷함). 아래 값들은 켰을 때만 쓴다.")]
    public bool flashlightShaderShadows = true;
    [Tooltip("그림자 경계를 흐리는 폭(월드 유닛). 0이면 블러 없음.")]
    [Min(0f)] public float flashlightShadowSoftness = 0.6f;
    [Tooltip("벽 접촉부의 선명한 그림자가 블러로 바뀌기 시작하는 거리.")]
    [Min(0.01f)] public float flashlightShadowInnerFade = 0.6f;
    [Tooltip("벽 뒤에서 블러가 완전히 적용되는 거리(월드 유닛). Inner Fade보다 작으면 Inner Fade를 쓴다.")]
    [Min(0.01f)] public float flashlightShadowSpreadDistance = 1.5f;
    [Tooltip("그림자를 벽 안쪽으로 밀어 넣는 폭. 벽과 그림자 사이 미세한 틈을 덮는다.")]
    [Min(0f)] public float flashlightShadowInset = 0f;
    [Tooltip("그림자 판정을 월드 픽셀 격자에 맞춘다(스프라이트 PPU와 같게). 이동 중 경계가 벽 테두리에서 미끄러지지 않는다. 0이면 끈다.")]
    [Min(0f)] public float flashlightShadowPixelsPerUnit = 16f;

    [Header("부드러움")]
    [Tooltip("손전등이 플레이어를 따라가는 빠르기(1/초). 물리 틱 사이를 메워 그림자가 뚝뚝 끊기지 않게 한다. 0이면 보간 안 함.")]
    [Min(0f)] public float flashlightFollowSharpness = 40f;

    public DarknessSettings Clone() => (DarknessSettings)MemberwiseClone();
}

/// <summary>
/// URP 2D 라이팅으로 암전과 손전등을 만든다. 이미 있는 조명만 조절한다.
///   - 어둠: Global Light2D 밝기·색조 / 플레이어 주변: Player의 Point Light2D를 빌려 씀
///   - 손전등: 그 Point Light2D를 복제한 Sprite Light2D + 직접 그린 빛 모양 그림(시작이 둥근 부채꼴)
///   - 벽 가림: 각도별 벽 거리(1D 그림자 맵) + FlashlightOcclusion 셰이더(URP 그림자는 경계를 못 흐려서)
/// GameManager 산하(GameManager.darknessManager). 없으면 처음 쓸 때 GameManager 아래에 생긴다.
/// 사용: 맵 씬의 MapDarkness / 이벤트 체인 SetDarknessAction·SetFlashlightAction / Instance.Apply·Clear·SetFlashlight.
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
                GameManager gameManager = GameManager.instance;
                _instance = gameManager && gameManager.darknessManager
                    ? gameManager.darknessManager
                    : FindObjectOfType<DarknessManager>();
                if (_instance == null)
                {
                    var go = new GameObject(nameof(DarknessManager));
                    if (gameManager) go.transform.SetParent(gameManager.transform, false);
                    _instance = go.AddComponent<DarknessManager>();
                }
                if (gameManager && !gameManager.darknessManager) gameManager.darknessManager = _instance;
            }
            return _instance;
        }
    }

    private const float FlashlightTurnSpeed = 720f;
    // 이보다 멀리 한 번에 움직이면(순간이동) 보간 없이 바로 옮긴다.
    private const float FlashlightSnapDistance = 1.5f;
    // 광원과 벽 사이 틈. 0이면 광원이 그림자 판 경계에 걸려 빛이 샌다.
    private const float WallGap = 0.1f;
    private static readonly int WallLayerMask = 1 << 3; // Wall

    // _current = 지금 값(전환 중엔 중간값), _target = 전환 끝 값.
    private DarknessSettings _current = new() { darkness = 0f };
    private DarknessSettings _target = new() { darkness = 0f };
    private float _flashOn;          // 켜짐 정도 0~1(켜고 끌 때 보간)
    private float _flashAngle = -90f; // 월드 각도(도)
    private bool _flashAngleInitialized;
    private Vector2 _flashPivot;
    private bool _flashPivotInitialized;
    private Coroutine _transition;
    private UnityEngine.Object _owner;

    private Light2D _globalLight;
    private float _globalBaseIntensity = 1f;
    private Color _globalBaseColor = Color.white;
    private float _nextGlobalLightSearch;

    private Player _trackedPlayer;
    private IDirAnimatable _facing;
    private Light2D _ambientLight;
    private LightSnapshot _ambientOriginal;
    private Light2D _flashLight;
    private Texture2D _beamBase;     // 가림 없는 원본 빛 모양
    private Texture2D _beamTexture;  // 조명이 쓰는 그림(원본 × 가림, 매 프레임 GPU 복사)
    private Sprite _beamSprite;
    private BeamShape _beamShape;
    private Vector4 _beamBounds;     // 로컬 영역: xMin, yMin, 폭, 높이
    // 빛 그림 텍셀이 화면 픽셀보다 커지면 광원이 움직일 때 그림자 경계가 일렁인다.
    private const int BeamTextureHeight = 512;

    private const string OcclusionShaderPath = "ScreenEffects/FlashlightOcclusion";
    // 각도 칸(0.18°). 굵으면 일직선 벽 경계가 계단지고 움직일 때 미끄러진다.
    private const int OcclusionBins = 2048;
    private const float VisualMarchStep = 0.1f;
    private Material _occlusionMaterial;
    private RenderTexture _beamTarget;
    private Texture2D _occlusionMap;
    private readonly float[] _occlusionDistances = new float[OcclusionBins * 2];
    private bool _beamOccluded;
    private bool _occlusionUnavailable;
    private static readonly int BoundsId = Shader.PropertyToID("_Bounds");
    private static readonly int SoftnessId = Shader.PropertyToID("_Softness");
    private static readonly int ShadowStrengthId = Shader.PropertyToID("_ShadowStrength");
    private static readonly int InsetId = Shader.PropertyToID("_Inset");
    private static readonly int InnerFadeId = Shader.PropertyToID("_InnerFade");
    private static readonly int SpreadDistanceId = Shader.PropertyToID("_SpreadDistance");
    private static readonly int RotationId = Shader.PropertyToID("_Rotation");
    private static readonly int OcclusionMapId = Shader.PropertyToID("_OcclusionMap");
    private static readonly int OriginId = Shader.PropertyToID("_Origin");
    private static readonly int PixelsPerUnitId = Shader.PropertyToID("_PixelsPerUnit");
    private readonly RaycastHit2D[] _wallHits = new RaycastHit2D[16];
    private readonly RaycastHit2D[] _adjacentHits = new RaycastHit2D[8];
    private readonly RaycastHit2D[] _visualHits = new RaycastHit2D[8];
    // 맵의 어떤 벽 높이(ZCollider2D.height)보다 커야 한다.
    private const float MaxOccluderHeight = 10f;
    // 손전등이 플레이어 자신을 비추지 않게 하는 그림자 전용 대역(CollectPlayerSprites 참고).
    private readonly List<SpriteRenderer> _playerSprites = new();
    private readonly List<SpriteRenderer> _shadowProxies = new();
    private GameObject _shadowProxyRoot;

    public bool IsActive => _current.darkness > 0.001f;
    public bool IsTransitioning => _transition != null;
    public bool FlashlightOn => _target.flashlight;
    /// <summary>지금 걸린 암전을 마지막으로 건 쪽(MapDarkness 등). 맵이 내려갈 때 자기 것만 걷는 데 쓴다.</summary>
    public UnityEngine.Object Owner => _owner;
    public DarknessSettings Target => _target.Clone();

    /// <summary>벽 그림자를 셰이더로 그리는 중인가. 아니면 URP 그림자(WallShadowCaster)를 쓴다.</summary>
    public bool UsesShaderShadows =>
        _current.flashlightShadows && _current.flashlightShaderShadows && !_occlusionUnavailable;

    /// <summary>손전등이 지금 비추고 있으면 그 광원 위치. WallShadowCaster가 벽 앞뒤를 판정할 때 쓴다.</summary>
    public bool TryGetFlashlightOrigin(out Vector2 origin)
    {
        bool on = _flashLight && _flashLight.isActiveAndEnabled && _flashLight.intensity > 0f;
        origin = on ? (Vector2)_flashLight.transform.position : default;
        return on;
    }

    private void Awake()
    {
        // 컴포넌트만 지운다 — GameManager 오브젝트에 직접 붙어 있을 수도 있다.
        if (_instance != null && _instance != this) { Destroy(this); return; }
        _instance = this;
        if (transform.parent == null) DontDestroyOnLoad(gameObject);
    }

    private void OnApplicationQuit() => _isQuitting = true;

    private void OnDestroy()
    {
        if (_instance != this) return;
        RestoreGlobalLight();
        ReleasePlayerLights();
        if (_beamSprite) Destroy(_beamSprite);
        if (_beamTexture) Destroy(_beamTexture);
        if (_beamBase) Destroy(_beamBase);
        if (_beamTarget) { _beamTarget.Release(); Destroy(_beamTarget); }
        if (_occlusionMap) Destroy(_occlusionMap);
        if (_occlusionMaterial) Destroy(_occlusionMaterial);
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
        // 모양(켜짐·방식)은 바로 목표값, 수치만 보간한다.
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
            _current.flashlightStartRoundness = Mathf.Lerp(from.flashlightStartRoundness, to.flashlightStartRoundness, t);
            _current.flashlightShadowIntensity = Mathf.Lerp(from.flashlightShadowIntensity, to.flashlightShadowIntensity, t);
            _current.flashlightShadowSoftness = Mathf.Lerp(from.flashlightShadowSoftness, to.flashlightShadowSoftness, t);
            _current.flashlightShadowInnerFade = Mathf.Lerp(from.flashlightShadowInnerFade, to.flashlightShadowInnerFade, t);
            _current.flashlightShadowSpreadDistance = Mathf.Lerp(from.flashlightShadowSpreadDistance, to.flashlightShadowSpreadDistance, t);
            _current.flashlightShadowInset = Mathf.Lerp(from.flashlightShadowInset, to.flashlightShadowInset, t);
            _current.flashlightOffset = Mathf.Lerp(from.flashlightOffset, to.flashlightOffset, t);
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
            // 전역 조명이 없는 씬에서 매 프레임 찾지 않도록 1초 간격.
            if (d <= 0.001f || Time.unscaledTime < _nextGlobalLightSearch) return;
            _nextGlobalLightSearch = Time.unscaledTime + 1f;
            CaptureGlobalLight();
            if (!_globalLight) return;
        }

        _globalLight.intensity = _globalBaseIntensity * (1f - d);
        _globalLight.color = Color.Lerp(_globalBaseColor, Hue(_current.color), d);
    }

    // 플레이어 조명은 전역 조명이 잃은 만큼만 더해 빛 안이 원래 밝기가 되게 한다.
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
        // 이 조명에 붙은 위치 확인용 점(SpriteRenderer)은 빌려 쓰는 동안 숨긴다.
        _ambientOriginal.HideRenderers(_ambientLight);
        _ambientLight.pointLightOuterRadius = _current.playerLightRadius;
        _ambientLight.pointLightInnerRadius = _current.playerLightRadius * (1f - _current.softness);
        _ambientLight.pointLightOuterAngle = 360f;
        _ambientLight.pointLightInnerAngle = 360f;
        _ambientLight.intensity = LostIntensity(d) * _current.playerLightStrength;
        _ambientOriginal.SetHeight(_ambientLight, _current.playerLightHeight);
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

        if (!_flashLight)
        {
            _flashLight = CreateFlashLight("Flashlight (DarknessManager)");
            if (_flashLight)
            {
                _flashLight.lightCookieSprite = EnsureBeamSprite();
                _flashLight.lightType = Light2D.LightType.Sprite;
            }
        }
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
        Vector2 pivot = FollowFlashPivot(_trackedPlayer.transform.position);
        float offset = _current.flashlightMode == FlashlightMode.Around ? 0f : _current.flashlightOffset;
        ConfigureFlashLight(_flashLight, ClampBeforeWall(pivot, pivot + dir * offset), LostIntensity(d) * Mathf.Clamp01(intensity));
    }

    private void ConfigureFlashLight(Light2D light, Vector2 position, float intensity)
    {
        Sprite beam = EnsureBeamSprite();
        if (light.lightCookieSprite != beam) light.lightCookieSprite = beam;
        light.intensity = intensity;
        light.shadowsEnabled = _current.flashlightShadows;
        light.shadowIntensity = _current.flashlightShadowIntensity;

        // 광원 위치는 격자에 맞추지 않는다(천천히 걸을 때 그림자가 뚝뚝 끊긴다). 경계 고정은 셰이더의 월드 픽셀 판정이 맡는다.
        Transform tr = light.transform;
        tr.position = new Vector3(position.x, position.y, tr.position.z);
        tr.rotation = Quaternion.Euler(0f, 0f, _flashAngle - 90f);

        UpdateOcclusion(position, _flashAngle - 90f);
    }

    // ─── 손전등 빛 모양 그림 ─────────────────────────────────────
    // 흰색 + 알파(=밝기). 피벗이 빛 시작점이고 빛은 그림의 +y로 뻗는다. 모양 설정이 바뀔 때만 다시 그린다.

    private struct BeamShape
    {
        public bool Around;
        public float Range, Angle, AngleFalloff, DistanceFalloff, Curve, Roundness;

        public static BeamShape From(DarknessSettings s) => new()
        {
            Around = s.flashlightMode == FlashlightMode.Around,
            // 전환 중 매 프레임 다시 그리지 않도록 반올림한다.
            Range = Mathf.Round(Mathf.Max(0.1f, s.flashlightRange) * 20f) / 20f,
            Angle = Mathf.Round(s.flashlightAngle * 2f) / 2f,
            AngleFalloff = Mathf.Round(s.flashlightAngleFalloff * 50f) / 50f,
            DistanceFalloff = Mathf.Round(s.flashlightDistanceFalloff * 50f) / 50f,
            Curve = Mathf.Round(s.flashlightFalloffCurve * 50f) / 50f,
            Roundness = Mathf.Round(s.flashlightStartRoundness * 50f) / 50f,
        };

        public bool Same(BeamShape o) => Around == o.Around && Range == o.Range && Angle == o.Angle &&
            AngleFalloff == o.AngleFalloff && DistanceFalloff == o.DistanceFalloff && Curve == o.Curve && Roundness == o.Roundness;
    }

    private Sprite EnsureBeamSprite()
    {
        BeamShape shape = BeamShape.From(_current);
        if (_beamSprite && shape.Same(_beamShape)) return _beamSprite;
        _beamShape = shape;

        float range = shape.Range;
        float round = shape.Around ? 0f : shape.Roundness;
        float half = shape.Angle * 0.5f * Mathf.Deg2Rad;
        float halfTan = Mathf.Tan(half);

        float xMin, xMax, yMin, yMax;
        if (shape.Around) { xMin = yMin = -range; xMax = yMax = range; }
        else
        {
            float side = Mathf.Min(range + round, round + range * Mathf.Sin(half) / Mathf.Max(0.01f, Mathf.Cos(half)));
            side = Mathf.Min(side, range + round);
            xMin = -side; xMax = side; yMin = -round - 0.05f; yMax = range;
        }
        float worldW = xMax - xMin, worldH = yMax - yMin;
        int height = BeamTextureHeight;
        int width = Mathf.Clamp(Mathf.RoundToInt(height * worldW / worldH), 16, 1024);

        // Sprite.Create는 가로세로 PPU가 같아야 해서 반올림한 폭에 맞춰 범위를 다시 잡는다.
        worldW = width * worldH / height;
        xMin = -worldW * 0.5f;
        xMax = worldW * 0.5f;

        if (!_beamTexture || _beamTexture.width != width || _beamTexture.height != height)
        {
            if (_beamTexture) Destroy(_beamTexture);
            if (_beamBase) Destroy(_beamBase);
            if (_beamTarget) { _beamTarget.Release(); Destroy(_beamTarget); }
            // 세 텍스처 형식이 같아야 CopyTexture가 된다.
            _beamBase = NewBeamTexture(width, height, "Flashlight Beam Base (DarknessManager)");
            _beamTexture = NewBeamTexture(width, height, "Flashlight Beam (DarknessManager)");
            _beamTarget = new RenderTexture(width, height, 0, GraphicsFormat.R8G8B8A8_UNorm)
            {
                name = "Flashlight Beam Target (DarknessManager)",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave,
            };
        }

        float exponent = Mathf.Lerp(0.5f, 2.5f, shape.Curve);
        float angleSoft = Mathf.Max(0.02f, shape.AngleFalloff);
        float distSoft = Mathf.Max(0.02f, shape.DistanceFalloff);
        var pixels = new Color32[width * height];
        for (int py = 0; py < height; py++)
        {
            float y = yMin + (py + 0.5f) / height * worldH;
            for (int px = 0; px < width; px++)
            {
                float x = xMin + (px + 0.5f) / width * worldW;
                float d = Mathf.Sqrt(x * x + y * y);
                float value = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(range * (1f - distSoft), range, d));
                if (!shape.Around)
                {
                    float u = y >= 0f
                        ? Mathf.Abs(x) / Mathf.Max(0.0001f, round + y * halfTan)
                        : d / Mathf.Max(0.0001f, round);
                    value *= 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1f - angleSoft, 1f, u));
                }
                value = Mathf.Pow(Mathf.Clamp01(value), exponent);
                pixels[py * width + px] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(value * 255f));
            }
        }
        _beamBase.SetPixels32(pixels);
        _beamBase.Apply(false);
        _beamTexture.SetPixels32(pixels);
        _beamTexture.Apply(false);
        _beamOccluded = false;
        _beamBounds = new Vector4(xMin, yMin, worldW, worldH);

        if (_beamSprite) Destroy(_beamSprite);
        Vector2 pivot = new(-xMin / worldW, -yMin / worldH);
        _beamSprite = Sprite.Create(_beamTexture, new Rect(0, 0, width, height), pivot, height / worldH, 0, SpriteMeshType.FullRect);
        _beamSprite.name = "Flashlight Beam (DarknessManager)";
        return _beamSprite;
    }

    private static Texture2D NewBeamTexture(int width, int height, string textureName) =>
        new(width, height, GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None)
        {
            name = textureName,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.DontSave,
        };

    // ─── 벽 가림(1D 그림자 맵 + 셰이더 블러) ──────────────────────
    // CPU: 각도별 벽 거리 → GPU: 가림 마스크·블러·합성 → 조명 그림에 CopyTexture(CPU로 읽어 오지 않음).

    private void UpdateOcclusion(Vector2 origin, float rotationDegrees)
    {
        bool wanted = UsesShaderShadows && _current.flashlightShadowIntensity > 0.001f;
        if (!wanted || !EnsureOcclusionResources())
        {
            if (_beamOccluded && _beamBase && _beamTexture)
            {
                Graphics.CopyTexture(_beamBase, _beamTexture);
                _beamOccluded = false;
            }
            return;
        }

        BuildOcclusionMap(origin, rotationDegrees);
        _occlusionMap.SetPixelData(_occlusionDistances, 0);
        _occlusionMap.Apply(false);

        _occlusionMaterial.SetTexture(OcclusionMapId, _occlusionMap);
        _occlusionMaterial.SetVector(BoundsId, _beamBounds);
        _occlusionMaterial.SetFloat(SoftnessId, _current.flashlightShadowSoftness);
        _occlusionMaterial.SetFloat(ShadowStrengthId, _current.flashlightShadowIntensity);
        _occlusionMaterial.SetFloat(InsetId, Mathf.Max(0f, _current.flashlightShadowInset));
        _occlusionMaterial.SetFloat(InnerFadeId, _current.flashlightShadowInnerFade);
        _occlusionMaterial.SetFloat(SpreadDistanceId, _current.flashlightShadowSpreadDistance);
        _occlusionMaterial.SetFloat(RotationId, rotationDegrees * Mathf.Deg2Rad);
        _occlusionMaterial.SetVector(OriginId, origin);
        _occlusionMaterial.SetFloat(PixelsPerUnitId, _current.flashlightShadowPixelsPerUnit);
        var descriptor = _beamTarget.descriptor;
        descriptor.depthBufferBits = 0;
        var mask = RenderTexture.GetTemporary(descriptor);
        var horizontal = RenderTexture.GetTemporary(descriptor);
        var blurred = RenderTexture.GetTemporary(descriptor);
        mask.filterMode = horizontal.filterMode = blurred.filterMode = FilterMode.Bilinear;
        mask.wrapMode = horizontal.wrapMode = blurred.wrapMode = TextureWrapMode.Clamp;
        try
        {
            Graphics.Blit(_beamBase, mask, _occlusionMaterial, 0);
            _occlusionMaterial.SetVector("_BlurAxis", new Vector4(1f / _beamBounds.z, 0f, 0f, 0f));
            Graphics.Blit(mask, horizontal, _occlusionMaterial, 1);
            _occlusionMaterial.SetVector("_BlurAxis", new Vector4(0f, 1f / _beamBounds.w, 0f, 0f));
            Graphics.Blit(horizontal, blurred, _occlusionMaterial, 1);
            _occlusionMaterial.SetTexture("_VisibilityMask", mask);
            _occlusionMaterial.SetTexture("_BlurredMask", blurred);
            Graphics.Blit(_beamBase, _beamTarget, _occlusionMaterial, 2);
            Graphics.CopyTexture(_beamTarget, _beamTexture);
        }
        finally
        {
            _occlusionMaterial.SetTexture("_VisibilityMask", null);
            _occlusionMaterial.SetTexture("_BlurredMask", null);
            RenderTexture.ReleaseTemporary(mask);
            RenderTexture.ReleaseTemporary(horizontal);
            RenderTexture.ReleaseTemporary(blurred);
        }
        _beamOccluded = true;
    }

    private bool EnsureOcclusionResources()
    {
        if (_occlusionUnavailable) return false;
        if (_occlusionMaterial && _occlusionMap && _beamTarget) return true;

        if ((SystemInfo.copyTextureSupport & UnityEngine.Rendering.CopyTextureSupport.RTToTexture) == 0 ||
            !SystemInfo.SupportsTextureFormat(TextureFormat.RFloat))
        {
            _occlusionUnavailable = true;
            Debug.LogWarning("[DarknessManager] 이 기기는 GPU 텍스처 복사나 RFloat 텍스처를 지원하지 않아 손전등 벽 가림을 끕니다.");
            return false;
        }
        if (!_occlusionMaterial)
        {
            Shader shader = Resources.Load<Shader>(OcclusionShaderPath);
            if (!shader)
            {
                _occlusionUnavailable = true;
                Debug.LogError($"[DarknessManager] Resources/{OcclusionShaderPath}.shader를 찾을 수 없어 손전등 벽 가림을 끕니다.");
                return false;
            }
            _occlusionMaterial = new Material(shader) { hideFlags = HideFlags.DontSave };
        }
        if (!_occlusionMap)
        {
            _occlusionMap = new Texture2D(OcclusionBins, 2, TextureFormat.RFloat, false, true)
            {
                name = "Flashlight Occlusion (DarknessManager)",
                wrapMode = TextureWrapMode.Repeat, // 각도는 -π와 π가 이어진다
                // 보간하면 벽 모서리에서 거리 중간값이 생겨 빛이 샌다.
                filterMode = FilterMode.Point,
                hideFlags = HideFlags.DontSave,
            };
        }
        return _beamTarget;
    }

    // 각도 칸은 월드 기준(0 = 위). 광원과 같이 돌면 칸 경계가 벽 위를 쓸고 지나가 일렁인다.
    private void BuildOcclusionMap(Vector2 origin, float rotationDegrees)
    {
        float range = _current.flashlightRange + 2f * _current.flashlightShadowSoftness + 0.5f;
        var filter = new ContactFilter2D { useTriggers = true, useLayerMask = true, layerMask = WallLayerMask };
        for (int i = 0; i < OcclusionBins; i++)
        {
            float angle = (i + 0.5f) / OcclusionBins * Mathf.PI * 2f - Mathf.PI;
            float exit = CastOcclusion(origin, new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)), range, filter);
            _occlusionDistances[i] = exit;
            _occlusionDistances[OcclusionBins + i] = exit;
        }
    }

    private float CastOcclusion(Vector2 origin, Vector2 dir, float maxDistance, ContactFilter2D filter)
    {
        int count = Physics2D.Raycast(origin, dir, filter, _wallHits, maxDistance);
        float nearestExit = 1000f;
        for (int i = 0; i < count; i++)
        {
            RaycastHit2D hit = _wallHits[i];
            if (hit.distance <= 0.001f || OccluderHeight(hit.collider) < 0f) continue;
            nearestExit = Mathf.Min(nearestExit,
                Mathf.Max(hit.distance, VisualExitDistance(origin, dir, hit.collider, hit.distance, maxDistance)));
        }
        return ContinueThroughAdjacentWalls(origin, dir, nearestExit, maxDistance, filter);
    }

    // 벽 그림을 빠져나간 광선이 곧바로 이웃 벽 그림 안이면 그 끝까지 그림자를 미룬다. 이웃 벽 바닥면 콜라이더를
    // 안 지나 Raycast에 안 걸리므로, 없으면 나란히 붙은 엔티티 벽에서 그림자가 옆 벽 앞면을 덮는다.
    private float ContinueThroughAdjacentWalls(Vector2 origin, Vector2 dir, float exit, float maxDistance, ContactFilter2D filter)
    {
        const float probe = 0.02f;
        for (int guard = 0; guard < 8 && exit < maxDistance; guard++)
        {
            Vector2 p = origin + dir * (exit + probe);
            int count = Physics2D.Linecast(p, p - new Vector2(0f, MaxOccluderHeight), filter, _adjacentHits);
            float next = exit;
            for (int i = 0; i < count; i++)
            {
                RaycastHit2D hit = _adjacentHits[i];
                float height = OccluderHeight(hit.collider);
                if (height <= 0f || p.y - hit.point.y > height) continue;
                // 앞면은 아래를 본다 — 광원이 벽 뒤면 빛을 못 받는다(없으면 1칸 벽 뒤 그림자가 뚫린다).
                if (origin.y >= hit.point.y) continue;
                next = Mathf.Max(next, VisualExitDistance(origin, dir, hit.collider, exit + probe, maxDistance));
            }
            if (next <= exit + probe) break;
            exit = next;
        }
        return exit;
    }

    // 광선이 벽 그림(바닥면을 위로 높이만큼 늘린 모양)을 빠져나가는 거리. start는 그 벽에 들어간 거리.
    private float VisualExitDistance(Vector2 origin, Vector2 dir, Collider2D wall, float start, float maxDistance)
    {
        float height = OccluderHeight(wall);

        if (wall is not CompositeCollider2D && wall is not UnityEngine.Tilemaps.TilemapCollider2D)
        {
            Bounds b = wall.bounds;
            return RayExitAabb(origin, dir, new Vector2(b.min.x, b.min.y), new Vector2(b.max.x, b.max.y + height));
        }

        // 타일맵 벽은 모양이 제각각이라 광선을 따라 나아가며 찾는다.
        float limit = maxDistance + height;
        float lastInside = start;
        for (float t = start; t < limit; t += VisualMarchStep)
        {
            Vector2 p = origin + dir * t;
            if (!InsideWallVisual(wall, p, height))
                return t <= start ? t : RefineVisualExit(wall, origin, dir, height, lastInside, t);
            lastInside = t;
        }
        if (lastInside < limit && !InsideWallVisual(wall, origin + dir * limit, height))
            return RefineVisualExit(wall, origin, dir, height, lastInside, limit);
        // 끝까지 벽 안: 출구를 지어내지 않고, "벽 없음"(1000)과 구분되는 먼 값을 준다.
        return limit + VisualMarchStep;
    }

    private float RefineVisualExit(Collider2D wall, Vector2 origin, Vector2 dir, float height, float inside, float outside)
    {
        for (int i = 0; i < 4; i++)
        {
            float mid = (inside + outside) * 0.5f;
            if (InsideWallVisual(wall, origin + dir * mid, height)) inside = mid;
            else outside = mid;
        }
        return outside;
    }

    // p에서 아래로 높이만큼 내린 선분이 바닥면에 닿으면 벽 그림 안. 점 몇 개만 찍으면 얇은 벽에서 판정에 구멍이 난다.
    private bool InsideWallVisual(Collider2D wall, Vector2 p, float height)
    {
        if (wall.OverlapPoint(p)) return true;
        if (height <= 0f) return false;
        var filter = new ContactFilter2D { useTriggers = true, useLayerMask = true, layerMask = 1 << wall.gameObject.layer };
        int count = Physics2D.Raycast(p, Vector2.down, filter, _visualHits, height);
        for (int i = 0; i < count; i++)
            if (_visualHits[i].collider == wall) return true;
        return false;
    }

    // 반직선이 상자를 빠져나가는 거리. 만나지 않으면 0.
    private static float RayExitAabb(Vector2 origin, Vector2 dir, Vector2 min, Vector2 max)
    {
        float tNear = 0f, tFar = float.MaxValue;
        for (int axis = 0; axis < 2; axis++)
        {
            float o = origin[axis], d = dir[axis];
            if (Mathf.Abs(d) < 1e-5f)
            {
                if (o < min[axis] || o > max[axis]) return 0f;
                continue;
            }
            float t1 = (min[axis] - o) / d, t2 = (max[axis] - o) / d;
            tNear = Mathf.Max(tNear, Mathf.Min(t1, t2));
            tFar = Mathf.Min(tFar, Mathf.Max(t1, t2));
        }
        return tFar >= tNear ? tFar : 0f;
    }

    // 벽 앞면 높이(ZCollider2D.height). 빛을 안 가리는 계단·경사는 -1.
    private readonly Dictionary<Collider2D, float> _occluderHeights = new();
    private float OccluderHeight(Collider2D collider)
    {
        if (_occluderHeights.TryGetValue(collider, out float cached)) return cached;
        float height = 0f;
        if (collider.TryGetComponent(out ZCollider2D z))
            height = z.isStair || z.useSlopeDU || z.useSlopeRL ? -1f : Mathf.Max(0f, z.height);
        _occluderHeights[collider] = height;
        return height;
    }

    // 광원이 벽 안에 들어가면 빛이 벽 너머로 샌다 — 벽에 닿으면 그 앞에서 멈춘다.
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

    // 8방향 입력을 그대로 쓰면 대각선 전환이 툭툭 끊겨서 빠르게 돌려 맞춘다.
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

    // 손전등이 플레이어 자신을 비추면 안 되는데(뒤통수가 밝아짐) Light2D는 정렬 레이어로만 대상을 고를 수 있고
    // 플레이어·벽이 모두 Entity라 못 뺀다. 그래서 몸 스프라이트마다 투명한 대역(selfShadows 실루엣)을 이 매니저 아래에 둔다.
    // URP 12는 castsShadows=false 캐스터가 혼자면 안 그려서 부모에 CompositeShadowCaster2D를 둔다.
    private void CollectPlayerSprites(Player player)
    {
        _playerSprites.Clear();
        foreach (SpriteRenderer sprite in player.GetComponentsInChildren<SpriteRenderer>(true))
            if (!sprite.GetComponent<Light2D>()) _playerSprites.Add(sprite); // 조명 위치 표시용 점은 뺀다

        if (!_shadowProxyRoot)
        {
            _shadowProxyRoot = new GameObject("Player Flashlight Shadow");
            _shadowProxyRoot.transform.SetParent(transform, false);
            // 빈 그룹 목록을 미리 만들지 않으면 URP가 NRE를 낸다.
            WallShadowCaster.AddShadowGroup(_shadowProxyRoot);
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

    // Light2D의 적용 정렬 레이어는 코드로 지정할 API가 없어서 올바르게 설정된 주변 빛을 복제한다.
    private Light2D CreateFlashLight(string lightName)
    {
        if (!_ambientLight) return null;
        bool wasActive = _ambientLight.gameObject.activeSelf;
        _ambientLight.gameObject.SetActive(false); // 복제본이 활성 상태로 한 프레임 나오지 않게
        Light2D light = Instantiate(_ambientLight, _ambientLight.transform.parent);
        _ambientLight.gameObject.SetActive(wasActive);
        light.name = lightName;
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
        _occluderHeights.Clear();
        _ambientLight = null;
        _flashLight = null;
        _facing = null;
        _trackedPlayer = null;
        _flashAngleInitialized = false;
        _flashPivotInitialized = false;
    }

    private Vector2 FollowFlashPivot(Vector2 target)
    {
        float sharpness = _current.flashlightFollowSharpness;
        if (sharpness <= 0f || !_flashPivotInitialized ||
            (target - _flashPivot).sqrMagnitude > FlashlightSnapDistance * FlashlightSnapDistance)
        {
            _flashPivot = target;
            _flashPivotInitialized = true;
            return _flashPivot;
        }
        _flashPivot = Vector2.Lerp(_flashPivot, target, 1f - Mathf.Exp(-sharpness * Time.unscaledDeltaTime));
        return _flashPivot;
    }

    private static Color Hue(Color c)
    {
        float max = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
        return max > 0.0001f ? new Color(c.r / max, c.g / max, c.b / max, 1f) : Color.white;
    }

    // 빌려 쓴 Player 조명을 원래 상태로 돌려놓기 위한 값.
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
