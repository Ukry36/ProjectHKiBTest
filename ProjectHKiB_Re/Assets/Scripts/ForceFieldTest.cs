using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class ForceFieldTest : MonoBehaviour
{
    public List<IMovable> movables = new();
    public Vector2 dir;
    public float strength;
    public bool isCenter;
    public bool yeet;

    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out IMovable component))
        {
            movables.Add(component);
            if (movables.Count < 2)
                StartCoroutine(Force());
        }
    }

    public IEnumerator Force()
    {
        while (movables.Count > 0)
        {
            for (int i = 0; i < movables.Count; i++)
            {
                if (yeet)
                    dir = Quaternion.Euler(0, 0, -1) * dir;

                if (isCenter)
                {
                    movables[i].ExForce.SetForce[this.GetInstanceID()]
                    = (this.transform.position - movables[i].MovePoint.transform.position).normalized * strength;
                }
                else
                {
                    movables[i].ExForce.SetForce[this.GetInstanceID()] = dir.normalized * strength;
                }
            }
            yield return null;
        }
    }

    public void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out IMovable component))
        {
            component.ExForce.SetForce[this.GetInstanceID()] = Vector3.zero;
            movables.Remove(component);
            collision.transform.position = component.MovePoint.transform.position;
        }
    }
}
