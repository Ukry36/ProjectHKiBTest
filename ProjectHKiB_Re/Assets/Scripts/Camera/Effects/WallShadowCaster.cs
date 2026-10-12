using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

/// <summary>
/// 손전등이 벽에 가려지게 한다. 빛을 가리는 건 바닥면 그림자 판(ShadowCaster2D), 판 너머라도 벽 그림(렌더러) 안은 밝게 남는다.
/// 판은 에디터에서 구워 둔 것이 있으면 그것을, 없으면 콜라이더 모양으로 플레이 때 만든다. 셰이더 그림자일 때는 판을 꺼 두고
/// DarknessManager가 모양만 읽어 간다.
/// URP 12는 ShadowCaster2D 모양·정렬 레이어를 넣는 공개 API가 없어 리플렉션을 쓴다 — URP를 올리면 필드 이름부터 확인할 것.
/// </summary>
public class WallShadowCaster : MonoBehaviour
{
    [Tooltip("그림자가 드리워질 정렬 레이어.")]
    [SerializeField] private string[] _shadowedSortingLayers = { "Default", "Bottom", "Entity" };

    [Tooltip("에디터에서 만든 바닥면 그림자 판 묶음. 있으면 콜라이더 대신 이 모양을 쓴다. 각 판의 Edit Shape로 고칠 수 있다.")]
    [SerializeField] private GameObject _bakedFootprints;

