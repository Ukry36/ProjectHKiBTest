using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CompositeCollider2D))]
public class ZCompositeCollider2D : ZCollider2D
{
    private CompositeCollider2D _col;
    protected override Collider2D Col
    {
        get
        {
            if (_col == null) _col = GetComponent<CompositeCollider2D>();
            return _col;
        }
    }

    public CompositeCollider2D.GeometryType GeometryType { get => _col.geometryType; set => _col.geometryType = value; }
    public float   EdgeRadius { get => _col.edgeRadius;  set => _col.edgeRadius = value; }
    public Vector2 Offset     { get => _col.offset;      set => _col.offset     = value; }
    public bool    IsTrigger  { get => _col.isTrigger;   set => _col.isTrigger  = value; }

    private readonly List<Vector2> _pathBuf = new List<Vector2>();

#if UNITY_EDITOR
    protected override void DrawGizmo()
    {
        var    pos    = transform.position;
        Vector2 off   = _col.offset;
        var    origin = new Vector3(pos.x + off.x, pos.y + off.y, 0);

        for (int i = 0; i < _col.pathCount; i++)
        {
            _col.GetPath(i, _pathBuf);
            var pts = _pathBuf.ToArray();

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
    }
#endif

    protected override void Awake()
    {
        _col = GetComponent<CompositeCollider2D>();
        base.Awake();
    }
}