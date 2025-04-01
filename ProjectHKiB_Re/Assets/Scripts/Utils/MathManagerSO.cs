using UnityEngine;

[CreateAssetMenu(fileName = "Math Manager", menuName = "Scriptable Objects/Manager/Math Manager", order = 1)]
public class MathManagerSO : ScriptableObject
{
    public float Absolute(float item)
    => item < 0 ? item * -1 : item;

    public int Ceiling(float item)
    => item < 0 ? (int)item : (int)item + 1;

    public int Floor(float item)
    => item < 0 ? (int)item - 1 : (int)item;

    public int Round(float item)
    => item < 0 ? (int)(item + 0.5f) - 1 : (int)(item + 0.5f);

    public Vector3 AllignInGrid(Vector3 item)
    => new() { x = Round(item.x), y = Round(item.y) };

    public Vector2 SetVectorOne(Vector2 item)
    => (item.x < 0 ? Vector3.left : item.x > 0 ? Vector3.right : Vector3.zero)
            + (item.y < 0 ? Vector3.down : item.y > 0 ? Vector3.up : Vector3.zero);

}
