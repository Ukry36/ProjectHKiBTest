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
    [Tooltip("어두워지는 곡선. 낮으면 완만하게, 높으면 가운데·가까운 곳에 빛이 몰리고 빨리 어두워진다.")]
    [Range(0f, 1f)] public float flashlightFalloffCurve = 0.5f;
    [Tooltip("빛이 시작되는 쪽(플레이어 쪽)의 둥근 정도(반지름, 월드 유닛). 0이면 부채꼴 꼭짓점이 뾰족하고, 키우면 그 반지름의 " +
             "반원으로 둥글게 시작한다. Around 모드에서는 쓰지 않는다.")]
    [Min(0f)] public float flashlightStartRoundness = 0.35f;
    [Tooltip("깜빡임 세기. 0이면 안정, 1이면 거의 꺼질 듯 흔들린다(배터리 연출).")]
    [Range(0f, 1f)] public float flicker;
    [Tooltip("손전등 광원을 바라보는 방향으로 띄우는 거리(월드 유닛). 발밑이 아니라 캐릭터 앞 바닥에서 빛이 시작되는 느낌. " +
             "벽에 붙어 있으면 벽 앞에서 멈춘다. Around 모드에서는 쓰지 않는다.")]
    [Min(0f)] public float flashlightOffset = 0.6f;

    [Header("손전등 그림자")]
    [Tooltip("벽(Wall 레이어, 계단·경사 제외)에 손전등 빛이 가려진다. 맵의 벽에는 MapDarkness가 WallShadowCaster를 자동으로 단다.")]
    public bool flashlightShadows = true;
    [Tooltip("그림자 진하기. 1이면 가려진 곳이 완전히 어둡고, 낮출수록 빛이 조금 비친다.")]
    [Range(0f, 1f)] public float flashlightShadowIntensity = 0.9f;
    [Tooltip("켜면 셰이더(FlashlightOcclusion)로 벽 그림자 경계를 부드럽게 흐린다. 끄면 원래 방식(URP 그림자, 경계가 또렷함)으로 " +
             "가린다. 셰이더를 못 쓰는 환경이면 켜 둬도 원래 방식으로 동작한다. 아래 Softness·Spread Distance·Inset은 켰을 때만 쓴다.")]
    public bool flashlightShaderShadows = true;
    [Tooltip("가림 마스크를 가로·세로로 흐리는 폭(월드 유닛). 원본 손전등의 테두리는 흐리지 않는다. 0이면 블러가 없다.")]
    [Min(0f)] public float flashlightShadowSoftness = 0.6f;
    [Tooltip("접촉부의 선명한 가림에서 2D 블러로 전환하는 거리. Spread Distance와 이 값 중 큰 거리를 사용한다.")]
    [Min(0.01f)] public float flashlightShadowInnerFade = 0.6f;
    [Tooltip("벽 뒤에서 2D 블러가 완전히 적용되는 거리(월드 유닛). Inner Fade보다 작으면 Inner Fade를 사용한다.")]
    [Min(0.01f)] public float flashlightShadowSpreadDistance = 1.5f;
    [Tooltip("그림자를 벽 그림 안쪽으로 밀어 넣는 폭(월드 유닛). 해상도 오차로 벽과 그림자 사이에 생기는 미세한 틈을 덮는다. " +
             "0이면 벽 그림 끝에서 정확히 시작하고, 키울수록 벽 가장자리가 그만큼 그림자에 겹친다.")]
    [Min(0f)] public float flashlightShadowInset = 0f;
    [Tooltip("그림자 판정을 월드 픽셀 격자(1칸당 이 수)에 맞춘다. 스프라이트 PPU(16)와 같게 두면 움직일 때 그림자 경계가 " +
             "벽 테두리 픽셀에 고정되어 자글거리지 않는다. 0이면 끈다(경계가 매끈하지만 이동 중 조금씩 미끄러진다).")]
    [Min(0f)] public float flashlightShadowPixelsPerUnit = 16f;

    [Header("부드러움")]
    [Tooltip("손전등이 플레이어를 따라가는 빠르기(1/초). 플레이어 위치는 물리 틱(62.5Hz)마다만 바뀌어 고주사율 화면에서 그림자가 " +
             "뚝뚝 움직이므로 매 프레임 이 속도로 쫓아가 그 사이를 메운다. 클수록 바로 붙고(끊김↑), 작을수록 부드럽지만 늦게 따라온다. " +
             "0이면 보간하지 않는다. 비용은 거의 없다(그림자는 원래 매 프레임 계산한다).")]
    [Min(0f)] public float flashlightFollowSharpness = 40f;

    public DarknessSettings Clone() => (DarknessSettings)MemberwiseClone();
}

