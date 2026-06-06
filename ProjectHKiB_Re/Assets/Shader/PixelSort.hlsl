// PixelSort.hlsl

void ApproximatedPixelSort_float(
    float2 UV,
    UnityTexture2D ScreenTex, UnitySamplerState ScreenSampler,
    UnityTexture2D MaskTex, UnitySamplerState MaskSampler,
    float2 Direction,
    float Strength,
    float StepSize,
    float WeightMultiplier,
    float LumaThreshold, // [추가됨] 밝기 임계값
    out float4 OutColor)
{
    // 현재 픽셀의 원본 색상 및 명도 계산
    float4 baseColor = ScreenTex.SampleLevel(ScreenSampler, UV, 0);
    float baseLuma = dot(baseColor.rgb, float3(0.299, 0.587, 0.114));
    float4 baseMask = MaskTex.SampleLevel(MaskSampler, UV, 0);

    // [핵심 로직] 마스크 밖이거나, 명도가 Threshold보다 낮으면 
    // 정렬에 참여하지 않고 원본 색상을 그대로 반환합니다. (이 픽셀들이 '벽'이 됩니다)
    if (baseMask.a <= 0.01 || baseLuma < LumaThreshold)
    {
        OutColor = baseColor;
        return;
    }

    int maxSearch = clamp((int)Strength, 1, 50);
    int startOffset = 0;
    int endOffset = 0;

    // 1. 블록의 시작점 찾기 (역방향)
    [loop]
    for (int i = 1; i <= maxSearch; i++)
    {
        float2 suv = UV - Direction * i * StepSize;
        float maskA = MaskTex.SampleLevel(MaskSampler, suv, 0).a;
        float4 col = ScreenTex.SampleLevel(ScreenSampler, suv, 0);
        float luma = dot(col.rgb, float3(0.299, 0.587, 0.114));
        
        // 탐색 중 마스크를 벗어나거나 어두운 픽셀을 만나면 그곳이 정렬 블록의 경계가 됨
        if (maskA <= 0.01 || luma < LumaThreshold) break;
        startOffset = i;
    }

    // 2. 블록의 끝점 찾기 (정방향)
    [loop]
    for (int j = 1; j <= maxSearch; j++)
    {
        float2 suv = UV + Direction * j * StepSize;
        float maskA = MaskTex.SampleLevel(MaskSampler, suv, 0).a;
        float4 col = ScreenTex.SampleLevel(ScreenSampler, suv, 0);
        float luma = dot(col.rgb, float3(0.299, 0.587, 0.114));
        
        if (maskA <= 0.01 || luma < LumaThreshold) break;
        endOffset = j;
    }

    int N = startOffset + endOffset + 1;
    if (N <= 1) 
    {
        OutColor = baseColor;
        return;
    }

    // 3. 배열 캐싱
    float lumaCache[101];
    float4 colorCache[101];
    int cacheIdx = 0;
    
    [loop]
    for (int k = -startOffset; k <= endOffset; k++)
    {
        float2 suv = UV + Direction * k * StepSize;
        float4 col = ScreenTex.SampleLevel(ScreenSampler, suv, 0);
        float maskA = MaskTex.SampleLevel(MaskSampler, suv, 0).a;
        
        colorCache[cacheIdx] = col;
        // 정렬 순위를 결정하는 최종 가중치 (명도 + 마스크 알파 참조)
        lumaCache[cacheIdx] = dot(col.rgb, float3(0.299, 0.587, 0.114)) + (maskA * WeightMultiplier);
        cacheIdx++;
    }

    float4 bestColor = colorCache[startOffset];

    // 4. 완벽한 픽셀 재배열 (Rank Sorting)
    [loop]
    for (int m = 0; m < N; m++)
    {
        int rank = 0;
        float candidateLuma = lumaCache[m];
        
        [loop]
        for (int n = 0; n < N; n++)
        {
            if (lumaCache[n] < candidateLuma)
            {
                rank++;
            }
            else if (lumaCache[n] == candidateLuma && n < m)
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