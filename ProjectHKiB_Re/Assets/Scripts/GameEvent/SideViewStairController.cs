using UnityEngine;

public class SideViewStairController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _frontStairRenderer;
    [SerializeField] private SpriteRenderer _rearStairRenderer;

    public void ActivateFrontStair()
    {
        _rearStairRenderer.sortingOrder = -1;
    }

    public void ActivateRearStair()
    {
        _rearStairRenderer.sortingOrder = _frontStairRenderer.sortingOrder - 1;
    }
}
