// Cartesian visibility filtering. The beam falloff is never blurred.
Shader "Hidden/FlashlightOcclusion"
{
    Properties { _MainTex ("Beam", 2D) = "white" {} _OcclusionMap ("Distances", 2D) = "white" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        CGINCLUDE
        #include "UnityCG.cginc"
        sampler2D _MainTex, _OcclusionMap, _VisibilityMask, _BlurredMask;
        float4 _OcclusionMap_TexelSize, _Bounds, _BlurAxis;
        float _Rotation, _Softness, _InnerFade, _SpreadDistance, _ShadowStrength, _Inset, _PixelsPerUnit;
        float4 _Origin;
        // Beam-local point -> center of the world pixel it falls in, back in beam-local space.
        // The beam texels move and rotate with the light; deciding each texel by its world pixel keeps
        // shadow edges fixed on the sprite pixel grid while the player (and camera) move.
        float2 SnapToWorldPixel(float2 p)
        {
            float s = sin(_Rotation), c = cos(_Rotation);
            float2 w = float2(c*p.x - s*p.y, s*p.x + c*p.y) + _Origin.xy;
            w = (floor(w * _PixelsPerUnit) + 0.5) / _PixelsPerUnit - _Origin.xy;
            return float2(c*w.x + s*w.y, -s*w.x + c*w.y);
        }
        float ExitDistance(float2 p)
        {
            float angle = atan2(p.x, p.y) - _Rotation;
            float x = (angle / 6.28318530718 + 0.5) * _OcclusionMap_TexelSize.z - 0.5;
            float bin = floor(x);
            float a = tex2Dlod(_OcclusionMap, float4((bin + 0.5) * _OcclusionMap_TexelSize.x, 0.25, 0, 0)).r;
            float b = tex2Dlod(_OcclusionMap, float4((bin + 1.5) * _OcclusionMap_TexelSize.x, 0.25, 0, 0)).r;
            // Interpolate continuous faces only. At corners choose the actual bin,
            // rather than spreading the nearest wall to both sides.
            return abs(a-b) < max(0.25, min(a,b)*0.1) ? lerp(a,b,frac(x)) : (frac(x)<0.5 ? a : b);
        }
        float2 MaskSample(float2 p, float aa)
        {
            float exit = ExitDistance(p);
            float behind = length(p) - exit + _Inset;
            // Each sample covers half a pixel. Keep the original inward transition placement.
            float covered = exit < 999 ? smoothstep(-0.75 * aa, -0.25 * aa, behind) : 0;
            float contact = saturate(max(0, behind) / max(0.01, max(_InnerFade, _SpreadDistance)));
            return float2(1-covered, covered*contact);
        }
        float4 Mask(v2f_img i) : SV_Target
        {
            float2 p = _Bounds.xy + i.uv * _Bounds.zw;
            float exit = ExitDistance(p);
            // Pixel coverage depends on the wall boundary, not just radial distance.
            // Ignore derivatives across corners/no-wall bins, where exit is discontinuous.
            float2 exitDelta = float2(ddx(exit), ddy(exit));
            float2 radialDelta = float2(ddx(length(p)), ddy(length(p)));
            float faceTolerance = max(0.25, min(exit, 20.0) * 0.1);
            float2 continuous = step(abs(exitDelta), float2(faceTolerance, faceTolerance));
            float2 edgeDelta = radialDelta - exitDelta * continuous;
            float aa = max(abs(edgeDelta.x) + abs(edgeDelta.y), 0.0001);
            float2 px = ddx(p) * 0.25;
            float2 py = ddy(p) * 0.25;
            float2 mask = (MaskSample(p-px-py, aa) + MaskSample(p+px-py, aa)
                         + MaskSample(p-px+py, aa) + MaskSample(p+px+py, aa)) * 0.25;
            // Pixel-art mode: one hard decision per world pixel. Supersampling would re-introduce
            // sub-pixel edges that slide relative to the walls as the light moves.
            // (Selected after the derivative ops so they stay outside flow control.)
            if (_PixelsPerUnit > 0) mask = MaskSample(SnapToWorldPixel(p), 0.0001);
            return float4(mask, 0, 1);
        }
        float4 Blur(v2f_img i) : SV_Target
        {
            float4 sum = 0;
            float total = 0;
            [unroll] for (int k=-8; k<=8; k++)
            {
                float t = k / 8.0;
                float weight = exp(-4.5*t*t);
                float2 uv = i.uv + _BlurAxis.xy * (2*_Softness*t);
                // Beyond the cookie bounds there is no occlusion; clamping would smear border shadows.
                float4 value = (all(uv>=0) && all(uv<=1)) ? tex2D(_MainTex, uv) : float4(1,0,0,1);
                sum += value * weight;
                total += weight;
            }
            return sum / total;
        }
        float4 Compose(v2f_img i) : SV_Target
        {
            float alpha = tex2D(_MainTex,i.uv).a;
            float2 hard = tex2D(_VisibilityMask,i.uv).rg;
            float2 soft = tex2D(_BlurredMask,i.uv).rg;
            float covered = 1-soft.r;
            // Contact remains attached to the wall. Farther shadow edges become symmetric.
            float centerCovered = 1-hard.r;
            float centerContact = hard.g / max(centerCovered,0.001);
            float neighborContact = soft.g / max(covered,0.001);
            float contact = lerp(neighborContact, centerContact, centerCovered);
            float lit = lerp(hard.r, soft.r, saturate(contact));
            // Use the same filtered coverage for wall protection instead of a second binary edge.
            lit = lerp(lit, 1.0, hard.r);
            return float4(1,1,1,alpha*lerp(1,lit,_ShadowStrength));
        }
        ENDCG
        Pass { CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment Mask
        ENDCG }
        Pass { CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment Blur
        ENDCG }
        Pass { CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment Compose
        ENDCG }
    }
}
