// Optimized_PixelSort_Advanced_VolumeJitter.hlsl

void ApproximatedPixelSort_float(
    float2 ScreenUV,
    float2 ObjectUV,
    float3 WorldPos,
    float2 WorldPPU,
    float2 OptimalPPU, 
    UnityTexture2D ScreenTex, UnitySamplerState ScreenSampler,
    UnityTexture2D MaskTex, UnitySamplerState MaskSampler,
    float2 Direction,
    float Strength,
    float WeightMultiplier,
    float LumaThreshold,
    int SortMode,
    float JitterStrength, // [NEW] 노이즈 강도 조절용 파라미터 (0.0 = 노이즈 없음 ~ 1.0 = 최대)
    out float4 OutColor)
{
    // 1. 오리지널 격자화 (Base Snapping) 진행 (기본 상태)
    float2 snappedWorldPos = floor(WorldPos.xy * WorldPPU) / WorldPPU + 0.5 / WorldPPU;
    float2 worldStep       = Direction * (1.0 / WorldPPU);

    // 2. 오리지널 격자 중심점을 Screen UV 공간으로 역투영
    float4 clipSnapped     = TransformWorldToHClip(float3(snappedWorldPos, WorldPos.z));
    float4 screenSnapped   = ComputeScreenPos(clipSnapped);
    float2 snappedScreenUV = screenSnapped.xy / screenSnapped.w;

    // 3. 스크린 UV 델타값 역산
    float4 clipOrig = TransformWorldToHClip(WorldPos);
    float2 uvOrig   = ComputeScreenPos(clipOrig).xy / clipOrig.w;
    
    float4 clipNext = TransformWorldToHClip(float3(WorldPos.xy + worldStep, WorldPos.z));
    float2 uvNext   = ComputeScreenPos(clipNext).xy / clipNext.w;
    
    float2 deltaScreen = uvNext - uvOrig;

    // 4. 편미분 함수를 통한 ObjectUV 추적 및 동기화
    float2 ddx_s = ddx(ScreenUV);
    float2 ddy_s = ddy(ScreenUV);
    float  det   = ddx_s.x * ddy_s.y - ddx_s.y * ddx_s.x;

    float2 stepObjectUV    = float2(0.0, 0.0);
    float2 snappedObjectUV = ObjectUV;

    if (abs(det) > 1e-6)
    {
        float2 ddx_o = ddx(ObjectUV);
        float2 ddy_o = ddy(ObjectUV);
        
        float2 dp;
        dp.x = (ddy_s.y * deltaScreen.x - ddy_s.x * deltaScreen.y) / det;
        dp.y = (-ddx_s.y * deltaScreen.x + ddx_s.x * deltaScreen.y) / det;
        stepObjectUV = dp.x * ddx_o + dp.y * ddy_o;

        float2 uvDiff = snappedScreenUV - ScreenUV;
        float2 dpDiff;
        dpDiff.x = (ddy_s.y * uvDiff.x - ddy_s.x * uvDiff.y) / det;
        dpDiff.y = (-ddx_s.y * uvDiff.x + ddx_s.x * uvDiff.y) / det;
        snappedObjectUV = ObjectUV + (dpDiff.x * ddx_o + dpDiff.y * ddy_o);
    }
    
    // 5. [Early-Out Check] 원본 좌표 기준으로 정렬 대상인지 검사
    float4 baseColor = ScreenTex.SampleLevel(ScreenSampler, snappedScreenUV, 0);
    float  baseLuma  = dot(baseColor.rgb, float3(0.299, 0.587, 0.114));
    float4 baseMask  = MaskTex.SampleLevel(MaskSampler, snappedObjectUV, 0);

    if (baseMask.b <= 0.01 || baseLuma < LumaThreshold)
    {
        OutColor = float4(ScreenTex.SampleLevel(ScreenSampler, ScreenUV, 0).rgb, 0.0);
        return;
    }
    
    // =====================================================================================
    // [NEW] 강도 조절형 제한적 디더링 (Bounded Jittering with Strength)
    // =====================================================================================
    float2 microGridPos = floor(WorldPos.xy * OptimalPPU);
    float dither = frac(52.9829189 * frac(dot(microGridPos, float2(0.06711056, 0.00583715))));
    
    // 계산된 지터 오프셋에 JitterStrength 파라미터를 곱해 강도를 동적으로 제어합니다.
    float2 jitterOffset = Direction * (dither - 0.5) * (1.0 / WorldPPU) * JitterStrength;
    
    // 지터가 적용된 새로운 시작점 좌표 계산
    float2 jitteredWorldPos = snappedWorldPos + jitterOffset;
    float4 clipJittered     = TransformWorldToHClip(float3(jitteredWorldPos, WorldPos.z));
    float4 screenJittered   = ComputeScreenPos(clipJittered);
    float2 jitteredScreenUV = screenJittered.xy / screenJittered.w;
    float2 jitteredObjectUV = snappedObjectUV;

    if (abs(det) > 1e-6)
    {
        float2 ddx_o = ddx(ObjectUV);
        float2 ddy_o = ddy(ObjectUV);
        float2 uvDiffJitter = jitteredScreenUV - ScreenUV;
        float2 dpDiffJitter;
        dpDiffJitter.x = (ddy_s.y * uvDiffJitter.x - ddy_s.x * uvDiffJitter.y) / det;
        dpDiffJitter.y = (-ddx_s.y * uvDiffJitter.x + ddx_s.x * uvDiffJitter.y) / det;
        jitteredObjectUV = ObjectUV + (dpDiffJitter.x * ddx_o + dpDiffJitter.y * ddy_o);
    }

    // 지터링된 위치의 샘플링 데이터로 갱신
    float4 jitteredBaseColor = ScreenTex.SampleLevel(ScreenSampler, jitteredScreenUV, 0);
    float  jitteredBaseLuma  = dot(jitteredBaseColor.rgb, float3(0.299, 0.587, 0.114));
    float  jitteredMaskB     = MaskTex.SampleLevel(MaskSampler, jitteredObjectUV, 0).b;
    
    float jitteredSortKey = 0.0;
    if (SortMode == 0)
    {
        jitteredSortKey = jitteredBaseLuma;
    }
    else
    {
        float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
        float4 p = lerp(float4(jitteredBaseColor.bg, K.wz), float4(jitteredBaseColor.gb, K.xy), step(jitteredBaseColor.b, jitteredBaseColor.g));
        float4 q = lerp(float4(p.xyw, jitteredBaseColor.r), float4(jitteredBaseColor.r, p.yzx), step(p.x, jitteredBaseColor.r));
        float  d = q.x - min(q.w, q.y);
        float  e = 1.0e-10;
        
        if (SortMode == 1) jitteredSortKey = abs(q.z + (q.w - q.y) / (6.0 * d + e));
        else               jitteredSortKey = d / (q.x + e);
    }

    int maxSearch   = clamp((int)Strength, 1, 100);
    int startOffset = 0;
    int endOffset   = 0;

    float sortKeyCache[201];
    float4 colorCache[201];
    
    sortKeyCache[100] = jitteredSortKey * jitteredMaskB * WeightMultiplier;
    colorCache[100]   = jitteredBaseColor;

    // 6. 역방향 탐색
    [loop]
    for (int i = 1; i <= maxSearch; i++)
    {
        float2 suv = jitteredScreenUV - deltaScreen * i;
        float2 ouv = jitteredObjectUV - stepObjectUV * i;
        
        if (suv.x < 0.0 || suv.x > 1.0 || suv.y < 0.0 || suv.y > 1.0 ||
            ouv.x < 0.0 || ouv.x > 1.0 || ouv.y < 0.0 || ouv.y > 1.0) break;

        float maskB = MaskTex.SampleLevel(MaskSampler, ouv, 0).b;
        if (maskB <= 0.01) break;

        float4 col  = ScreenTex.SampleLevel(ScreenSampler, suv, 0);
        float  luma = dot(col.rgb, float3(0.299, 0.587, 0.114));
        if (luma < LumaThreshold) break;

        float sortKey = 0.0;
        if (SortMode == 0)
        {
            sortKey = luma;
        }
        else
        {
            float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
            float4 p = lerp(float4(col.bg, K.wz), float4(col.gb, K.xy), step(col.b, col.g));
            float4 q = lerp(float4(p.xyw, col.r), float4(col.r, p.yzx), step(p.x, col.r));
            float  d = q.x - min(q.w, q.y);
            float  e = 1.0e-10;
            if (SortMode == 1) sortKey = abs(q.z + (q.w - q.y) / (6.0 * d + e));
            else               sortKey = d / (q.x + e);
        }

        startOffset = i;
        int idx = 100 - i;
        colorCache[idx] = col;
        sortKeyCache[idx] = sortKey * maskB * WeightMultiplier;
    }

    // 7. 정방향 탐색
    [loop]
    for (int j = 1; j <= maxSearch; j++)
    {
        float2 suv = jitteredScreenUV + deltaScreen * j;
        float2 ouv = jitteredObjectUV + stepObjectUV * j;
        
        if (suv.x < 0.0 || suv.x > 1.0 || suv.y < 0.0 || suv.y > 1.0 ||
            ouv.x < 0.0 || ouv.x > 1.0 || ouv.y < 0.0 || ouv.y > 1.0) break;

        float maskB = MaskTex.SampleLevel(MaskSampler, ouv, 0).b;
        if (maskB <= 0.01) break;

        float4 col  = ScreenTex.SampleLevel(ScreenSampler, suv, 0);
        float  luma = dot(col.rgb, float3(0.299, 0.587, 0.114));
        if (luma < LumaThreshold) break;

        float sortKey = 0.0;
        if (SortMode == 0)
        {
            sortKey = luma;
        }
        else
        {
            float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
            float4 p = lerp(float4(col.bg, K.wz), float4(col.gb, K.xy), step(col.b, col.g));
            float4 q = lerp(float4(p.xyw, col.r), float4(col.r, p.yzx), step(p.x, col.r));
            float  d = q.x - min(q.w, q.y);
            float  e = 1.0e-10;
            if (SortMode == 1) sortKey = abs(q.z + (q.w - q.y) / (6.0 * d + e));
            else               sortKey = d / (q.x + e);
        }

        endOffset = j;
        int idx = 100 + j;
        colorCache[idx] = col;
        sortKeyCache[idx] = sortKey * maskB * WeightMultiplier;
    }

    // 8. Rank Sorting
    int startIdx = 100 - startOffset;
    int endIdx = 100 + endOffset;
    int N = startOffset + endOffset + 1;

    if (N <= 1) 
    {
        OutColor = float4(baseColor.rgb, 0.0);
        return;
    }

    float4 bestColor = colorCache[100];

    [loop]
    for (int m = startIdx; m <= endIdx; m++)
    {
        int rank = 0;
        float candidateKey = sortKeyCache[m];
        
        [loop]
        for (int n = startIdx; n <= endIdx; n++)
        {
            if (sortKeyCache[n] < candidateKey)
            {
                rank++;
            }
            else if (sortKeyCache[n] == candidateKey && n < m)
            {
                rank++;
            }
        }
        
        if (rank == startOffset)
        {
            bestColor = colorCache[m];
            break;
        }
    }

    OutColor = bestColor;
}