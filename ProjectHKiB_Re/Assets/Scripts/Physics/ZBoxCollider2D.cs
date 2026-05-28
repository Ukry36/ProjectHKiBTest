using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class ZBoxCollider2D : ZCollider2D
{
    private BoxCollider2D _col;
    protected override Collider2D Col
    {
        get
        {
            if (_col == null) _col = GetComponent<BoxCollider2D>();
            return _col;
        }
    }

    public Vector2 Size     { get => _col.size;      set => _col.size      = value; }
    public Vector2 Offset   { get => _col.offset;    set => _col.offset    = value; }
    public bool    IsTrigger { get => _col.isTrigger; set => _col.isTrigger = value; }

#if UNITY_EDITOR
    protected override void DrawGizmo()
    {
        var    pos  = transform.position;
        Vector2 off = _col.offset;
        Vector2 sz  = _col.size;

        float cx   = pos.x + off.x,  cy   = pos.y + off.y;
        float minX = cx - sz.x * .5f, maxX = cx + sz.x * .5f;
        float minY = cy - sz.y * .5f, maxY = cy + sz.y * .5f;

        bool sloped = useSlopeDU || useSlopeRL;

        // ── 경사 없음: 기존 동작 ─────────────────────────────────────────────
        if (!sloped)
        {
            if (UseZAxis)
            {
                Gizmos.DrawWireCube(
                    new Vector3(cx, cy, ZCoeff * (pos.z + zCenter)),
                    new Vector3(sz.x, sz.y, height));
            }
            else
            {
                Gizmos.DrawWireCube(new Vector3(cx, cy + zCenter + pos.z, 0), new Vector3(sz.x, sz.y + height, 0.001f));
                Gizmos.DrawWireCube(new Vector3(cx, cy + ZMin,            0), new Vector3(sz.x, sz.y,          0.001f));
                Gizmos.DrawWireCube(new Vector3(cx, cy + ZMax,            0), new Vector3(sz.x, sz.y,          0.001f));
            }
            return;
        }

        // ── 경사 있음: 기울어진 육면체 ──────────────────────────────────────
        //
        // zA_{min,max} = 경사 '시작' 끝단의 Z 범위
        // zB_{min,max} = 경사 '끝'   끝단의 Z 범위
        //   DU: A = 아래(minY), B = 위(maxY)
        //   RL: A = 왼쪽(minX), B = 오른쪽(maxX)

        float offA   = useSlopeDU ? downMostOffset : leftMostOffset;
        float offB   = useSlopeDU ? upMostOffset   : rightMostOffset;
        float zA_min = ZMin + offA, zA_max = ZMax + offA;
        float zB_min = ZMin + offB, zB_max = ZMax + offB;

        // 꼭짓점 배치 (DrawHexahedron 규약: face A = v[0..3], face B = v[4..7])
        //
        //   v[2]─v[3]    v[6]─v[7]
        //    |     |  ──  |     |
        //   v[0]─v[1]    v[4]─v[5]
        //
        var v = new Vector3[8];

        if (UseZAxis)
        {
            if (useSlopeDU)
            {
                // Face A: y = minY (아래)
                v[0] = new Vector3(minX, minY, ZCoeff * zA_min);
                v[1] = new Vector3(maxX, minY, ZCoeff * zA_min);
                v[2] = new Vector3(minX, minY, ZCoeff * zA_max);
                v[3] = new Vector3(maxX, minY, ZCoeff * zA_max);
                // Face B: y = maxY (위)
                v[4] = new Vector3(minX, maxY, ZCoeff * zB_min);
                v[5] = new Vector3(maxX, maxY, ZCoeff * zB_min);
                v[6] = new Vector3(minX, maxY, ZCoeff * zB_max);
                v[7] = new Vector3(maxX, maxY, ZCoeff * zB_max);
            }
            else // RL
            {
                // Face A: x = minX (왼쪽)
                v[0] = new Vector3(minX, minY, ZCoeff * zA_min);
                v[1] = new Vector3(minX, maxY, ZCoeff * zA_min);
                v[2] = new Vector3(minX, minY, ZCoeff * zA_max);
                v[3] = new Vector3(minX, maxY, ZCoeff * zA_max);
                // Face B: x = maxX (오른쪽)
                v[4] = new Vector3(maxX, minY, ZCoeff * zB_min);
                v[5] = new Vector3(maxX, maxY, ZCoeff * zB_min);
                v[6] = new Vector3(maxX, minY, ZCoeff * zB_max);
                v[7] = new Vector3(maxX, maxY, ZCoeff * zB_max);
            }
        }
        else // 2D 뷰: Z → Y 투영
        {
            if (useSlopeDU)
            {
                // Face A: y = minY (아래 끝단), Z를 Y 오프셋으로
                v[0] = new Vector3(minX, minY + zA_min, 0);
                v[1] = new Vector3(maxX, minY + zA_min, 0);
                v[2] = new Vector3(minX, minY + zA_max, 0);
                v[3] = new Vector3(maxX, minY + zA_max, 0);
                // Face B: y = maxY (위 끝단)
                v[4] = new Vector3(minX, maxY + zB_min, 0);
                v[5] = new Vector3(maxX, maxY + zB_min, 0);
                v[6] = new Vector3(minX, maxY + zB_max, 0);
                v[7] = new Vector3(maxX, maxY + zB_max, 0);
            }
            else // RL
            {
                // Face A: x = minX (왼쪽 끝단)
                v[0] = new Vector3(minX, minY + zA_min, 0);
                v[1] = new Vector3(minX, maxY + zA_min, 0);
                v[2] = new Vector3(minX, minY + zA_max, 0);
                v[3] = new Vector3(minX, maxY + zA_max, 0);
                // Face B: x = maxX (오른쪽 끝단)
                v[4] = new Vector3(maxX, minY + zB_min, 0);
                v[5] = new Vector3(maxX, maxY + zB_min, 0);
                v[6] = new Vector3(maxX, minY + zB_max, 0);
                v[7] = new Vector3(maxX, maxY + zB_max, 0);
            }
        }

        DrawHexahedron(v);
    }
#endif

    protected override void Awake()
    {
        _col = GetComponent<BoxCollider2D>();
        base.Awake();
    }
}