    private static readonly FieldInfo ShapePathField = typeof(ShadowCaster2D).GetField("m_ShapePath", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo ShapePathHashField = typeof(ShadowCaster2D).GetField("m_ShapePathHash", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo SortingLayersField = typeof(ShadowCaster2D).GetField("m_ApplyToSortingLayers", BindingFlags.Instance | BindingFlags.NonPublic);

    public static readonly List<WallShadowCaster> Active = new();

    // 지울 때 되돌리기 위해 이 컴포넌트가 만든 것만 기억한다(에디터에서 만든 판은 지우지 않는다).
    private readonly List<Object> _created = new();
    private readonly List<ShadowCaster2D> _footprints = new();
    private readonly List<ShadowCaster2D> _silhouettes = new();
    private bool _silhouettesOn = true; // 만들 때 켜진 채로 만든다
    private readonly List<Renderer> _visualRenderers = new();
    private GameObject _footprintRoot;
    private bool _built;

    private void OnEnable() => Active.Add(this);

    private void OnDisable() => Active.Remove(this);

    private void Start() => Build();

    private void OnDestroy() => Teardown();

    // 셰이더 그림자일 때는 판도 실루엣도 끈다. 켜 두면 URP가 손전등마다 그림자 패스를 돌려(실루엣이 특히 비싸다)
    // 벽이 많은 곳에서 프레임이 떨어진다.
    private void SyncFootprintCasters()
    {
        if (!_footprintRoot) return;
        DarknessManager manager = DarknessManager.Instance;
        bool urpShadows = !(manager && manager.UsesShaderShadows);
        if (_footprintRoot.activeSelf != urpShadows) _footprintRoot.SetActive(urpShadows);
        if (_silhouettesOn == urpShadows) return;
        _silhouettesOn = urpShadows;
        foreach (ShadowCaster2D silhouette in _silhouettes)
            if (silhouette) silhouette.enabled = urpShadows;
    }

    private void Build()
    {
        if (ShapePathField == null || ShapePathHashField == null || SortingLayersField == null)
        {
            Debug.LogError("[WallShadowCaster] ShadowCaster2D 내부 필드를 찾지 못했습니다. URP 버전이 바뀌었는지 확인하세요.", this);
            return;
        }

        Teardown();
        int[] layers = ResolveSortingLayers();
        Transform group = EnsureGroup();

        // 벽 스프라이트 실루엣 — 그림자는 드리우지 않고, 자기 모양만큼 그림자를 지워 벽 앞면을 밝게 남긴다.
        // 같은 렌더러들이 셰이더 방식의 "벽 그림"이다. 그림자 판을 만들기 전에 모아야 판이 섞이지 않는다.
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
        {
            if (_bakedFootprints && renderer.transform.IsChildOf(_bakedFootprints.transform)) continue;
            _visualRenderers.Add(renderer);
            if (renderer.TryGetComponent(out ShadowCaster2D _)) continue; // 이미 손으로 단 것은 존중한다
            if (!IsUnder(renderer.transform, group)) continue;            // 그룹 밖이면 지워 주지 못한다
            ShadowCaster2D silhouette = renderer.gameObject.AddComponent<ShadowCaster2D>();
            silhouette.castsShadows = false;
            silhouette.useRendererSilhouette = true;
            silhouette.selfShadows = false;
            SortingLayersField.SetValue(silhouette, layers);
            _created.Add(silhouette);
            _silhouettes.Add(silhouette);
        }

        if (_bakedFootprints)
        {
            _footprintRoot = _bakedFootprints;
            _footprints.AddRange(_bakedFootprints.GetComponentsInChildren<ShadowCaster2D>(true));
        }
        else
        {
            List<Vector2[]> shapes = CollectShapes();
            if (shapes.Count == 0)
                Debug.LogWarning($"[WallShadowCaster] '{name}'에서 그림자를 만들 콜라이더 모양을 찾지 못했습니다.", this);
            _footprintRoot = new GameObject("WallShadows");
            _footprintRoot.transform.SetParent(transform, false);
            _created.Add(_footprintRoot);
            for (int i = 0; i < shapes.Count; i++)
                _footprints.Add(CreateCaster(_footprintRoot.transform, i, shapes[i], layers));
        }

        SyncFootprintCasters();
        _built = true;
    }

    private void LateUpdate() => SyncFootprintCasters();

    // ─── 셰이더 그림자용 모양 ─────────────────────────────────────

    // 판 모양·위치나 벽 그림이 바뀌면 달라진다. DarknessManager가 격자를 다시 만들지 판단한다.
    public int ShapeVersion
    {
        get
        {
            if (!_built) return 0;
            int hash = 17;
            foreach (ShadowCaster2D caster in _footprints)
            {
                if (!caster) continue;
                hash = hash * 31 + (int)ShapePathHashField.GetValue(caster);
                hash = hash * 31 + caster.transform.localToWorldMatrix.GetHashCode();
            }
            hash = hash * 31 + _footprints.Count;
            foreach (Renderer renderer in _visualRenderers)
                if (renderer) hash = hash * 31 + renderer.bounds.GetHashCode();
            return hash;
        }
    }

    public void GetFootprintPolygons(List<Vector2[]> output)
    {
        foreach (ShadowCaster2D caster in _footprints)
        {
            if (!caster || caster.shapePath == null || caster.shapePath.Length < 3) continue;
            Vector3[] path = caster.shapePath;
            var world = new Vector2[path.Length];
            for (int i = 0; i < path.Length; i++) world[i] = caster.transform.TransformPoint(path[i]);
            output.Add(world);
        }
    }

    // 타일맵은 타일마다 사각형.
    public void GetVisualRects(List<Rect> output)
    {
        foreach (Renderer renderer in _visualRenderers)
        {
            if (!renderer || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
            if (renderer is TilemapRenderer && renderer.TryGetComponent(out Tilemap tilemap))
            {
                AddTileRects(tilemap, output);
                continue;
            }
            Bounds b = renderer.bounds;
            output.Add(Rect.MinMaxRect(b.min.x, b.min.y, b.max.x, b.max.y));
        }
    }

    // 타일 그림은 타일 기준점(tileAnchor)에 스프라이트 피벗을 맞춰 그린다.
    private static void AddTileRects(Tilemap tilemap, List<Rect> output)
    {
        Transform tr = tilemap.transform;
        Vector3 scale = tr.lossyScale;
        foreach (Vector3Int cell in tilemap.cellBounds.allPositionsWithin)
        {
            if (!tilemap.HasTile(cell)) continue;
            Vector3 anchor = tr.TransformPoint(tilemap.CellToLocalInterpolated(cell + tilemap.tileAnchor));
            Sprite sprite = tilemap.GetSprite(cell);
            Vector3 min, max;
            if (sprite)
            {
                Bounds b = sprite.bounds;
                min = anchor + Vector3.Scale(b.min, scale);
                max = anchor + Vector3.Scale(b.max, scale);
            }
            else
            {
                min = tr.TransformPoint(tilemap.CellToLocal(cell));
                max = tr.TransformPoint(tilemap.CellToLocal(cell + Vector3Int.one));
            }
            output.Add(Rect.MinMaxRect(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y),
                                       Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y)));
        }
    }

    // ─── 에디터: 그림자 판 생성 ──────────────────────────────────

#if UNITY_EDITOR
    [NaughtyAttributes.Button("그림자 판 생성 (편집 가능)")]
    public void BakeFootprints()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("[WallShadowCaster] 그림자 판 생성은 플레이 중이 아닐 때만 됩니다(플레이 중 만든 것은 저장되지 않는다).", this);
            return;
        }
        if (ShapePathField == null || ShapePathHashField == null || SortingLayersField == null)
        {
            Debug.LogError("[WallShadowCaster] ShadowCaster2D 내부 필드를 찾지 못했습니다. URP 버전이 바뀌었는지 확인하세요.", this);
            return;
        }

