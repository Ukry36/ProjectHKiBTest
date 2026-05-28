using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class ZCircleCollider2D : ZCollider2D
{
    private CircleCollider2D _col;
    protected override Collider2D Col 
    {
        get {
            if (_col == null) _col = GetComponent<CircleCollider2D>();
            return _col;
        }
    }

    public float Radius   { get => _col.radius;    set => _col.radius = value; }
    public Vector2 Offset { get => _col.offset;    set => _col.offset = value; }
    public bool IsTrigger { get => _col.isTrigger; set => _col.isTrigger = value; }
#if UNITY_EDITOR
    protected override void DrawGizmo()
    {
        var pos = transform.position;
        var off = _col.offset;
        float r = _col.radius;

        if (UseZAxis)
            DrawCylinder3D(new Vector3(pos.x + off.x, pos.y + off.y, pos.z), r, ZCoeff * (pos.z + ZMin), ZCoeff * (pos.z + ZMax));
        else
        {
            DrawCircleXY(new Vector3(pos.x + off.x, pos.y + off.y + ZMax, 0), r);
            DrawCircleXY(new Vector3(pos.x + off.x, pos.y + off.y + ZMin, 0), r);
            Gizmos.DrawLine(new Vector3(pos.x + off.x + r, pos.y + off.y + ZMax, 0),
                                new Vector3(pos.x + off.x + r, pos.y + off.y + ZMin, 0));
            Gizmos.DrawLine(new Vector3(pos.x + off.x - r, pos.y + off.y + ZMax, 0),
                                new Vector3(pos.x + off.x - r, pos.y + off.y + ZMin, 0));
        }
            
    }
#endif
    protected override void Awake() { _col = GetComponent<CircleCollider2D>(); base.Awake(); }
}