using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// 꿈 잠식 단계가 오를 때의 화면·소리 연출을 한 곳에서 맡는다 — 포스트프로세스, 화면 노이즈,
/// 순간 슬로우모션, 불협화음.
/// DreamErosionModule은 단계 판정과 세이브만 책임지고 연출은 전부 여기로 넘긴다. 단계마다
/// 효과음만 재생하던 방식을 대체한 것이라, 새 연출을 붙일 때도 모듈이 아니라 이 클래스를 고친다.
/// </summary>
public sealed class DreamErosionPresentation : MonoBehaviour
{
    // TimeManager의 연출 배속은 키별로 등록된다. 다른 연출과 동시에 걸려도 서로를 지우지 않도록
    // 이 클래스 전용 키를 쓴다 — 해제할 때도 같은 키로만 지운다.
    private const string SlowMotionKey = "DreamErosionImpact";

    private Volume _volume;
    private ColorAdjustments _colorAdjustments;
    private Vignette _vignette;
    private FilmGrain _filmGrain;

    // 씬의 Volume은 우리 소유가 아니다. 건드리기 전 값을 한 번만 보관해 두고 Clear에서 되돌린다.
    private bool _hasOriginalPostProcess;
    private float _originalPostExposure;
    private float _originalContrast;
    private float _originalSaturation;
    private Color _originalColorFilter;
    private float _originalVignetteIntensity;
    private Color _originalVignetteColor;
    private float _originalGrainIntensity;

    private AudioSource _dissonanceSource;
    private AudioClip _generatedClip;
    private Coroutine _impactRoutine;

    // 지금 걸려 있어야 할 단계. 씬이 바뀌면 이 값을 보고 지속 연출을 다시 건다.
    private int _level;

    private void OnEnable() => SceneManager.sceneLoaded += HandleSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= HandleSceneLoaded;

    /// <summary>단계가 **오른 순간**의 연출. 지속분에 더해 효과음·불협화음·슬로우모션까지 친다.</summary>
    public void ApplyLevel(int level, EffectAudioCue cue)
    {
        if (level <= 0) { Clear(); return; }
        _level = level;
        ApplyPersistent();
        // 배선된 단계별 효과음이 먼저 울리고, 그 위에 절차 생성 불협화음이 겹친다.
        cue?.Play();
        PlayDissonance(level);
        // 연달아 단계가 오르면 앞의 충격 연출은 버린다 — 슬로우모션이 겹치면 해제 시점이 어긋난다.
        if (_impactRoutine != null) StopCoroutine(_impactRoutine);
        _impactRoutine = StartCoroutine(ImpactRoutine(level));
    }

    /// <summary>
    /// 세이브 복원처럼 "단계가 오른 순간"이 아닐 때 지속 연출만 현재 단계에 맞춘다.
    /// 잠식 단계는 지속 상태라 화면은 계속 물들어 있어야 하지만, 충격 연출(효과음·불협화음·
    /// 슬로우모션)은 사건이라 다시 재생하면 안 된다 — 그래서 ApplyLevel과 진입점을 나눈다.
    /// </summary>
    public void RestoreLevel(int level)
    {
        if (level <= 0) { Clear(); return; }
        _level = level;
        ApplyPersistent();
    }

    /// <summary>걸어 둔 연출을 전부 원상 복구한다. 모듈 파괴·잠식 해제·0단계 복원이 여기로 들어온다.</summary>
    public void Clear()
    {
        _level = 0;
        if (_impactRoutine != null) { StopCoroutine(_impactRoutine); _impactRoutine = null; }
        // 코루틴이 중간에 끊겨도 배속은 반드시 풀어 준다 — 안 그러면 게임 전체가 느린 채로 남는다.
        TimeManager timeManager = GameManager.instance != null ? GameManager.instance.timeManager : null;
        timeManager?.ClearPresentationSpeedMultiplier(SlowMotionKey);
        ScreenEffectManager.Instance?.StopNoise();
        RestorePostProcess();
        if (_dissonanceSource != null) _dissonanceSource.Stop();
        if (_generatedClip != null) { Destroy(_generatedClip); _generatedClip = null; }
    }

    // 단계가 유지되는 동안 화면에 남아 있어야 하는 것들. 사건이 아니라 상태다.
    private void ApplyPersistent()
    {
        ApplyPostProcess(_level);
        ApplyNoise(_level);
    }

