
using UnityEngine;

public class MovePoint : MonoBehaviour
{
    private Transform parent;
    public void Initialize()
    {
        parent = transform.parent;
        transform.parent = null;
    }
    public void Die()
    {
        transform.parent = parent;
    }
}