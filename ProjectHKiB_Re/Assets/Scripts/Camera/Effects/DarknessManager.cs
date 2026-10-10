using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
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
    [Tooltip("손전등을 켠 채로 시작한다. 게임 중에는 F키(손전등 액션)로 켜고 끈다.")]
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
    [Tooltip("광원을 바라보는 방향으로 띄우는 거리(월드 유닛). 벽 그림자 판 앞에서 멈춘다. Facing 전용.")]
    [Min(0f)] public float flashlightOffset = 0.6f;

    [Header("손전등 그림자")]
    [Tooltip("벽이 손전등 빛을 가린다. 벽 모양은 WallShadowCaster의 그림자 판(MapDarkness가 맵 벽에 자동으로 단다)을 쓴다.")]
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
    [Tooltip("빛이 벽 가장자리부터 그림자 판 안으로 들어가 밝히는 깊이(월드 유닛). 이 깊이 동안 부드럽게 어두워져 " +
             "큰 벽 덩어리 안쪽까지 빛이 파고들지 않는다. 광원보다 위 벽(하단)은 Wall Bottom Scale 원도 함께 쓴다. " +
             "판 밖으로 솟은 벽 앞면(그림이 판보다 큰 벽)은 이 값과 상관없이 밝다. 셰이더 그림자 전용.")]
    [Min(0f)] public float flashlightWallLightDepth = 1f;
    [Tooltip("벽 하단 조명 원의 크기 — 손전등 범위(Flashlight Range)에 곱하는 배율. 1이면 손전등 범위만큼. " +
             "광원보다 위에 있는 벽(광원이 벽 아래쪽 = 벽 하단이 보이는 쪽)은 가장자리 띠 대신 이 원 안을 밝힌다" +
             "(바깥 30%에서 부드럽게 어두워짐). 광원보다 아래 벽은 Wall Light Depth 띠를 쓴다. 0이면 끈다. 셰이더 그림자 전용.")]
    [Range(0f, 2f)] public float flashlightWallBottomScale = 1f;
    [Tooltip("그림자·벽 밝기 판정을 월드 픽셀 격자에 맞춘다(스프라이트 PPU와 같게). 이동 중 경계가 벽 테두리에서 미끄러지지 않고, 벽 그라데이션도 픽셀 단위로 칠해진다. 0이면 끈다.")]
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
///   - 벽 가림: 각도별 벽 거리(1D 그림자 맵) + FlashlightOcclusion 셰이더(URP 그림자는 경계를 못 흐려서).
///     벽 모양은 WallShadowCaster의 그림자 판·벽 그림에서 읽는다 — URP 그림자 방식과 같은 모양
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

    [Tooltip("손전등 벽 가림을 Burst Job으로 병렬 계산한다. 끄면 메인 스레드에서 같은 계산을 한다(프로파일러 비교용).")]
    [SerializeField] private bool _burstOcclusion = true;

    private const float FlashlightTurnSpeed = 720f;
    // 이보다 멀리 한 번에 움직이면(순간이동) 보간 없이 바로 옮긴다.
    private const float FlashlightSnapDistance = 1.5f;
    // 광원과 벽(그림자 판) 사이 틈. 0이면 광원이 판 경계에 걸려 빛이 샌다.
    private const float WallGap = 0.1f;

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
    private Material _occlusionMaterial;
    private RenderTexture _beamTarget;
    private Texture2D _occlusionMap;
    // 가림 맵 한 줄에 각도 칸 OcclusionBins개, 4줄: 0 = 그림자 시작 거리, 1 = 벽(판)에 들어간 거리,
    // 2 = 판을 벗어난(또는 깊이 제한에 걸린) 거리, 3 = 벽 하단 가중치(0~1, 벽이 광원보다 위일수록 1).
    // 셰이더가 1~2 사이를 벽 그라데이션(가장자리 띠 ↔ 하단이면 광원 중심 원)으로 칠한다.
    private const int OcclusionRows = 4;
    private readonly float[] _occlusionDistances = new float[OcclusionBins * OcclusionRows];
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
    private static readonly int WallLightDepthId = Shader.PropertyToID("_WallLightDepth");
    private static readonly int WallBottomRadiusId = Shader.PropertyToID("_WallBottomRadius");
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
        UnityEngine.SceneManagement.SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    // 맵이 내려가면 그 맵 벽의 캐시(높이·격자)를 버린다 — 격자는 맵 크기만큼 메모리를 쓴다.
    private void OnSceneUnloaded(UnityEngine.SceneManagement.Scene _)
    {
        _wallGrids.Clear();
        DisposeOcclusionJobGrids();
    }

    private void OnApplicationQuit() => _isQuitting = true;

    private void OnDestroy()
    {
        if (_instance != this) return;
        UnityEngine.SceneManagement.SceneManager.sceneUnloaded -= OnSceneUnloaded;
        RestoreGlobalLight();
        ReleasePlayerLights();
        if (_beamSprite) Destroy(_beamSprite);
        if (_beamTexture) Destroy(_beamTexture);
        if (_beamBase) Destroy(_beamBase);
        if (_beamTarget) { _beamTarget.Release(); Destroy(_beamTarget); }
        if (_occlusionMap) Destroy(_occlusionMap);
        if (_occlusionMaterial) Destroy(_occlusionMaterial);
        DisposeOcclusionJobData();
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
            _current.flashlightWallLightDepth = Mathf.Lerp(from.flashlightWallLightDepth, to.flashlightWallLightDepth, t);
            _current.flashlightWallBottomScale = Mathf.Lerp(from.flashlightWallBottomScale, to.flashlightWallBottomScale, t);
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
        ConfigureFlashLight(_flashLight, ClampBeforeWall(pivot, dir, offset), LostIntensity(d) * Mathf.Clamp01(intensity));
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

        // 광원·방향·설정·주변 벽이 그대로면 지난 결과를 그대로 쓴다(가만히 서 있을 때 비용 0).
        CollectOccluders(origin);
        var key = new OcclusionKey
        {
            Origin = origin,
            Rotation = rotationDegrees,
            Bounds = _beamBounds,
            ParamsA = new Vector4(_current.flashlightShadowSoftness, _current.flashlightShadowIntensity,
                                  _current.flashlightShadowInset, _current.flashlightShadowInnerFade),
            ParamsB = new Vector4(_current.flashlightShadowSpreadDistance, _current.flashlightShadowPixelsPerUnit,
                                  _current.flashlightWallLightDepth, WallBottomRadius),
            Walls = _occluderStateHash,
        };
        if (_beamOccluded && key.Same(_lastOcclusionKey)) return;
        _lastOcclusionKey = key;

        if (_burstOcclusion)
            RunOcclusionJob(origin, rotationDegrees);
        else
        {
            BuildOcclusionMap(origin, rotationDegrees);
            _occlusionMap.SetPixelData(_occlusionDistances, 0);
        }
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
        _occlusionMaterial.SetFloat(WallLightDepthId, Mathf.Max(0.001f, _current.flashlightWallLightDepth));
        _occlusionMaterial.SetFloat(WallBottomRadiusId, WallBottomRadius);
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
            _occlusionMap = new Texture2D(OcclusionBins, OcclusionRows, TextureFormat.RFloat, false, true)
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
    // 칸마다 빛 그림(_beamBounds) 밖으로는 볼 필요가 없어서, 광선 길이를 그 방향의 그림 끝까지로 줄인다
    // (부채꼴 옆·뒤 방향은 몇 칸이면 끝난다).
    // Burst를 끈 경우의 메인 스레드 경로 — FlashlightOcclusionJob과 같은 계산이다.
    private void BuildOcclusionMap(Vector2 origin, float rotationDegrees)
    {
        const float margin = 0.5f;
        float rot = rotationDegrees * Mathf.Deg2Rad;
        float c = Mathf.Cos(rot), s = Mathf.Sin(rot);
        Vector2 boundsMin = new(_beamBounds.x, _beamBounds.y);
        Vector2 boundsMax = boundsMin + new Vector2(_beamBounds.z, _beamBounds.w);
        for (int i = 0; i < OcclusionBins; i++)
        {
            float angle = (i + 0.5f) / OcclusionBins * Mathf.PI * 2f - Mathf.PI;
            Vector2 dir = new(Mathf.Sin(angle), Mathf.Cos(angle));
            Vector2 local = new(c * dir.x + s * dir.y, -s * dir.x + c * dir.y);
            float reach = RayExitAabb(Vector2.zero, local, boundsMin, boundsMax) + margin;
            float exit = CastOcclusion(origin, dir, reach, out float wallEntry, out float footEnd, out float bottom);
            _occlusionDistances[i] = exit;
            _occlusionDistances[OcclusionBins + i] = wallEntry;
            _occlusionDistances[2 * OcclusionBins + i] = footEnd;
            _occlusionDistances[3 * OcclusionBins + i] = bottom;
        }
    }

    // ─── 벽 모양: WallShadowCaster의 그림자 판 + 벽 그림 ─────────────────
    // 콜라이더(물리)는 그림과 다를 수 있어서(ZCollider 높이 1인데 그림은 다르다 등) 쓰지 않는다. URP 그림자 방식과
    // 같은 것을 본다 — 빛을 가리는 건 바닥면 그림자 판(ShadowCaster2D, 에디터에서 고칠 수 있다), 그 너머라도
    // 벽 그림(렌더러) 안은 밝게 남는다(URP의 실루엣과 같은 역할).
    // 판정: 광선이 판에 처음 들어간 뒤 벽 그림을 벗어나는 곳부터 그림자. 벗어나자마자 이웃 벽 그림 안이면(나란히 붙은 벽)
    // 그 끝까지 미룬다 — 단 광원이 그 벽 뒤(바닥면보다 위)면 앞면이 빛을 못 받으므로 미루지 않는다.

    private struct OcclusionKey
    {
        public Vector2 Origin;
        public float Rotation;
        public Vector4 Bounds, ParamsA;
        public Vector4 ParamsB;
        public int Walls;

        public bool Same(OcclusionKey o) => Origin == o.Origin && Rotation == o.Rotation && Bounds == o.Bounds &&
                                             ParamsA == o.ParamsA && ParamsB == o.ParamsB && Walls == o.Walls;
    }

    private readonly List<WallVisualGrid> _gridOccluders = new();
    private int _occluderStateHash;
    private OcclusionKey _lastOcclusionKey;

    // 빛 그림이 닿는 범위 안의 벽 격자만 모은다.
    private void CollectOccluders(Vector2 origin)
    {
        _gridOccluders.Clear();
        Vector2 min = new(_beamBounds.x, _beamBounds.y), max = min + new Vector2(_beamBounds.z, _beamBounds.w);
        float reach = Mathf.Sqrt(Mathf.Max(min.sqrMagnitude, max.sqrMagnitude,
                                           new Vector2(min.x, max.y).sqrMagnitude, new Vector2(max.x, min.y).sqrMagnitude)) + 1f;

        int hash = 17;
        foreach (WallShadowCaster wall in WallShadowCaster.Active)
        {
            WallVisualGrid grid = GetWallGrid(wall);
            if (grid == null) continue;
            float dx = Mathf.Max(grid.MinX - origin.x, 0f, origin.x - (grid.MinX + grid.Width * grid.Cell));
            float dy = Mathf.Max(grid.MinY - origin.y, 0f, origin.y - (grid.MinY + grid.Height * grid.Cell));
            if (dx * dx + dy * dy > reach * reach) continue;
            _gridOccluders.Add(grid);
            hash = hash * 31 + grid.Version;
        }
        _occluderStateHash = hash;
    }

    // wallEntry~footEnd: 가장 가까운 벽의 판 안을 지나는 구간 — 셰이더가 여기를 벽 가장자리부터 안쪽으로 어두워지게 칠한다.
    // bottom: 벽 하단 가중치 — 광선이 벽에 닿은 곳이 광원보다 위일수록 1(광원이 벽 아래쪽 = 벽 하단이 보인다).
    // 들어온 가장자리(아래/옆)로 가르면 계단 모양 벽에서 이웃 광선끼리 뒤바뀌어 줄무늬가 생겨서, 높이 차로 부드럽게 정한다.
    // 하단이면 판 안을 가장자리 깊이와 하단 원 반경 중 먼 곳까지 밝힌다.
    private const float WallBottomBlend = 0.5f;

    private float CastOcclusion(Vector2 origin, Vector2 dir, float maxDistance, out float wallEntry, out float footEnd, out float bottom)
    {
        float nearestExit = 1000f;
        wallEntry = footEnd = 1000f;
        bottom = 0f;
        foreach (WallVisualGrid grid in _gridOccluders)
        {
            float hit = grid.FirstFootprintEntry(origin, dir, maxDistance);
            if (hit <= 0.001f) continue; // 안 만남
            float weight = Mathf.SmoothStep(0f, 1f, dir.y * hit / WallBottomBlend);
            float depth = WallLightDepthFor(hit, weight);
            float exit = Mathf.Max(hit, grid.ExitDistance(origin, dir, hit, maxDistance + grid.MaxRise, NoExitOvershoot,
                                                           depth, out float runEnd));
            if (exit >= nearestExit) continue;
            nearestExit = exit;
            wallEntry = hit;
            footEnd = runEnd;
            bottom = weight;
        }
        return ContinueThroughAdjacentWalls(origin, dir, nearestExit, maxDistance);
    }

    // 벽 하단 조명 원 반경(월드 유닛) = 손전등 범위 × 배율.
    private float WallBottomRadius => _current.flashlightRange * _current.flashlightWallBottomScale;

    // 판 안으로 빛이 들어가는 깊이. 하단이면 광원 중심 원(반경 WallBottomRadius)이 더 멀리 닿으면 거기까지.
    private float WallLightDepthFor(float entry, float bottom) =>
        bottom > 0f ? Mathf.Max(_current.flashlightWallLightDepth, WallBottomRadius - entry)
             : _current.flashlightWallLightDepth;

    private float ContinueThroughAdjacentWalls(Vector2 origin, Vector2 dir, float exit, float maxDistance)
    {
        const float probe = 0.02f;
        for (int guard = 0; guard < 8 && exit < maxDistance; guard++)
        {
            Vector2 p = origin + dir * (exit + probe);
            // 판 안이면(깊이 제한에 걸려 벽 덩어리 속에서 끊긴 경우) 잇지 않는다 — 이으면 덩어리 속으로 계속 파고든다.
            bool inFootprint = false;
            foreach (WallVisualGrid grid in _gridOccluders) inFootprint |= grid.InFootprint(p);
            if (inFootprint) break;
            float next = exit;
            foreach (WallVisualGrid grid in _gridOccluders)
            {
                if (!grid.InVisual(p, out float footY)) continue;
                if (origin.y >= footY) continue; // 광원이 그 벽 뒤 — 앞면은 빛을 못 받는다
                next = Mathf.Max(next, grid.ExitDistance(origin, dir, exit + probe, maxDistance + grid.MaxRise, NoExitOvershoot,
                                                         _current.flashlightWallLightDepth, out _));
            }
            if (next <= exit + probe) break;
            exit = next;
        }
        return exit;
    }

    // ─── Burst Job 경로 ─────────────────────────────────────────
    // CastOcclusion과 같은 계산을 FlashlightOcclusionJob으로 칸마다 병렬로 돌린다. 격자 데이터(수 MB)는 격자가
    // 바뀔 때만 네이티브 배열로 복사해 두고, 매 프레임은 격자 정보만 채운다.

    private NativeArray<ushort> _jobBelow;
    private NativeArray<byte> _jobClear;
    private NativeArray<byte> _jobVisual;
    private NativeArray<FlashlightOcclusionJob.Grid> _jobGrids;
    private NativeArray<float> _jobDistances;
    private readonly Dictionary<int, int> _jobGridOffsets = new(); // 격자 Version → 네이티브 배열 안 시작 위치

    private void RunOcclusionJob(Vector2 origin, float rotationDegrees)
    {
        EnsureJobGridData();

        if (!_jobGrids.IsCreated || _jobGrids.Length < Mathf.Max(1, _gridOccluders.Count))
        {
            if (_jobGrids.IsCreated) _jobGrids.Dispose();
            _jobGrids = new NativeArray<FlashlightOcclusionJob.Grid>(Mathf.Max(16, _gridOccluders.Count * 2), Allocator.Persistent);
        }
        for (int i = 0; i < _gridOccluders.Count; i++)
        {
            WallVisualGrid g = _gridOccluders[i];
            _jobGrids[i] = new FlashlightOcclusionJob.Grid
            {
                MinX = g.MinX, MinY = g.MinY, Cell = g.Cell, MaxRise = g.MaxRise,
                Width = g.Width, Height = g.Height, Offset = _jobGridOffsets[g.Version],
            };
        }
        if (!_jobDistances.IsCreated)
            _jobDistances = new NativeArray<float>(OcclusionBins * OcclusionRows, Allocator.Persistent);

        float rot = rotationDegrees * Mathf.Deg2Rad;
        Vector2 boundsMin = new(_beamBounds.x, _beamBounds.y);
        var job = new FlashlightOcclusionJob
        {
            Grids = _jobGrids, Below = _jobBelow, Clear = _jobClear, Visual = _jobVisual,
            GridCount = _gridOccluders.Count,
            Origin = origin, Cos = Mathf.Cos(rot), Sin = Mathf.Sin(rot),
            BoundsMin = boundsMin, BoundsMax = boundsMin + new Vector2(_beamBounds.z, _beamBounds.w),
            Margin = 0.5f, Overshoot = NoExitOvershoot,
            FootDepth = _current.flashlightWallLightDepth, BottomRadius = WallBottomRadius,
            BottomBlend = WallBottomBlend,
            Bins = OcclusionBins, Distances = _jobDistances,
        };
        job.Schedule(OcclusionBins, 64).Complete();
        _occlusionMap.SetPixelData(_jobDistances, 0);
    }

    // 지금 쓰는 격자들이 네이티브 배열에 다 있으면 그대로, 하나라도 없으면(새 맵·모양 변경) 통째로 다시 복사한다.
    private void EnsureJobGridData()
    {
        bool upToDate = _jobBelow.IsCreated && _jobGridOffsets.Count == _gridOccluders.Count;
        for (int i = 0; upToDate && i < _gridOccluders.Count; i++)
            upToDate = _jobGridOffsets.ContainsKey(_gridOccluders[i].Version);
        if (upToDate) return;

        DisposeOcclusionJobGrids();
        int total = 0;
        foreach (WallVisualGrid g in _gridOccluders) total += g.Below.Length;
        _jobBelow = new NativeArray<ushort>(total, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        _jobClear = new NativeArray<byte>(total, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        _jobVisual = new NativeArray<byte>(total, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        int offset = 0;
        foreach (WallVisualGrid g in _gridOccluders)
        {
            NativeArray<ushort>.Copy(g.Below, 0, _jobBelow, offset, g.Below.Length);
            NativeArray<byte>.Copy(g.Clear, 0, _jobClear, offset, g.Clear.Length);
            NativeArray<byte>.Copy(g.Visual, 0, _jobVisual, offset, g.Visual.Length);
            _jobGridOffsets[g.Version] = offset;
            offset += g.Below.Length;
        }
    }

    private void DisposeOcclusionJobGrids()
    {
        if (_jobBelow.IsCreated) _jobBelow.Dispose();
        if (_jobClear.IsCreated) _jobClear.Dispose();
        if (_jobVisual.IsCreated) _jobVisual.Dispose();
        _jobGridOffsets.Clear();
    }

    private void DisposeOcclusionJobData()
    {
        DisposeOcclusionJobGrids();
        if (_jobGrids.IsCreated) _jobGrids.Dispose();
        if (_jobDistances.IsCreated) _jobDistances.Dispose();
    }

    // ─── 벽 격자 ────────────────────────────────────────────────
    // 벽마다 그림자 판(바닥면)과 벽 그림을 1/16 칸으로 한 번 래스터화해 두고 정수 DDA로 걷는다. 물리 질의는 없다.
    // 판 모양·위치·벽 그림이 바뀌면(WallShadowCaster.ShapeVersion) 다시 만든다.

    // 스프라이트 PPU(16)와 같게 — 타일 경계가 칸 경계에 정확히 걸린다.
    private const float WallGridCell = 1f / 16f;
    // 광선이 끝까지 벽 그림 안일 때 "벽 없음"(1000)과 구분되게 끝보다 조금 더 먼 값을 준다.
    private const float NoExitOvershoot = 0.1f;
    private static int _wallGridVersion;

    private sealed class WallGridEntry
    {
        public WallVisualGrid Grid;
        public int SourceVersion;
        public int CheckedFrame = -1;
    }

    private readonly Dictionary<WallShadowCaster, WallGridEntry> _wallGrids = new();
    private readonly List<Vector2[]> _polygonBuffer = new();
    private readonly List<Rect> _rectBuffer = new();

    private sealed class WallVisualGrid
    {
        public float MinX, MinY, Cell;
        public int Width, Height;
        // 벽 그림이 바닥면보다 가장 높이 솟은 거리 — 광선 길이 상한에 더한다.
        public float MaxRise;
        // 칸마다 아래로 가장 가까운 바닥면 칸까지의 칸 수(0 = 바닥면, ushort.MaxValue = 없음).
        public ushort[] Below;
        // 칸마다 가장 가까운 바닥면 칸까지의 체비셰프 칸 거리(255에서 자름). 빈 공간을 건너뛰는 데 쓴다.
        public byte[] Clear;
        // 1 = 벽 그림(빛을 받는 앞면) 또는 바닥면.
        public byte[] Visual;
        public int Version;

        private bool InGrid(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

        // 정수 칸 DDA. 걸을 때마다 t에서 칸 번호를 다시 계산하면 안 된다 — 축에 거의 나란한 광선은 부동소수 오차로
        // 같은 칸이 다시 나와 t가 멈추고 무한 루프가 된다(손전등을 켜면 에디터가 멈췄다). 칸 번호는 정수로만 옮긴다.
        private struct Walk
        {
            public int X, Y, StepX, StepY;
            public float NextX, NextY, DeltaX, DeltaY;

            // 다음 칸으로 한 걸음 옮기고, 그 칸에 들어간 거리를 돌려준다.
            public float Step()
            {
                if (NextX < NextY) { float t = NextX; NextX += DeltaX; X += StepX; return t; }
                float ty = NextY; NextY += DeltaY; Y += StepY; return ty;
            }
        }

        // 바닥면 경계 위의 점은 바깥 칸으로 반올림될 수 있어 광선 방향으로 살짝 밀어 넣고 칸을 고른다.
        private const float Nudge = 0.0001f;
        private Walk BeginWalk(Vector2 origin, Vector2 dir, float t)
        {
            Vector2 p = origin + dir * (t + Nudge);
            var w = new Walk
            {
                X = Mathf.FloorToInt((p.x - MinX) / Cell),
                Y = Mathf.FloorToInt((p.y - MinY) / Cell),
                StepX = dir.x > 0f ? 1 : -1,
                StepY = dir.y > 0f ? 1 : -1,
            };
            float ax = Mathf.Abs(dir.x), ay = Mathf.Abs(dir.y);
            w.DeltaX = ax > 1e-6f ? Cell / ax : float.PositiveInfinity;
            w.DeltaY = ay > 1e-6f ? Cell / ay : float.PositiveInfinity;
            w.NextX = ax > 1e-6f ? (MinX + (dir.x > 0f ? w.X + 1 : w.X) * Cell - origin.x) / dir.x : float.PositiveInfinity;
            w.NextY = ay > 1e-6f ? (MinY + (dir.y > 0f ? w.Y + 1 : w.Y) * Cell - origin.y) / dir.y : float.PositiveInfinity;
            // 오차로 경계가 t보다 뒤에 잡혀도 거리가 줄어들지 않게.
            w.NextX = Mathf.Max(w.NextX, t);
            w.NextY = Mathf.Max(w.NextY, t);
            return w;
        }

        // 걸음 수 상한 — 정수 DDA는 격자를 벗어나면 끝나지만, 혹시라도 에디터를 멈추게 하지 않도록.
        private int MaxSteps => 4 * (Width + Height) + 16;

        public bool InFootprint(Vector2 p)
        {
            int x = Mathf.FloorToInt((p.x - MinX) / Cell), y = Mathf.FloorToInt((p.y - MinY) / Cell);
            return InGrid(x, y) && Clear[y * Width + x] == 0;
        }

        /// <summary>p에서 dir로 나아가 바닥면을 벗어나는 거리. p가 이미 밖이면 0, limit 안에 못 벗어나면 limit.</summary>
        public float FootprintExitDistance(Vector2 p, Vector2 dir, float limit)
        {
            float t = 0f;
            Walk w = BeginWalk(p, dir, 0f);
            for (int guard = MaxSteps; guard > 0 && t < limit; guard--)
            {
                if (!InGrid(w.X, w.Y) || Clear[w.Y * Width + w.X] != 0) return t;
                t = w.Step();
            }
            return limit;
        }

        /// <summary>
        /// 광선이 처음 바닥면에 들어가는 거리. 없으면 -1.
        /// 광원이 이미 바닥면 안이면 그 바닥면을 빠져나간 뒤의 다음 바닥면을 찾는다 — 맵 벽이 판 하나로 이어져 있어
        /// "광원이 판 안이면 이 판은 무시"로 하면 맵 전체 그림자가 꺼진다.
        /// </summary>
        public float FirstFootprintEntry(Vector2 origin, Vector2 dir, float maxDistance)
        {
            Vector2 gridMin = new(MinX, MinY), gridMax = new(MinX + Width * Cell, MinY + Height * Cell);
            float tEnter = 0f, tLeave = maxDistance;
            for (int axis = 0; axis < 2; axis++)
            {
                float o = origin[axis], d = dir[axis];
                if (Mathf.Abs(d) < 1e-6f)
                {
                    if (o < gridMin[axis] || o > gridMax[axis]) return -1f;
                    continue;
                }
                float t1 = (gridMin[axis] - o) / d, t2 = (gridMax[axis] - o) / d;
                tEnter = Mathf.Max(tEnter, Mathf.Min(t1, t2));
                tLeave = Mathf.Min(tLeave, Mathf.Max(t1, t2));
            }
            if (tEnter > tLeave) return -1f;

            float t = tEnter;
            Walk w = BeginWalk(origin, dir, t);
            bool leaving = tEnter <= 0f && InGrid(w.X, w.Y) && Clear[w.Y * Width + w.X] == 0;
            for (int guard = MaxSteps; guard > 0 && t <= tLeave; guard--)
            {
                if (InGrid(w.X, w.Y))
                {
                    int clear = Clear[w.Y * Width + w.X];
                    if (leaving)
                    {
                        if (clear == 0) { t = w.Step(); continue; }
                        leaving = false;
                    }
                    if (clear == 0) return t;
                    // 체비셰프 거리 k 칸이면 (k-1)칸 이내 어디에도 바닥면이 없다 — 유클리드 거리로 그만큼 건너뛰어도 안전하다.
                    if (clear >= 2)
                    {
                        t += (clear - 1) * Cell;
                        w = BeginWalk(origin, dir, t);
                        continue;
                    }
                }
                else if (t > tEnter + Cell) return -1f; // 격자를 빠져나갔다(들어오는 첫 칸은 오차로 밖일 수 있어 봐준다)
                t = w.Step();
            }
            return -1f;
        }

        /// <summary>
        /// 광선이 벽 그림을 빠져나가는 거리(여기부터 그림자). start에서 이미 밖이면 start. 끝까지 벽 그림 안이면 limit + overshoot.
        /// 바닥면(판) 안을 footDepth만큼 지나면 그림 안이어도 거기서 끊는다 — 맵 벽이 큰 덩어리 하나면 빛이 반대편 끝까지 파고들었다.
        /// footEnd: start부터 이어진 첫 판 구간이 끝난(또는 깊이 제한에 걸린) 거리 — 셰이더의 벽 그라데이션 끝.
        /// </summary>
        public float ExitDistance(Vector2 origin, Vector2 dir, float start, float limit, float overshoot, float footDepth, out float footEnd)
        {
            float t = start, inFoot = 0f;
            bool firstRun = true;
            footEnd = start;
            Walk w = BeginWalk(origin, dir, t);
            for (int guard = MaxSteps; guard > 0 && t < limit; guard--)
            {
                if (!InGrid(w.X, w.Y)) return t;
                int i = w.Y * Width + w.X;
                if (Visual[i] == 0) return t;
                bool foot = Clear[i] == 0;
                float next = w.Step();
                if (foot)
                {
                    if (inFoot + (next - t) >= footDepth)
                    {
                        float cut = t + (footDepth - inFoot);
                        if (firstRun) footEnd = cut;
                        return cut;
                    }
                    inFoot += next - t;
                    if (firstRun) footEnd = next;
                }
                else firstRun = false;
                t = next;
            }
            return limit + overshoot;
        }

        /// <summary>p가 벽 그림 안인가. footY는 그 아래 가장 가까운 바닥면의 윗변(없으면 -무한) — 광원이 벽 뒤인지 판정용.</summary>
        public bool InVisual(Vector2 p, out float footY)
        {
            footY = float.NegativeInfinity;
            int x = Mathf.FloorToInt((p.x - MinX) / Cell), y = Mathf.FloorToInt((p.y - MinY) / Cell);
            if (!InGrid(x, y)) return false;
            int i = y * Width + x;
            if (Visual[i] == 0) return false;
            int below = Below[i];
            if (below != ushort.MaxValue) footY = below == 0 ? p.y : MinY + (y - below + 1) * Cell;
            return true;
        }
    }

    private WallVisualGrid GetWallGrid(WallShadowCaster wall)
    {
        if (!_wallGrids.TryGetValue(wall, out WallGridEntry entry))
            _wallGrids[wall] = entry = new WallGridEntry();
        if (entry.CheckedFrame == Time.frameCount) return entry.Grid;
        entry.CheckedFrame = Time.frameCount;

        int version = wall.ShapeVersion;
        if (entry.Grid != null && entry.SourceVersion == version) return entry.Grid;
        entry.SourceVersion = version;
        entry.Grid = BuildWallGrid(wall);
        return entry.Grid;
    }

    private WallVisualGrid BuildWallGrid(WallShadowCaster wall)
    {
        _polygonBuffer.Clear();
        _rectBuffer.Clear();
        wall.GetFootprintPolygons(_polygonBuffer);
        if (_polygonBuffer.Count == 0) return null;
        wall.GetVisualRects(_rectBuffer);

        // 범위: 판 + 벽 그림.
        Vector2 lo = new(float.MaxValue, float.MaxValue), hi = new(float.MinValue, float.MinValue);
        float footTop = float.MinValue;
        foreach (Vector2[] polygon in _polygonBuffer)
            foreach (Vector2 p in polygon)
            {
                lo = Vector2.Min(lo, p);
                hi = Vector2.Max(hi, p);
                footTop = Mathf.Max(footTop, p.y);
            }
        foreach (Rect r in _rectBuffer)
        {
            lo = Vector2.Min(lo, r.min);
            hi = Vector2.Max(hi, r.max);
        }

        float cell = WallGridCell;
        float minX = Mathf.Floor(lo.x / cell) * cell;
        float minY = Mathf.Floor(lo.y / cell) * cell;
        int width = Mathf.CeilToInt((hi.x - minX) / cell) + 1;
        int height = Mathf.CeilToInt((hi.y - minY) / cell) + 1;
        int size = width * height;

        // 바닥면: 판마다 칸 중심 기준 스캔라인 채우기(판 하나는 짝홀 규칙, 판끼리는 합집합).
        var footprint = new bool[size];
        var crossings = new List<float>();
        foreach (Vector2[] polygon in _polygonBuffer)
        {
            float pMin = float.MaxValue, pMax = float.MinValue;
            foreach (Vector2 p in polygon) { pMin = Mathf.Min(pMin, p.y); pMax = Mathf.Max(pMax, p.y); }
            int y0 = Mathf.Max(0, Mathf.FloorToInt((pMin - minY) / cell));
            int y1 = Mathf.Min(height - 1, Mathf.CeilToInt((pMax - minY) / cell));
            for (int y = y0; y <= y1; y++)
            {
                float wy = minY + (y + 0.5f) * cell;
                crossings.Clear();
                for (int k = 0; k < polygon.Length; k++)
                {
                    Vector2 a = polygon[k], b = polygon[(k + 1) % polygon.Length];
                    if ((a.y <= wy) != (b.y <= wy))
                        crossings.Add(a.x + (wy - a.y) * (b.x - a.x) / (b.y - a.y));
                }
                crossings.Sort();
                for (int k = 0; k + 1 < crossings.Count; k += 2)
                {
                    int from = Mathf.Max(0, Mathf.CeilToInt((crossings[k] - minX) / cell - 0.5f));
                    int to = Mathf.Min(width - 1, Mathf.CeilToInt((crossings[k + 1] - minX) / cell - 0.5f) - 1);
                    for (int x = from; x <= to; x++) footprint[y * width + x] = true;
                }
            }
        }

        // 벽 그림: 바닥면 + 렌더러 사각형(칸 중심이 안에 든 칸).
        var visual = new byte[size];
        for (int i = 0; i < size; i++) visual[i] = footprint[i] ? (byte)1 : (byte)0;
        foreach (Rect r in _rectBuffer)
        {
            int x0 = Mathf.Max(0, Mathf.CeilToInt((r.xMin - minX) / cell - 0.5f));
            int x1 = Mathf.Min(width - 1, Mathf.CeilToInt((r.xMax - minX) / cell - 0.5f) - 1);
            int y0 = Mathf.Max(0, Mathf.CeilToInt((r.yMin - minY) / cell - 0.5f));
            int y1 = Mathf.Min(height - 1, Mathf.CeilToInt((r.yMax - minY) / cell - 0.5f) - 1);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++) visual[y * width + x] = 1;
        }

        // 아래로 가장 가까운 바닥면까지의 칸 수.
        var below = new ushort[size];
        for (int x = 0; x < width; x++)
        {
            int lastFoot = int.MinValue / 2;
            for (int y = 0; y < height; y++)
            {
                if (footprint[y * width + x]) lastFoot = y;
                below[y * width + x] = (ushort)Mathf.Min(y - lastFoot, ushort.MaxValue);
            }
        }

        // 바닥면까지의 체비셰프 거리 — 두 번 훑기(앞쪽 이웃 → 뒤쪽 이웃)로 정확히 나온다.
        var clear = new byte[size];
        for (int i = 0; i < size; i++) clear[i] = footprint[i] ? (byte)0 : byte.MaxValue;
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int i = y * width + x, d = clear[i];
                if (d == 0) continue;
                if (x > 0) d = Mathf.Min(d, clear[i - 1] + 1);
                if (y > 0)
                {
                    d = Mathf.Min(d, clear[i - width] + 1);
                    if (x > 0) d = Mathf.Min(d, clear[i - width - 1] + 1);
                    if (x < width - 1) d = Mathf.Min(d, clear[i - width + 1] + 1);
                }
                clear[i] = (byte)Mathf.Min(d, byte.MaxValue);
            }
        for (int y = height - 1; y >= 0; y--)
            for (int x = width - 1; x >= 0; x--)
            {
                int i = y * width + x, d = clear[i];
                if (d == 0) continue;
                if (x < width - 1) d = Mathf.Min(d, clear[i + 1] + 1);
                if (y < height - 1)
                {
                    d = Mathf.Min(d, clear[i + width] + 1);
                    if (x < width - 1) d = Mathf.Min(d, clear[i + width + 1] + 1);
                    if (x > 0) d = Mathf.Min(d, clear[i + width - 1] + 1);
                }
                clear[i] = (byte)Mathf.Min(d, byte.MaxValue);
            }

        return new WallVisualGrid
        {
            MinX = minX, MinY = minY, Cell = cell,
            Width = width, Height = height,
            MaxRise = Mathf.Max(0f, hi.y - footTop),
            Below = below, Clear = clear, Visual = visual,
            Version = ++_wallGridVersion,
        };
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

    // 광원이 벽 안에 들어가면 빛이 벽 너머로 샌다 — 그림자 판(격자) 기준으로 막는다. 콜라이더는 판과 다를 수 있고,
    // Linecast는 기준점이 이미 콜라이더 안이면 거리 0으로 걸려 광원을 벽 안에 그대로 두었다(맵 벽이 판 하나라
    // 그러면 맵 전체 그림자가 꺼졌다).
    //   1) 기준점(발밑)이 판 안이면 가장 가까운 판 밖으로 꺼낸다(뒤쪽 → 상하좌우 중 짧은 쪽, PushOutLimit 이내).
    //   2) 바라보는 방향으로 offset만큼 띄우다가 판 WallGap 앞에서 멈춘다.
    private const float PushOutLimit = 1.5f;
    private static readonly Vector2[] PushOutAxes = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

    private Vector2 ClampBeforeWall(Vector2 pivot, Vector2 dir, float offset)
    {
        CollectNearbyGrids(pivot, offset + PushOutLimit + WallGap);
        if (_nearbyGrids.Count == 0) return pivot + dir * offset;

        Vector2 from = pivot;
        if (InAnyFootprint(from))
        {
            float best = float.MaxValue;
            Vector2 bestDir = Vector2.zero;
            TryPushOut(from, -dir, ref best, ref bestDir);
            if (best > PushOutLimit)
                foreach (Vector2 axis in PushOutAxes) TryPushOut(from, axis, ref best, ref bestDir);
            if (best <= PushOutLimit) from += bestDir * (best + WallGap);
        }

        if (offset < 0.0001f) return from;
        float allowed = offset;
        foreach (WallVisualGrid grid in _nearbyGrids)
        {
            float hit = grid.FirstFootprintEntry(from, dir, offset + WallGap);
            if (hit >= 0f) allowed = Mathf.Min(allowed, hit - WallGap);
        }
        return from + dir * Mathf.Max(0f, allowed);
    }

    // p에서 dir로 나아가 모든 판 밖으로 나오는 거리(겹친 판·이웃 판을 이어서 빠져나간다).
    private void TryPushOut(Vector2 p, Vector2 dir, ref float best, ref Vector2 bestDir)
    {
        float t = 0f;
        for (int guard = 0; guard < 8 && t <= PushOutLimit; guard++)
        {
            float next = t;
            foreach (WallVisualGrid grid in _nearbyGrids)
                next = Mathf.Max(next, t + grid.FootprintExitDistance(p + dir * t, dir, PushOutLimit));
            if (next <= t) break;
            t = next;
        }
        if (t < best) { best = t; bestDir = dir; }
    }

    private bool InAnyFootprint(Vector2 p)
    {
        foreach (WallVisualGrid grid in _nearbyGrids)
            if (grid.InFootprint(p)) return true;
        return false;
    }

    private readonly List<WallVisualGrid> _nearbyGrids = new();

    private void CollectNearbyGrids(Vector2 p, float radius)
    {
        _nearbyGrids.Clear();
        foreach (WallShadowCaster wall in WallShadowCaster.Active)
        {
            WallVisualGrid grid = GetWallGrid(wall);
            if (grid == null) continue;
            float dx = Mathf.Max(grid.MinX - p.x, 0f, p.x - (grid.MinX + grid.Width * grid.Cell));
            float dy = Mathf.Max(grid.MinY - p.y, 0f, p.y - (grid.MinY + grid.Height * grid.Cell));
            if (dx * dx + dy * dy <= radius * radius) _nearbyGrids.Add(grid);
        }
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
