using UnityEngine;

public class AreaInfoProvider : MonoBehaviour
{
    [SerializeField] private AreaInfo areaInfo;

    public LayerMask layerMask;
    private void OnTriggerEnter2D(Collider2D other)
    {

    }

    private void OnTriggerExit2D(Collider2D other)
    {

    }
}