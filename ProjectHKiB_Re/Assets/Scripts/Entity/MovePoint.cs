
using UnityEngine;

public class MovePoint : MonoBehaviour
{
    public IPhysics movable;
    public Transform parent;
    public float radius;
    public void Initialize(IPhysics movable)
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