    // 씬이 바뀌면 이전 Volume은 파괴되고 새 씬의 프로필은 원래 값이라 연출이 통째로 사라진다.
    // 세이브 로드 직후에는 목적지 맵이 아직 안 올라와 있을 수도 있어서, 복원 호출만으로는 부족하다.
    // 잠식이 걸려 있는 한 씬이 올라올 때마다 지속분을 다시 건다 — 충격 연출은 재생하지 않는다.
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_level > 0) ApplyPersistent();
    }

    private void ApplyPostProcess(int level)
    {
        Volume activeVolume = FindObjectOfType<Volume>();
        if (activeVolume == null) return; // URP Volume이 없는 씬에서는 조용히 건너뛴다.

        // 씬이 바뀌어 다른 Volume을 잡게 되면, 이전 Volume을 먼저 되돌린 뒤 갈아탄다.
        if (_volume != activeVolume)
        {
            RestorePostProcess();
            _volume = activeVolume;
            _hasOriginalPostProcess = false;
        }

        VolumeProfile profile = _volume.profile;
        if (profile == null) return;
        // 프로필에 오버라이드가 없으면 만들어 쓴다. 단계 연출이 프로필을 손으로 맞춰 둔 씬에서만
        // 동작하면 안 되기 때문이다.
        if (!profile.TryGet(out _colorAdjustments)) _colorAdjustments = profile.Add<ColorAdjustments>(true);
        if (!profile.TryGet(out _vignette)) _vignette = profile.Add<Vignette>(true);
        if (!profile.TryGet(out _filmGrain)) _filmGrain = profile.Add<FilmGrain>(true);

        // 원본 보관은 첫 적용 때 한 번만. 2단계에서 3단계로 오를 때 또 저장하면 1단계 값이 "원본"으로 굳는다.
        if (!_hasOriginalPostProcess)
        {
            _originalPostExposure = _colorAdjustments.postExposure.value;
            _originalContrast = _colorAdjustments.contrast.value;
            _originalSaturation = _colorAdjustments.saturation.value;
            _originalColorFilter = _colorAdjustments.colorFilter.value;
            _originalVignetteIntensity = _vignette.intensity.value;
            _originalVignetteColor = _vignette.color.value;
            _originalGrainIntensity = _filmGrain.intensity.value;
            _hasOriginalPostProcess = true;
        }

        // 단계를 0~1로 정규화해 세기를 보간한다. 어두워지고, 대비가 서고, 색이 빠지면서 붉게 물든다.
        float t = level / (float)DreamErosionState.MaxLevel;
        _colorAdjustments.active = true;
        _colorAdjustments.postExposure.value = _originalPostExposure - Mathf.Lerp(.22f, .72f, t);
        _colorAdjustments.contrast.value = _originalContrast + Mathf.Lerp(8f, 28f, t);
        _colorAdjustments.saturation.value = _originalSaturation - Mathf.Lerp(12f, 42f, t);
        _colorAdjustments.colorFilter.value = Color.Lerp(_originalColorFilter, new Color(.63f, .08f, .10f, 1f), Mathf.Lerp(.32f, .75f, t));
        _vignette.active = true;
        // 원본이 이미 더 진하면 그대로 둔다 — 연출이 화면을 오히려 밝히지는 않는다.
        _vignette.intensity.value = Mathf.Max(_originalVignetteIntensity, Mathf.Lerp(.22f, .58f, t));
        _vignette.color.value = Color.Lerp(_originalVignetteColor, new Color(.18f, .005f, .008f, 1f), Mathf.Lerp(.5f, .9f, t));
        _filmGrain.active = true;
        _filmGrain.intensity.value = Mathf.Max(_originalGrainIntensity, Mathf.Lerp(.12f, .42f, t));
    }

    private void RestorePostProcess()
    {
        if (!_hasOriginalPostProcess) return;
        // active 플래그는 되돌리지 않는다. 원래 꺼져 있던 오버라이드도 값만 원상복구하면 화면에 영향이 없다.
        if (_colorAdjustments != null)
        {
            _colorAdjustments.postExposure.value = _originalPostExposure;
            _colorAdjustments.contrast.value = _originalContrast;
            _colorAdjustments.saturation.value = _originalSaturation;
            _colorAdjustments.colorFilter.value = _originalColorFilter;
        }
        if (_vignette != null)
        {
            _vignette.intensity.value = _originalVignetteIntensity;
            _vignette.color.value = _originalVignetteColor;
        }
        if (_filmGrain != null) _filmGrain.intensity.value = _originalGrainIntensity;
        _hasOriginalPostProcess = false;
    }

    // 지속 시간 0 = 잠식이 풀릴 때까지 계속. 단계가 오를수록 세지고(세기 ↑) 입자가 굵어진다(스케일 ↓).
    private static void ApplyNoise(int level)
    {
        float t = level / (float)DreamErosionState.MaxLevel;
        ScreenEffectManager.Instance?.SetNoise(Mathf.Lerp(.10f, .34f, t), 0f, Mathf.Lerp(26f, 10f, t), Mathf.Lerp(.08f, .28f, t));
    }

    // 단계가 오른 "순간"에만 걸리는 짧은 충격 연출. 포스트프로세스·노이즈는 단계가 유지되는 동안 남지만
    // 슬로우모션과 글리치는 여기서 스스로 풀린다.
    private IEnumerator ImpactRoutine(int level)
    {
        float t = level / (float)DreamErosionState.MaxLevel;
        TimeManager timeManager = GameManager.instance != null ? GameManager.instance.timeManager : null;
        timeManager?.SetPresentationSpeedMultiplier(SlowMotionKey, Mathf.Lerp(.76f, .48f, t));
        ScreenEffectManager.Instance?.SetGlitch(Mathf.Lerp(.12f, .36f, t), .32f, .025f, .004f);
        // 배속을 우리가 낮춘 상태라 실시간으로 센다. WaitForSeconds면 슬로우모션만큼 대기도 같이 늘어난다.
        yield return new WaitForSecondsRealtime(Mathf.Lerp(.18f, .42f, t));
        timeManager?.ClearPresentationSpeedMultiplier(SlowMotionKey);
        _impactRoutine = null;
    }

    private void PlayDissonance(int level)
    {
        if (_dissonanceSource == null)
        {
            _dissonanceSource = gameObject.AddComponent<AudioSource>();
            _dissonanceSource.playOnAwake = false;
            _dissonanceSource.spatialBlend = 0f; // 위치와 무관한 2D 음.
            // 잠식 연출은 일시정지·슬로우모션 중에도 끝까지 들려야 한다.
            _dissonanceSource.ignoreListenerPause = true;
        }
        _dissonanceSource.Stop();
        if (_generatedClip != null) Destroy(_generatedClip);
        _generatedClip = CreateDissonanceClip(level);
        _dissonanceSource.clip = _generatedClip;
        _dissonanceSource.volume = Mathf.Lerp(.22f, .42f, level / (float)DreamErosionState.MaxLevel);
        _dissonanceSource.pitch = Mathf.Lerp(.88f, .64f, level / (float)DreamErosionState.MaxLevel);
        _dissonanceSource.Play();
    }

    // 에셋 없이 그 자리에서 만드는 불협화음. 단계별 사운드가 확정되기 전까지 쓰는 임시 음원이라
    // 에디터에 배선할 것이 없고, 대신 만든 클립은 재생성·Clear 때 반드시 Destroy한다.
    private static AudioClip CreateDissonanceClip(int level)
    {
        const int sampleRate = 44100;
        float seconds = 1.35f + level * .18f;
        int sampleCount = Mathf.CeilToInt(sampleRate * seconds);
        float[] samples = new float[sampleCount];
        float root = 92f + level * 7f; // 단계가 오를수록 기음이 조금씩 높아진다.
        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            // 앞은 짧게 세우고 뒤는 길게 눕힌다 — 딸깍거림 없이 시작해 서서히 사라진다.
            float envelope = Mathf.Clamp01(time / .035f) * Mathf.Clamp01((seconds - time) / .42f);
            float wobble = Mathf.Sin(time * 3.7f) * 1.7f; // 음정을 미세하게 흔들어 불안정하게 들리게 한다.
            // 단3도(1.1892)·증4도(1.4142)를 겹친다. 증4도가 불협의 핵심이라 화음이 안정적으로 맺히지 않는다.
            float chord = Mathf.Sin(2f * Mathf.PI * (root + wobble) * time)
                + .72f * Mathf.Sin(2f * Mathf.PI * (root * 1.1892f - wobble) * time)
                + .44f * Mathf.Sin(2f * Mathf.PI * (root * 1.4142f + wobble * 1.8f) * time);
            float hiss = (Mathf.PerlinNoise(time * 49f, level * 13.7f) - .5f) * .18f; // 백색소음 대용 잡음층.
            samples[i] = (chord * .22f + hiss) * envelope * (.22f + level * .025f);
        }
        AudioClip clip = AudioClip.Create("DreamErosion_Dissonance", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void OnDestroy() => Clear();
}
