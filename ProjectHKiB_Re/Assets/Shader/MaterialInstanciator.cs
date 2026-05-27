using UnityEngine;

public class MaterialInstanciator : MonoBehaviour
{
    [Header("This is Material Instanciator. \nMI is essential for pixel base depth sorting." 
    + "\nInstanciate is automatically called when attatched. \nIf you want to rollback, you have to find the original material and replace with it."
    + "\nMI delete itself when started")]
    public bool enable = true;

    [NaughtyAttributes.Button]
    public void INSTANCIATE()
    {
        if (enable && TryGetComponent(out Renderer renderer)) 
        {
            var tempMaterial = new Material(renderer.sharedMaterial);
            renderer.sharedMaterial = tempMaterial;
            Debug.Log("Instanciated: " + transform.name);
        }
        else Debug.LogWarning("Instating failed: " + transform.name);
    }

    [NaughtyAttributes.Button]
    public void INSTANCIATEALL()
    {
        MaterialInstanciator[] all = FindObjectsOfType<MaterialInstanciator>();
        foreach (var inst in all) inst.INSTANCIATE();
    }

    public void Reset()
    {
        INSTANCIATE();
    }

    public void Start()
    {
        Destroy(this);
    }
}
