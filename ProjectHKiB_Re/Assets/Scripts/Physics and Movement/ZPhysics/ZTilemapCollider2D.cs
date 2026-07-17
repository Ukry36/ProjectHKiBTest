using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(TilemapCollider2D))]
public class ZTilemapCollider2D : ZCollider2D
{
    private TilemapCollider2D _col;
    protected override Collider2D Col 
    {
        get {
            if (_col == null) _col = GetComponent<TilemapCollider2D>();
            return _col;
        }
    }

    public bool UsedByComposite { get => _col.usedByComposite; set => _col.usedByComposite = value; }
    public bool IsTrigger       { get => _col.isTrigger;       set => _col.isTrigger = value; }
#if UNITY_EDITOR
    protected override void DrawGizmo()
    {
        return;
    }
#endif
    protected override void Awake() { _col = GetComponent<TilemapCollider2D>(); base.Awake(); }
}