using UnityEngine;


[RequireComponent(typeof(CapsuleCollider2D))]
public class ZCapsuleCollider2D : ZCollider2D
{
    private CapsuleCollider2D _col;
    protected override Collider2D Col 
    {
        get {
            if (_col == null) _col = GetComponent<CapsuleCollider2D>();
            return _col;
        }
    }

    public Vector2 Size           { get => _col.size;      set => _col.size = value; }
    public CapsuleDirection2D Dir { get => _col.direction; set => _col.direction = value; }
    public Vector2 Offset         { get => _col.offset;    set => _col.offset = value; }
    public bool IsTrigger         { get => _col.isTrigger; set => _col.isTrigger = value; }

#if UNITY_EDITOR
    protected override void DrawGizmo()
    {
        var pos = transform.position;
        var off = _col.offset;
        var c   = new Vector3(pos.x + off.x, pos.y + off.y, -pos.z);

        if (UseZAxis)
        {
            DrawCapsuleXY(c, _col.size, _col.direction, ZCoeff * (pos.z + ZMin));
            DrawCapsuleXY(c, _col.size, _col.direction, ZCoeff * (pos.z + ZMax));

            bool vert      = _col.direction == CapsuleDirection2D.Vertical;
            Vector3 perpDir = vert ? Vector3.right : Vector3.up;
            Vector3 bodyDir = vert ? Vector3.up    : Vector3.right;
            float sideExt  = (vert ? _col.size.x : _col.size.y) / 2f;
            float bodyExt  = (vert ? _col.size.y : _col.size.x) / 2f;

            foreach (var pt in new[] { perpDir * sideExt, -perpDir * sideExt, bodyDir * bodyExt, -bodyDir * bodyExt })
                Gizmos.DrawLine(new Vector3(c.x + pt.x, c.y + pt.y, ZCoeff *(pos.z + ZMin)),
                                new Vector3(c.x + pt.x, c.y + pt.y, ZCoeff * (pos.z + ZMax)));
        }
        else
        {
            var cUp = c + Vector3.up * ZMax;
            var cDown = c + Vector3.up * ZMin;
            DrawCapsuleXY(cUp, _col.size, _col.direction, 0);
            DrawCapsuleXY(cDown, _col.size, _col.direction, 0);

            bool vert      = _col.direction == CapsuleDirection2D.Vertical;
            Vector3 perpDir = vert ? Vector3.right : Vector3.up;
            Vector3 bodyDir = vert ? Vector3.up    : Vector3.right;
            float sideExt  = (vert ? _col.size.x : _col.size.y) / 2f;
            float bodyExt  = (vert ? _col.size.y : _col.size.x) / 2f;

            foreach (var pt in new[] { perpDir * sideExt, -perpDir * sideExt, bodyDir * bodyExt, -bodyDir * bodyExt })
                Gizmos.DrawLine(new Vector3(cUp.x + pt.x, cUp.y + pt.y, 0),
                                new Vector3(cDown.x + pt.x, cDown.y + pt.y, 0));
            /*
            // 2D 모드: XY 평면에서 Y=Z 치환 캡슐 (X폭 vs Z높이)
            float r2d      = Mathf.Min(_col.size.x / 2f, height / 2f);
            float halfBody = Mathf.Max(0f, height / 2f - r2d);
            var   c2d      = new Vector3(pos.x + off.x, zCenter, 0);
            var   cap1     = c2d + Vector3.up * halfBody;
            var   cap2     = c2d - Vector3.up * halfBody;

            DrawArc(cap1, r2d,   0, 180, Vector3.right, Vector3.up, 16);
            DrawArc(cap2, r2d, 180, 360, Vector3.right, Vector3.up, 16);
            Gizmos.DrawLine(cap1 + Vector3.right * r2d, cap2 + Vector3.right * r2d);
            Gizmos.DrawLine(cap1 - Vector3.right * r2d, cap2 - Vector3.right * r2d);*/
        }
    }
#endif
    protected override void Awake() { _col = GetComponent<CapsuleCollider2D>(); base.Awake(); }
}