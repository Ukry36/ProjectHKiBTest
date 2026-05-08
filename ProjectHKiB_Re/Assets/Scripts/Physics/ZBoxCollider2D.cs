using UnityEngine;


[RequireComponent(typeof(BoxCollider2D))]
public class ZBoxCollider2D : ZCollider2D
{
    private BoxCollider2D _col;
    protected override Collider2D Col 
    {
        get {
            if (_col == null) _col = GetComponent<BoxCollider2D>();
            return _col;
        }
    }

    public Vector2 Size   { get => _col.size;      set => _col.size = value; }
    public Vector2 Offset { get => _col.offset;    set => _col.offset = value; }
    public bool IsTrigger { get => _col.isTrigger; set => _col.isTrigger = value; }
#if UNITY_EDITOR
    protected override void DrawGizmo()
    {
        var pos = transform.position;
        var off = _col.offset;
        var sz  = _col.size;

        if (UseZAxis)
            Gizmos.DrawWireCube(
                new Vector3(pos.x + off.x, pos.y + off.y, ZCoeff * (pos.z + zCenter)),
                new Vector3(sz.x, sz.y, height));
        else
        {
            Gizmos.DrawWireCube(
                new Vector3(pos.x + off.x, pos.y + off.y + zCenter + pos.z, 0),
                new Vector3(sz.x, sz.y + height, 0.001f));
            Gizmos.DrawWireCube(
                new Vector3(pos.x + off.x, pos.y + off.y + ZMin, 0),
                new Vector3(sz.x, sz.y, 0.001f));
            Gizmos.DrawWireCube(
                new Vector3(pos.x + off.x, pos.y + off.y + ZMax, 0),
                new Vector3(sz.x, sz.y, 0.001f));
        }
    }
#endif
    protected override void Awake() { _col = GetComponent<BoxCollider2D>(); base.Awake(); }
}