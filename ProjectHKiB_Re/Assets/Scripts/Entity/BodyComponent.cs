using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BodyComponent : MonoBehaviour
{
    private SpriteRenderer _renderer;
    private Vector3 _initialOffset;
    private float   _zHeightOffset;
    private Vector2 _snapOffset;
    private Vector2 _renderOffset;
    public void Awake()
    {
        _renderer      = GetComponent<SpriteRenderer>();
        _initialOffset = transform.localPosition;
    }

    public void SetZ(float z)
    {
        _zHeightOffset = z;
        if (_renderer) _renderer.material.SetFloat("_Offset", z);
    }

    /// <summary>
    /// Called before snap occours to keep the renderer's world position same after snap
    /// </summary>
    public void SetSnapOffset(Vector2 nextParentWorldPos)
    {
        _snapOffset += (Vector2)transform.parent.position - nextParentWorldPos;
        ApplyLocalPosition();
    }

    /// <summary>
    /// Manual offset (like shaking motion or something)
    /// </summary>
    public void SetRenderOffset(Vector2 offset)
    {
        _renderOffset = offset;
        ApplyLocalPosition();
    }

    /// <summary>
    /// Manual instant snap
    /// </summary>
    public void Snap()
    {
        _snapOffset             = Vector2.zero;
        _renderOffset           = Vector2.zero;
        transform.localPosition = _initialOffset + new Vector3(0, 0 + _zHeightOffset, transform.localPosition.z);
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
        Vector2 dynamic = _snapOffset + _renderOffset;
        transform.localPosition = _initialOffset + new Vector3(dynamic.x, dynamic.y + _zHeightOffset, transform.localPosition.z);
    }
}