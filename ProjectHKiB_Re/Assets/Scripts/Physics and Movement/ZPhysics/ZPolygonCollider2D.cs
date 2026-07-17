using UnityEngine;

[RequireComponent(typeof(PolygonCollider2D))]
public class ZPolygonCollider2D : ZCollider2D
{
    private PolygonCollider2D _col;
    protected override Collider2D Col
    {
        get
        {
            if (_col == null) _col = GetComponent<PolygonCollider2D>();
            return _col;
        }
    }

    public Vector2[] Points  { get => _col.points;    set => _col.points    = value; }
    public Vector2   Offset  { get => _col.offset;    set => _col.offset    = value; }
    public bool      IsTrigger { get => _col.isTrigger; set => _col.isTrigger = value; }

#if UNITY_EDITOR
    protected override void DrawGizmo()
    {
        var    pos    = transform.position;
        Vector2 off   = _col.offset;
        var    origin = new Vector3(pos.x + off.x, pos.y + off.y, 0);
        var    pts    = _col.points;

        if (useSlopeDU || useSlopeRL)
        {
            if (UseZAxis) DrawSlopedExtrudedPolygon3D(pts, origin);
            else          DrawSlopedExtrudedPolygon2D(pts, origin);
        }
        else
        {
            if (UseZAxis) DrawExtrudedPolygon3D(pts, origin, ZCoeff * ZMin, ZCoeff * ZMax);
            else          DrawExtrudedPolygon2D(pts, pos + (Vector3)off, ZMin, ZMax);
        }
    }
#endif

    protected override void Awake()
    {
        _col = GetComponent<PolygonCollider2D>();
        base.Awake();
    }
}