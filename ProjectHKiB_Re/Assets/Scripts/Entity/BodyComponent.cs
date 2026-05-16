using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BodyComponent: MonoBehaviour
{
    private SpriteRenderer _renderer;
    private Vector3 _initialOffset;
    private float _zHeightOffset;
    private Vector2 _snapOffset; 
    private Vector2 _renderOffset;
    public void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _initialOffset = transform.localPosition;
    } 
    public void SetZ(float z)
    {
        _zHeightOffset = z;
        if (_renderer)_renderer.material.SetFloat("_Offset", z);
    }
    /// <summary>
    /// 스냅 직전에 호출. 부모가 nextParentWorldPos로 순간이동해도
    /// body가 시각적으로 현재 위치에 머물도록 스냅 오프셋에 누적.
    /// </summary>
    public void SetSnapOffset(Vector2 nextParentWorldPos)
    {
        _snapOffset += (Vector2)transform.parent.position - nextParentWorldPos;
        ApplyLocalPosition();
    }

    public void DecayOffsets(float renderDecaySpeed, float snapDecaySpeed)
    {
        const float epsilon = 0.00001f;

        if (_snapOffset.sqrMagnitude > epsilon)
        {
            _snapOffset = Vector2.Lerp(_snapOffset, Vector2.zero, snapDecaySpeed * Time.fixedDeltaTime);
            if (_snapOffset.sqrMagnitude < epsilon) _snapOffset = Vector2.zero;
        }

        if (_renderOffset.sqrMagnitude > epsilon)
        {
            _renderOffset = Vector2.Lerp(_renderOffset, Vector2.zero, renderDecaySpeed * Time.fixedDeltaTime);
            if (_renderOffset.sqrMagnitude < epsilon) _renderOffset = Vector2.zero;
        }

        ApplyLocalPosition();
    }

    private void ApplyLocalPosition()
    {
        Vector2 total = _snapOffset + _renderOffset;
        transform.localPosition = _initialOffset + new Vector3(
            total.x,
            total.y + _zHeightOffset,
            transform.localPosition.z);
    }
}