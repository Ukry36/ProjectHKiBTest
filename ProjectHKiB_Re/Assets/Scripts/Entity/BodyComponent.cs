using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class BodyComponent: MonoBehaviour
{
    private Renderer _renderer;
    public void Awake()
    {
        _renderer = GetComponent<Renderer>();
    } 
    public void SetZ(float z, float d)
    {
        transform.position += Vector3.up * d;
        if (_renderer)_renderer.material.SetFloat("_Offset", z);
    }
}