using UnityEngine;

[RequireComponent(typeof(Renderer))]
public sealed class NewProgressivePixelSortController : MonoBehaviour
{
    public enum SortMode
    {
        Luma = 0,
        Hue = 1,
        Saturation = 2,
        ExternalTexture = 3
    }

    [Header("Compute")]
    public ComputeShader pixelSortCompute;
    public Vector2Int resolution = new Vector2Int(256, 256);
    [Range(1, 128)] public int stepsPerFrame = 8;
    public bool initializeEveryFrame;
    public int initializeMultiplier;

    [Header("Input")]
    public Texture sourceTexture;
    public string globalSourceTextureName = "_CameraSortingLayerTexture";
    public Camera sourceCamera;
    public bool mapSourceFromRendererBounds = true;
    public Vector4 sourceUvRect = new Vector4(0, 0, 1, 1);

    [Header("Mask")]
    public Texture maskTexture;
    public string fallbackMaskTextureProperty = "_MainTex";
    public Vector4 maskUvRect = new Vector4(0, 0, 1, 1);
    [Range(0, 3)] public int maskChannel = 2;
    [Range(0f, 1f)] public float maskThreshold = 0.01f;

    [Header("Sort")]
    public SortMode sortMode = SortMode.Luma;
    public Texture sortKeyTexture;
    public Vector4 sortKeyUvRect = new Vector4(0, 0, 1, 1);
    [Range(0, 3)] public int sortKeyChannel;
    public Vector2 direction = Vector2.down;
    public bool ascending = true;
    [Range(0f, 1f)] public float lumaThreshold = 0.1f;
    public float weightMultiplier = 1f;
    [Range(0f, 1f)] public float maskKeyWeight = 1f;

    [Header("ShaderGraph Preprocess Bridge")]
    public Material preprocessMaterial;
    public bool usePreprocessOutputAsSource = true;
    public bool usePreprocessAlphaAsMask = true;

    [Header("Output")]
    public bool assignOutputToRenderer = true;
    public string outputTextureProperty = "_MainTex";
    public string globalOutputTextureName = "";

    private static readonly int SourceTexId = Shader.PropertyToID("_SourceTex");
    private static readonly int MaskTexId = Shader.PropertyToID("_MaskTex");
    private static readonly int SortKeyTexId = Shader.PropertyToID("_SortKeyTex");
    private static readonly int StateReadId = Shader.PropertyToID("_StateRead");
    private static readonly int StateWriteId = Shader.PropertyToID("_StateWrite");
    private static readonly int KeyReadId = Shader.PropertyToID("_KeyRead");
    private static readonly int KeyWriteId = Shader.PropertyToID("_KeyWrite");
    private static readonly int SlotTexId = Shader.PropertyToID("_SlotTex");
    private static readonly int TextureSizeId = Shader.PropertyToID("_TextureSize");
    private static readonly int SourceSizeId = Shader.PropertyToID("_SourceSize");
    private static readonly int MaskSizeId = Shader.PropertyToID("_MaskSize");
    private static readonly int SortKeySizeId = Shader.PropertyToID("_SortKeySize");
    private static readonly int SourceUvRectId = Shader.PropertyToID("_SourceUvRect");
    private static readonly int MaskUvRectId = Shader.PropertyToID("_MaskUvRect");
    private static readonly int SortKeyUvRectId = Shader.PropertyToID("_SortKeyUvRect");
    private static readonly int OutputSizeId = Shader.PropertyToID("_OutputSize");
    private static readonly int MaskThresholdId = Shader.PropertyToID("_MaskThreshold");
    private static readonly int LumaThresholdId = Shader.PropertyToID("_LumaThreshold");
    private static readonly int WeightMultiplierId = Shader.PropertyToID("_WeightMultiplier");
    private static readonly int MaskKeyWeightId = Shader.PropertyToID("_MaskKeyWeight");
    private static readonly int MaskChannelId = Shader.PropertyToID("_MaskChannel");
    private static readonly int SortKeyChannelId = Shader.PropertyToID("_SortKeyChannel");
    private static readonly int SortModeId = Shader.PropertyToID("_SortMode");
    private static readonly int UseSortKeyTexId = Shader.PropertyToID("_UseSortKeyTex");
    private static readonly int StepId = Shader.PropertyToID("_Step");
    private static readonly int ParityId = Shader.PropertyToID("_Parity");
    private static readonly int AscendingId = Shader.PropertyToID("_Ascending");

    private Renderer targetRenderer;
    private MaterialPropertyBlock propertyBlock;
    
