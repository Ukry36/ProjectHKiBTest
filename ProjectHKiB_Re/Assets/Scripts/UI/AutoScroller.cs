using UnityEngine;
using UnityEngine.UI;

public class AutoScroller : MonoBehaviour
{
    public ScrollRect scrollRect;
    private Vector3[] _viewPortCorners = new Vector3[4];
    private Vector3[] _targetCorners = new Vector3[4];

    public void ScrollTo(RectTransform target)
    {
        RectTransform contentPanel = scrollRect.content;
        RectTransform viewport = scrollRect.viewport;

        if (contentPanel == null || viewport == null || target == null) return;

        viewport.GetWorldCorners(_viewPortCorners);
        target.GetWorldCorners(_targetCorners);

        float moveX = 0f;
        float moveY = 0f;
        // lefter than min
        if (_targetCorners[0].x < _viewPortCorners[0].x)      moveX = _viewPortCorners[0].x - _targetCorners[0].x;
        // righter than max
        else if (_targetCorners[2].x > _viewPortCorners[2].x) moveX = _viewPortCorners[2].x - _targetCorners[2].x;

        // lower than min
        if (_targetCorners[0].y < _viewPortCorners[0].y)      moveY = _viewPortCorners[0].y - _targetCorners[0].y;
        //higher than max
        else if (_targetCorners[2].y > _viewPortCorners[2].y) moveY = _viewPortCorners[2].y - _targetCorners[2].y;

        if (moveX != 0f || moveY != 0f)
        {
            Vector2 finalPos = contentPanel.anchoredPosition;
            finalPos.x += moveX / contentPanel.lossyScale.x;
            finalPos.y += moveY / contentPanel.lossyScale.y;
            contentPanel.anchoredPosition = finalPos;
        }
    }
}