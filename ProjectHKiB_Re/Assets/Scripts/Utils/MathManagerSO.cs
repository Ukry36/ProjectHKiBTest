using Unity.Burst.Intrinsics;
using UnityEngine;

[CreateAssetMenu(fileName = "Math Manager", menuName = "Scriptable Objects/Manager/Math Manager", order = 1)]
public class MathManagerSO : ScriptableObject
{
    public const float sqrt2 = 0.414213562373f;
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
    => (item.x < 0 ? Vector2.left : item.x > 0 ? Vector2.right : Vector2.zero)
            + (item.y < 0 ? Vector2.down : item.y > 0 ? Vector2.up : Vector2.zero);

    public Vector2 SetDirectionOne(Vector2 item)
    {
        if (Absolute(item.x) * sqrt2 > Absolute(item.y))
        {
            return item.x > 0 ? Vector2.right : Vector2.left;
        }
        if (Absolute(item.y) * sqrt2 > Absolute(item.x))
        {
            return item.y > 0 ? Vector2.up : Vector2.down;
        }
        return SetVectorOne(item);
    }

}
