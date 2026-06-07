// Progressive_PixelSort_Stable_Fixed.hlsl

void ProgressivePixelSort_float(
    float2 UV,                  
    float4 ScreenBounds,        
    UnityTexture2D PrevFrameTex, UnitySamplerState PrevSampler,
    float4 PrevFrameTex_TexelSize, // [수정] 유니티가 제공하는 텍스처 정보 변수 수신 (.zw가 가로세로 크기)
    UnityTexture2D ScreenTex, UnitySamplerState ScreenSampler, 
    UnityTexture2D MaskTex, UnitySamplerState MaskSampler,     
    float2 Direction,
    float LumaThreshold,
    int FrameParity,
    out float4 OutColor)
{
    // 1. 현재 연산 중인 픽셀이 마스크 내부인지 확인
    float baseMask = MaskTex.SampleLevel(MaskSampler, UV, 0).a; 
    if (baseMask <= 0.01)
    {
        OutColor = float4(0, 0, 0, 0); 
        return;
    }

    // 2. 현재 픽셀 정보 샘플링 및 빈 캔버스 초기화
    float4 centerCol = PrevFrameTex.SampleLevel(PrevSampler, UV, 0);
    
    if (centerCol.a < 0.01) 
    {
        float2 screenUV = float2(
            lerp(ScreenBounds.x, ScreenBounds.z, UV.x),
            lerp(ScreenBounds.y, ScreenBounds.w, UV.y)
        );
        centerCol = ScreenTex.SampleLevel(ScreenSampler, screenUV, 0);
        centerCol.a = 1.0; 
    }

    float centerLuma = dot(centerCol.rgb, float3(0.299, 0.587, 0.114));
    if (centerLuma < LumaThreshold)
    {
        OutColor = centerCol;
        return;
    }

    // 3. [수정] GetDimensions 대신 TexelSize 매크로 데이터 활용
    // _TexelSize.xy = (1/w, 1/h) 이고, _TexelSize.zw = (w, h) 입니다.
    float2 texelSize = PrevFrameTex_TexelSize.xy; 
    float2 textureSize = PrevFrameTex_TexelSize.zw;
    
    float pixelPos = dot(UV, Direction) * dot(textureSize, Direction);
    int pixelIndex = (int)round(pixelPos);
    int stepDir = ((pixelIndex + FrameParity) % 2 == 0) ? 1 : -1;

    float2 stepUV = Direction * texelSize;
    float2 neighborUV = UV + stepUV * stepDir;

    if (neighborUV.x < 0.0 || neighborUV.x > 1.0 || neighborUV.y < 0.0 || neighborUV.y > 1.0)
    {
        OutColor = centerCol;
        return;
    }

    float neighborMask = MaskTex.SampleLevel(MaskSampler, neighborUV, 0).a;
    if (neighborMask <= 0.01)
    {
        OutColor = centerCol;
        return;
    }

    float neighborCol = PrevFrameTex.SampleLevel(PrevSampler, neighborUV, 0);
    if (neighborCol.a < 0.01)
    {
        float2 nScreenUV = float2(
            lerp(ScreenBounds.x, ScreenBounds.z, neighborUV.x),
            lerp(ScreenBounds.y, ScreenBounds.w, neighborUV.y)
        );
        neighborCol = ScreenTex.SampleLevel(ScreenSampler, nScreenUV, 0);
        neighborCol.a = 1.0;
    }

    float neighborLuma = dot(neighborCol.rgb, float3(0.299, 0.587, 0.114));
    if (neighborLuma < LumaThreshold)
    {
        OutColor = centerCol;
        return;
    }

    // 4. 스왑 판별
    OutColor = centerCol;
    if (stepDir == 1)
    {
        if (centerLuma > neighborLuma) OutColor = neighborCol;
    }
    else
    {
        if (neighborLuma > centerLuma) OutColor = neighborCol;
    }
}