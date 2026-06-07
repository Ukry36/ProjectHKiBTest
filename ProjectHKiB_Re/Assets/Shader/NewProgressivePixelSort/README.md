# NewProgressivePixelSort

Compute-shader based progressive pixel sorting.

The effect is different from `ApproximatedPixelSort`: each compute step compares only one adjacent texel pair, then repeated dispatches accumulate the sort over time. The controller uses ping-pong render textures for color and sort keys.

## Files

- `NewProgressivePixelSort.compute`: main compute shader.
- `NewProgressivePixelSortController.cs`: runtime dispatcher and render texture owner.
- `NewProgressivePixelSortBridge.hlsl`: optional ShaderGraph Custom Function helpers for packing preprocessing outputs.

## Data Layout

`KPrepare` builds:

- State texture: sorted color candidate. Alpha is masked out when outside the mask.
- Key texture: scalar sort key carried with the moving color.
- Slot texture: fixed sortable slots. Masked-out or below-threshold slots are barriers.

`KStep` performs one odd-even adjacent compare/swap pass. Dispatching it repeatedly is the progressive sort.

## ShaderGraph Preprocess Bridge

No ShaderGraph asset is created here. To preprocess through ShaderGraph:

1. Create a ShaderGraph material that can be used with `Graphics.Blit`.
2. Output RGB as the color to sort.
3. Output A as the mask value.
4. Assign that material to `preprocessMaterial` on `NewProgressivePixelSortController`.
5. Keep `usePreprocessOutputAsSource` and `usePreprocessAlphaAsMask` enabled.

The controller sets these properties before the blit:

- `_SourceTex`
- `_MaskTex`
- `_SortKeyTex`
- `_SourceUvRect`
- `_MaskUvRect`
- `_SortKeyUvRect`
- `_OutputSize` where xy is pixel size and zw is inverse pixel size.

For an external sort key, render or assign a separate key texture, set `sortMode` to `ExternalTexture`, and choose `sortKeyChannel`.
