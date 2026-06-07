// Optimized_PixelSort_Advanced.hlsl

void ApproximatedPixelSort_float(
    float2 ScreenUV,
    float2 ObjectUV,
    UnityTexture2D ScreenTex, UnitySamplerState ScreenSampler,
    UnityTexture2D MaskTex, UnitySamplerState MaskSampler,
    float2 Direction,
    float Strength,
    float StepSize,
    float WeightMultiplier,
    float LumaThreshold,
    int SortMode, // [추가] 0: Luma(명도), 1: Hue(색상), 2: Saturation(채도)
    out float4 OutColor)
{
    // 1. ScreenUV Step을 ObjectUV Step으로 수학적 변환
    float2 deltaScreen = Direction * StepSize;
    float2 ddx_s = ddx(ScreenUV);
    float2 ddy_s = ddy(ScreenUV);
    float det = ddx_s.x * ddy_s.y - ddx_s.y * ddy_s.x;
    float2 dp = float2(0.0, 0.0);
    
    if (abs(det) > 1e-6)
    {
        dp.x = (ddy_s.y * deltaScreen.x - ddy_s.x * deltaScreen.y) / det;
        dp.y = (-ddx_s.y * deltaScreen.x + ddx_s.x * deltaScreen.y) / det;
    }
    
    float2 ddx_o = ddx(ObjectUV);
    float2 ddy_o = ddy(ObjectUV);
    float2 stepObjectUV = dp.x * ddx_o + dp.y * ddy_o;

    // 2. 현재 픽셀 기본 데이터 추출
    float4 baseColor = ScreenTex.SampleLevel(ScreenSampler, ScreenUV, 0);
    float baseLuma = dot(baseColor.rgb, float3(0.299, 0.587, 0.114));
    float4 baseMask = MaskTex.SampleLevel(MaskSampler, ObjectUV, 0);

    if (baseMask.b <= 0.01 || baseLuma < LumaThreshold)
    {
        OutColor = baseColor;
        return;
    }

    // [핵심] 현재 픽셀의 정렬 기준 값(Key) 계산
    float baseSortKey = 0.0;
    if (SortMode == 0)
    {
        baseSortKey = baseLuma;
    }
    else
    {
        // 고속 Branchless RGB to HSV 추출 알고리즘
        float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
        float4 p = lerp(float4(baseColor.bg, K.wz), float4(baseColor.gb, K.xy), step(baseColor.b, baseColor.g));
        float4 q = lerp(float4(p.xyw, baseColor.r), float4(baseColor.r, p.yzx), step(p.x, baseColor.r));
        float d = q.x - min(q.w, q.y); // Chroma
        float e = 1.0e-10;

        if (SortMode == 1)
            baseSortKey = abs(q.z + (q.w - q.y) / (6.0 * d + e)); // Hue
        else
            baseSortKey = d / (q.x + e); // Saturation
    }

    int maxSearch = clamp((int)Strength, 1, 100);
    int startOffset = 0;
    int endOffset = 0;

    // 명칭 변경: lumaCache -> sortKeyCache
    float sortKeyCache[201];
    float4 colorCache[201];
    
    sortKeyCache[100] = baseSortKey * baseMask.b * WeightMultiplier;
    colorCache[100] = baseColor;

    // 3. 역방향 탐색
    [loop]
    for (int i = 1; i <= maxSearch; i++)
    {
        float2 suv = ScreenUV - deltaScreen * i;
        float2 ouv = ObjectUV - stepObjectUV * i;
        
        if (suv.x < 0.0 || suv.x > 1.0 || suv.y < 0.0 || suv.y > 1.0 ||
            ouv.x < 0.0 || ouv.x > 1.0 || ouv.y < 0.0 || ouv.y > 1.0) break;

        float maskB = MaskTex.SampleLevel(MaskSampler, ouv, 0).b;
        if (maskB <= 0.01) break;

        float4 col = ScreenTex.SampleLevel(ScreenSampler, suv, 0);
        float luma = dot(col.rgb, float3(0.299, 0.587, 0.114));
        if (luma < LumaThreshold) break;
        
        // 탐색된 픽셀의 정렬 값 계산
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
            float d = q.x - min(q.w, q.y);
            float e = 1.0e-10;

            if (SortMode == 1) sortKey = abs(q.z + (q.w - q.y) / (6.0 * d + e));
            else               sortKey = d / (q.x + e);
        }

        startOffset = i;
        int idx = 100 - i;
        colorCache[idx] = col;
        sortKeyCache[idx] = sortKey * maskB * WeightMultiplier;
    }

    // 4. 정방향 탐색
    [loop]
    for (int j = 1; j <= maxSearch; j++)
    {
        float2 suv = ScreenUV + deltaScreen * j;
        float2 ouv = ObjectUV + stepObjectUV * j;
        
        if (suv.x < 0.0 || suv.x > 1.0 || suv.y < 0.0 || suv.y > 1.0 ||
            ouv.x < 0.0 || ouv.x > 1.0 || ouv.y < 0.0 || ouv.y > 1.0) break;

        float maskB = MaskTex.SampleLevel(MaskSampler, ouv, 0).b;
        if (maskB <= 0.01) break;

        float4 col = ScreenTex.SampleLevel(ScreenSampler, suv, 0);
        float luma = dot(col.rgb, float3(0.299, 0.587, 0.114));
        if (luma < LumaThreshold) break;
        
        // 탐색된 픽셀의 정렬 값 계산
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
            float d = q.x - min(q.w, q.y);
            float e = 1.0e-10;

            if (SortMode == 1) sortKey = abs(q.z + (q.w - q.y) / (6.0 * d + e));
            else               sortKey = d / (q.x + e);
        }

        endOffset = j;
        int idx = 100 + j;
        colorCache[idx] = col;
        sortKeyCache[idx] = sortKey * maskB * WeightMultiplier;
    }

    // 5. 랭크 소팅 수행
    int startIdx = 100 - startOffset;
    int endIdx = 100 + endOffset;
    int N = startOffset + endOffset + 1;

    if (N <= 1) 
    {
        OutColor = baseColor;
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