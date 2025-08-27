using DG.Tweening;
using UnityEngine;

public class CursorFollower : MonoBehaviour
{

    [NaughtyAttributes.MinMaxSlider(-20, 20)]
    public Vector2 xMinMax;
    [NaughtyAttributes.MinMaxSlider(-20, 20)]
    public Vector2 yMinMax;

    public Vector2 multiplyer;
    public Transform follower;

    public void Follow(Vector3 target)
    {
        Vector3 vector = (target - transform.position) * multiplyer;
        if (vector.x < xMinMax.x) vector.x = xMinMax.x;
        if (vector.x > xMinMax.y) vector.x = xMinMax.y;
        if (vector.y < yMinMax.x) vector.y = yMinMax.x;
        if (vector.y > yMinMax.y) vector.y = yMinMax.y;

        follower.localPosition = vector;
    }
    public void ResetFollow() => follower.localPosition = Vector3.zero;
}