    // 에디터 직렬화 시 변수 유실을 막기 위해 어트리뷰트 추가
    [SerializeField, HideInInspector] private RenderTexture stateA;
    [SerializeField, HideInInspector] private RenderTexture stateB;
    [SerializeField, HideInInspector] private RenderTexture keyA;
    [SerializeField, HideInInspector] private RenderTexture keyB;
    [SerializeField, HideInInspector] private RenderTexture slotTex;
    [SerializeField, HideInInspector] private RenderTexture preprocessTex;
    [SerializeField, HideInInspector] private bool readIsA = true;
    [SerializeField, HideInInspector] private bool initialized;
    [SerializeField, HideInInspector] private int stepIndex;
    [SerializeField, HideInInspector] private int init;

    private int prepareKernel = -1;
    private int stepKernel = -1;
    private int globalSourceTextureId;
    private bool warnedNoCompute;

    public RenderTexture OutputTexture
    {
        get { return readIsA ? stateA : stateB; }
    }

    [ContextMenu("Reset Sort")]
    public void ResetSort()
    {
        initialized = false;
        stepIndex = 0;
        readIsA = true;
    }

    private void OnEnable()
    {
        targetRenderer = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();
        sourceCamera = sourceCamera != null ? sourceCamera : Camera.main;
        globalSourceTextureId = Shader.PropertyToID(globalSourceTextureName);
        CacheKernels();
        ResetSort();
    }

    private void LateUpdate()
    {
        if (!SystemInfo.supportsComputeShaders)
        {
            if (!warnedNoCompute)
            {
                Debug.LogWarning("NewProgressivePixelSort requires compute shader support.", this);
                warnedNoCompute = true;
            }
            return;
        }

        if (pixelSortCompute == null || !CacheKernels())
        {
            return;
        }

        EnsureResources();

        if ((initializeEveryFrame && init < 1) || !initialized)
        {
            PrepareState();
            init = initializeMultiplier;
        }
        init --;

        for (int i = 0; i < stepsPerFrame; i++)
        {
            DispatchStep();
        }

        ApplyOutputTexture();
    }

    private bool CacheKernels()
    {
        if (pixelSortCompute == null)
        {
            return false;
        }

        try
        {
            if (prepareKernel < 0)
            {
                prepareKernel = pixelSortCompute.FindKernel("KPrepare");
            }

            if (stepKernel < 0)
            {
                stepKernel = pixelSortCompute.FindKernel("KStep");
            }
        }
        catch (System.ArgumentException)
        {
            prepareKernel = -1;
            stepKernel = -1;
            Debug.LogWarning("NewProgressivePixelSort.compute must contain KPrepare and KStep kernels.", this);
            return false;
        }

        return prepareKernel >= 0 && stepKernel >= 0;
    }

    private void EnsureResources()
    {
        resolution.x = Mathf.Max(1, resolution.x);
        resolution.y = Mathf.Max(1, resolution.y);

        if (stateA != null && stateA.width == resolution.x && stateA.height == resolution.y)
        {
            return;
        }

        ReleaseResources();

        stateA = CreateRenderTexture("NewProgressivePixelSort_StateA", RenderTextureFormat.ARGBHalf, true);
        stateB = CreateRenderTexture("NewProgressivePixelSort_StateB", RenderTextureFormat.ARGBHalf, true);
        keyA = CreateRenderTexture("NewProgressivePixelSort_KeyA", RenderTextureFormat.RFloat, true);
        keyB = CreateRenderTexture("NewProgressivePixelSort_KeyB", RenderTextureFormat.RFloat, true);
        slotTex = CreateRenderTexture("NewProgressivePixelSort_Slots", RenderTextureFormat.RFloat, true);
        preprocessTex = CreateRenderTexture("NewProgressivePixelSort_Preprocess", RenderTextureFormat.ARGBHalf, false);

        ResetSort();
    }

    private RenderTexture CreateRenderTexture(string textureName, RenderTextureFormat format, bool randomWrite)
    {
        RenderTexture rt = new(resolution.x, resolution.y, 0, format, RenderTextureReadWrite.Linear)
        {
            name = textureName,
            enableRandomWrite = randomWrite,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            useMipMap = false,
            autoGenerateMips = false
        };
        rt.Create();
        return rt;
    }

