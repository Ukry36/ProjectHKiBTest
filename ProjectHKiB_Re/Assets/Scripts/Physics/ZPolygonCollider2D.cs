using UnityEngine;

[RequireComponent(typeof(PolygonCollider2D))]
public class ZPolygonCollider2D : ZCollider2D
{
    private PolygonCollider2D _col;
    protected override Collider2D Col 
    {
        get {
            if (_col == null) _col = GetComponent<PolygonCollider2D>();
            return _col;
        }
    }

    public Vector2[] Points { get => _col.points;    set => _col.points = value; }
    public Vector2 Offset   { get => _col.offset;    set => _col.offset = value; }
    public bool IsTrigger   { get => _col.isTrigger; set => _col.isTrigger = value; }
#if UNITY_EDITOR
    protected override void DrawGizmo()
    {
        var pos    = transform.position;
        var off    = _col.offset;
        var origin = new Vector3(pos.x + off.x, pos.y + off.y, pos.z);

        if (UseZAxis)
            DrawExtrudedPolygon3D(_col.points, origin, ZCoeff * (pos.z + ZMin), ZCoeff * (pos.z + ZMax));
        else
            DrawExtrudedPolygon2D(_col.points, pos + (Vector3)off, ZMin, ZMax);
    }
#endif
    protected override void Awake() { _col = GetComponent<PolygonCollider2D>(); base.Awake(); }
}