        List<Vector2[]> shapes = CollectShapes();
        if (shapes.Count == 0)
        {
            Debug.LogWarning($"[WallShadowCaster] '{name}'에서 그림자를 만들 콜라이더 모양을 찾지 못했습니다.", this);
            return;
        }

        UnityEditor.Undo.RecordObject(this, "그림자 판 생성");
        if (_bakedFootprints) UnityEditor.Undo.DestroyObjectImmediate(_bakedFootprints);

        var root = new GameObject("WallShadows (편집 가능)");
        UnityEditor.Undo.RegisterCreatedObjectUndo(root, "그림자 판 생성");
        root.transform.SetParent(transform, false);
        int[] layers = ResolveSortingLayers();
        for (int i = 0; i < shapes.Count; i++) CreateCaster(root.transform, i, shapes[i], layers);

        _bakedFootprints = root;
        UnityEditor.EditorUtility.SetDirty(this);
    }

    [NaughtyAttributes.Button("그림자 판 지우기 (콜라이더로 자동 생성)")]
    public void ClearBakedFootprints()
    {
        if (Application.isPlaying || !_bakedFootprints) return;
        UnityEditor.Undo.RecordObject(this, "그림자 판 지우기");
        UnityEditor.Undo.DestroyObjectImmediate(_bakedFootprints);
        _bakedFootprints = null;
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif

    // ─── 내부 ────────────────────────────────────────────────────

    private void Teardown()
    {
        foreach (Object obj in _created)
        {
            if (!obj) continue;
            if (obj is GameObject go) Destroy(go);
            else Destroy(obj);
        }
        _created.Clear();
        _footprints.Clear();
        _silhouettes.Clear();
        _silhouettesOn = true;
        _visualRenderers.Clear();
        _footprintRoot = null;
        _built = false;
    }

    // 실루엣이 그림자를 지우려면 그림자 판과 같은 그룹이어야 한다. URP 12는 "부모 쪽"의 CompositeShadowCaster2D만
    // 그룹으로 보므로, 이 오브젝트 자체에 렌더러가 있으면(타일맵) 그룹을 한 단계 위(부모)에 둔다.
    private Transform EnsureGroup()
    {
        Transform host = TryGetComponent(out Renderer _) && transform.parent ? transform.parent : transform;
        if (!host.TryGetComponent(out CompositeShadowCaster2D _))
            _created.Add(AddShadowGroup(host.gameObject));
        return host;
    }

    // URP 12 그룹은 첫 캐스터가 등록될 때야 내부 목록을 만들어, 그 전에 렌더링되면 NRE가 난다.
    // null을 넣었다 빼서 빈 목록을 미리 만든다.
    internal static CompositeShadowCaster2D AddShadowGroup(GameObject host)
    {
        CompositeShadowCaster2D group = host.AddComponent<CompositeShadowCaster2D>();
        group.RegisterShadowCaster2D(null);
        group.UnregisterShadowCaster2D(null);
        return group;
    }

    private static bool IsUnder(Transform t, Transform ancestor)
    {
        for (Transform p = t.parent; p != null; p = p.parent)
            if (p == ancestor) return true;
        return false;
    }

    private List<Vector2[]> CollectShapes()
    {
        var shapes = new List<Vector2[]>();
        var points = new List<Vector2>();

        if (TryGetComponent(out CompositeCollider2D composite))
        {
            TryGetComponent(out Tilemap tilemap);
            composite.GenerateGeometry();
            for (int i = 0; i < composite.pathCount; i++)
            {
                composite.GetPath(i, points);
                if (points.Count >= 3 && (!tilemap || HasWallTile(tilemap, points))) shapes.Add(points.ToArray());
            }
            return shapes;
        }

        foreach (BoxCollider2D box in GetComponents<BoxCollider2D>())
        {
            if (box.usedByComposite) continue;
            Vector2 c = box.offset, h = box.size * 0.5f;
            shapes.Add(new[]
            {
                c + new Vector2(-h.x, -h.y), c + new Vector2(-h.x, h.y),
                c + new Vector2(h.x, h.y), c + new Vector2(h.x, -h.y),
            });
        }

        foreach (PolygonCollider2D polygon in GetComponents<PolygonCollider2D>())
        {
            if (polygon.usedByComposite) continue;
            for (int i = 0; i < polygon.pathCount; i++)
            {
                Vector2[] path = polygon.GetPath(i);
                if (path.Length < 3) continue;
                for (int p = 0; p < path.Length; p++) path[p] += polygon.offset;
                shapes.Add(path);
            }
        }
        return shapes;
    }

    // 높이 타일맵에는 바닥보다 낮은 칸(zLevel 0 이하)도 같은 콜라이더로 들어 있다. 그런 칸만으로 된 모양은 벽이 아니다.
    // 벽 칸에 붙은 낮은 칸은 콜라이더 외곽선이 벽과 하나로 합쳐져 있어 걸러지지 않는다.
    private static bool HasWallTile(Tilemap tilemap, List<Vector2> path)
    {
        Vector2 min = path[0], max = path[0];
        foreach (Vector2 p in path) { min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
        Vector3Int from = tilemap.LocalToCell(min), to = tilemap.LocalToCell(max);
        for (int y = from.y; y <= to.y; y++)
            for (int x = from.x; x <= to.x; x++)
            {
                var cell = new Vector3Int(x, y, 0);
                TileBase tile = tilemap.GetTile(cell);
                if (!tile || (tile is HeightTile height && height.zLevel <= 0)) continue;
                if (Contains(path, tilemap.GetCellCenterLocal(cell))) return true;
            }
        return false;
    }

    private static bool Contains(List<Vector2> path, Vector2 p)
    {
        bool inside = false;
        for (int i = 0, j = path.Count - 1; i < path.Count; j = i++)
        {
            Vector2 a = path[i], b = path[j];
            if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x) inside = !inside;
        }
        return inside;
    }

    // 꺼진 채로 모양을 넣고 켜야 한다 — ShadowCaster2D는 켜질 때(Awake/OnEnable) 경로가 비어 있으면
    // 바운딩 박스로 채워 버리고, 켜질 때 넣은 경로로 그림자 메시를 만든다.
    private static ShadowCaster2D CreateCaster(Transform parent, int index, Vector2[] points, int[] layers)
    {
        var go = new GameObject($"WallShadow_{index}");
        go.SetActive(false);
        go.transform.SetParent(parent, false);

        ShadowCaster2D caster = go.AddComponent<ShadowCaster2D>();
        caster.useRendererSilhouette = false;
        caster.selfShadows = false;

        var path = new Vector3[points.Length];
        int hash = 17;
        for (int p = 0; p < points.Length; p++)
        {
            path[p] = points[p];
            hash = hash * 31 + points[p].GetHashCode();
        }
        ShapePathField.SetValue(caster, path);
        ShapePathHashField.SetValue(caster, hash);
        SortingLayersField.SetValue(caster, layers);

        go.SetActive(true);
        return caster;
    }

    private int[] ResolveSortingLayers()
    {
        var ids = new List<int>();
        foreach (string layerName in _shadowedSortingLayers)
        {
            int id = SortingLayer.NameToID(layerName);
            if (SortingLayer.IsValid(id)) ids.Add(id);
            else Debug.LogWarning($"[WallShadowCaster] 정렬 레이어 '{layerName}'이 없습니다.", this);
        }
        return ids.ToArray();
    }
}
