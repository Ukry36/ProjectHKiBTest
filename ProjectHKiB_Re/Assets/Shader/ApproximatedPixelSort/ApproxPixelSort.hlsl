// Optimized_PixelSort_Advanced_Jittered.hlsl

void ApproximatedPixelSort_float(
    float2 ScreenUV,
    float2 ObjectUV,
    float3 WorldPos,
    float2 WorldPPU,
    float2 OptimalPPU, // [NEW] 셰이더 그래프 노드에 추가해야 할 파라미터
    UnityTexture2D ScreenTex, UnitySamplerState ScreenSampler,
    UnityTexture2D MaskTex, UnitySamplerState MaskSampler,
    float2 Direction,
    float Strength,
    float WeightMultiplier,
    float LumaThreshold,
    int SortMode,
    out float4 OutColor)
{
    // 1. 월드 포지션 기준으로 기본 격자화 (Base Snapping) 진행
    float2 baseSnappedWorldPos = floor(WorldPos.xy * WorldPPU) / WorldPPU + 0.5 / WorldPPU;
    
    // =====================================================================================
    // [NEW] 디더링(Jittering) 적용 구간
    // 최적 해상도(OptimalPPU) 기준으로 현재 픽셀이 속한 미세 격자(Micro-grid) 좌표 계산
    float2 microGridPos = floor(WorldPos.xy * OptimalPPU);
    
    // IGN (Interleaved Gradient Noise)를 이용한 고속 슈도-랜덤 생성 (0.0 ~ 1.0 범위)
    // 텍스처를 읽지 않고 수학적 연산만으로 픽셀 아트에 어울리는 노이즈 패턴을 만듭니다.
    float dither = frac(52.9829189 * frac(dot(microGridPos, float2(0.06711056, 0.00583715))));
    
    // 정렬 방향(Direction)을 기준으로, 낮아진 PPU 블록 크기 내에서 중심점을 무작위로 밀어냅니다.
    // (dither - 0.5)를 통해 -0.5 ~ 0.5 범위로 흔들어 굵은 격자의 경계선을 분쇄합니다.
    float2 jitterOffset = Direction * (dither - 0.5) * (1.0 / WorldPPU);
    
    // 최종 스내핑 위치 확정
    float2 snappedWorldPos = baseSnappedWorldPos + jitterOffset;
    // =====================================================================================

    float2 worldStep = Direction * (1.0 / WorldPPU);

    // 2. 월드 격자 중심점을 현재 카메라 기준의 Screen UV 공간으로 정밀 역투영
    float4 clipSnapped     = TransformWorldToHClip(float3(snappedWorldPos, WorldPos.z));
    float4 screenSnapped   = ComputeScreenPos(clipSnapped);
    float2 snappedScreenUV = screenSnapped.xy / screenSnapped.w;

    // 3. 월드 기준 1 가상 픽셀만큼 이동했을 때의 스크린 UV 델타값 역산
    float4 clipOrig = TransformWorldToHClip(WorldPos);
    float2 uvOrig   = ComputeScreenPos(clipOrig).xy / clipOrig.w;
    
    float4 clipNext = TransformWorldToHClip(float3(WorldPos.xy + worldStep, WorldPos.z));
    float2 uvNext   = ComputeScreenPos(clipNext).xy / clipNext.w;
    
    float2 deltaScreen = uvNext - uvOrig;

    // 4. 편미분 함수를 통한 ObjectUV 추적 및 동기화 스내핑
    float2 ddx_s = ddx(ScreenUV);
    float2 ddy_s = ddy(ScreenUV);
    float  det   = ddx_s.x * ddy_s.y - ddx_s.y * ddx_s.x;

    float2 stepObjectUV    = float2(0.0, 0.0);
    float2 snappedObjectUV = ObjectUV;

    if (abs(det) > 1e-6)
    {
        float2 ddx_o = ddx(ObjectUV);
        float2 ddy_o = ddy(ObjectUV);
        
        // 탐색을 위한 오브젝트 UV 증분 계산
        float2 dp;
        dp.x = (ddy_s.y * deltaScreen.x - ddy_s.x * deltaScreen.y) / det;
        dp.y = (-ddx_s.y * deltaScreen.x + ddx_s.x * deltaScreen.y) / det;
        stepObjectUV = dp.x * ddx_o + dp.y * ddy_o;

        // ScreenUV 스내핑 오차만큼 ObjectUV의 시작점도 보정하여 매칭 안정화
        float2 uvDiff = snappedScreenUV - ScreenUV;
        float2 dpDiff;
        dpDiff.x = (ddy_s.y * uvDiff.x - ddy_s.x * uvDiff.y) / det;
        dpDiff.y = (-ddx_s.y * uvDiff.x + ddx_s.x * uvDiff.y) / det;
        snappedObjectUV = ObjectUV + (dpDiff.x * ddx_o + dpDiff.y * ddy_o);
    }
    
    // 5. 기준점 샘플링 데이터 추출
    float4 baseColor = ScreenTex.SampleLevel(ScreenSampler, snappedScreenUV, 0);
    float  baseLuma  = dot(baseColor.rgb, float3(0.299, 0.587, 0.114));
    float4 baseMask  = MaskTex.SampleLevel(MaskSampler, snappedObjectUV, 0);

    if (baseMask.b <= 0.01 || baseLuma < LumaThreshold)
    {
        OutColor = float4(baseColor.rgb, 0.0);
        return;
    }
    
    // 현재 픽셀의 정렬 기준 값(Key) 계산
    float baseSortKey = 0.0;
    if (SortMode == 0)
    {
        baseSortKey = baseLuma;
    }
    else
    {
        float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
        float4 p = lerp(float4(baseColor.bg, K.wz), float4(baseColor.gb, K.xy), step(baseColor.b, baseColor.g));
        float4 q = lerp(float4(p.xyw, baseColor.r), float4(baseColor.r, p.yzx), step(p.x, baseColor.r));
        float  d = q.x - min(q.w, q.y);
        float  e = 1.0e-10;
        
        if (SortMode == 1) baseSortKey = abs(q.z + (q.w - q.y) / (6.0 * d + e));
        else               baseSortKey = d / (q.x + e);
    }

    int maxSearch   = clamp((int)Strength, 1, 100);
    int startOffset = 0;
    int endOffset   = 0;

    float sortKeyCache[201];
    float4 colorCache[201];
    
    sortKeyCache[100] = baseSortKey * baseMask.b * WeightMultiplier;
    colorCache[100] = baseColor;

    // 6. 역방향 탐색
    [loop]
    for (int i = 1; i <= maxSearch; i++)
    {
        float2 suv = snappedScreenUV - deltaScreen * i;
        float2 ouv = ObjectUV - stepObjectUV * i;
        
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
        float2 suv = snappedScreenUV + deltaScreen * j;
        float2 ouv = ObjectUV + stepObjectUV * j;
        
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