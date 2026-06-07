#ifndef NEW_PROGRESSIVE_PIXEL_SORT_BRIDGE_INCLUDED
#define NEW_PROGRESSIVE_PIXEL_SORT_BRIDGE_INCLUDED

void NewProgressivePixelSortPack_float(
    float3 Color,
    float Mask,
    float SortKey,
    out float4 ColorMask,
    out float4 Key)
{
    ColorMask = float4(Color, saturate(Mask));
    Key = float4(SortKey, 0.0, 0.0, 1.0);
}

void NewProgressivePixelSortLumaKey_float(float3 Color, out float Key)
{
    Key = dot(Color, float3(0.299, 0.587, 0.114));
}

#endif