    private void PrepareState()
    {
        Texture source = GetActiveSourceTexture();
        Texture mask = GetActiveMaskTexture();
        Texture sortKey = sortKeyTexture != null ? sortKeyTexture : Texture2D.blackTexture;
        Vector4 activeSourceUvRect = GetSourceUvRect();
        Vector4 activeMaskUvRect = maskUvRect;
        int activeMaskChannel = Mathf.Clamp(maskChannel, 0, 3);

        if (preprocessMaterial != null)
        {
            RunPreprocess(source, mask, sortKey, activeSourceUvRect);

            if (usePreprocessOutputAsSource)
            {
                source = preprocessTex;
                activeSourceUvRect = new Vector4(0, 0, 1, 1);
            }

            if (usePreprocessAlphaAsMask)
            {
                mask = preprocessTex;
                activeMaskUvRect = new Vector4(0, 0, 1, 1);
                activeMaskChannel = 3;
            }
        }

        SetCommonParameters(source, mask, sortKey, activeSourceUvRect, activeMaskUvRect, activeMaskChannel);

        pixelSortCompute.SetTexture(prepareKernel, SourceTexId, source);
        pixelSortCompute.SetTexture(prepareKernel, MaskTexId, mask);
        pixelSortCompute.SetTexture(prepareKernel, SortKeyTexId, sortKey);
        pixelSortCompute.SetTexture(prepareKernel, StateWriteId, stateA);
        pixelSortCompute.SetTexture(prepareKernel, KeyWriteId, keyA);
        pixelSortCompute.SetTexture(prepareKernel, SlotTexId, slotTex);

        Dispatch(prepareKernel);

        readIsA = true;
        initialized = true;
        stepIndex = 0;
    }

    private void DispatchStep()
    {
        RenderTexture stateRead = readIsA ? stateA : stateB;
        RenderTexture stateWrite = readIsA ? stateB : stateA;
        RenderTexture keyRead = readIsA ? keyA : keyB;
        RenderTexture keyWrite = readIsA ? keyB : keyA;
        Vector2Int step = GetStep();

        pixelSortCompute.SetInts(StepId, step.x, step.y);
        pixelSortCompute.SetInt(ParityId, stepIndex & 1);
        pixelSortCompute.SetInt(AscendingId, ascending ? 1 : 0);

        pixelSortCompute.SetTexture(stepKernel, StateReadId, stateRead);
        pixelSortCompute.SetTexture(stepKernel, StateWriteId, stateWrite);
        pixelSortCompute.SetTexture(stepKernel, KeyReadId, keyRead);
        pixelSortCompute.SetTexture(stepKernel, KeyWriteId, keyWrite);
        pixelSortCompute.SetTexture(stepKernel, SlotTexId, slotTex);

        Dispatch(stepKernel);

        readIsA = !readIsA;
        stepIndex++;
    }

    private void SetCommonParameters(Texture source, Texture mask, Texture sortKey, Vector4 activeSourceUvRect, Vector4 activeMaskUvRect, int activeMaskChannel)
    {
        int mode = sortMode == SortMode.ExternalTexture && sortKeyTexture != null ? 3 : (int)sortMode;

        pixelSortCompute.SetInts(TextureSizeId, resolution.x, resolution.y);
        pixelSortCompute.SetInts(SourceSizeId, TextureWidth(source), TextureHeight(source));
        pixelSortCompute.SetInts(MaskSizeId, TextureWidth(mask), TextureHeight(mask));
        pixelSortCompute.SetInts(SortKeySizeId, TextureWidth(sortKey), TextureHeight(sortKey));

        pixelSortCompute.SetVector(SourceUvRectId, activeSourceUvRect);
        pixelSortCompute.SetVector(MaskUvRectId, activeMaskUvRect);
        pixelSortCompute.SetVector(SortKeyUvRectId, sortKeyUvRect);

        pixelSortCompute.SetFloat(MaskThresholdId, maskThreshold);
        pixelSortCompute.SetFloat(LumaThresholdId, lumaThreshold);
        pixelSortCompute.SetFloat(WeightMultiplierId, weightMultiplier);
        pixelSortCompute.SetFloat(MaskKeyWeightId, maskKeyWeight);
        pixelSortCompute.SetInt(MaskChannelId, activeMaskChannel);
        pixelSortCompute.SetInt(SortKeyChannelId, Mathf.Clamp(sortKeyChannel, 0, 3));
        pixelSortCompute.SetInt(SortModeId, mode);
        pixelSortCompute.SetInt(UseSortKeyTexId, sortKeyTexture != null ? 1 : 0);
    }

    private Texture GetActiveSourceTexture()
    {
        if (sourceTexture != null)
        {
            return sourceTexture;
        }

        if (!string.IsNullOrEmpty(globalSourceTextureName))
        {
            Texture globalTexture = Shader.GetGlobalTexture(globalSourceTextureId);
            if (globalTexture != null)
            {
                return globalTexture;
            }
        }

        return Texture2D.blackTexture;
    }

