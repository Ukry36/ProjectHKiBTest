using UnityEngine;

[RequireComponent(typeof(EdgeCollider2D))]
public class ZEdgeCollider2D : ZCollider2D
{
    private EdgeCollider2D _col;
    protected override Collider2D Col 
    {
        get {
            if (_col == null) _col = GetComponent<EdgeCollider2D>();
            return _col;
        }
    }

    public Vector2[] Points { get => _col.points;      set => _col.points = value; }
    public float EdgeRadius { get => _col.edgeRadius;  set => _col.edgeRadius = value; }
    public Vector2 Offset   { get => _col.offset;      set => _col.offset = value; }
    public bool IsTrigger   { get => _col.isTrigger;   set => _col.isTrigger = value; }
#if UNITY_EDITOR
    protected override void DrawGizmo()
    {
        var pos    = transform.position;
        var off    = _col.offset;
        var origin = new Vector3(pos.x + off.x, pos.y + off.y, pos.z);

        if (UseZAxis)
            DrawExtrudedEdge3D(_col.points, origin, ZCoeff * (pos.z + ZMin), ZCoeff * (pos.z + ZMax));
        else
            DrawExtrudedEdge2D(_col.points, (Vector2)pos + off, ZMin, ZMax);
    }
#endif
    protected override void Awake() { _col = GetComponent<EdgeCollider2D>(); base.Awake(); }
}