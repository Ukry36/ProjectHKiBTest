using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 손전등이 벽에 가려지게 한다. 두 방식이 있고 DarknessManager의 Flashlight Shader Shadows로 고른다.
///   - 셰이더 방식(기본): 빛 가림은 DarknessManager(1D 그림자 맵 + FlashlightOcclusion 셰이더)가 흐린 경계로 칠한다.
///     이 컴포넌트의 벽 바닥면 그림자 판은 꺼 둔다.
///   - 원래 방식: 벽 콜라이더 바닥면을 ShadowCaster2D 그림자 판으로 만들어 URP 그림자로 가린다(경계가 또렷함).
/// 두 방식 모두 반 3D라서 생기는 "벽 앞면 그림" 문제는 아래 실루엣이 맡는다.
/// 타일맵 벽(CompositeCollider2D)과 엔티티 벽(BoxCollider2D·PolygonCollider2D) 모두 된다.
/// MapDarkness가 맵의 벽(Wall 레이어, 계단·경사 제외)에 자동으로 붙이므로 보통은 손으로 붙일 필요가 없다.
///
/// 벽 스프라이트마다 그림자를 드리우지 않는 ShadowCaster2D(실루엣 사용)를 단다. 매 프레임 손전등 광원이 벽 바닥면보다
/// 위(벽 뒤)에 있으면 실루엣을 selfShadows로 바꿔 앞면에 그림자를 칠하고(몸이 벽에 가려지듯 빛도 가려진다),
/// 앞에 있으면 끈다. 그림자를 쓰는 조명은 손전등뿐이라 전역·주변 빛에는 영향이 없다.
/// URP 12는 castsShadows=false인 실루엣이 혼자 그룹이면 그리지 않으므로 CompositeShadowCaster2D 그룹으로 묶는다.
///
/// 이 URP(12)는 ShadowCaster2D 정렬 레이어를 코드로 넣는 공개 API가 없어 직렬화 필드에 리플렉션으로 넣는다 —
/// URP를 올리면 필드 이름부터 확인할 것.
/// </summary>
public class WallShadowCaster : MonoBehaviour
{
    [Tooltip("그림자가 드리워질 정렬 레이어.")]
    [SerializeField] private string[] _shadowedSortingLayers = { "Default", "Bottom", "Entity" };

    private static readonly FieldInfo ShapePathField = typeof(ShadowCaster2D).GetField("m_ShapePath", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo ShapePathHashField = typeof(ShadowCaster2D).GetField("m_ShapePathHash", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo SortingLayersField = typeof(ShadowCaster2D).GetField("m_ApplyToSortingLayers", BindingFlags.Instance | BindingFlags.NonPublic);

    // 지울 때 되돌리기 위해 이 컴포넌트가 만든 것만 기억한다.
    private readonly List<Object> _created = new();
    private readonly List<ShadowCaster2D> _silhouettes = new();
    private readonly List<Rect> _footprints = new(); // 월드 좌표 바닥면 — 광원이 벽 앞인지 뒤인지 판정용
    private bool _lightBehind;
    private GameObject _footprintRoot;

    private void SyncFootprintCasters()
    {
        if (!_footprintRoot) return;
        bool urpShadows = !(DarknessManager.HasInstance && DarknessManager.Instance.UsesShaderShadows);
        if (_footprintRoot.activeSelf != urpShadows) _footprintRoot.SetActive(urpShadows);
    }

    private void Start() => Build();

    private void OnDestroy() => Teardown();

    [NaughtyAttributes.Button("그림자 다시 만들기")]
    public void Build()
    {
        if (!Application.isPlaying) return;
        if (ShapePathField == null || ShapePathHashField == null || SortingLayersField == null)
        {
            Debug.LogError("[WallShadowCaster] ShadowCaster2D 내부 필드를 찾지 못했습니다. URP 버전이 바뀌었는지 확인하세요.", this);
            return;
        }

        Teardown();
        List<Vector2[]> shapes = CollectShapes();
        if (shapes.Count == 0)
        {
            Debug.LogWarning($"[WallShadowCaster] '{name}'에서 그림자를 만들 콜라이더 모양을 찾지 못했습니다.", this);
            return;
        }

        int[] layers = ResolveSortingLayers();
        Transform group = EnsureGroup();

        // 벽 스프라이트 실루엣 — 그림자는 드리우지 않고, 자기 모양만큼 그림자를 지워 벽 앞면을 밝게 남긴다.
        // 그림자 판을 만들기 전에 모아야 그림자 판(렌더러 없음)이 섞이지 않는다.
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
        {
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

        // 벽 바닥면 그림자 판 — 원래 방식(URP 그림자)일 때만 켠다. 셰이더 방식일 때 켜 두면 셰이더가 흐린 경계를
        // URP 그림자가 다시 칼같이 잘라 버리므로 끈다(LateUpdate에서 DarknessManager.UsesShaderShadows를 보고 전환).
        _footprintRoot = new GameObject("WallShadows");
        _footprintRoot.transform.SetParent(transform, false);
        _created.Add(_footprintRoot);
        for (int i = 0; i < shapes.Count; i++)
        {
            CreateCaster(_footprintRoot.transform, i, shapes[i], layers);
            _footprints.Add(WorldBounds(shapes[i]));
        }
        SyncFootprintCasters();
        _lightBehind = false;
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

    // 가장 가까운 바닥면의 윗변보다 광원이 위에 있으면 벽 뒤다. 옆에 붙어 있을 때(같은 높이)는 앞면이 보이므로 앞으로 본다.
    // 타일맵처럼 바닥면이 여러 개면 실루엣이 하나라 가장 가까운 벽 기준으로 판정한다.
    private bool IsBehind(Vector2 origin)
    {
        float best = float.MaxValue;
        bool behind = false;
        foreach (Rect r in _footprints)
        {
            Vector2 nearest = new(Mathf.Clamp(origin.x, r.xMin, r.xMax), Mathf.Clamp(origin.y, r.yMin, r.yMax));
            float d = (origin - nearest).sqrMagnitude;
            if (d >= best) continue;
            best = d;
            behind = origin.y > r.yMax;
        }
        return behind;
    }

    private Rect WorldBounds(Vector2[] localPoints)
    {
        Vector2 min = new(float.MaxValue, float.MaxValue), max = new(float.MinValue, float.MinValue);
        foreach (Vector2 p in localPoints)
        {
            Vector2 w = transform.TransformPoint(p);
            min = Vector2.Min(min, w);
            max = Vector2.Max(max, w);
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

    // 콜라이더 모양을 이 오브젝트 로컬 좌표의 다각형들로 모은다.
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
    private void CreateCaster(Transform parent, int index, Vector2[] points, int[] layers)
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