    private Texture GetActiveMaskTexture()
    {
        if (maskTexture != null)
        {
            return maskTexture;
        }

        Material sharedMaterial = targetRenderer != null ? targetRenderer.sharedMaterial : null;
        if (sharedMaterial != null && sharedMaterial.HasProperty(fallbackMaskTextureProperty))
        {
            Texture materialMask = sharedMaterial.GetTexture(fallbackMaskTextureProperty);
            if (materialMask != null)
            {
                return materialMask;
            }
        }

        return Texture2D.whiteTexture;
    }

    private void RunPreprocess(Texture source, Texture mask, Texture sortKey, Vector4 activeSourceUvRect)
    {
        preprocessMaterial.SetTexture(SourceTexId, source);
        preprocessMaterial.SetTexture(MaskTexId, mask);
        preprocessMaterial.SetTexture(SortKeyTexId, sortKey);
        preprocessMaterial.SetVector(SourceUvRectId, activeSourceUvRect);
        preprocessMaterial.SetVector(MaskUvRectId, maskUvRect);
        preprocessMaterial.SetVector(SortKeyUvRectId, sortKeyUvRect);
        preprocessMaterial.SetVector(OutputSizeId, new Vector4(resolution.x, resolution.y, 1f / resolution.x, 1f / resolution.y));

        Graphics.Blit(source, preprocessTex, preprocessMaterial);
    }

    private Vector4 GetSourceUvRect()
    {
        if (!mapSourceFromRendererBounds || sourceCamera == null || targetRenderer == null)
        {
            return sourceUvRect;
        }

        Bounds bounds = targetRenderer.bounds;
        Vector3 screenMin = sourceCamera.WorldToScreenPoint(bounds.min);
        Vector3 screenMax = sourceCamera.WorldToScreenPoint(bounds.max);
        float invWidth = sourceCamera.pixelWidth > 0 ? 1f / sourceCamera.pixelWidth : 1f;
        float invHeight = sourceCamera.pixelHeight > 0 ? 1f / sourceCamera.pixelHeight : 1f;

        return new Vector4(
            Mathf.Min(screenMin.x, screenMax.x) * invWidth,
            Mathf.Min(screenMin.y, screenMax.y) * invHeight,
            Mathf.Max(screenMin.x, screenMax.x) * invWidth,
            Mathf.Max(screenMin.y, screenMax.y) * invHeight);
    }

    private Vector2Int GetStep()
    {
        Vector2 dir = direction.sqrMagnitude > 0.000001f ? direction : Vector2.right;

        if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
        {
            return new Vector2Int(dir.x >= 0f ? 1 : -1, 0);
        }

        return new Vector2Int(0, dir.y >= 0f ? 1 : -1);
    }

    private void ApplyOutputTexture()
    {
        RenderTexture output = OutputTexture;
        if (output == null)
        {
            return;
        }

        if (assignOutputToRenderer && targetRenderer != null && !string.IsNullOrEmpty(outputTextureProperty))
        {
            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetTexture(outputTextureProperty, output);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }

        if (!string.IsNullOrEmpty(globalOutputTextureName))
        {
            Shader.SetGlobalTexture(globalOutputTextureName, output);
        }
    }

    private void Dispatch(int kernel)
    {
        int groupsX = Mathf.CeilToInt(resolution.x / 8f);
        int groupsY = Mathf.CeilToInt(resolution.y / 8f);
        pixelSortCompute.Dispatch(kernel, groupsX, groupsY, 1);
    }

    private static int TextureWidth(Texture texture)
    {
        return texture != null ? Mathf.Max(1, texture.width) : 1;
    }

    private static int TextureHeight(Texture texture)
    {
        return texture != null ? Mathf.Max(1, texture.height) : 1;
    }

    private void OnValidate()
    {
        resolution.x = Mathf.Max(1, resolution.x);
        resolution.y = Mathf.Max(1, resolution.y);
        stepsPerFrame = Mathf.Max(1, stepsPerFrame);
        maskChannel = Mathf.Clamp(maskChannel, 0, 3);
        sortKeyChannel = Mathf.Clamp(sortKeyChannel, 0, 3);
        globalSourceTextureId = Shader.PropertyToID(globalSourceTextureName);
    }

    private void OnDisable()
    {
        ReleaseResources();
    }

    private void OnDestroy()
    {
        ReleaseResources();
    }

    private void ReleaseResources()
    {
        Release(ref stateA);
        Release(ref stateB);
        Release(ref keyA);
        Release(ref keyB);
        Release(ref slotTex);
        Release(ref preprocessTex);
        initialized = false;
    }

    private static void Release(ref RenderTexture rt)
    {
        if (rt == null)
        {
            return;
        }

        rt.Release();
        if (Application.isPlaying)
        {
            UnityEngine.Object.Destroy(rt);
        }
        else
        {
            UnityEngine.Object.DestroyImmediate(rt);
        }

        rt = null;
    }
}
