using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class ForceFieldTest : MonoBehaviour
{
    private HashSet<IMovable> movables = new();
    public Vector2 dir;
    public float strength;
    public bool isCenter;
    public bool yeet;

    private void FixedUpdate()
    {
        if (movables.Count < 1) return;
        if (yeet) dir = Quaternion.Euler(0, 0, -1) * dir;

        foreach (IMovable movable in movables)
        {
            Vector3 force = isCenter
                ? (transform.position - movable.MovePoint.transform.position).normalized * strength
                : (Vector3)(dir.normalized * strength);

            movable.ExForce.SetForce(GetInstanceID(), force);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out MovePoint component))
        {
            movables.Add(component.movable);
            Debug.Log("dd");
        }
            
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out MovePoint component))
        {
            component.movable.ExForce.SetForce(GetInstanceID(), Vector3.zero);
            movables.Remove(component.movable);
        }
    }

    private void OnDisable()
    {
        foreach (IMovable movable in movables)
            movable.ExForce.SetForce(GetInstanceID(), Vector3.zero);
        movables.Clear();
    }
}