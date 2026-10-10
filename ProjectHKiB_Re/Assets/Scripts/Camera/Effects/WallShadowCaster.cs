using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

/// <summary>
/// 손전등이 벽에 가려지게 한다. 벽 모양은 두 가지로 정한다 — 두 그림자 방식(URP / 셰이더) 모두 같은 것을 쓴다.
///   - 바닥면 그림자 판(ShadowCaster2D): 빛을 가리는 모양. 그림자는 이 판 너머에 생긴다.
///   - 벽 그림(이 벽의 렌더러): 빛을 받는 앞면. 판 너머라도 벽 그림 안은 밝게 남는다(URP는 실루엣, 셰이더는 격자로).
/// 콜라이더는 처음 모양을 만들 때만 쓴다. 물리 콜라이더(ZCollider 높이 등)는 그림과 다를 수 있어서다.
///
/// 그림자 판은 두 가지로 만들 수 있다.
///   - 에디터에서 "그림자 판 생성"(또는 MapDarkness의 "맵 벽 그림자 판 생성"): 씬에 저장되는 ShadowCaster2D가 생기고,
///     각 판의 Edit Shape로 모양을 고칠 수 있다. 있으면 이것을 쓴다.
///   - 없으면 플레이할 때 콜라이더로 임시 판을 만든다(이전 동작).
/// 셰이더 방식일 때는 판을 꺼 두고(URP 그림자가 흐린 경계를 다시 자르지 않게) DarknessManager가 판 모양을 읽어 간다.
///
/// 반 3D라서 생기는 "벽 앞면 그림" 문제는 실루엣이 맡는다 — 벽 스프라이트마다 그림자를 드리우지 않는 ShadowCaster2D를 단다.
/// 매 프레임 손전등 광원이 벽 바닥면보다 위(벽 뒤)에 있으면 실루엣을 selfShadows로 바꿔 앞면에 그림자를 칠하고, 앞이면 끈다.
/// URP 12는 castsShadows=false인 실루엣이 혼자 그룹이면 그리지 않으므로 CompositeShadowCaster2D 그룹으로 묶는다.
///
/// 이 URP(12)는 ShadowCaster2D 모양·정렬 레이어를 코드로 넣는 공개 API가 없어 직렬화 필드에 리플렉션으로 넣는다 —
/// URP를 올리면 필드 이름부터 확인할 것.
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

    /// <summary>지금 켜져 있는 벽들. DarknessManager가 셰이더 그림자 모양을 여기서 모은다.</summary>
    public static readonly List<WallShadowCaster> Active = new();

    // 지울 때 되돌리기 위해 이 컴포넌트가 만든 것만 기억한다(에디터에서 만든 판은 지우지 않는다).
    private readonly List<Object> _created = new();
    private readonly List<ShadowCaster2D> _silhouettes = new();
    private readonly List<ShadowCaster2D> _footprints = new();
    private readonly List<Renderer> _visualRenderers = new();
    private readonly List<Rect> _footprintBounds = new(); // 월드 좌표 바닥면 — 광원이 벽 앞인지 뒤인지 판정용
    private bool _lightBehind;
    private GameObject _footprintRoot;
    private bool _built;

    private void OnEnable() => Active.Add(this);

    private void OnDisable() => Active.Remove(this);

    private void Start() => Build();

    private void OnDestroy() => Teardown();

    private void SyncFootprintCasters()
    {
        if (!_footprintRoot) return;
        bool urpShadows = !(DarknessManager.HasInstance && DarknessManager.Instance.UsesShaderShadows);
        if (_footprintRoot.activeSelf != urpShadows) _footprintRoot.SetActive(urpShadows);
    }

    [NaughtyAttributes.Button("그림자 다시 만들기 (플레이 중)")]
    public void Build()
    {
        if (!Application.isPlaying) return;
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
            // 에디터에서 만든(사람이 고쳤을 수 있는) 판을 그대로 쓴다.
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

        var polygons = new List<Vector2[]>();
        GetFootprintPolygons(polygons);
        foreach (Vector2[] polygon in polygons) _footprintBounds.Add(Bounds2D(polygon));

        SyncFootprintCasters();
        _lightBehind = false;
        _built = true;
    }

    private void LateUpdate()
    {
        SyncFootprintCasters();
        if (_silhouettes.Count == 0) return;
        bool behind = DarknessManager.HasInstance
                      && DarknessManager.Instance.TryGetFlashlightOrigin(out Vector2 origin)
                      && IsBehind(origin);
        if (behind == _lightBehind) return;

        _lightBehind = behind;
        foreach (ShadowCaster2D silhouette in _silhouettes)
            if (silhouette) silhouette.selfShadows = behind;
    }

    // ─── 셰이더 그림자용 모양 ─────────────────────────────────────

    /// <summary>그림자 판 모양·위치나 벽 그림이 바뀌면 달라지는 값. DarknessManager가 격자를 다시 만들지 판단한다.</summary>
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

    /// <summary>바닥면 그림자 판 모양(월드 좌표 다각형).</summary>
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

    /// <summary>벽 그림(빛을 받는 앞면) — 렌더러마다, 타일맵은 타일마다 월드 사각형.</summary>
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

    // 가장 가까운 바닥면의 윗변보다 광원이 위에 있으면 벽 뒤다. 옆에 붙어 있을 때(같은 높이)는 앞면이 보이므로 앞으로 본다.
    // 타일맵처럼 바닥면이 여러 개면 실루엣이 하나라 가장 가까운 벽 기준으로 판정한다.
    private bool IsBehind(Vector2 origin)
    {
        float best = float.MaxValue;
        bool behind = false;
        foreach (Rect r in _footprintBounds)
        {
            Vector2 nearest = new(Mathf.Clamp(origin.x, r.xMin, r.xMax), Mathf.Clamp(origin.y, r.yMin, r.yMax));
            float d = (origin - nearest).sqrMagnitude;
            if (d >= best) continue;
            best = d;
            behind = origin.y > r.yMax;
        }
        return behind;
    }

    private static Rect Bounds2D(Vector2[] points)
    {
        Vector2 min = new(float.MaxValue, float.MaxValue), max = new(float.MinValue, float.MinValue);
        foreach (Vector2 p in points)
        {
            min = Vector2.Min(min, p);
            max = Vector2.Max(max, p);
        }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private void Teardown()
    {
        foreach (Object obj in _created)
        {
            if (!obj) continue;
            if (obj is GameObject go) Destroy(go);
            else Destroy(obj);
        }
        _created.Clear();
        _silhouettes.Clear();
        _footprints.Clear();
        _visualRenderers.Clear();
        _footprintBounds.Clear();
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

    /// <summary>
    /// CompositeShadowCaster2D를 붙이고 내부 그림자 판 목록을 바로 만들어 둔다.
    /// URP 12의 ShadowCasterGroup2D는 그룹이 켜지는 순간 렌더링 목록에 오르지만 내부 목록은 첫 그림자 판이 등록될 때야
    /// 만들어서, 그 전에 렌더링이 돌면 CacheValues()에서 NullReferenceException이 난다(ShadowCasterGroup2D.cs:15).
    /// 그림자 판이 꺼져 있거나 다음 프레임에야 등록되는 동안에도 안전하도록 빈 목록을 미리 만든다(null을 넣었다 뺀다).
    /// </summary>
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

    // 콜라이더 모양을 이 오브젝트 로컬 좌표의 다각형들로 모은다(처음 그림자 판을 만들 때만).
    private List<Vector2[]> CollectShapes()
    {
        var shapes = new List<Vector2[]>();
        var points = new List<Vector2>();

        if (TryGetComponent(out CompositeCollider2D composite))
        {
            composite.GenerateGeometry();
            for (int i = 0; i < composite.pathCount; i++)
            {
                composite.GetPath(i, points);
                if (points.Count >= 3) shapes.Add(points.ToArray());
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