/// <summary>
/// URP 2D 라이팅으로 암전과 손전등을 만든다. 새 렌더링을 그리지 않고 이미 씬에 있는 조명만 조절한다.
///   - 어둠: System 씬의 Global Light2D 밝기를 낮추고 색조를 입힌다.
///   - 플레이어 주변: Player에 달린 Point Light2D(평소엔 꺼져 있음)를 켜서 반경만 맞춘다.
///   - 손전등: 그 Point Light2D를 복제해 Sprite Light2D로 바꾸고, 빛 모양 그림(시작이 둥근 부채꼴 + 방사형 감쇠)을
///     설정값으로 직접 그려 넣는다. Point 조명은 꼭짓점이 늘 뾰족해서 시작 부분을 둥글게 만들 수 없기 때문이다.
///   - 벽 가림: URP 12 그림자(ShadowCaster2D)는 경계를 흐릴 수 없어서 쓰지 않는다. 매 프레임 광원에서 각도별로 광선을 쏴
///     "각도별 벽 거리"(1D 그림자 맵)를 만들고, FlashlightOcclusion 셰이더가 빛 모양 그림에 가려진 곳을 흐린 경계로 칠한다.
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
    // 손전등 광원이 따라가는 빠르기는 DarknessSettings.flashlightFollowSharpness.
    // 이보다 멀리 한 번에 움직이면(순간이동·맵 이동) 쫓아가지 않고 바로 옮긴다.
    private const float FlashlightSnapDistance = 1.5f;
    // 광원이 벽 앞에서 멈출 때 벽과 남겨 둘 틈. 0이면 광원이 그림자 판 경계에 걸려 빛이 샌다.
    private const float WallGap = 0.1f;
    private static readonly int WallLayerMask = 1 << 3; // Wall

    // 지금 걸린 값(전환 중엔 중간값). _target은 전환이 끝났을 때의 값.
    private DarknessSettings _current = new() { darkness = 0f };
    private DarknessSettings _target = new() { darkness = 0f };
    private float _flashOn;          // 손전등 켜짐 정도 0~1 — 켜고 끌 때 툭 끊기지 않게 보간한다.
    private float _flashAngle = -90f; // 월드 기준 각도(도). 기본은 아래(정면).
    private bool _flashAngleInitialized;
    private Vector2 _flashPivot;      // 보간된 손전등 기준점(플레이어 위치를 매 프레임 쫓아간다)
    private bool _flashPivotInitialized;
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
    // 손전등 빛 모양 그림. 모양을 정하는 설정이 바뀔 때만 다시 그린다.
    private Texture2D _beamBase;     // 가림 없는 원본 빛 모양(CPU에서 그림)
    private Texture2D _beamTexture;  // 조명이 실제로 쓰는 그림 — 매 프레임 원본 × 가림을 GPU에서 복사해 넣는다
    private Sprite _beamSprite;
    private BeamShape _beamShape;
    private Vector4 _beamBounds;     // 그림이 덮는 로컬 영역: xMin, yMin, 폭, 높이
    // 빛 그림 한 픽셀이 화면 픽셀보다 작아야 광원이 돌거나 움직일 때 그림자 경계가 일렁이지 않는다.
    private const int BeamTextureHeight = 512;

    // 벽 가림(1D 그림자 맵)
    private const string OcclusionShaderPath = "ScreenEffects/FlashlightOcclusion";
    // 각도 칸(월드 기준, 0.18°). 칸이 굵으면 일직선 벽 경계가 칸마다 계단지고, 움직일 때 그 계단이 미끄러져 자글거린다.
    // 전체 각도를 탐색해 부채꼴 밖의 가림 정보도 블러에 사용할 수 있게 한다.
    private const int OcclusionBins = 2048;
    // 타일맵 벽 그림 밖으로 나가는 지점을 찾을 때 한 번에 나아가는 거리(월드 유닛).
    private const float VisualMarchStep = 0.1f;
    private Material _occlusionMaterial;
    private RenderTexture _beamTarget;
    private Texture2D _occlusionMap;
    // 벽 그림의 원본 끝 거리. Inset은 마스크 생성 시 적용한다.
    private readonly float[] _occlusionDistances = new float[OcclusionBins * 2];
    private bool _beamOccluded;      // 지금 조명 그림에 가림이 칠해져 있나(끌 때 원본으로 되돌리기 위해)
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
    // 이웃 벽 그림을 찾을 때 아래로 훑는 거리 — 맵의 어떤 벽 높이(ZCollider2D.height)보다 커야 한다.
    private const float MaxOccluderHeight = 10f;
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

    /// <summary>
    /// 손전등 벽 그림자를 셰이더(흐린 경계)로 그리는 중인가. 아니면 URP 그림자(WallShadowCaster의 벽 바닥면 그림자 판)를 쓴다.
    /// </summary>
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
        // 그림자는 손전등에만 켠다 — 그림자 비용은 조명 수에 비례하고, 은은한 주변 빛은 가려져도 티가 안 난다.
        light.shadowsEnabled = _current.flashlightShadows;
        light.shadowIntensity = _current.flashlightShadowIntensity;

        // 광원 위치는 격자에 맞추지 않는다 — 맞추면 천천히 걸을 때 1/16칸을 넘을 때만 그림자가 바뀌어 뚝뚝 끊겨 보였다.
        // 경계가 벽 테두리에서 미끄러지는 건 셰이더가 월드 픽셀 단위로 판정해서 막는다(flashlightShadowPixelsPerUnit).
        Transform tr = light.transform;
        tr.position = new Vector3(position.x, position.y, tr.position.z);
        // 빛 모양 그림은 위쪽(local up)을 향해 그려져 있다.
        tr.rotation = Quaternion.Euler(0f, 0f, _flashAngle - 90f);

        UpdateOcclusion(position, _flashAngle - 90f);
    }

    // ─── 손전등 빛 모양 그림 ─────────────────────────────────────
    // Sprite Light2D는 그림의 모양 그대로 빛난다. 흰색 + 알파로 밝기를 담으면 블렌드 방식과 상관없이 알파가 곧 밝기다
    // (Light2D Overlap Operation이 Additive면 color*=cookie*cookie.a 후 One One, Alpha Blend면 color*=cookie 후 SrcAlpha로 섞는다).
    // 그림의 원점(피벗)이 빛 시작 지점이고, 빛은 그림의 위쪽(+y)으로 뻗는다.

    private struct BeamShape
    {
        public bool Around;
        public float Range, Angle, AngleFalloff, DistanceFalloff, Curve, Roundness;

        public static BeamShape From(DarknessSettings s) => new()
        {
            Around = s.flashlightMode == FlashlightMode.Around,
            // 전환 중 매 프레임 다시 그리지 않도록 눈에 안 띄는 단위로 반올림한다.
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

        // 빛이 닿을 수 있는 범위만 그린다. 부채꼴: 시작 반원(아래 round) ~ 앞쪽 range, 좌우는 range + round.
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

        // Sprite.Create uses one pixels-per-unit value for both axes. Match the
        // CPU beam and shader bounds to the actual width after pixel rounding.
        // Preserve the centered beam and its world-space origin (sprite pivot).
        worldW = width * worldH / height;
        xMin = -worldW * 0.5f;
        xMax = worldW * 0.5f;

        if (!_beamTexture || _beamTexture.width != width || _beamTexture.height != height)
        {
            if (_beamTexture) Destroy(_beamTexture);
            if (_beamBase) Destroy(_beamBase);
            if (_beamTarget) { _beamTarget.Release(); Destroy(_beamTarget); }
            // 원본·대상·렌더 타깃을 같은 형식으로 맞춰야 GPU 복사(CopyTexture)가 된다.
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

        // 곡선: 낮으면 완만(지수 < 1), 높으면 가운데·가까운 곳에 빛이 몰린다(지수 > 1).
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
                    // 가로 위치를 그 높이의 빛 폭으로 나눈 값(가운데 0 ~ 가장자리 1). 시작 쪽(y<0)은 반지름 round의 반원.
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
        _beamTexture.SetPixels32(pixels); // 가림을 못 칠하는 환경에서도 원본 모양은 보이도록
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
    // 1) 광원에서 각도별로 광선을 쏴 가장 가까운 벽까지 거리를 한 줄 텍스처에 담는다(CPU, 광선 수백 번).
    // 2) 선명한 가림 마스크를 만들고 가로·세로 가우시안 블러 후 접촉부를 보존하여 합성한다(GPU).
    // 3) 결과를 조명이 쓰는 그림에 GPU끼리 복사한다(CopyTexture) — CPU로 읽어 오지 않는다.

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
        // Cartesian visibility mask, then separable Gaussian blur. Never blur the beam itself.
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
                // 보간하면 벽 모서리에서 가까운 벽·먼 벽 거리의 중간값이 생겨 빛이 샌다. 부드러움은 셰이더의 다중 샘플이 맡는다.
                filterMode = FilterMode.Point,
                hideFlags = HideFlags.DontSave,
            };
        }
        return _beamTarget;
    }

    // 각도 칸은 월드 기준이다(칸 i = 월드 각도 w, 0 = 월드 위쪽). 광원이 돌아도 칸 경계가 벽 위를 쓸고 지나가지 않게 —
    // 칸이 광원과 같이 돌면 같은 벽을 매 프레임 다른 위치에서 재서 경계가 일렁인다. 셰이더가 그림 각도를 월드 각도로 바꿔 읽는다.
    private void BuildOcclusionMap(Vector2 origin, float rotationDegrees)
    {
        // Full circle: no cone boundary or rounded-start cutoff can create false shadow fragments.
        float range = _current.flashlightRange + 2f * _current.flashlightShadowSoftness + 0.5f;
        var filter = new ContactFilter2D { useTriggers = true, useLayerMask = true, layerMask = WallLayerMask };
        for (int i = 0; i < OcclusionBins; i++)
        {
            float angle = (i + 0.5f) / OcclusionBins * Mathf.PI * 2f - Mathf.PI;
            float exit = CastOcclusion(origin, new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)), range, filter);
            // No angular min dilation: it changes corner topology and causes moving stair steps.
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
            // Compare all visual exits, not just the nearest footprint.
            nearestExit = Mathf.Min(nearestExit,
                Mathf.Max(hit.distance, VisualExitDistance(origin, dir, hit.collider, hit.distance, maxDistance)));
        }
        return ContinueThroughAdjacentWalls(origin, dir, nearestExit, maxDistance, filter);
    }

    // 벽 그림을 옆면으로 빠져나간 광선이 곧바로 이웃 벽의 그림(앞면) 안으로 들어가면, 그 벽 그림 끝까지 그림자를 미룬다.
    // 광선은 이웃 벽의 바닥면 콜라이더를 지나지 않고 그림 영역만 지나므로 Raycast에 걸리지 않는다 — 그대로 두면
    // 따로 놓인 엔티티 벽이 나란히 붙어 있을 때(예: 회전 벽 양옆) 그림자가 옆 벽 앞면을 덮었다. 타일맵 벽은 한 콜라이더라 해당 없음.
    private float ContinueThroughAdjacentWalls(Vector2 origin, Vector2 dir, float exit, float maxDistance, ContactFilter2D filter)
    {
        const float probe = 0.02f;
        for (int guard = 0; guard < 8 && exit < maxDistance; guard++)
        {
            Vector2 p = origin + dir * (exit + probe);
            // 이 점 바로 아래(벽 높이 안쪽)에 바닥면이 있는 벽 = 이 점이 그 벽 그림 안이다.
            int count = Physics2D.Linecast(p, p - new Vector2(0f, MaxOccluderHeight), filter, _adjacentHits);
            float next = exit;
            for (int i = 0; i < count; i++)
            {
                RaycastHit2D hit = _adjacentHits[i];
                float height = OccluderHeight(hit.collider);
                if (height <= 0f || p.y - hit.point.y > height) continue;
                // 벽 앞면은 아래(카메라 쪽)를 본다 — 광원이 그 벽 바닥면보다 위(벽 뒤)면 앞면은 빛을 못 받는다.
                // 이 조건이 없으면 1칸 벽 바로 뒤에 다른 벽의 앞면 그림 영역이 겹칠 때 벽 뒤 그림자가 뚫렸다.
                if (origin.y >= hit.point.y) continue;
                next = Mathf.Max(next, VisualExitDistance(origin, dir, hit.collider, exit + probe, maxDistance));
            }
            if (next <= exit + probe) break;
            exit = next;
        }
        return exit;
    }

    // 광선이 벽 그림 영역(바닥면을 위로 높이만큼 늘린 모양)을 빠져나가는 거리. start는 광선이 그 벽에 들어간 거리.
    private float VisualExitDistance(Vector2 origin, Vector2 dir, Collider2D wall, float start, float maxDistance)
    {
        float height = OccluderHeight(wall);

        // 엔티티 벽(박스 등 하나짜리 콜라이더)은 바닥면 사각형을 위로 늘린 상자로 보고 바로 계산한다.
        if (wall is not CompositeCollider2D && wall is not UnityEngine.Tilemaps.TilemapCollider2D)
        {
            Bounds b = wall.bounds;
            return RayExitAabb(origin, dir, new Vector2(b.min.x, b.min.y), new Vector2(b.max.x, b.max.y + height));
        }

        // 타일맵 벽은 모양이 제각각이라 광선을 따라 조금씩 나아가며 벽 그림 밖으로 나가는 지점을 찾는다.
        // 한 지점이 벽 그림 안인지는 "그 점이나 그 점에서 아래로 높이 안쪽의 점이 바닥면에 들어 있나"로 본다.
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
        // Still inside the wall: do not invent a visible exit at the search cutoff.
        // This finite blocker stays beyond every point used by the beam/blur, while
        // remaining distinct from the 1000 sentinel meaning no blocking wall.
        return limit + VisualMarchStep;
    }

    // 0.1칸 마칭에서 마지막으로 벽 안이던 점과 첫 바깥 점 사이를 좁혀, 보호 거리의 바깥쪽 오차를 줄인다.
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

    // 점 p에서 아래로 높이만큼 내려가는 선분이 이 벽 바닥면에 닿으면 벽 그림 안이다.
    // 예전엔 p, p-높이/2, p-높이 세 점만 찍어 봐서, 바닥면 두께가 높이/2(1.5칸)보다 얇은 타일맵 벽은
    // 바닥면 위 1.5~2칸 띠가 "벽 밖"으로 판정됐다 — 그 띠에서 그림자가 벽 앞면을 파고들어 건물 위 그림자가 자글거렸다.
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

    // 반직선이 상자를 빠져나가는 거리(2D 슬랩). 상자와 만나지 않으면 0.
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

    // 벽이면 그 앞면 높이(ZCollider2D.height), 빛을 가리지 않는 것(계단·경사)이면 -1. 콜라이더마다 한 번만 계산한다.
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
            // 대역은 손전등을 켜기 전까지 꺼져 있어 그룹에 등록되지 않는다 — 빈 목록을 미리 만들어 두지 않으면 URP가 NRE를 낸다.
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
        // 프레임 속도와 무관하게 같은 속도로 따라가도록 지수 감쇠로 섞는다.
        _flashPivot = Vector2.Lerp(_flashPivot, target, 1f - Mathf.Exp(-sharpness * Time.unscaledDeltaTime));
        return _flashPivot;
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
