using Unity.Burst.Intrinsics;
using UnityEngine;

[CreateAssetMenu(fileName = "Math Manager", menuName = "Scriptable Objects/Manager/Math Manager", order = 1)]
public class MathManagerSO : ScriptableObject
{
    public const float tan225 = 0.4142135623730950488016887242097f;
    public float Absolute(float item)
    => item < 0 ? item * -1 : item;

    public int Ceiling(float item)
    => item < 0 ? (int)item : (int)item + 1;

    public int Floor(float item)
    => item < 0 ? (int)item - 1 : (int)item;

    public int Round(float item)
    => item + 0.5f < 0 ? (int)(item + 0.5f) - 1 : (int)(item + 0.5f);

    public Vector3 AllignInGrid(Vector3 item)
    => new() { x = Round(item.x), y = Round(item.y) };

    public Vector2 SetVectorOne(Vector2 item)
    => (item.x < 0 ? Vector2.left : item.x > 0 ? Vector2.right : Vector2.zero)
            + (item.y < 0 ? Vector2.down : item.y > 0 ? Vector2.up : Vector2.zero);

    public Vector2 SetDirection8One(Vector2 item)
    {
        if (Absolute(item.x) * tan225 > Absolute(item.y))
        {
            return item.x > 0 ? Vector2.right : Vector2.left;
        }
        if (Absolute(item.y) * tan225 > Absolute(item.x))
        {
            return item.y > 0 ? Vector2.up : Vector2.down;
        }
        return SetVectorOne(item);
    }

    public bool IsVector2HasComponent(Vector2 vector2, Vector2 component)
    {
        if (component.x == 0 && component.y == 0)
            return vector2.x == 0 && vector2.y == 0;

        float vy = component.y * vector2.y;
        if (component.x == 0)
            return vy > 0;

        float vx = component.x * vector2.x;
        if (component.y == 0)
            return vx > 0;

        return vx > 0 && vy > 0;
    }

}
