
using UnityEngine;

public class MovePoint : MonoBehaviour
{
    public IMovable movable;
    public Transform parent;
    public float radius;
    public void Initialize(IMovable movable)
    {
        this.movable = movable;
        parent = transform.parent;
        transform.parent = null;
    }
    public void Die()
    {
        transform.parent = parent;
    }
}